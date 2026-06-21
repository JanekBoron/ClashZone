using System;
using System.Collections.Generic;
using System.Linq;

namespace ClashZone.App.Domains
{
    public static class GameCatalog
    {
        public const string Cs2 = "Counter Strike 2";
        public const string Lol = "League of Legends";

        public static readonly IReadOnlyList<string> AllGames = new[] { Cs2, Lol };

        public static bool IsValid(string? game) =>
            !string.IsNullOrWhiteSpace(game) &&
            AllGames.Contains(game, StringComparer.OrdinalIgnoreCase);

        public static string? Normalize(string? game) =>
            AllGames.FirstOrDefault(g => string.Equals(g, game, StringComparison.OrdinalIgnoreCase));

        public static IReadOnlyList<string> AllowedFormats(string? game) =>
            IsLol(game) ? new[] { "1v1", "5v5" } : new[] { "1v1", "2v2", "5v5" };

        public static bool IsFormatAllowed(string? game, string? format) =>
            !string.IsNullOrWhiteSpace(format) &&
            AllowedFormats(game).Contains(format, StringComparer.OrdinalIgnoreCase);

        public static int RosterSize(string? format)
        {
            if (string.IsNullOrWhiteSpace(format)) return 1;
            var prefix = new string(format.TakeWhile(char.IsDigit).ToArray());
            return int.TryParse(prefix, out var n) && n > 0 ? n : 1;
        }

        public static string ScoreLabel(string? game) => IsLol(game) ? "Wygrane mapy" : "Rundy";

        public static bool UsesChampions(string? game) => IsLol(game);

        public static string ShortBadge(string? game) =>
            IsLol(game) ? "LoL" : IsCs2(game) ? "CS2" : (game ?? string.Empty);

        public static bool IsLol(string? game) => string.Equals(game, Lol, StringComparison.OrdinalIgnoreCase);
        public static bool IsCs2(string? game) => string.Equals(game, Cs2, StringComparison.OrdinalIgnoreCase);

        public static List<string> DefaultRules(string? game)
        {
            if (IsLol(game))
            {
                return new List<string>
                {
                    "Mecze rozgrywane są na mapie Summoner's Rift w trybie Draft Pick.",
                    "Zakaz używania bohaterów spoza oficjalnej rotacji turniejowej.",
                    "Składy drużyn są zamrożone po zakończeniu zapisów."
                };
            }
            if (IsCs2(game))
            {
                return new List<string>
                {
                    "Każdy mecz rozgrywany jest w formacie best of 3 (do dwóch wygranych map).",
                    "Zabrania się wykorzystywania błędów gry oraz niedozwolonych skryptów.",
                    "Skład drużyny musi liczyć dokładnie 5 zawodników."
                };
            }
            return new List<string>
            {
                "Obowiązują standardowe zasady fair play.",
                "Każdy mecz musi zostać rozegrany w ustalonym terminie."
            };
        }

        public static readonly IReadOnlyList<string> Champions = new[]
        {
            "Ahri", "Lee Sin", "Yasuo", "Jinx", "Thresh", "Lux", "Zed", "Ekko",
            "Kai'Sa", "Garen", "Darius", "Vi", "Jhin", "Leona", "Riven", "Akali",
            "Sona", "Caitlyn", "Malphite", "Orianna", "Sett", "Nautilus", "Viktor",
            "Camille", "Senna", "Pyke", "Aatrox", "Morgana", "Kayn", "Irelia",
            "Lucian", "Nami", "Fizz", "Volibear", "Syndra"
        };
    }
}
