using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClashZone.Services
{
    /// <summary>
    /// Service responsible for generating and annotating tournament brackets.
    /// This implementation has been extended to dispatch email notifications
    /// for initial matchups (reminders) and automatic advances (byes).
    /// </summary>
    public class BracketService : IBracketService
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly UserManager<ClashUser> _userManager;
        private readonly IEmailService _emailService;

        public BracketService(
            ITournamentsRepository tournamentsRepository,
            UserManager<ClashUser> userManager,
            IEmailService emailService)
        {
            _tournamentsRepository = tournamentsRepository;
            _userManager = userManager;
            _emailService = emailService;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <summary>
        /// Sends email reminders for the first round of the tournament bracket and
        /// notifies teams that automatically advance due to byes.  This method
        /// builds a mapping from team names to team entities in order to look up
        /// members and captains for emailing.  It is invoked from within
        /// <see cref="GetBracketAsync"/> after the bracket is generated.
        /// </summary>
        /// <param name="tournament">The tournament for which the bracket was generated.</param>
        /// <param name="teams">List of participating teams for lookup.</param>
        /// <param name="rounds">Annotated rounds of the bracket.</param>
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

        /// <summary>
        /// Sends a match reminder to all members of the specified team about an upcoming match.
        /// </summary>
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

        /// <summary>
        /// Sends a promotion notification to all members of the specified team indicating they have advanced to the given round.
        /// </summary>
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

        /// <summary>
        /// Private helper to generate bracket rounds for a list of team names.
        /// The algorithm shuffles the teams randomly, determines the next power
        /// of two bracket size and fills byes with null entries.  It returns a
        /// nested list of matches for each round where each match contains two
        /// team names or null for byes.
        /// </summary>
        private static List<List<MatchInfo>> GenerateBracketRounds(List<string> teams)
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
            // First round
            var firstRound = new List<MatchInfo>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                firstRound.Add(new MatchInfo { Team1Name = slots[i], Team2Name = slots[i + 1] });
            }
            result.Add(firstRound);
            // Determine winners for next rounds (only byes)
            var winners = new List<string?>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var t1 = slots[i];
                var t2 = slots[i + 1];
                if (t1 != null && t2 == null)
                {
                    winners.Add(t1);
                }
                else if (t2 != null && t1 == null)
                {
                    winners.Add(t2);
                }
                else
                {
                    winners.Add(null);
                }
            }
            for (int r = 1; r < rounds; r++)
            {
                var roundMatches = new List<MatchInfo>();
                var nextWinners = new List<string?>();
                for (int i = 0; i < winners.Count; i += 2)
                {
                    var match = new MatchInfo
                    {
                        Team1Name = winners[i],
                        Team2Name = (i + 1 < winners.Count) ? winners[i + 1] : null
                    };
                    roundMatches.Add(match);
                    var w1 = winners[i];
                    var w2 = (i + 1 < winners.Count) ? winners[i + 1] : null;
                    if (w1 != null && w2 == null)
                    {
                        nextWinners.Add(w1);
                    }
                    else if (w2 != null && w1 == null)
                    {
                        nextWinners.Add(w2);
                    }
                    else
                    {
                        nextWinners.Add(null);
                    }
                }
                result.Add(roundMatches);
                winners = nextWinners;
            }
            return result;
        }

        /// <summary>
        /// Internal helper to generate bracket rounds with random scores.  The winners of
        /// each match are chosen based on the higher score (or bye) and placed in the
        /// appropriate position in the next round. Each MatchInfo is annotated
        /// with the Round number and MatchNum.
        /// </summary>
        private static List<List<MatchInfo>> GenerateBracketWithResults(List<string> teams)
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
        /// Annotates each match in the bracket with its round and match number for display purposes.
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
    }
}