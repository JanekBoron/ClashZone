using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using DataAccess;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClashZone.Services
{
    /// <summary>
    /// Service responsible for generating and annotating tournament brackets.
    /// This implementation has been extended to support random per?match
    /// simulation, persisting statistics for individual matches and
    /// constructing a bracket view based on already played matches.
    /// </summary>
    public class BracketService : IBracketService
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly UserManager<ClashUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly ApplicationDbContext _context;

        public BracketService(
            ITournamentsRepository tournamentsRepository,
            UserManager<ClashUser> userManager,
            IEmailService emailService,
            ApplicationDbContext context)
        {
            _tournamentsRepository = tournamentsRepository;
            _userManager = userManager;
            _emailService = emailService;
            _context = context;
        }

        /// <summary>
        /// Generates a bracket structure for a tournament without any scores.  Only
        /// the first round contains team names; subsequent rounds remain blank
        /// until matches are played.  Played match results stored in the
        /// database will populate the appropriate scores and winners.
        /// </summary>
        public async Task<BracketViewModel?> GetBracketAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                teamNames.Add(name);
            }
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfo>>()
                };
            }
            // Build a skeleton bracket without propagating winners
            var skeleton = GenerateBracketSkeleton(teamNames);
            // Populate scores/names from already played matches
            var bracket = await BuildBracketWithDbAsync(tournamentId, teamNames, skeleton, teams);
            var annotated = AnnotateRounds(bracket);
            // Send notifications only the first time the bracket is built (no matches recorded yet)
            var existingMatches = await _context.Matches.AnyAsync(m => m.TournamentId == tournamentId);
            if (!existingMatches)
            {
                await NotifyInitialMatchesAsync(tournament, teams, annotated);
            }
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = annotated
            };
        }

        /// <summary>
        /// Generates a bracket with random scores for every match.  All matches
        /// in the bracket are simulated in memory; no results are persisted and
        /// participant names are propagated automatically through the rounds.
        /// Empty pairings (two byes) are omitted.
        /// </summary>
        public async Task<BracketViewModel?> GetBracketWithResultsAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                teamNames.Add(name);
            }
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfo>>()
                };
            }
            var rounds = GenerateBracketWithResults(teamNames);
            // Persist results and random player statistics for the simulated bracket.  This ensures
            // that matches appear in the "Mecze" tab and per?player stats are available.
            await SaveBracketStatsAsync(rounds, tournament, teams);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
        }

        /// <summary>
        /// Generates a bracket with random scores and persists the results
        /// and random player statistics to the database.  This method uses
        /// GenerateBracketWithResults internally and then records the results
        /// in the persistence layer.
        /// </summary>
        public async Task<BracketViewModel?> GetBracketWithStatsAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                teamNames.Add(name);
            }
            if (teamNames.Count < 2)
            {
                return new BracketViewModel
                {
                    Tournament = tournament,
                    Rounds = new List<List<MatchInfo>>()
                };
            }
            var rounds = GenerateBracketWithResults(teamNames);
            // Persist match results and generate random stats
            await SaveBracketStatsAsync(rounds, tournament, teams);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
        }

        /// <summary>
        /// Simulates a single match within the bracket.  The specified match must have
        /// both participants determined and not yet have a score.  Random scores
        /// are generated, persisted and player statistics recorded.  The updated
        /// bracket reflecting the new result is returned.  If the match cannot be
        /// simulated (due to missing participants or existing scores) null is returned.
        /// </summary>
        public async Task<BracketViewModel?> SimulateMatchAsync(int tournamentId, int roundNumber, int matchNumber)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string>();
            var nameToTeam = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    teamNames.Add(name);
                    if (!nameToTeam.ContainsKey(name))
                    {
                        nameToTeam[name] = team;
                    }
                }
            }
            if (teamNames.Count < 2)
            {
                return null;
            }
            var skeleton = GenerateBracketSkeleton(teamNames);
            var bracket = await BuildBracketWithDbAsync(tournamentId, teamNames, skeleton, teams);
            var annotated = AnnotateRounds(bracket);
            int rIdx = roundNumber - 1;
            if (rIdx < 0 || rIdx >= annotated.Count)
            {
                return null;
            }
            int mIdx = matchNumber - 1;
            if (mIdx < 0 || mIdx >= annotated[rIdx].Count)
            {
                return null;
            }
            var matchInfo = annotated[rIdx][mIdx];
            // Only simulate if both participants are defined and no score exists
            if (string.IsNullOrEmpty(matchInfo.Team1Name) || string.IsNullOrEmpty(matchInfo.Team2Name))
            {
                return null;
            }
            if (matchInfo.Team1Score.HasValue || matchInfo.Team2Score.HasValue)
            {
                return null;
            }
            Team? team1 = nameToTeam.TryGetValue(matchInfo.Team1Name, out var t1) ? t1 : null;
            Team? team2 = nameToTeam.TryGetValue(matchInfo.Team2Name, out var t2) ? t2 : null;
            var rnd = new Random();
            int score1 = rnd.Next(0, 16);
            int score2 = rnd.Next(0, 16);
            var matchEntity = new Match
            {
                TournamentId = tournamentId,
                Team1Id = team1?.Id,
                Team2Id = team2?.Id,
                Team1Score = score1,
                Team2Score = score2,
                PlayedAt = DateTime.UtcNow
            };
            _context.Matches.Add(matchEntity);
            await _context.SaveChangesAsync();
            // generate player stats for the single match
            await GenerateStatsForMatchAsync(matchEntity, team1, team2);
            // rebuild bracket to reflect new result
            bracket = await BuildBracketWithDbAsync(tournamentId, teamNames, skeleton, teams);
            annotated = AnnotateRounds(bracket);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = annotated
            };
        }

        /// <summary>
        /// Helper to generate random per?player statistics for a single match and
        /// update aggregated user stats.
        /// </summary>
        private async Task GenerateStatsForMatchAsync(Match matchEntity, Team? team1, Team? team2)
        {
            var random = new Random();
            // team1 stats
            if (team1 != null)
            {
                var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team1.Id);
                var allMemberIds = new HashSet<string>(memberIds) { team1.CaptainId };
                foreach (var userId in allMemberIds)
                {
                    int kills = random.Next(0, 21);
                    int deaths = random.Next(0, 21);
                    int assists = random.Next(0, 11);
                    var playerStat = new PlayerMatchStat
                    {
                        MatchId = matchEntity.Id,
                        UserId = userId,
                        Kills = kills,
                        Deaths = deaths,
                        Assists = assists
                    };
                    _context.PlayerMatchStats.Add(playerStat);
                    var agg = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (agg == null)
                    {
                        agg = new UserStat { UserId = userId, TotalKills = kills, TotalDeaths = deaths, TotalAssists = assists, MatchesPlayed = 1 };
                        _context.UserStats.Add(agg);
                    }
                    else
                    {
                        agg.TotalKills += kills;
                        agg.TotalDeaths += deaths;
                        agg.TotalAssists += assists;
                        agg.MatchesPlayed += 1;
                        _context.UserStats.Update(agg);
                    }
                }
            }
            // team2 stats
            if (team2 != null)
            {
                var memberIds2 = await _tournamentsRepository.GetTeamMemberIdsAsync(team2.Id);
                var allMemberIds2 = new HashSet<string>(memberIds2) { team2.CaptainId };
                foreach (var userId in allMemberIds2)
                {
                    int kills = random.Next(0, 21);
                    int deaths = random.Next(0, 21);
                    int assists = random.Next(0, 11);
                    var playerStat = new PlayerMatchStat
                    {
                        MatchId = matchEntity.Id,
                        UserId = userId,
                        Kills = kills,
                        Deaths = deaths,
                        Assists = assists
                    };
                    _context.PlayerMatchStats.Add(playerStat);
                    var agg = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
                    if (agg == null)
                    {
                        agg = new UserStat { UserId = userId, TotalKills = kills, TotalDeaths = deaths, TotalAssists = assists, MatchesPlayed = 1 };
                        _context.UserStats.Add(agg);
                    }
                    else
                    {
                        agg.TotalKills += kills;
                        agg.TotalDeaths += deaths;
                        agg.TotalAssists += assists;
                        agg.MatchesPlayed += 1;
                        _context.UserStats.Update(agg);
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Generates a basic bracket skeleton.  Only the first round contains
        /// team names; subsequent rounds contain empty slots.  Pairs where both
        /// entries are null are omitted from the first round.  A placeholder for
        /// the winners list is maintained internally to compute the correct number
        /// of matches in later rounds.
        /// </summary>
        private static List<List<MatchInfo>> GenerateBracketSkeleton(List<string> teamNames)
        {
            int n = teamNames.Count;
            int rounds = (int)Math.Ceiling(Math.Log2(n));
            int bracketSize = (int)Math.Pow(2, rounds);
            var slots = new string?[bracketSize];
            for (int i = 0; i < bracketSize; i++)
            {
                slots[i] = i < n ? teamNames[i] : null;
            }
            var roundsList = new List<List<MatchInfo>>();
            var initialWinners = new List<string?>();
            var firstRound = new List<MatchInfo>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var t1 = slots[i];
                var t2 = slots[i + 1];
                if (t1 == null && t2 == null)
                {
                    initialWinners.Add(null);
                    continue;
                }
                firstRound.Add(new MatchInfo { Team1Name = t1, Team2Name = t2 });
                initialWinners.Add(t1 ?? t2);
            }
            roundsList.Add(firstRound);
            var winners = initialWinners;
            while (winners.Count > 1)
            {
                int nextCount = (int)Math.Ceiling(winners.Count / 2.0);
                var nextRound = new List<MatchInfo>();
                for (int i = 0; i < nextCount; i++)
                {
                    nextRound.Add(new MatchInfo { Team1Name = null, Team2Name = null });
                }
                roundsList.Add(nextRound);
                var tmp = new List<string?>();
                for (int i = 0; i < nextCount; i++)
                {
                    tmp.Add(null);
                }
                winners = tmp;
            }
            return roundsList;
        }

        /// <summary>
        /// Builds a bracket from the provided skeleton and fills in scores/names
        /// based on matches stored in the database.  Only matches that have been
        /// played will populate participant names and scores in later rounds.
        /// Otherwise names remain null to hide upcoming pairings until they are
        /// determined by previous matches.
        /// </summary>
        private async Task<List<List<MatchInfo>>> BuildBracketWithDbAsync(int tournamentId, List<string> teamNames, List<List<MatchInfo>> skeleton, List<Team> teams)
        {
            // mapping between team IDs and their display names
            var idToName = new Dictionary<int, string>();
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    idToName[team.Id] = name;
                }
            }
            // Fetch all played matches for this tournament
            var matches = await _context.Matches
                .Include(m => m.Team1)
                .Include(m => m.Team2)
                .Where(m => m.TournamentId == tournamentId)
                .ToListAsync();
            var matchDict = new Dictionary<(string?, string?), Match>(new TeamPairComparer());
            foreach (var m in matches)
            {
                string? t1Name = null;
                string? t2Name = null;
                if (m.Team1Id.HasValue && idToName.TryGetValue(m.Team1Id.Value, out var n1))
                {
                    t1Name = n1;
                }
                if (m.Team2Id.HasValue && idToName.TryGetValue(m.Team2Id.Value, out var n2))
                {
                    t2Name = n2;
                }
                if (t1Name != null && t2Name != null)
                {
                    matchDict[(t1Name, t2Name)] = m;
                    matchDict[(t2Name, t1Name)] = m;
                }
            }
            // Determine winners for the first round
            var prevWinners = new List<(string? name, bool fromMatch)>();
            for (int i = 0; i < skeleton[0].Count; i++)
            {
                var mInfo = skeleton[0][i];
                var t1Name = mInfo.Team1Name;
                var t2Name = mInfo.Team2Name;
                if (!string.IsNullOrEmpty(t1Name) && !string.IsNullOrEmpty(t2Name))
                {
                    if (matchDict.TryGetValue((t1Name!, t2Name!), out var m))
                    {
                        // orientation: if stored Team1Id matches first name
                        if (m.Team1Id.HasValue && idToName.TryGetValue(m.Team1Id.Value, out var name1) && string.Equals(name1, t1Name, StringComparison.OrdinalIgnoreCase))
                        {
                            mInfo.Team1Score = m.Team1Score;
                            mInfo.Team2Score = m.Team2Score;
                            string winnerName = m.Team1Score >= m.Team2Score ? t1Name! : t2Name!;
                            prevWinners.Add((winnerName, true));
                        }
                        else
                        {
                            // reversed orientation
                            mInfo.Team1Score = m.Team2Score;
                            mInfo.Team2Score = m.Team1Score;
                            string winnerName = m.Team2Score >= m.Team1Score ? t1Name! : t2Name!;
                            prevWinners.Add((winnerName, true));
                        }
                    }
                    else
                    {
                        // not yet played
                        prevWinners.Add((null, false));
                    }
                }
                else if (!string.IsNullOrEmpty(t1Name) || !string.IsNullOrEmpty(t2Name))
                {
                    prevWinners.Add((t1Name ?? t2Name, false));
                }
                else
                {
                    prevWinners.Add((null, false));
                }
            }
            // Build subsequent rounds
            for (int r = 1; r < skeleton.Count; r++)
            {
                var currentWinners = new List<(string? name, bool fromMatch)>();
                for (int i = 0; i < skeleton[r].Count; i++)
                {
                    var idx1 = 2 * i;
                    var idx2 = 2 * i + 1;
                    (string? nameA, bool fromA) = idx1 < prevWinners.Count ? prevWinners[idx1] : (null, false);
                    (string? nameB, bool fromB) = idx2 < prevWinners.Count ? prevWinners[idx2] : (null, false);
                    var mInfo = skeleton[r][i];
                    // match ready only when both participants originate from played matches
                    if (!string.IsNullOrEmpty(nameA) && !string.IsNullOrEmpty(nameB) && fromA && fromB)
                    {
                        mInfo.Team1Name = nameA;
                        mInfo.Team2Name = nameB;
                        if (matchDict.TryGetValue((nameA!, nameB!), out var m))
                        {
                            // fill scores and compute winner
                            if (m.Team1Id.HasValue && idToName.TryGetValue(m.Team1Id.Value, out var nm1) && string.Equals(nm1, nameA, StringComparison.OrdinalIgnoreCase))
                            {
                                mInfo.Team1Score = m.Team1Score;
                                mInfo.Team2Score = m.Team2Score;
                                string winnerName = m.Team1Score >= m.Team2Score ? nameA! : nameB!;
                                currentWinners.Add((winnerName, true));
                            }
                            else
                            {
                                mInfo.Team1Score = m.Team2Score;
                                mInfo.Team2Score = m.Team1Score;
                                string winnerName = m.Team2Score >= m.Team1Score ? nameA! : nameB!;
                                currentWinners.Add((winnerName, true));
                            }
                        }
                        else
                        {
                            currentWinners.Add((null, false));
                        }
                    }
                    else if (!string.IsNullOrEmpty(nameA) && string.IsNullOrEmpty(nameB))
                    {
                        // Bye: participant automatically advances but names stay hidden
                        currentWinners.Add((nameA, fromA));
                    }
                    else if (string.IsNullOrEmpty(nameA) && !string.IsNullOrEmpty(nameB))
                    {
                        currentWinners.Add((nameB, fromB));
                    }
                    else
                    {
                        currentWinners.Add((null, false));
                    }
                }
                prevWinners = currentWinners;
            }
            return skeleton;
        }

        /// <summary>
        /// Generates a bracket with random scores across all rounds.  Random values
        /// are assigned only for matches where both participants exist.  Empty
        /// pairings consisting of two byes are skipped.  Names propagate to later
        /// rounds automatically.
        /// </summary>
        private static List<List<MatchInfo>> GenerateBracketWithResults(List<string> teamNames)
        {
            var random = new Random();
            var shuffled = teamNames.OrderBy(_ => random.Next()).ToList();
            int n = shuffled.Count;
            int rounds = (int)Math.Ceiling(Math.Log2(n));
            int bracketSize = (int)Math.Pow(2, rounds);
            var slots = new string?[bracketSize];
            for (int i = 0; i < bracketSize; i++)
            {
                slots[i] = i < n ? shuffled[i] : null;
            }
            var result = new List<List<MatchInfo>>();
            var winners = new List<string?>();
            // first round
            var firstRound = new List<MatchInfo>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var t1 = slots[i];
                var t2 = slots[i + 1];
                if (t1 == null && t2 == null)
                {
                    winners.Add(null);
                    continue;
                }
                var m = new MatchInfo { Team1Name = t1, Team2Name = t2 };
                if (t1 != null)
                {
                    m.Team1Score = random.Next(0, 16);
                }
                if (t2 != null)
                {
                    m.Team2Score = random.Next(0, 16);
                }
                if (t1 != null && t2 != null)
                {
                    winners.Add(m.Team1Score >= m.Team2Score ? t1 : t2);
                }
                else if (t1 != null || t2 != null)
                {
                    winners.Add(t1 ?? t2);
                }
                else
                {
                    winners.Add(null);
                }
                firstRound.Add(m);
            }
            result.Add(firstRound);
            // subsequent rounds
            while (winners.Count > 1)
            {
                int nextCount = (int)Math.Ceiling(winners.Count / 2.0);
                var roundMatches = new List<MatchInfo>();
                var nextWinners = new List<string?>();
                for (int i = 0; i < nextCount; i++)
                {
                    var idx1 = 2 * i;
                    var idx2 = 2 * i + 1;
                    var nameA = idx1 < winners.Count ? winners[idx1] : null;
                    var nameB = idx2 < winners.Count ? winners[idx2] : null;
                    if (nameA == null && nameB == null)
                    {
                        nextWinners.Add(null);
                        continue;
                    }
                    var m = new MatchInfo();
                    if (nameA != null)
                    {
                        m.Team1Name = nameA;
                    }
                    if (nameB != null)
                    {
                        m.Team2Name = nameB;
                    }
                    if (nameA != null && nameB != null)
                    {
                        m.Team1Score = random.Next(0, 16);
                        m.Team2Score = random.Next(0, 16);
                        nextWinners.Add(m.Team1Score >= m.Team2Score ? nameA : nameB);
                    }
                    else
                    {
                        nextWinners.Add(nameA ?? nameB);
                    }
                    roundMatches.Add(m);
                }
                result.Add(roundMatches);
                winners = nextWinners;
            }
            return AnnotateRounds(result);
        }

        /// <summary>
        /// Assigns round and match numbers to each match in the bracket.  This helper
        /// remains unchanged from the original implementation.
        /// </summary>
        private static List<List<MatchInfo>> AnnotateRounds(List<List<MatchInfo>> rounds)
        {
            var annotated = new List<List<MatchInfo>>();
            for (int r = 0; r < rounds.Count; r++)
            {
                var roundList = new List<MatchInfo>();
                int matchNum = 1;
                foreach (var match in rounds[r])
                {
                    match.Round = r + 1;
                    match.MatchNum = matchNum++;
                    roundList.Add(match);
                }
                annotated.Add(roundList);
            }
            return annotated;
        }

        /// <summary>
        /// Persists randomly generated results and statistics for each match in the
        /// provided bracket.  This logic is largely unchanged from the original
        /// implementation but now avoids persisting completely empty matches.
        /// </summary>
        private async Task SaveBracketStatsAsync(List<List<MatchInfo>> rounds, Tournament tournament, List<Team> teams)
        {
            // Build dictionary from team display names to team objects
            var teamNameMap = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    teamNameMap[name] = team;
                }
            }
            var random = new Random();
            foreach (var round in rounds)
            {
                foreach (var matchInfo in round)
                {
                    // Only persist matches where at least one team is present
                    if (matchInfo.Team1Name == null && matchInfo.Team2Name == null) continue;
                    Team? team1 = null;
                    Team? team2 = null;
                    if (matchInfo.Team1Name != null && teamNameMap.TryGetValue(matchInfo.Team1Name, out var t1))
                    {
                        team1 = t1;
                    }
                    if (matchInfo.Team2Name != null && teamNameMap.TryGetValue(matchInfo.Team2Name, out var t2))
                    {
                        team2 = t2;
                    }
                    var match = new Match
                    {
                        TournamentId = tournament.Id,
                        Team1Id = team1?.Id,
                        Team2Id = team2?.Id,
                        Team1Score = matchInfo.Team1Score ?? 0,
                        Team2Score = matchInfo.Team2Score ?? 0,
                        PlayedAt = DateTime.UtcNow
                    };
                    _context.Matches.Add(match);
                    await _context.SaveChangesAsync();
                    // Generate stats for players of team1
                    if (team1 != null)
                    {
                        var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team1.Id);
                        var allMemberIds = new HashSet<string>(memberIds) { team1.CaptainId };
                        foreach (var userId in allMemberIds)
                        {
                            int kills = random.Next(0, 21);
                            int deaths = random.Next(0, 21);
                            int assists = random.Next(0, 11);
                            var playerStat = new PlayerMatchStat
                            {
                                MatchId = match.Id,
                                UserId = userId,
                                Kills = kills,
                                Deaths = deaths,
                                Assists = assists
                            };
                            _context.PlayerMatchStats.Add(playerStat);
                            var agg = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
                            if (agg == null)
                            {
                                agg = new UserStat { UserId = userId, TotalKills = kills, TotalDeaths = deaths, TotalAssists = assists, MatchesPlayed = 1 };
                                _context.UserStats.Add(agg);
                            }
                            else
                            {
                                agg.TotalKills += kills;
                                agg.TotalDeaths += deaths;
                                agg.TotalAssists += assists;
                                agg.MatchesPlayed += 1;
                                _context.UserStats.Update(agg);
                            }
                        }
                    }
                    // Generate stats for players of team2
                    if (team2 != null)
                    {
                        var memberIds2 = await _tournamentsRepository.GetTeamMemberIdsAsync(team2.Id);
                        var allMemberIds2 = new HashSet<string>(memberIds2) { team2.CaptainId };
                        foreach (var userId in allMemberIds2)
                        {
                            int kills = random.Next(0, 21);
                            int deaths = random.Next(0, 21);
                            int assists = random.Next(0, 11);
                            var playerStat = new PlayerMatchStat
                            {
                                MatchId = match.Id,
                                UserId = userId,
                                Kills = kills,
                                Deaths = deaths,
                                Assists = assists
                            };
                            _context.PlayerMatchStats.Add(playerStat);
                            var agg = await _context.UserStats.FirstOrDefaultAsync(s => s.UserId == userId);
                            if (agg == null)
                            {
                                agg = new UserStat { UserId = userId, TotalKills = kills, TotalDeaths = deaths, TotalAssists = assists, MatchesPlayed = 1 };
                                _context.UserStats.Add(agg);
                            }
                            else
                            {
                                agg.TotalKills += kills;
                                agg.TotalDeaths += deaths;
                                agg.TotalAssists += assists;
                                agg.MatchesPlayed += 1;
                                _context.UserStats.Update(agg);
                            }
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }

        #region Email Notifications
        // The methods below are from the original implementation and handle
        // notifying teams of their first round matches and byes.  They are
        // retained unchanged to preserve existing functionality.
        private async Task NotifyInitialMatchesAsync(Tournament tournament, List<Team> teams, List<List<MatchInfo>> rounds)
        {
            if (rounds.Count == 0)
            {
                return;
            }
            var teamNameMap = new Dictionary<string, Team>(StringComparer.OrdinalIgnoreCase);
            foreach (var team in teams)
            {
                string? name = team.Name;
                if (string.IsNullOrEmpty(name))
                {
                    var captain = await _userManager.FindByIdAsync(team.CaptainId);
                    name = captain?.UserName != null ? $"team_{captain.UserName}" : $"Team_{team.Id}";
                }
                if (!string.IsNullOrEmpty(name))
                {
                    teamNameMap[name] = team;
                }
            }
            var firstRound = rounds[0];
            foreach (var match in firstRound)
            {
                if (!string.IsNullOrEmpty(match.Team1Name) && !string.IsNullOrEmpty(match.Team2Name))
                {
                    if (teamNameMap.TryGetValue(match.Team1Name!, out var team1))
                    {
                        await NotifyTeamMatchAsync(team1, match.Team2Name!, tournament);
                    }
                    if (teamNameMap.TryGetValue(match.Team2Name!, out var team2))
                    {
                        await NotifyTeamMatchAsync(team2, match.Team1Name!, tournament);
                    }
                }
                else
                {
                    var advancingName = match.Team1Name ?? match.Team2Name;
                    if (!string.IsNullOrEmpty(advancingName) && teamNameMap.TryGetValue(advancingName, out var team))
                    {
                        await NotifyTeamAdvanceAsync(team, tournament, 2);
                    }
                }
            }
        }

        private async Task NotifyTeamMatchAsync(Team team, string opponentName, Tournament tournament)
        {
            var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
            var allMembers = new HashSet<string>(memberIds) { team.CaptainId };
            foreach (var memberId in allMembers)
            {
                var user = await _userManager.FindByIdAsync(memberId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendMatchReminderAsync(user.Email, tournament.Name, opponentName, scheduledAt: null);
                }
            }
        }

        private async Task NotifyTeamAdvanceAsync(Team team, Tournament tournament, int round)
        {
            var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team.Id);
            var allMembers = new HashSet<string>(memberIds) { team.CaptainId };
            foreach (var memberId in allMembers)
            {
                var user = await _userManager.FindByIdAsync(memberId);
                if (user != null && !string.IsNullOrEmpty(user.Email))
                {
                    await _emailService.SendPromotionNotificationAsync(user.Email, tournament.Name, round);
                }
            }
        }
        #endregion
    }
}


public class TeamPairComparer : IEqualityComparer<(string? A, string? B)>
{
    private static readonly StringComparer C = StringComparer.OrdinalIgnoreCase;

    public bool Equals((string? A, string? B) x, (string? A, string? B) y) =>
        C.Equals(x.A ?? string.Empty, y.A ?? string.Empty) &&
        C.Equals(x.B ?? string.Empty, y.B ?? string.Empty);

    public int GetHashCode((string? A, string? B) obj) =>
        HashCode.Combine(
            C.GetHashCode(obj.A ?? string.Empty),
            C.GetHashCode(obj.B ?? string.Empty)
        );
}