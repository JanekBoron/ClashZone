using ClashZone.DataAccess.Models;
using ClashZone.DataAccess.Repository.Interfaces;
using ClashZone.Services.Interfaces;
using ClashZone.ViewModels;
using Microsoft.AspNetCore.Identity;

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
            // Bracket becomes available only after start time
            /*if (DateTime.UtcNow < tournament.StartDate)
            {
                return null;
            }*/
            // Retrieve all teams for the tournament
            var teams = await _tournamentsRepository.GetTeamsForTournamentAsync(tournamentId);
            // Build list of team names, using captain username as fallback
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
            // Generate bracket rounds using private helper
            List<List<MatchInfo>> rounds;
            if (teamNames.Count < 2)
            {
                rounds = new List<List<MatchInfo>>();
            }
            else
            {
                rounds = GenerateBracketRounds(teamNames);
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
            // Determine winners for next rounds
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
    }
}
