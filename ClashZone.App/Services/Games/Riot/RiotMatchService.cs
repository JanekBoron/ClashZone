using ClashZone.App.Domains.Riot;
using ClashZone.App.Ports.DataAccess;
using ClashZone.App.Ports.Games;

namespace ClashZone.App.Services.Games.Riot
{
    /// <summary>
    /// Implementacja <see cref="IRiotMatchService"/>. Mapuje użytkowników ClashZone
    /// na konta Riot (po zapisanym PUUID), wyszukuje ostatnią wspólną grę obu drużyn
    /// w Match-V5 i buduje z niej <see cref="RiotMatchOutcome"/>.
    /// </summary>
    public sealed class RiotMatchService : IRiotMatchService
    {
        private readonly IRiotApiClient _riotApi;
        private readonly IUserRepository _userRepository;
        private readonly RiotOptions _options;

        public RiotMatchService(IRiotApiClient riotApi, IUserRepository userRepository, RiotOptions options)
        {
            _riotApi = riotApi;
            _userRepository = userRepository;
            _options = options;
        }

        public async Task<RiotMatchOutcome?> ResolveMatchAsync(
            IReadOnlyList<string> team1UserIds,
            IReadOnlyList<string> team2UserIds,
            bool forceDevMode = false,
            CancellationToken ct = default)
        {
            if (!_riotApi.IsConfigured)
            {
                return null;
            }

            // Tryb deweloperski: zamiast szukać wspólnej gry obu drużyn, bierzemy
            // najnowszy realny mecz jednego konta-źródła (admina). Reszta graczy
            // jest pomijana – staty mapujemy po kolei na składy ClashZone.
            // Włączany globalną flagą Riot:DevMode lub wymuszany przez admina (forceDevMode).
            if (_options.DevMode || forceDevMode)
            {
                return await ResolveDevModeAsync(team1UserIds, team2UserIds, ct);
            }

            // Mapowanie PUUID -> (userId, numer drużyny ClashZone). Pomijamy graczy
            // bez powiązanego konta Riot.
            var puuidToUser = new Dictionary<string, (string UserId, int Team)>(StringComparer.OrdinalIgnoreCase);
            var team1Puuids = await CollectPuuidsAsync(team1UserIds, 1, puuidToUser);
            var team2Puuids = await CollectPuuidsAsync(team2UserIds, 2, puuidToUser);

            // Bez choć jednego powiązanego konta po każdej stronie nie zweryfikujemy meczu.
            if (team1Puuids.Count == 0 || team2Puuids.Count == 0)
            {
                return null;
            }

            // Szukamy wspólnej gry, przeglądając ostatnie mecze jednego z graczy drużyny 1.
            var seedPuuid = team1Puuids[0];
            var matchIds = await _riotApi.GetRecentMatchIdsAsync(seedPuuid, _options.MatchLookbackCount, ct);

            foreach (var matchId in matchIds)
            {
                var match = await _riotApi.GetMatchAsync(matchId, ct);
                if (match == null)
                {
                    continue;
                }

                var outcome = BuildOutcome(match, puuidToUser, team1Puuids, team2Puuids);
                if (outcome != null)
                {
                    return outcome;
                }
            }

            return null;
        }

        private async Task<List<string>> CollectPuuidsAsync(
            IReadOnlyList<string> userIds,
            int teamNumber,
            Dictionary<string, (string, int)> puuidToUser)
        {
            var puuids = new List<string>();
            foreach (var userId in userIds)
            {
                var user = await _userRepository.FindByIdAsync(userId);
                var puuid = user?.RiotPuuid;
                if (!string.IsNullOrWhiteSpace(puuid) && !puuidToUser.ContainsKey(puuid))
                {
                    puuidToUser[puuid] = (userId, teamNumber);
                    puuids.Add(puuid);
                }
            }
            return puuids;
        }

        // ---- Tryb deweloperski ----

        /// <summary>
        /// Buduje wynik z najnowszego realnego meczu konta-źródła (admina). Strona
        /// admina w grze staje się drużyną 1 ClashZone, przeciwna – drużyną 2.
        /// Zwraca null, gdy nie ma konta-źródła ani żadnego meczu z matchmakingu.
        /// </summary>
        private async Task<RiotMatchOutcome?> ResolveDevModeAsync(
            IReadOnlyList<string> team1UserIds,
            IReadOnlyList<string> team2UserIds,
            CancellationToken ct)
        {
            var seedPuuid = await ResolveSeedPuuidAsync(team1UserIds, team2UserIds, ct);
            if (string.IsNullOrWhiteSpace(seedPuuid))
            {
                return null;
            }

            // Lista meczów zwraca tylko gry z matchmakingu (nie custom), więc
            // pierwszy pasujący mecz to najnowsza realna gra konta-źródła.
            var matchIds = await _riotApi.GetRecentMatchIdsAsync(seedPuuid, _options.MatchLookbackCount, ct);
            foreach (var matchId in matchIds)
            {
                var match = await _riotApi.GetMatchAsync(matchId, ct);
                var seed = match?.Participants.FirstOrDefault(p =>
                    string.Equals(p.Puuid, seedPuuid, StringComparison.OrdinalIgnoreCase));
                if (seed == null)
                {
                    continue;
                }

                return BuildDevOutcome(match!, seed, team1UserIds, team2UserIds);
            }

            return null;
        }

