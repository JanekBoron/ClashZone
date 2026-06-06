using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ClashZone.App.Services.Games.Riot;
using Xunit;

namespace ClashZone.Tests
{
    /// <summary>
    /// Testy <see cref="RiotApiClient"/> — parsowania odpowiedzi Account-V1/Match-V5
    /// oraz defensywnego zachowania (brak klucza, błędny status). HTTP jest
    /// podstawiane przez własny <see cref="StubHandler"/>, bez realnej sieci.
    /// </summary>
    public class RiotApiClientTests
    {
        private sealed class StubHandler : HttpMessageHandler
        {
            private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
            public int Calls { get; private set; }

            public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Calls++;
                return Task.FromResult(_responder(request));
            }
        }

        private static RiotApiClient CreateClient(StubHandler handler, string apiKey = "dev-key")
        {
            var http = new HttpClient(handler) { BaseAddress = new Uri("https://europe.api.riotgames.com") };
            return new RiotApiClient(http, new RiotOptions { ApiKey = apiKey });
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body)
            => new(status) { Content = new StringContent(body) };

        [Fact]
        public async Task ResolveAccountAsync_ReturnsAccount_FromJson()
        {
            var handler = new StubHandler(_ => Json(HttpStatusCode.OK,
                "{\"puuid\":\"abc-123\",\"gameName\":\"Faker\",\"tagLine\":\"KR1\"}"));
            var client = CreateClient(handler);

            var account = await client.ResolveAccountAsync("Faker", "KR1");

            Assert.NotNull(account);
            Assert.Equal("abc-123", account!.Puuid);
            Assert.Equal("Faker#KR1", account.RiotId);
        }

        [Fact]
        public async Task ResolveAccountAsync_ReturnsNull_AndSkipsHttp_WhenNotConfigured()
        {
            var handler = new StubHandler(_ => Json(HttpStatusCode.OK, "{}"));
            var client = CreateClient(handler, apiKey: "");

            var account = await client.ResolveAccountAsync("Faker", "KR1");

            Assert.Null(account);
            Assert.Equal(0, handler.Calls); // bez klucza nie wykonujemy żadnego żądania
        }

        [Fact]
        public async Task GetMatchAsync_MapsParticipants()
        {
            const string json = "{\"info\":{\"participants\":[" +
                "{\"puuid\":\"p1\",\"teamId\":100,\"win\":true,\"kills\":7,\"deaths\":2,\"assists\":10,\"championName\":\"Ahri\"}," +
                "{\"puuid\":\"p2\",\"teamId\":200,\"win\":false,\"kills\":3,\"deaths\":8,\"assists\":5,\"championName\":\"Zed\"}" +
                "]}}";
            var handler = new StubHandler(_ => Json(HttpStatusCode.OK, json));
            var client = CreateClient(handler);

            var match = await client.GetMatchAsync("EUW1_1");

            Assert.NotNull(match);
            Assert.Equal(2, match!.Participants.Count);
            var ahri = Assert.Single(match.Participants, p => p.Puuid == "p1");
            Assert.True(ahri.Win);
            Assert.Equal(100, ahri.TeamId);
            Assert.Equal(7, ahri.Kills);
            Assert.Equal("Ahri", ahri.ChampionName);
        }

        [Fact]
        public async Task GetMatchAsync_ReturnsNull_OnHttpError()
        {
            var handler = new StubHandler(_ => Json(HttpStatusCode.NotFound, ""));
            var client = CreateClient(handler);

            Assert.Null(await client.GetMatchAsync("EUW1_unknown"));
        }
    }
}
