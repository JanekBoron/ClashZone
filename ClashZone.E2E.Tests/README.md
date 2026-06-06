# ClashZone.E2E.Tests

Testy end-to-end napędzające prawdziwą przeglądarkę (Playwright + Chromium) przeciwko
realnie uruchomionej aplikacji ClashZone i prawdziwemu SQL Serverowi.

## Jak to działa

- `Infrastructure/WebAppFixture.cs` (`[SetUpFixture]`) startuje aplikację webową raz na
  całą sesję (`dotnet run` projektu `ClashZone`), wystawia ją pod `http://127.0.0.1:5099`
  i zatrzymuje po testach. Aplikacja celuje w **osobną bazę `CLASHZONE_E2E`**, więc testy
  nie dotykają danych deweloperskich. Migracje i seed (admin, przykładowe turnieje, plany
  subskrypcji) wykonuje sama aplikacja przy starcie (`DbInitializer`).
- `Infrastructure/E2ETestBase.cs` – baza testów (Playwright `PageTest`): bazowy adres,
  pomocnicze przepływy `RegisterAsync` / `LoginAsync` / `LogoutAsync`, generowanie
  unikalnych danych oraz znane konto admina z seedu.
- `Tests/` – właściwe scenariusze: konto, turnieje, nawigacja.

## Wymagania

1. **SQL Server** dostępny pod connection stringiem z `WebAppFixture` (domyślnie
   `localhost\SQLEXPRESS`, `sa`/`123`). Można nadpisać zmienną środowiskową:
   ```powershell
   $env:CLASHZONE_E2E_CONNECTION = "Server=...;Database=CLASHZONE_E2E;..."
   ```
2. **Przeglądarki Playwright** zainstalowane raz po pierwszym buildzie:
   ```powershell
   pwsh bin/Debug/net9.0/playwright.ps1 install chromium
   ```
   (lub `powershell -File bin/Debug/net9.0/playwright.ps1 install chromium`)

## Uruchamianie

```powershell
dotnet test ClashZone.E2E.Tests/ClashZone.E2E.Tests.csproj
```

Domyślnie Chromium działa w trybie headless. Aby zobaczyć przeglądarkę:

```powershell
$env:HEADED = "1"
dotnet test ClashZone.E2E.Tests/ClashZone.E2E.Tests.csproj -- Playwright.LaunchOptions.Headless=false
```

## Uwagi

- Jeśli aplikacja jest już uruchomiona pod `http://127.0.0.1:5099`, fixture ją wykryje
  i użyje istniejącej instancji (przydatne przy debugowaniu pojedynczego testu).
- Rejestracja w aplikacji nie loguje automatycznie – testy rejestrują, a potem logują się
  jawnie (zgodnie z zachowaniem `AccountController`).
- Przyciski wybieramy po nazwie (np. „Zaloguj", „Opublikuj"), bo navbar zalogowanego
  użytkownika zawiera dodatkowy `button[type=submit]` („Log off").