        /// <summary>
        /// Ustala PUUID konta-źródła: najpierw skonfigurowany Riot ID admina
        /// (<see cref="RiotOptions.DevSeedRiotId"/>), a w razie braku – pierwsze
        /// powiązane konto wśród graczy meczu.
        /// </summary>
        private async Task<string?> ResolveSeedPuuidAsync(
            IReadOnlyList<string> team1UserIds,
            IReadOnlyList<string> team2UserIds,
            CancellationToken ct)
        {
            var configured = _options.DevSeedRiotId;
            if (!string.IsNullOrWhiteSpace(configured) && configured.Contains('#'))
            {
                var parts = configured.Split('#', 2);
                var account = await _riotApi.ResolveAccountAsync(parts[0].Trim(), parts[1].Trim(), ct);
                if (!string.IsNullOrWhiteSpace(account?.Puuid))
                {
                    return account!.Puuid;
                }
            }

            foreach (var userId in team1UserIds.Concat(team2UserIds))
            {
                var user = await _userRepository.FindByIdAsync(userId);
                if (!string.IsNullOrWhiteSpace(user?.RiotPuuid))
                {
                    return user!.RiotPuuid;
                }
            }

            return null;
        }

        private static RiotMatchOutcome BuildDevOutcome(
            RiotMatch match,
            RiotMatchParticipant seed,
            IReadOnlyList<string> team1UserIds,
            IReadOnlyList<string> team2UserIds)
        {
            var team1Side = match.Participants.Where(p => p.TeamId == seed.TeamId).ToList();
            var team2Side = match.Participants.Where(p => p.TeamId != seed.TeamId).ToList();

            bool team1Won = seed.Win;

            var players = new List<RiotMatchPlayerStat>();
            players.AddRange(MapSide(team1UserIds, team1Side));
            players.AddRange(MapSide(team2UserIds, team2Side));

            return new RiotMatchOutcome
            {
                SourceMatchId = match.MatchId,
                Team1Score = team1Won ? 1 : 0,
                Team2Score = team1Won ? 0 : 1,
                Players = players
            };
        }

        /// <summary>
        /// Mapuje statystyki uczestników jednej strony gry na graczy ClashZone
        /// (po kolei). Nadmiarowi uczestnicy/gracze są pomijani.
        /// </summary>
        private static IEnumerable<RiotMatchPlayerStat> MapSide(
            IReadOnlyList<string> userIds,
            IReadOnlyList<RiotMatchParticipant> participants)
        {
            int count = Math.Min(userIds.Count, participants.Count);
            for (int i = 0; i < count; i++)
            {
                var p = participants[i];
                yield return new RiotMatchPlayerStat
                {
                    UserId = userIds[i],
                    Kills = p.Kills,
                    Deaths = p.Deaths,
                    Assists = p.Assists,
                    Champion = string.IsNullOrEmpty(p.ChampionName) ? null : p.ChampionName
                };
            }
        }

        /// <summary>
        /// Buduje wynik z meczu Riota, jeśli zawiera on graczy z obu drużyn ClashZone
        /// stojących po przeciwnych stronach. W przeciwnym razie zwraca null
        /// (to nie był wspólny mecz turniejowy).
        /// </summary>
        private static RiotMatchOutcome? BuildOutcome(
            RiotMatch match,
            IReadOnlyDictionary<string, (string UserId, int Team)> puuidToUser,
            IReadOnlyList<string> team1Puuids,
            IReadOnlyList<string> team2Puuids)
        {
            // Uczestnicy meczu należący do naszych drużyn.
            var relevant = match.Participants
                .Where(p => puuidToUser.ContainsKey(p.Puuid))
                .ToList();

            var team1InMatch = relevant.Where(p => puuidToUser[p.Puuid].Team == 1).ToList();
            var team2InMatch = relevant.Where(p => puuidToUser[p.Puuid].Team == 2).ToList();
            if (team1InMatch.Count == 0 || team2InMatch.Count == 0)
            {
                return null;
            }

            // Obie drużyny ClashZone muszą stać po przeciwnych stronach gry.
            var riotTeam1 = team1InMatch[0].TeamId;
            var riotTeam2 = team2InMatch[0].TeamId;
            if (riotTeam1 == riotTeam2)
            {
                return null;
            }

            bool team1Won = team1InMatch[0].Win;

            var outcome = new RiotMatchOutcome
            {
                SourceMatchId = match.MatchId,
                Team1Score = team1Won ? 1 : 0,
                Team2Score = team1Won ? 0 : 1,
                Players = relevant.Select(p => new RiotMatchPlayerStat
                {
                    UserId = puuidToUser[p.Puuid].UserId,
                    Kills = p.Kills,
                    Deaths = p.Deaths,
                    Assists = p.Assists,
                    Champion = string.IsNullOrEmpty(p.ChampionName) ? null : p.ChampionName
                }).ToList()
            };

            return outcome;
        }
    }
}
