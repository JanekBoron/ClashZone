using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClashZone.Services
{
    public class BracketService : IBracketService
    {
        private readonly ITournamentsRepository _tournamentsRepository;
        private readonly UserManager<ClashUser> _userManager;
        public BracketService(ITournamentsRepository tournamentsRepository, UserManager<ClashUser> userManager)
        {
            _tournamentsRepository = tournamentsRepository;
            _userManager = userManager;
        }

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
            return new BracketViewModel
            {
                Tournament = tournament,
                Rounds = annotatedRounds
            };
        }

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
        /// Internal helper to generate bracket rounds with random scores. The winners of
        /// each match are chosen based on the higher score (or bye) and placed in the
        /// appropriate position in the next round. Each MatchInfo is annotated with the
        /// Round number and MatchNum.
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
            var winners = new List<string?>();
            int matchCounter = 1;
            var firstRound = new List<MatchInfo>();
            for (int i = 0; i < bracketSize; i += 2)
            {
                var team1 = slots[i];
                var team2 = slots[i + 1];
                var match = new MatchInfo
                {
                    Team1Name = team1,
                    Team2Name = team2,
                    Round = 1,
                    MatchNum = matchCounter++
                };
                if (team1 != null && team2 != null)
                {
                    int score1 = random.Next(0, 11);
                    int score2 = random.Next(0, 11);
                    if (score1 == score2)
                    {
                        score2 += 1;
                    }
                    match.Team1Score = score1;
                    match.Team2Score = score2;
                    winners.Add(score1 >= score2 ? team1 : team2);
                }
                else if (team1 != null && team2 == null)
                {
                    match.Team1Score = 1;
                    match.Team2Score = 0;
                    winners.Add(team1);
                }
                else if (team2 != null && team1 == null)
                {
                    match.Team1Score = 0;
                    match.Team2Score = 1;
                    winners.Add(team2);
                }
                else
                {
                    winners.Add(null);
                }
                firstRound.Add(match);
            }
            result.Add(firstRound);

            for (int r = 2; r <= rounds; r++)
            {
                var nextWinners = new List<string?>();
                var roundMatches = new List<MatchInfo>();
                matchCounter = 1;
                for (int i = 0; i < winners.Count; i += 2)
                {
                    var w1 = winners[i];
                    var w2 = (i + 1 < winners.Count) ? winners[i + 1] : null;
                    var match = new MatchInfo
                    {
                        Team1Name = w1,
                        Team2Name = w2,
                        Round = r,
                        MatchNum = matchCounter++
                    };
                    if (w1 != null && w2 != null)
                    {
                        int score1 = random.Next(0, 11);
                        int score2 = random.Next(0, 11);
                        if (score1 == score2)
                        {
                            score1 += 1;
                        }
                        match.Team1Score = score1;
                        match.Team2Score = score2;
                        nextWinners.Add(score1 >= score2 ? w1 : w2);
                    }
                    else if (w1 != null && w2 == null)
                    {
                        match.Team1Score = 1;
                        match.Team2Score = 0;
                        nextWinners.Add(w1);
                    }
                    else if (w2 != null && w1 == null)
                    {
                        match.Team1Score = 0;
                        match.Team2Score = 1;
                        nextWinners.Add(w2);
                    }
                    else
                    {
                        nextWinners.Add(null);
                    }
                    roundMatches.Add(match);
                }
                result.Add(roundMatches);
                winners = nextWinners;
            }
            return result;
        }

        /// <summary>
        /// Updates the bracket rounds generated by GenerateBracketRounds to include Round
        /// and MatchNum on each match. This helper is used when generating a bracket
        /// without scores so that the UI can rely on these properties for ordering.
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