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
    /// This implementation has been extended to support the generation of
    /// random match results and detailed player statistics.  When statistics
    /// are generated the results are persisted to the database so that
    /// players can view their aggregated performance on their profile pages.
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

        /// <inheritdoc/>
        public async Task<BracketViewModel?> GetBracketAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string?>();
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
            List<List<MatchInfo>> rounds;
            if (teamNames.Count < 2)
            {
                rounds = new List<List<MatchInfo>>();
            }
            else
            {
                rounds = GenerateBracketRounds(teamNames);
            }
            var annotatedRounds = AnnotateRounds(rounds);
            // Send notifications for first round matchups and byes
            await NotifyInitialMatchesAsync(tournament, teams, annotatedRounds);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = annotatedRounds
            };
        }

        /// <inheritdoc/>
        public async Task<BracketViewModel?> GetBracketWithResultsAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string?>();
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
            List<List<MatchInfo>> rounds;
            if (teamNames.Count < 2)
            {
                rounds = new List<List<MatchInfo>>();
            }
            else
            {
                rounds = GenerateBracketWithResults(teamNames);
            }
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
        }

        /// <inheritdoc/>
        public async Task<BracketViewModel?> GetBracketWithStatsAsync(int tournamentId)
        {
            var tournament = await _tournamentsRepository.GetTournamentByIdAsync(tournamentId);
            if (tournament == null)
            {
                return null;
            }
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            var teamNames = new List<string?>();
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
            List<List<MatchInfo>> rounds;
            if (teamNames.Count < 2)
            {
                rounds = new List<List<MatchInfo>>();
            }
            else
            {
                rounds = GenerateBracketWithResults(teamNames);
            }
            // Persist match results and generate random stats
            await SaveBracketStatsAsync(rounds, tournament, teams);
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = rounds
            };
        }

        /// <summary>
        /// Saves the randomly generated bracket results to the database as
        /// <see cref="Match"/> and <see cref="PlayerMatchStat"/> entities.  For
        /// each match random kills, deaths and assists are generated for
        /// every player on both teams.  Aggregated statistics are stored
        /// in the <see cref="UserStat"/> entity.
        /// </summary>
        /// <param name="rounds">Bracket rounds with scores assigned.</param>
        /// <param name="tournament">Tournament for which the bracket was generated.</param>
        /// <param name="teams">List of participating teams for lookup.</param>
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
                    // Only persist matches where both teams are present
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
                        // Include captain
                        var allMemberIds = new HashSet<string>(memberIds) { team1.CaptainId };
                        foreach (var userId in allMemberIds)
                        {
                            // Random stats within a plausible range for CS2 matches
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
                            // Update aggregated stats
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
                        var memberIds = await _tournamentsRepository.GetTeamMemberIdsAsync(team2.Id);
                        var allMemberIds = new HashSet<string>(memberIds) { team2.CaptainId };
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
                    await _context.SaveChangesAsync();
                }
            }
        }

        #region Bracket Generation Helpers
        // The methods below are copied from the previous implementation.  They
        // generate bracket rounds with or without random scores and annotate
        // rounds for display.  They remain unchanged.
        private static List<List<MatchInfo>> GenerateBracketRounds(List<string?> teams)
        {
            int n = teams.Count;
            int rounds = (int)Math.Ceiling(Math.Log2(n));
            int bracketSize = (int)Math.Pow(2, rounds);
            var slots = new string?[bracketSize];
            for (int i = 0; i < bracketSize; i++)
            {
                slots[i] = i < n ? teams[i] : null;
            }
            var result = new List<List<MatchInfo>>();
            // First round without scores
            var firstRound = new List<MatchInfo>();
            var winners = new List<string?>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var m = new MatchInfo { Team1Name = slots[i], Team2Name = slots[i + 1] };
                winners.Add(m.Team1Name ?? m.Team2Name);
                firstRound.Add(m);
            }
            result.Add(firstRound);
            // Subsequent rounds
            for (int r = 1; r < rounds; r++)
            {
                var roundMatches = new List<MatchInfo>();
                var nextWinners = new List<string?>();
                for (int i = 0; i < winners.Count; i += 2)
                {
                    var m = new MatchInfo { Team1Name = winners[i], Team2Name = (i + 1 < winners.Count) ? winners[i + 1] : null };
                    nextWinners.Add(m.Team1Name);
                    roundMatches.Add(m);
                }
                result.Add(roundMatches);
                winners = nextWinners;
            }
            return result;
        }

        private static List<List<MatchInfo>> GenerateBracketWithResults(List<string?> teams)
        {
            var random = new Random();
            var shuffled = teams.OrderBy(_ => random.Next()).ToList();
            int n = shuffled.Count;
            int rounds = (int)Math.Ceiling(Math.Log2(n));
            int bracketSize = (int)Math.Pow(2, rounds);
            var slots = new string?[bracketSize];
            for (int i = 0; i < bracketSize; i++)
            {
                slots[i] = i < n ? shuffled[i] : null;
            }
            var result = new List<List<MatchInfo>>();
            // First round with random scores
            var firstRound = new List<MatchInfo>();
            var winners = new List<string?>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var m = new MatchInfo { Team1Name = slots[i], Team2Name = slots[i + 1] };
                // Assign random scores (0-16) for CS:GO style simulation
                m.Team1Score = m.Team1Name != null ? random.Next(0, 16) : (int?)null;
                m.Team2Score = m.Team2Name != null ? random.Next(0, 16) : (int?)null;
                // Determine winner by higher score (or bye)
                if (m.Team1Score.HasValue && m.Team2Score.HasValue)
                {
                    winners.Add(m.Team1Score > m.Team2Score ? m.Team1Name : m.Team2Name);
                }
                else if (m.Team1Score.HasValue)
                {
                    winners.Add(m.Team1Name);
                }
                else if (m.Team2Score.HasValue)
                {
                    winners.Add(m.Team2Name);
                }
                else
                {
                    winners.Add(null);
                }
                firstRound.Add(m);
            }
            result.Add(firstRound);
            // Subsequent rounds
            for (int r = 1; r < rounds; r++)
            {
                var roundMatches = new List<MatchInfo>();
                var nextWinners = new List<string?>();
                for (int i = 0; i < winners.Count; i += 2)
                {
                    var m = new MatchInfo { Team1Name = winners[i], Team2Name = (i + 1 < winners.Count) ? winners[i + 1] : null };
                    m.Team1Score = m.Team1Name != null ? random.Next(0, 16) : (int?)null;
                    m.Team2Score = m.Team2Name != null ? random.Next(0, 16) : (int?)null;
                    if (m.Team1Score.HasValue && m.Team2Score.HasValue)
                    {
                        nextWinners.Add(m.Team1Score > m.Team2Score ? m.Team1Name : m.Team2Name);
                    }
                    else if (m.Team1Score.HasValue)
                    {
                        nextWinners.Add(m.Team1Name);
                    }
                    else if (m.Team2Score.HasValue)
                    {
                        nextWinners.Add(m.Team2Name);
                    }
                    else
                    {
                        nextWinners.Add(null);
                    }
                    roundMatches.Add(m);
                }
                result.Add(roundMatches);
                winners = nextWinners;
            }
            // Annotate rounds and matches for readability
            var annotated = AnnotateRounds(result);
            return annotated;
        }

        /// <summary>
        /// Annotates each match in the bracket with its round and match number for
        /// display purposes.
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

        #endregion

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
            var firstRound = rounds[0];
            foreach (var match in firstRound)
            {
                // Both teams present: send match reminders to both sides
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
                    // One team has a bye; notify that team of advancement
                    var advancingName = match.Team1Name ?? match.Team2Name;
                    if (!string.IsNullOrEmpty(advancingName) && teamNameMap.TryGetValue(advancingName, out var team))
                    {
                        // Advance to round 2
                        await NotifyTeamAdvanceAsync(team, tournament, 2);
                    }
                }
            }
        }

        private async Task NotifyTeamMatchAsync(Team team, string opponentName, Tournament tournament)
        {
            // Collect all unique member IDs including the captain
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
                    // Use the email service to inform the participant that they have advanced
                    // to the specified round.  This helper wraps a generic SendEmailAsync call
                    // with a templated subject and body.
                    await _emailService.SendPromotionNotificationAsync(user.Email, tournament.Name, round);
                }
            }
        }
        #endregion
    }
}