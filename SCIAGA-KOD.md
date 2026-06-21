# ClashZone — Ściąga do kodu (mapa aplikacji)

> Ściąga pod szybkie wyszukiwanie (Ctrl+F). Opisuje architekturę, technologie oraz każdą dużą
> funkcjonalność: gdzie jest kod, co robi klasa, jakie są reguły biznesowe.
> Platforma e-sportowa: turnieje (drabinki), liga (sezony FACEIT-style), subskrypcje (Stripe),
> sklep za ClashCoins, statystyki/rankingi (w tym realne dane z Riot API dla LoL).

---

## 1. Spis treści (słowa kluczowe do Ctrl+F)

- [Architektura i projekty](#2-architektura--struktura-projektów)
- [Technologie / biblioteki](#3-technologie-i-biblioteki)
- [Warstwa danych: DbContext, modele, migracje](#4-warstwa-danych-dataaccess)
- [Repozytoria (porty + adaptery)](#5-repozytoria-portyadaptery)
- [AutoMapper / profile mapowania](#6-automapper--profile-mapowania)
- [Konto / logowanie / rejestracja / ban](#7-konto-rejestracja-logowanie-reset-hasła-ban)
- [Turnieje (Tournaments)](#8-turnieje-tournaments)
- [Drabinka (Bracket) + symulacja meczów](#9-drabinka-bracketservice--symulacja)
- [Mecze i szczegóły meczu (Matches)](#10-mecze-matchesservice)
- [Liga / Sezony (League)](#11-liga--sezony-league)
- [Drużyny ligowe (Team / LeagueTeam)](#12-drużyny-ligowe-leagueteamservice)
- [Subskrypcje + płatności Stripe](#13-subskrypcje--płatności-stripe)
- [Sklep ClashCoins (CoinShop) + portfel](#14-sklep-clashcoins-coinshop--portfel)
- [Zamówienia (Orders / ProductRedeem)](#15-zamówienia-orders--realizacja-nagród)
- [Produkty / admin sklepu (Products)](#16-produkty-admin-products)
- [Profil użytkownika + Riot link](#17-profil-użytkownika-profile)
- [Rankingi (Rankings)](#18-rankingi-rankings)
- [Riot API / League of Legends](#19-riot-api--integracja-lol)
- [Czat (turniej + liga)](#20-czat-chatservice--czat-ligi)
- [E-mail (MailKit)](#21-e-mail-emailservice)
- [Administracja użytkownikami / role](#22-administracja-użytkownikami-users--useradminservice)
- [Seed bazy / dane startowe](#23-seed-dbinitializer)
- [Testy](#24-testy)
- [Słownik encji](#25-słownik-encji-modele)

---

## 2. Architektura — struktura projektów

Wzorzec **Ports & Adapters (heksagonalny)** + ASP.NET Core MVC (kontrolery + Razor Views).
Migracja z architektury warstwowej (commit `cafc1a4`).

| Projekt | Rola | Zawiera |
|---|---|---|
| **ClashZone** (web) | Warstwa prezentacji / hosting | `Controllers/`, `Views/`, `wwwroot/`, `Program.cs`, `Security/` |
| **ClashZone.App** | Rdzeń aplikacji (logika + kontrakty) | `Services/` (logika biznesowa), `Ports/` (interfejsy), `Domains/` (modele domenowe), `ViewModels/` |
| **ClashZone.DataAccess** | Adapter persystencji (EF Core) | `ApplicationDbContext`, `Models/` (encje EF), `Repository/` (adaptery portów), `Config/` (profile AutoMapper), `Migrations/`, `DbInitializer/` |
| **ClashZone.Tests** | Testy jednostkowe | xUnit + Moq + EF InMemory |
| **ClashZone.E2E.Tests** | Testy E2E | Playwright + NUnit (realny browser na działającej apce) |

**Przepływ zależności:** `Controllers → Services (App) → Ports (interfejsy w App) ← Repository (DataAccess implementuje porty)`.
`DataAccess` referencjuje `App` (żeby implementować porty i używać `*Domain`). Web referencjuje oba.

**Kluczowe katalogi:**
- Porty danych: `ClashZone.App/Ports/DataAccess/I*Repository.cs`
- Porty gier (Riot): `ClashZone.App/Ports/Games/IRiot*.cs`
- Interfejsy serwisów: `ClashZone.App/Services/Interfaces/I*Service.cs`
- Modele domenowe (DTO logiki): `ClashZone.App/Domains/*Domain.cs`
- Encje EF (tabele): `DataAccess/Models/*.cs`

**Konwencja:** `XyzDomain` = model domenowy (w App), `Xyz` (bez sufiksu) = encja EF (w DataAccess).
AutoMapper mapuje encja ↔ domain w repozytoriach.

**`Program.cs`** (`ClashZone/Program.cs`) — kompozycja/DI:
- `AddControllersWithViews`, Swagger (Swashbuckle, skonfigurowany ale `UseSwagger` zakomentowany).
- `AddAutoMapper` (z kluczem licencyjnym, profile z `Program` i `ClashUserProfile`).
- `AddDbContext<ApplicationDbContext>` → SQL Server (`DefaultConnection`).
- `AddIdentity<ClashUser, IdentityRole>` (bez wymogu potwierdzenia e-mail), EF stores, token providers.
- Podmiana `SignInManager` na `BannedAwareSignInManager`.
- Rejestracja wszystkich repozytoriów i serwisów jako **Scoped**.
- `RiotOptions` z sekcji `Riot` → Singleton + `AddHttpClient<IRiotApiClient, RiotApiClient>` (nagłówek `X-Riot-Token`).
- `Stripe.StripeConfiguration.ApiKey` z `Stripe:SecretKey`.
- Pipeline: HTTPS redirect, static files, routing, **Authentication → Authorization**, default route `{controller=Home}/{action=Index}/{id?}`.
- `await DbInitializer.InitializeAsync(...)` — migracje + seed na starcie.

---

## 3. Technologie i biblioteki

| Technologia | Wersja | Gdzie / po co |
|---|---|---|
| **.NET / C#** | net9.0, nullable enabled | wszystkie projekty |
| **ASP.NET Core MVC** | 9.0 | kontrolery + Razor Views |
| **ASP.NET Core Identity** | 9.0 | logowanie, role, użytkownicy (`ClashUser : IdentityUser`) |
| **Entity Framework Core** | 9.0.7 | ORM, `ApplicationDbContext : IdentityDbContext<ClashUser>` |
| **EF Core SqlServer** | 9.0.7 | baza SQL Server (`UseSqlServer`) |
| **EF Migrations** | — | `DataAccess/Migrations/` (~22 migracje) |
| **AutoMapper** | 16.0.0 | mapowanie encja ↔ domain; profile w `DataAccess/Config/` |
| **Stripe.net** | 48.4.0 | płatności subskrypcji (Checkout Session); `PaymentController` |
| **MailKit** | 4.13.0 | wysyłka e-maili (SMTP); `EmailService` |
| **Swashbuckle.AspNetCore** | 9.0.3 | Swagger/OpenAPI (skonfigurowany, wyłączony domyślnie) |
| **Riot Games API** | Account-V1, Match-V5 | realne statystyki LoL; `RiotApiClient` przez `HttpClient` |
| **xUnit + Moq** | 2.9.3 / 4.20.72 | testy jednostkowe |
| **EF Core InMemory** | 9.0.8 | baza w testach |
| **Playwright + NUnit** | 1.49.0 / 4.2.2 | testy E2E (browser) |
| **Bootstrap** (RGB UI) | — | front (wwwroot), klasy `bg-success` itd. |

**Mechanizmy własne:**
- **ClashCoins** — wirtualna waluta (portfel + transakcje), nagrody turniejowe/ligowe, sklep.
- **GameCatalog** — katalog gier (CS2 / LoL) z regułami formatów, rozmiarem rosteru, listą bohaterów LoL.

---

## 4. Warstwa danych (DataAccess)

**`ApplicationDbContext`** (`DataAccess/ApplicationDbContext.cs`) — `IdentityDbContext<ClashUser>`.
DbSety (= tabele):
- Turnieje: `Tournaments`, `Teams`, `TeamMembers`, `ChatMessages`
- Subskrypcje: `SubscriptionPlans`, `UserSubscriptions`
- Sklep/ClashCoins: `Products`, `ProductCategories`, `ProductImages`, `CoinWallets`, `CoinWalletTransactions`, `ProductRedeems`
- Statystyki: `Matches`, `PlayerMatchStats`, `UserStats`
- Liga: `LeagueTeams`, `LeagueTeamMembers`, `Seasons`, `LeagueRegistrations`, `LeagueMatches`

**Migracje** (`DataAccess/Migrations/`) — historia schematu, m.in.:
`SettingUpIndetity`, `addingDetailTournaments`, `addingChatFunctionality`, `addingBracket`,
`addingSubscriptionPayments`, `AddingCoinShopModels`, `AddProfileAndMatchesFunctionality`,
`BannableUsers`, `ReportFunctionality`, `AddGameAndChampionToStats`, `AddTournamentRules`,
`AddLeague`, `AddRiotAccountToClashUser`, `AddTeamReadyConfirm`, `AddShippingAddressToProductRedeem`.

**`DbInitializer`** + **`RoleSeeder`** (`DataAccess/DbInitializer/`) — migracje + seed (patrz §23).

---

## 5. Repozytoria (porty/adaptery)

Interfejsy (porty) w `ClashZone.App/Ports/DataAccess/`, implementacje w `DataAccess/Repository/`.
Wszystkie wstrzykiwane jako Scoped w `Program.cs`. Każde mapuje encje EF → `*Domain` przez AutoMapper.

| Port (interfejs) | Adapter | Odpowiada za |
|---|---|---|
| `ITournamentsRepository` | `TournamentsRepository` | turnieje, drużyny turniejowe, członkowie, gotowość/potwierdzenie, czat turnieju |
| `IMatchesRepository` | `MatchesRepository` | mecze turnieju, statystyki per mecz, sprawdzanie przyznania nagród |
| `IPlayerStatsRepository` | `PlayerStatsRepository` | `PlayerMatchStat` + agregaty `UserStat`, ulubiony bohater LoL |
| `ISubscriptionRepository` | `SubscriptionRepository` | plany, aktywna subskrypcja, zakup subskrypcji |
| `IProductsRepository` | `ProductRepository` | produkty sklepu (CRUD, aktywne) |
| `IProductRedeemRepository` | `ProductRedeemRepository` | zamówienia/realizacje nagród + statusy |
| `ICoinWalletRepository` | `CoinWalletRepository` | portfel ClashCoins, transakcje, saldo, kupione produkty |
| `IUserRepository` | `UserRepository` | użytkownicy, role, ban/unban, powiązanie konta Riot |
| `ILeagueRepository` | `LeagueRepository` | sezony, rejestracje, mecze ligowe, czat ligi, statystyki meczu ligowego |
| `ILeagueTeamsRepository` | `LeagueTeamsRepository` | trwałe drużyny ligowe, członkowie, kody, cooldown dołączeń |

**Cooldown dołączeń do drużyn ligowych** — `ILeagueTeamsRepository`:
`CountMembersJoinedSinceAsync`, `UserJoinedAnyTeamSinceAsync` (anty-spam zmiany składu).

---

## 6. AutoMapper — profile mapowania

Lokalizacja: `DataAccess/Config/*Profile.cs`. Po jeden profil na encję.
Rejestracja w `Program.cs` (`AddAutoMapper`, typy zaczepne: `Program`, `ClashUserProfile`).

Profile: `ClashUserProfile`, `TournamentProfile`, `TournamentFormProfile`, `TeamProfile`, `TeamMemberProfile`,
`ChatMessageProfile`, `MatchProfile`, `MatchInfoProfile`, `PlayerMatchStatProfile`, `UserStatProfile`,
`SubscriptionPlanProfile`, `UserSubscriptionProfile`, `ProductProfile`, `ProductCategoryProfile`,
`ProductImageProfile`, `ProductRedeemProfile`, `CoinWalletProfile`, `CoinWalletTransactionProfile`,
`StripeSettingsProfile`, `LeagueProfile`.

> **PUŁAPKA (z pamięci projektu):** `ReverseMap()` wywala się przy zapełnionych nawigacjach
> (domain→entity) — trzeba ignorować nawigacje. Realne błędy runtime trafiły w mapowanie
> `UserStat` i `Product`. Już naprawione.

---

## 7. Konto: rejestracja, logowanie, reset hasła, ban

**`AccountController`** (`ClashZone/Controllers/AccountController.cs`) — Identity bez warstwy serwisowej.
- `Register` (GET/POST) — sprawdza unikalność nazwy i e-maila, `UserManager.CreateAsync`, `EmailConfirmed=true`.
- `Login` (GET/POST) — `SignInManager.PasswordSignInAsync`.
- `Logout`.
- `ForgotPassword` (GET/POST) — generuje token, wysyła e-mail (`IEmailService`).
- `ResetPassword` (GET/POST) — token + e-mail → nowe hasło.
- `ConfirmEmail`.
- ViewModele: `RegisterViewModel`, `LoginViewModel`, `ForgotPasswordViewModel`, `ResetPasswordViewModel`.

**Ban — `BannedAwareSignInManager`** (`ClashZone/Security/BannedAwareSignInManager.cs`):
nadpisuje `CanSignInAsync` → gdy `user.IsBanned == true`, blokuje logowanie. Zarejestrowany w DI zamiast domyślnego.

**`ClashUser`** (`DataAccess/Models/ClashUser.cs`) — `IdentityUser` + pola:
`DisplayName`, `Score`, `Awards`, `ProfilePicturePath`, `IsBanned`, `RiotId`, `RiotPuuid`.

---

## 8. Turnieje (Tournaments)

**`TournamentsController`** (`ClashZone/Controllers/TournamentsController.cs`) — centralny kontroler turniejów.
Akcje: `Index(format)` (lista nadchodzących, filtr formatu), `MyTournaments`, `Create`/`CreateTournament`,
`Details(id)`, `Join(id)`, `ToggleReady(id)`, `ConfirmTeam(id)`, `JoinTeam(teamId, code)`,
`Matches(id)`, `MatchDetails`, `Rules(id)`, `ParticipantsList(id)`, `Chat`/`PostMessage`/`PostReportMessage`,
`Bracket(id)`, `GenerateResults(id)`, `Manage`, `Edit`, `Delete`.

**`TournamentService`** (`ClashZone.App/Services/TournamentService.cs`) — `ITournamentService`. Reguły:
- `CreateTournamentAsync` — prywatny turniej dostaje `JoinCode` (Guid, 8 znaków).
- `GetTournamentDetailsAsync` — buduje VM, liczy **potwierdzone** drużyny vs `MaxParticipants` (`TournamentFull`),
  ustala `IsCaptain`, `CanConfirm` (kapitan + skład = wymagany roster + wszyscy `Ready` + jest miejsce).
- `JoinTournamentAsync` — walidacje kolejno: nie istnieje / limit / **premium wymaga subskrypcji** (`IsPremiumAccess`) /
  już dołączony → inaczej tworzy drużynę z kapitanem (`CreateTeamWithCaptainAsync`). Zwraca `JoinTournamentResult`.
- `ToggleReadyAsync` — przełącza `IsReady`; blokuje ustawienie gotowości gdy turniej pełny.
- `ConfirmTeamAsync` — tylko kapitan; skład == `RosterSize(Format)`; wszyscy `Ready`; jest miejsce →
  `ConfirmTeamAsync` (drużyna wchodzi do drabinki). Zwraca `TeamActionResult`.
- `JoinTeamAsync(teamId, userId, code)` — dołączenie po kodzie zaproszenia.

> **Model gotowości/potwierdzenia (z pamięci):** `Team.IsConfirmed` + `TeamMember.IsReady`.
> Tylko **potwierdzone** drużyny wchodzą do drabinki i liczą się do `MaxParticipants`.

**ViewModele:** `TournamentFormViewModel`, `TournamentDetailsViewModel`, `ParticipantsViewModel`,
`RulesViewModel`, `JoinTournamentResult`, `TeamActionResult`, `TournamentMatchesViewModel`.

**Format i roster:** `GameCatalog` (`ClashZone.App/Domains/GameCatalog.cs`):
- `Cs2 = "Counter Strike 2"`, `Lol = "League of Legends"`.
- `AllowedFormats`: CS2 → 1v1/2v2/5v5; LoL → 1v1/5v5.
- `RosterSize(format)` — z prefiksu liczbowego ("5v5"→5).
- `ScoreLabel`: LoL = "Wygrane mapy", CS2 = "Rundy".
- `DefaultRules(game)`, `Champions` (35 bohaterów LoL), `ShortBadge`.

---

## 9. Drabinka (BracketService) + symulacja

**`BracketController`** (`ClashZone/Controllers/BracketController.cs`) — `[Authorize]`, akcja
`SimulateMatch(id, roundNumber, matchNumber)` → renderuje `Views/Tournaments/Bracket.cshtml`.

**`BracketService`** (`ClashZone.App/Services/BracketService.cs`) — `IBracketService`. **Serce turniejów.**
Single-elimination, rozmiar drabinki = potęga 2, bye dla niepełnych. Metody:
- `GetBracketAsync` — buduje skeleton + nakłada wyniki z DB; przy pierwszym budowaniu wysyła e-maile o meczach (`NotifyInitialMatchesAsync`).
- `GetBracketWithResultsAsync` — generuje pełną losową drabinkę z wynikami (nie zapisuje).
- `GetBracketWithStatsAsync` — jw. + zapisuje mecze i statystyki (`SaveBracketStatsAsync`).
- `SimulateMatchAsync(tournamentId, round, match, isAdmin)` — **rozgrywa jeden mecz**:
  - **LoL** → realne dane z Riot (`IRiotMatchService.ResolveMatchAsync`, `forceDevMode: isAdmin`), zapis `PersistRiotPlayerStatsAsync`.
  - **CS2** → losowy wynik (`GenerateMatchScores`) + losowe statystyki (`GenerateStatsForMatchAsync`).
  - W finale (`isFinalRound`) → `AwardTournamentPrizesAsync`.

**Logika pomocnicza:**
- `GenerateBracketSkeleton` — pusta struktura rund.
- `BuildBracketWithDbAsync` — wstrzykuje wyniki z tabeli `Matches`, wylicza zwycięzców i propaguje do kolejnych rund (`TeamPairComparer` — porównanie par drużyn niezależnie od kolejności).
- `GenerateMatchScores` — CS2: 0–15/0–15; LoL: 2:loserMaps (BO3).
- **Statystyki:** CS2 K/D/A: kills 0–20, deaths 0–20, assists 0–10. LoL: kills 0–15, deaths 0–11, assists 0–24 + bohater (`PickChampion`, bez duplikatów w meczu).
- **Agregacja:** każdy `PlayerMatchStat` aktualizuje `UserStat` (TotalKills/Deaths/Assists, MatchesPlayed) — wspólne z rankingami i ligą.
- **Nagrody (`AwardTournamentPrizesAsync` / `AwardPrizeAsync`):** parsuje napis nagrody, działa tylko gdy zawiera "ClashCoins" + liczbę; kredytuje **każdego** członka drużyny przez `ICoinWalletService.CreditAsync` (referencja `Tournament:{id}:{place}Place`). 1./2./3. miejsce (3. = przegrany półfinału). `IsTournamentAlreadyAwardedAsync` chroni przed podwójnym przyznaniem.
- **E-maile (`#region Email Notifications`):** `NotifyTeamMatchAsync` (przypomnienie o meczu), `NotifyTeamAdvanceAsync` (awans/bye).

---

## 10. Mecze (MatchesService)

**`MatchesService`** (`ClashZone.App/Services/MatchesService.cs`) — `IMatchesService`:
- `GetMatchesForTournamentAsync(tournamentId)` → `List<MatchListItemViewModel>`.
- `GetMatchDetailsAsync(tournamentId, matchId)` → `MatchDetailsViewModel` (statystyki per gracz, podział na drużyny).

Dane z `IMatchesRepository`. ViewModele: `MatchListItemViewModel`, `MatchDetailsViewModel`, `PlayerStatViewModel`.

---

## 11. Liga / Sezony (League)

> **Koncept (z pamięci):** Liga FACEIT-style — **trwałe** drużyny `LeagueTeam` (niezależne od jednego turnieju),
> symulowane wyniki, współdzielone tabele `ChatMessage`/`PlayerMatchStat` (nullable FK na sezon/mecz ligowy),
> bramka dostępu `IsLeagueAccess` (pakiet League/Ultra), cooldown na zmiany składu.

**`LeagueController`** (`ClashZone/Controllers/LeagueController.cs`):
- Publiczne/gracz: `Index` (sezony), `Details(id)`, `Register(seasonId, leagueTeamId)`,
  `Standings(id)`, `Matches(id)`, `MatchDetails(seasonId, id)`, `Bracket(id)`, `Participants(id)`, `Rules(id)`,
  `Chat`/`PostMessage`/`PostReportMessage`, `Schedule(matchId)` (GET/POST — propozycja terminu).
- Admin/zarządzanie: `Manage`, `Create`, `Edit`, `CloseRegistration`, `GenerateMatchups`,
  `GenerateWeek`, `GenerateResults`, `GeneratePlayoffs`, `SimulatePlayoffMatch`.

**`LeagueService`** (`ClashZone.App/Services/LeagueService.cs`) — `ILeagueService`. **Pełny cykl życia sezonu:**
- **Sezony:** `GetSeasonsAsync`, `GetSeasonDetailsAsync` (liczy zapisy, tabelę, czy user ma dostęp/zarejestrowany, kwalifikujące się drużyny), `CreateSeasonAsync`/`UpdateSeasonAsync`, `GetSeasonFormAsync`.
- **Rejestracja (`RegisterTeamAsync`):** walidacje — sezon istnieje / status `Registration` + okno dat / **wymaga pakietu ligi** (`IsLeagueAccess`) / tylko kapitan / nie zarejestrowany / roster ≥ `RosterSize`. Posiadanie pakietu = opłacone wpisowe (`FeePaid=true`).
- **`CloseRegistrationAndPruneAsync`** — usuwa drużyny bez opłaty/za małym rosterem, status → `RegularSeason`.
- **Generowanie meczów:** `GenerateOpeningMatchupsAsync` (tydzień 1, 2 mecze/drużynę losowo), `GenerateNextWeekAsync` (parowanie wg zbliżonego win-ratio — sąsiedzi w tabeli, unikanie powtórek przez `PairKey`), `BuildRoundPairs` (z obsługą bye).
- **Wyniki (`GenerateResultsAsync` / `PlayMatchAsync`):** symulacja wyniku (`GenerateScores`, CS2 0–15, LoL BO3) + statystyki rosteru (`GenerateRosterStatsAsync`, agregacja do `UserStat` — **wspólne rankingi z turniejami**).
- **Tabela (`GetStandingsAsync`):** zlicza W/L/played, sort po WinRatio → Wins → nazwa.
- **Play-off (`GeneratePlayoffsAsync`):** top `PlayoffSpots` z tabeli, seeding 1 vs N, bye dla najwyższego przy nieparzystej liczbie; status → `Playoffs`. `SimulatePlayoffMatchAsync` rozgrywa mecz i **automatycznie buduje kolejną rundę** gdy bieżąca kompletna; po finale → `AwardPlayoffPrizesAsync` + status `Completed`. `SimulateNextPlayoffMatchAsync` — kolejny nierozegrany.
- **Nagrody (`AwardPrizeAsync`):** jak w turniejach — parsuje "ClashCoins", kredytuje cały roster (`League:{seasonId}:{place}Place`).
- **Scheduling:** `GetScheduleAsync` / `ProposeScheduleAsync` — gracz drużyny proponuje termin + serwer; propozycja zostaje przyjęta jako `ScheduledTime`.
- **Czat ligi:** `GetChatAsync` / `PostMessageAsync` / `PostReportMessageAsync` (ogólny / drużynowy / zgłoszenia do adminów).
- **Szczegóły meczu:** `GetMatchDetailsAsync` (statystyki per gracz, podział na drużyny, avatar kapitana).

**`SeasonStatus`** (`ClashZone.App/Domains/SeasonStatus.cs`): `Registration=0`, `RegularSeason=1`, `Playoffs=2`, `Completed=3`.

**ViewModele ligi:** `LeagueViewModels.cs` (`SeasonDetailsViewModel`, `LeagueStandingRow`, `LeagueMatchesViewModel`,
`LeagueBracketViewModel`, `LeagueParticipantsViewModel`, `LeagueRulesViewModel`, `LeagueScheduleViewModel`,
`LeagueChatViewModel`, `LeagueMatchDetailsViewModel`, `LeagueRegistrationResult`), `SeasonFormViewModel`.

---

## 12. Drużyny ligowe (LeagueTeamService)

**`TeamController`** (`ClashZone/Controllers/TeamController.cs`) — `[Authorize]`:
- `Index` (moje drużyny), `Create` (GET/POST — walidacja gry i nazwy), `Details(id)` (generuje link zaproszenia z `JoinCode`), `Join(teamId, code)`.

**`LeagueTeamService`** (`ClashZone.App/Services/LeagueTeamService.cs`) — `ILeagueTeamService`:
- `CreateTeamAsync(name, game, captainId)` — tworzy trwałą drużynę (kapitan jest też członkiem).
- `GetUserTeamsAsync`, `GetTeamDetailsAsync`.
- `JoinByCodeAsync(teamId, code, userId)` — dołączenie po kodzie (z cooldownem zmian składu, zwraca `JoinLeagueTeamResult`).

**Encje:** `LeagueTeam` (Name, CaptainId, JoinCode, Game), `LeagueTeamMember` (LeagueTeamId, UserId, JoinedAt).
**ViewModele:** `TeamManagementViewModels.cs` (`TeamManagementViewModel`, `LeagueTeamFormViewModel`,
`LeagueTeamDetailsViewModel`, `JoinLeagueTeamResult`).

---

## 13. Subskrypcje + płatności Stripe

**`SubscriptionController`** (`ClashZone/Controllers/SubscriptionController.cs`) — `[Authorize]`:
`Index` (plany + aktywna subskrypcja), `Purchase(planId)` (ścieżka bez płatności — bezpośredni zapis).

**`PaymentController`** (`ClashZone/Controllers/PaymentController.cs`) — **Stripe Checkout**:
- `CreateCheckoutSession(planId)` — buduje `SessionCreateOptions` (karta, mode `payment`, cena `Price*100` w **EUR**), `SessionService.CreateAsync` → redirect na `session.Url`.
- `Success(planId)` — po płatności `CreateSubscriptionAsync(userId, planId)` → redirect do Subscription.
- `Cancel` — komunikat o anulowaniu.
- Klucz Stripe: `Stripe:SecretKey` (ustawiany w `Program.cs`); `StripeSettings` przez `IOptions`.

**`SubscriptionService`** (`ClashZone.App/Services/SubscriptionService.cs`) — `ISubscriptionService`:
`GetSubscriptionIndexViewModelAsync`, `PurchaseSubscriptionAsync`. Dane przez `ISubscriptionRepository`.

**Plany (`SubscriptionPlan`):** flagi `IsPremiumAccess` (dostęp do turniejów premium) i `IsLeagueAccess` (dostęp do ligi), `DurationDays`.
Seedowane: **Premium** (10€, premium), **League** (10€, liga), **Ultra** (18,99€, premium+liga). `UserSubscription` — aktywna subskrypcja użytkownika.
ViewModel: `SubscriptionIndexViewModel`.

---

## 14. Sklep ClashCoins (CoinShop) + portfel

**`CoinShopController`** (`ClashZone/Controllers/CoinShopController.cs`):
- `Index` (katalog + saldo via `ViewBag.Balance`), `Details(id)`, `Checkout(id)` (GET — formularz adresu), `Checkout(model)` (POST — zakup).

**`CoinShopService`** (`ClashZone.App/Services/CoinShopService.cs`) — `ICoinShopService`:
- `GetCatalogAsync` (tylko aktywne), `GetProductAsync`.
- **`PurchaseAsync(productId, userId, shipping)`** — pełna ścieżka zakupu:
  walidacje (adres / produkt aktywny / stan magazynowy / saldo ≥ cena) → transakcja `Spend` w portfelu →
  `ProductRedeem` (status `Pending` + adres wysyłki) → dekrementacja `Stock` → zapis. Zwraca `(bool ok, string? error)`.

**`CoinWalletService`** (`ClashZone.App/Services/CoinWalletService.cs`) — `ICoinWalletService`:
- `GetBalanceAsync(userId)`, `CreditAsync(userId, amount, reference)` (doładowanie, używane przez nagrody turniejów/ligi i seed).

**Encje:** `CoinWallet`, `CoinWalletTransaction` (Type `Credit`/`Spend`, Amount, Reference, opcjonalnie ProductId),
`Product` (ClashCoins=cena, Stock, IsActive, LimitedEdition, Category, Images), `ProductCategory`, `ProductImage` (IsPrimary).
ViewModele: `CheckoutViewModel`, `ShippingAddressDomain`.

---

## 15. Zamówienia (Orders) — realizacja nagród

**`OrdersController`** (`ClashZone/Controllers/OrdersController.cs`) — `[Authorize(Roles="Admin")]`, route `admin/orders`:
- `Index` — lista zamówień + nazwy kupujących (cache) + dane wysyłki.
- `UpdateStatus(id, status, notes)` — zmiana statusu (waliduje `OrderStatus.IsValid`).

**`OrderStatus`** (`ClashZone.App/Domains/OrderStatus.cs`): `Pending`, `Processing`, `Completed`, `Cancelled`
+ `Label` (PL) i `BadgeClass` (Bootstrap: secondary/warning/success/danger).

**Encja `ProductRedeem`** — UserId, ProductId, Status, RedeemedAt, Notes + pełny adres wysyłki (`Shipping*`).
ViewModel: `OrderListItemViewModel`.

---

## 16. Produkty (admin Products)

**`ProductsController`** (`ClashZone/Controllers/ProductsController.cs`) — `[Authorize(Roles="Admin")]`, route `admin/products`:
- `Index`, `Create` (GET/POST), `Edit` (GET/POST), `ConfirmDelete`.
- `SaveImageAsync` — upload zdjęcia do `wwwroot/images/products/{guid}.ext`.

**`ProductsService`** (`ClashZone.App/Services/ProductsService.cs`) — `IProductsService`:
`GetAllAsync`, `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` (z opcjonalnym `primaryImageUrl`).

---

## 17. Profil użytkownika (Profile)

**`ProfileController`** (`ClashZone/Controllers/ProfileController.cs`) — `[Authorize]`:
- `Index` — statystyki CS2 i LoL (`IPlayerStatsRepository`), ulubiony bohater LoL, subskrypcja, saldo portfela, kupione produkty.
- `Edit` (GET/POST) — edycja profilu (DisplayName, avatar `ProfilePicturePath`, hasło).
- `Orders` — historia zamówień użytkownika.
- **`LinkRiot(riotId)`** — powiązanie konta Riot: rozwiązuje `Nazwa#TAG` przez `IRiotApiClient.ResolveAccountAsync` → zapis `RiotId`+`RiotPuuid` (`SetRiotAccountAsync`).

ViewModele: `ProfileViewModel`, `EditProfileViewModel`.

---

## 18. Rankingi (Rankings)

**`RankingsController`** (`ClashZone/Controllers/RankingControllers.cs`):
- `Index(game, page, search)` — ranking per gra (domyślnie CS2), sort: LoL po `AverageKDA`, CS2 po `AverageKD`;
  wyszukiwanie po DisplayName/UserName; paginacja (10/stronę); dla LoL dokłada ulubionego bohatera.

Dane: `IPlayerStatsRepository.GetAllUserStatsAsync(game)`. `UserStat` liczy `AverageKD`/`AverageKDA`.
ViewModele: `RankingViewModel`, `RankingRowViewModel`.

---

## 19. Riot API — integracja LoL

> **Z pamięci:** LoL pobiera **realne** dane Match-V5 (CS2 zostaje losowe). Podejście dev-key
> (lookup ostatnich meczów), powiązanie konta przez `RiotPuuid`, wstrzyknięcie w `SimulateMatchAsync`.
> Wyłączone dopóki nie ustawiony `ApiKey`.

**Porty:** `ClashZone.App/Ports/Games/` — `IRiotApiClient`, `IRiotMatchService`.

**`RiotApiClient`** (`ClashZone.App/Services/Games/Riot/RiotApiClient.cs`) — `IRiotApiClient` (sealed, `HttpClient`):
- `IsConfigured` (czy ustawiony `ApiKey`), `ResolveAccountAsync(gameName, tagLine)` (Account-V1 → `RiotAccount`/PUUID),
  `GetRecentMatchIdsAsync(puuid, count)`, `GetMatchAsync(matchId)` (Match-V5).

**`RiotMatchService`** (`ClashZone.App/Services/Games/Riot/RiotMatchService.cs`) — `IRiotMatchService`:
- `ResolveMatchAsync(team1UserIds, team2UserIds, forceDevMode)` — zwraca `RiotMatchOutcome?` lub null gdy brak konfiguracji/danych.
  - **Tryb normalny:** zbiera PUUID graczy obu drużyn (`CollectPuuidsAsync`), bierze ostatnie mecze seed-gracza, szuka meczu z obiema drużynami (`BuildOutcome`) → wynik 1:0/0:1 + K/D/A + bohater per gracz.
  - **Tryb dev (`DevMode` lub `forceDevMode`/admin):** bierze jeden realny mecz seed-konta (`DevSeedRiotId` lub pierwszy powiązany), mapuje dwie strony meczu na drużyny ClashZone (`BuildDevOutcome`/`MapSide`).

**`RiotOptions`** (`...Riot/RiotOptions.cs`): `RegionalHost` (domyślnie `europe.api.riotgames.com`), `ApiKey`,
`DevMode`, `DevSeedRiotId`, `MatchLookbackCount`, `IsConfigured`. Sekcja `Riot` w configu.

**Modele Riot:** `ClashZone.App/Domains/Riot/RiotModels.cs` — `RiotAccount`, `RiotMatch`, `RiotMatchParticipant`,
`RiotMatchOutcome`, `RiotMatchPlayerStat`.

**Wpięcie:** `BracketService.SimulateMatchAsync` — dla `GameCatalog.IsLol(...)` woła `IRiotMatchService`,
zapisuje realne statystyki przez `PersistRiotPlayerStatsAsync`.

---

## 20. Czat (ChatService) + czat ligi

**`ChatService`** (`ClashZone.App/Services/ChatService.cs`) — `IChatService` (turnieje):
- `GetChatAsync(tournamentId, userId, isAdmin)` — czat ogólny + drużynowy + zgłoszenia (report).
- `PostMessageAsync(tournamentId, userId, message, toTeam)` — wiadomość ogólna lub drużynowa.
- `PostReportMessageAsync(...)` — zgłoszenie (widoczne dla adminów / własnej drużyny).

**Czat ligi** — analogicznie w `LeagueService` (`GetChatAsync`/`PostMessageAsync`/`PostReportMessageAsync`).

**Encja `ChatMessage`** — współdzielona: nullable FK `TournamentId` **lub** `SeasonId`, opcjonalny `TeamId`,
flaga `IsReport`, `Message`, `SentAt`. ViewModele: `ChatViewModel`.

---

## 21. E-mail (EmailService)

**`EmailService`** (`ClashZone.App/Services/EmailService.cs`) — `IEmailService`, **MailKit** (SMTP):
- `SendEmailAsync(to, subject, html)` — bazowa wysyłka.
- `SendTournamentRegistrationConfirmationAsync` — potwierdzenie zapisu na turniej.
- `SendMatchReminderAsync(to, tournament, opponent, scheduledAt)` — przypomnienie o meczu.
- `SendPromotionNotificationAsync(to, tournament, round)` — awans do kolejnej rundy.

Używane głównie przez `BracketService` (powiadomienia o drabince) i `AccountController` (reset hasła).

---

## 22. Administracja użytkownikami (Users / UserAdminService)

**`UsersController`** (`ClashZone/Controllers/UsersController.cs`) — `[Authorize(Roles="Admin")]`:
`Index`, `MakeOrganizer`, `MakeAdmin`, `RemoveOrganizer`, `RemoveAdmin`, `Ban`, `Unban`.

**`UserAdminService`** (`ClashZone.App/Services/UserAdminService.cs`) — `IUserAdminService`:
`GetAllAsync` (lista z rolami/statusem), `AddToRoleAsync`, `RemoveFromRoleAsync`, `BanAsync`, `UnbanAsync`.
Dane przez `IUserRepository`. ViewModel: `UserListItemVm`.

**Role** (`RoleSeeder`): `Admin`, `Organizer`, `Player`. Ban → `ClashUser.IsBanned` → blokada w `BannedAwareSignInManager`.

---

## 23. Seed (DbInitializer)

**`DbInitializer.InitializeAsync`** (`DataAccess/DbInitializer/DbInitializer.cs`) — uruchamiany w `Program.cs`.
Idempotentny (każdy seed bramkowany `if (!context.Set<...>().Any())`). Kolejność:
1. `Database.MigrateAsync()` + `RoleSeeder.SeedAsync` (role + admin).
2. **Turnieje:** "Summer CS2 Cup" (6 drużyn), "LoL 1v1 Duel", "LoL Riot API Test" (2 drużyny 5v5), "Spring CS2 Championship 2026" (8 drużyn, zakończony).
3. **Plany subskrypcji:** Premium / League / Ultra.
4. **Sezon ligi:** "ClashZone League – Sezon 1" (CS2, status Registration).
5. **Produkty sklepu:** słuchawki / mysz / klawiatura (kategoria "Peryferia").
6. **Drużyny turniejowe** (`SeedTournamentTeamsAsync`) — gracze `user1..userN` (hasło `User#123`, rola Player), kapitan = pierwszy członek, wszyscy `IsReady`, drużyny `IsConfirmed`.
7. **Drużyny ligowe** (`SeedLeagueTeamsAsync`) — 6 trwałych drużyn `user81..user110`, zarejestrowane z `FeePaid`.
8. **Rozegranie zakończonego turnieju** (`PlayOutTournamentAsync`) — pełna symulacja drabinki tą samą ścieżką co aplikacja (`IBracketService.SimulateMatchAsync`), żeby wypełnić rankingi/historię/nagrody. Bramkowane brakiem `UserStat`.
9. **Przykładowe zamówienia** (`SeedSampleOrdersAsync`) — doładowanie ClashCoins (`CreditAsync` 5000) + zakupy kanoniczną ścieżką (`ICoinShopService.PurchaseAsync`) z adresami wysyłki + zróżnicowanie statusów.

`EnsureUserAsync` — idempotentne tworzenie konta gracza.

---

## 24. Testy

**`ClashZone.Tests`** (xUnit + Moq + EF InMemory) — `TestSupport.cs` (helpery).
Pokrycie kontrolerów i serwisów, m.in.: `AccountControllerTests`, `BracketControllerTests`, `BracketServiceTests`,
`ChatServiceTests`, `CoinShopControllerTests`, `CoinShopServiceTests`, `LeagueServiceTests`, `LeagueTeamServiceTests`,
`MatchesServiceTests`, `OrdersControllerTests`, `PaymentControllerTests`, `Products*Tests`, `ProfileControllerTests`,
`RankingsControllerTests`, `RiotApiClientTests`, `RiotMatchServiceTests`, `Subscription*Tests`, `Tournament*Tests`,
`UserAdminServiceTests`, `UsersControllerTests`.

**`ClashZone.E2E.Tests`** (Playwright + NUnit) — realny browser przeciw **działającej apce + SQL Server**.
`Infrastructure/` (`E2ETestBase`, `WebAppFixture`), `Tests/` (`AccountTests`, `LeagueTests`, `NavigationTests`,
`SubscriptionTests`, `TournamentTests`). `IsTestProject=true`, nie odpalać na zwykłym buildzie CI.

---

## 25. Słownik encji (modele)

Encje EF: `DataAccess/Models/`. Modele domenowe: `ClashZone.App/Domains/*Domain.cs`.

| Encja | Kluczowe pola / sens |
|---|---|
| `ClashUser` | IdentityUser + DisplayName, Score, Awards, ProfilePicturePath, IsBanned, RiotId, RiotPuuid |
| `Tournament` | Name, GameTitle, StartDate, MaxParticipants, Format, First/Second/ThirdPlacePrize, IsPublic, IsPremium, JoinCode, Rules |
| `Team` | TournamentId, CaptainId, Name, JoinCode, **IsConfirmed** (turniejowa, jednorazowa) |
| `TeamMember` | TeamId, UserId, **IsReady** |
| `Match` | TournamentId, Team1Id/Team2Id (nullable), Team1Score/Team2Score, PlayedAt |
| `PlayerMatchStat` | MatchId **lub** LeagueMatchId (nullable), UserId, Kills/Deaths/Assists, Game, Champion |
| `UserStat` | UserId, Game, TotalKills/Deaths/Assists, MatchesPlayed → AverageKD / AverageKDA |
| `ChatMessage` | TournamentId **lub** SeasonId (nullable), TeamId?, UserId, Message, SentAt, IsReport |
| `SubscriptionPlan` | Name, Price, IsPremiumAccess, IsLeagueAccess, DurationDays |
| `UserSubscription` | UserId, PlanId, daty ważności |
| `Product` / `ProductCategory` / `ProductImage` | sklep: ClashCoins (cena), Stock, IsActive, LimitedEdition, Category, Images (IsPrimary) |
| `CoinWallet` / `CoinWalletTransaction` | portfel + transakcje (Credit/Spend, Amount, Reference, ProductId?) |
| `ProductRedeem` | zamówienie: UserId, ProductId, Status, RedeemedAt, Notes, Shipping* |
| `Season` | liga: Name, Game, Format, daty rejestracji/startu, RosterSize, PlayoffSpots, prizes, DefaultMatchDay, Status, Rules |
| `LeagueTeam` / `LeagueTeamMember` | **trwała** drużyna ligowa: Name, CaptainId, JoinCode, Game / member + JoinedAt |
| `LeagueRegistration` | zapis drużyny do sezonu: SeasonId, LeagueTeamId, RegisteredAt, FeePaid, IsRemoved |
| `LeagueMatch` | SeasonId, Week, Round?, IsPlayoff, Home/AwayRegistrationId?, Home/AwayScore, IsPlayed, ScheduledTime, ProposedTime, Server |
| `StripeSettings` | konfiguracja Stripe (klucze) |

---

*Wygenerowano jako mapa kodu pod szybkie wyszukiwanie. Przy zmianach w kodzie aktualizuj odpowiednią sekcję.*
