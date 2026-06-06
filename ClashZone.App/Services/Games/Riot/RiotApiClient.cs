using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using ClashZone.App.Domains.Riot;
using ClashZone.App.Ports.Games;

namespace ClashZone.App.Services.Games.Riot
{
    /// <summary>
    /// Implementacja <see cref="IRiotApiClient"/> oparta o typed HttpClient.
    /// Host bazowy (regionalny) i nagłówek X-Riot-Token są konfigurowane przy
    /// rejestracji klienta w kontenerze DI. Klient jest defensywny: przy braku
    /// klucza, błędzie HTTP czy wyjątku sieciowym zwraca null/pustą listę, aby
    /// nie wywracać przepływu turnieju (CS2 i tak działa niezależnie).
    /// </summary>
    public sealed class RiotApiClient : IRiotApiClient
    {
        private readonly HttpClient _http;
        private readonly RiotOptions _options;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public RiotApiClient(HttpClient http, RiotOptions options)
        {
            _http = http;
            _options = options;
        }

        public bool IsConfigured => _options.IsConfigured;

        public async Task<RiotAccount?> ResolveAccountAsync(string gameName, string tagLine, CancellationToken ct = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(gameName) || string.IsNullOrWhiteSpace(tagLine))
            {
                return null;
            }

            var url = $"/riot/account/v1/accounts/by-riot-id/{Uri.EscapeDataString(gameName)}/{Uri.EscapeDataString(tagLine)}";
            var dto = await GetJsonAsync<AccountDto>(url, ct);
            if (dto == null || string.IsNullOrEmpty(dto.Puuid))
            {
                return null;
            }

            return new RiotAccount
            {
                Puuid = dto.Puuid,
                GameName = dto.GameName ?? gameName,
                TagLine = dto.TagLine ?? tagLine
            };
        }

        public async Task<IReadOnlyList<string>> GetRecentMatchIdsAsync(string puuid, int count, CancellationToken ct = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(puuid))
            {
                return Array.Empty<string>();
            }

            count = count <= 0 ? 5 : Math.Min(count, 20);
            var url = $"/lol/match/v5/matches/by-puuid/{Uri.EscapeDataString(puuid)}/ids?start=0&count={count}";
            var ids = await GetJsonAsync<List<string>>(url, ct);
            return ids ?? (IReadOnlyList<string>)Array.Empty<string>();
        }

        public async Task<RiotMatch?> GetMatchAsync(string matchId, CancellationToken ct = default)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(matchId))
            {
                return null;
            }

            var url = $"/lol/match/v5/matches/{Uri.EscapeDataString(matchId)}";
            var dto = await GetJsonAsync<MatchDto>(url, ct);
            if (dto?.Info?.Participants == null)
            {
                return null;
            }

            return new RiotMatch
            {
                MatchId = matchId,
                Participants = dto.Info.Participants.Select(p => new RiotMatchParticipant
                {
                    Puuid = p.Puuid ?? string.Empty,
                    TeamId = p.TeamId,
                    Win = p.Win,
                    Kills = p.Kills,
                    Deaths = p.Deaths,
                    Assists = p.Assists,
                    ChampionName = p.ChampionName ?? string.Empty
                }).ToList()
            };
        }

        /// <summary>
        /// Wykonuje GET i deserializuje JSON. Obsługuje pojedynczy retry przy 429
        /// (limit zapytań) z poszanowaniem nagłówka Retry-After. Każdy inny błąd
        /// (4xx/5xx, wyjątek sieciowy) kończy się zwróceniem domyślnej wartości.
        /// </summary>
        private async Task<T?> GetJsonAsync<T>(string url, CancellationToken ct)
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                try
                {
                    using var response = await _http.GetAsync(url, ct);

                    if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt == 0)
                    {
                        var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(1);
                        await Task.Delay(retryAfter, ct);
                        continue;
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        return default;
                    }

                    var json = await response.Content.ReadAsStringAsync(ct);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return default;
                    }

                    return JsonSerializer.Deserialize<T>(json, JsonOptions);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
                {
                    return default;
                }
            }

            return default;
        }

        // ---- Wewnętrzne DTO odwzorowujące kształt odpowiedzi Riota (camelCase) ----

        private sealed class AccountDto
        {
            public string? Puuid { get; set; }
            public string? GameName { get; set; }
            public string? TagLine { get; set; }
        }

        private sealed class MatchDto
        {
            public MatchInfoDto? Info { get; set; }
        }

        private sealed class MatchInfoDto
        {
            public List<ParticipantDto>? Participants { get; set; }
        }

        private sealed class ParticipantDto
        {
            public string? Puuid { get; set; }
            public int TeamId { get; set; }
            public bool Win { get; set; }
            public int Kills { get; set; }
            public int Deaths { get; set; }
            public int Assists { get; set; }
            public string? ChampionName { get; set; }
        }
    }
}
