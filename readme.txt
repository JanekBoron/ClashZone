===============================================================================
 ClashZone — uruchomienie w nowym środowisku
===============================================================================

Platforma turniejowo-ligowa (CS2 / League of Legends): ASP.NET Core MVC (.NET 9),
Entity Framework Core + SQL Server, ASP.NET Identity, AutoMapper, Stripe, Riot API.

Poniżej krok po kroku, jak postawić aplikację na czystej maszynie.

Uwaga o ścieżkach: "katalog główny repozytorium" to folder zawierający podfoldery
projektów: ClashZone\, ClashZone.App\, DataAccess\, ClashZone.Tests\, ClashZone.E2E.Tests\.


-------------------------------------------------------------------------------
1. WYMAGANIA WSTĘPNE
-------------------------------------------------------------------------------

Zainstaluj:

  - .NET SDK 9.0           ->  https://dotnet.microsoft.com/download
                              sprawdzenie:  dotnet --version   (powinno być 9.x)

  - SQL Server (jedna z opcji):
      a) SQL Server Express  (domyślny connection string używa instancji SQLEXPRESS)
      b) SQL Server LocalDB  (najlżejszy do dev; instalowany z "Visual Studio" / "SQL Server Express LocalDB")
      c) SQL Server w Dockerze (zob. punkt 3, wariant C)

  - Git (do pobrania kodu)

Opcjonalnie:
  - Visual Studio 2022 / VS Code / Rider (do wygody, nie jest wymagane)
  - Narzędzie EF Core (tylko jeśli chcesz ręcznie zarządzać migracjami):
        dotnet tool install --global dotnet-ef


-------------------------------------------------------------------------------
2. POBRANIE KODU
-------------------------------------------------------------------------------

  git clone <adres-repozytorium>
  cd ClashZone            (wejdź do katalogu głównego repozytorium)


-------------------------------------------------------------------------------
3. KONFIGURACJA POŁĄCZENIA Z BAZĄ DANYCH
-------------------------------------------------------------------------------

Aplikacja czyta connection string "DefaultConnection" z pliku:
        ClashZone\appsettings.json

Domyślnie wskazuje on na lokalny SQL Server Express i bazę o nazwie CLASHZONE.
Dostosuj go do swojego środowiska — wybierz wariant:

  WARIANT A — SQL Server Express (wartość domyślna):
      Server=localhost\SQLEXPRESS;Database=CLASHZONE;User Id=sa;Password=<twoje_haslo>;TrustServerCertificate=true

  WARIANT B — SQL Server LocalDB (zalecane na maszynie dev):
      Server=(localdb)\MSSQLLocalDB;Database=CLASHZONE;Trusted_Connection=True;TrustServerCertificate=true

  WARIANT C — SQL Server w Dockerze:
      docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Your_strong_Pass1" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
      connection string:
      Server=localhost,1433;Database=CLASHZONE;User Id=sa;Password=Your_strong_Pass1;TrustServerCertificate=true

WAŻNE: bazy nie trzeba tworzyć ręcznie ani uruchamiać migracji.
Przy starcie aplikacja sama:
  - utworzy bazę i zastosuje wszystkie migracje EF (Database.MigrateAsync),
  - wypełni ją danymi startowymi (seed) — patrz punkt 7.
Wystarczy, że sam serwer SQL jest dostępny i connection string jest poprawny.

Nie chcesz zmieniać appsettings.json? Możesz nadpisać connection string zmienną
środowiskową (podwójne podkreślenie zastępuje dwukropek):
      Windows PowerShell:
          $env:ConnectionStrings__DefaultConnection = "Server=...;Database=CLASHZONE;..."
      Linux/macOS:
          export ConnectionStrings__DefaultConnection="Server=...;Database=CLASHZONE;..."


-------------------------------------------------------------------------------
4. KONFIGURACJA SEKRETÓW (opcjonalne, zależnie od tego co chcesz pokazać)
-------------------------------------------------------------------------------

W appsettings.json są sekcje na klucze zewnętrznych usług. Aplikacja URUCHOMI SIĘ
bez ich uzupełniania — po prostu dane funkcje będą nieaktywne:

  - "Stripe":"SecretKey"   -> płatności subskrypcji. Bez prawidłowego klucza
                              testowego ekran płatności Stripe nie zadziała.
  - "Riot":"ApiKey"        -> prawdziwe dane meczu League of Legends.
                              Pusty klucz = integracja Riot jest świadomie WYŁĄCZONA
                              (LoL zachowuje się jak "brak danych"), reszta działa.
  - "Smtp":{...}           -> wysyłka e-maili (potwierdzenie konta, reset hasła).
                              Bez działającego SMTP rejestracja zadziała, ale maile
                              nie dojdą.

BEZPIECZEŃSTWO: nie trzymaj prawdziwych kluczy/haseł w appsettings.json w repo.
Zalecany sposób na maszynie dev — User Secrets (UserSecretsId jest już w projekcie):

      cd ClashZone
      dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
      dotnet user-secrets set "Riot:ApiKey" "RGAPI-..."
      dotnet user-secrets set "Smtp:Password" "<haslo-aplikacji>"

albo zmiennymi środowiskowymi (np. Stripe__SecretKey, Riot__ApiKey, Smtp__Password).
Wartości z User Secrets / zmiennych środowiskowych nadpisują appsettings.json.


-------------------------------------------------------------------------------
5. PRZYWRÓCENIE ZALEŻNOŚCI I BUDOWA
-------------------------------------------------------------------------------

W katalogu głównym repozytorium:

      dotnet restore ClashZone\ClashZone.csproj
      dotnet build   ClashZone\ClashZone.csproj

(Repozytorium nie ma pliku .sln — operujemy na konkretnym projekcie.
 Budowa projektu web pociąga za sobą ClashZone.App i DataAccess.)


-------------------------------------------------------------------------------
6. (OPCJONALNIE) RĘCZNE ZASTOSOWANIE MIGRACJI
-------------------------------------------------------------------------------

Normalnie NIEPOTRZEBNE — migracje wykonują się automatycznie przy starcie.
Jeśli jednak chcesz przygotować bazę zawczasu:

      dotnet ef database update --project DataAccess\ClashZone.DataAccess.csproj --startup-project ClashZone\ClashZone.csproj


-------------------------------------------------------------------------------
7. URUCHOMIENIE APLIKACJI
-------------------------------------------------------------------------------

W katalogu głównym repozytorium:

      dotnet run --project ClashZone\ClashZone.csproj

Przy pierwszym starcie aplikacja utworzy bazę, zastosuje migracje i zaseeduje dane:
  - role: Player / Organizer / Admin oraz konto administratora,
  - graczy testowych user1..user110,
  - turnieje (zakończony "Spring CS2 Championship 2026", nadchodzący "Summer CS2 Cup",
    "LoL Riot API Test"), ligę z sezonem, sklep z produktami i przykładowe zamówienia.
Seed jest idempotentny — kolejne uruchomienia nie duplikują danych.

Adres w przeglądarce (profil "http" z Properties\launchSettings.json):
      http://localhost:5005
profil "https":
      https://localhost:7116

Uruchomienie przez "dotnet run" bez profilu startuje na domyślnym porcie Kestrela
(zwykle http://localhost:5000) — adres zobaczysz w logu "Now listening on: ...".


-------------------------------------------------------------------------------
8. KONTA TESTOWE (po seedzie)
-------------------------------------------------------------------------------

  Administrator:
      e-mail:  admin@clashzone.local
      hasło:   Admin#123

  Gracze:
      login/e-mail:  user1@clashzone.local ... user110@clashzone.local
      hasło:         User#123

  Panel admina: /Users (role, ban), /admin/products (produkty), /admin/orders (zamówienia).

UWAGA: to dane wyłącznie deweloperskie — zmień je przed jakimkolwiek wdrożeniem.


-------------------------------------------------------------------------------
9. (OPCJONALNIE) TESTY
-------------------------------------------------------------------------------

Testy jednostkowe (xUnit + Moq) — nie wymagają bazy ani działającej aplikacji:

      dotnet test ClashZone.Tests\ClashZone.Tests.csproj

Testy end-to-end (Playwright) — wymagają DZIAŁAJĄCEJ aplikacji oraz bazy SQL,
a także jednorazowej instalacji przeglądarek Playwright:

      dotnet build ClashZone.E2E.Tests\ClashZone.E2E.Tests.csproj
      pwsh ClashZone.E2E.Tests\bin\Debug\net9.0\playwright.ps1 install
      (następnie uruchom aplikację i odpal:)
      dotnet test ClashZone.E2E.Tests\ClashZone.E2E.Tests.csproj


-------------------------------------------------------------------------------
10. ROZWIĄZYWANIE PROBLEMÓW
-------------------------------------------------------------------------------

- "Cannot connect to SQL Server" / timeout przy starcie:
    * sprawdź, czy serwer SQL działa i czy nazwa instancji w connection stringu jest poprawna
      (SQLEXPRESS vs (localdb)\MSSQLLocalDB vs host,port),
    * dla logowania SQL (sa) upewnij się, że hasło i tryb "SQL Server Authentication" są włączone,
    * "TrustServerCertificate=true" jest już w stringu — potrzebne przy lokalnym certyfikacie.

- Ostrzeżenie/blokada HTTPS w przeglądarce (profil https) — zaufaj certyfikatowi dev:
        dotnet dev-certs https --trust

- Port zajęty: zmień port w Properties\launchSettings.json (applicationUrl) lub uruchom:
        dotnet run --project ClashZone\ClashZone.csproj --urls "http://localhost:5080"

- Chcesz zacząć bazę od zera (czysty seed): usuń bazę CLASHZONE w SQL Server
  i uruchom aplikację ponownie — utworzy się i zaseeduje na nowo.
  (Przykład w SQL: DROP DATABASE CLASHZONE;  — uwaga, kasuje wszystkie dane.)

- "dotnet ef" nie znaleziono: zainstaluj narzędzie  ->  dotnet tool install --global dotnet-ef

- Riot/LoL nie pobiera danych: to oczekiwane przy pustym "Riot:ApiKey"
  (integracja jest wtedy wyłączona). Uzupełnij klucz, by ją aktywować.


-------------------------------------------------------------------------------
 SKRÓT (TL;DR)
-------------------------------------------------------------------------------
  1) zainstaluj .NET 9 SDK + SQL Server
  2) ustaw connection string w ClashZone\appsettings.json
  3) dotnet run --project ClashZone\ClashZone.csproj
  4) otwórz http://localhost:5005  (lub adres z logu)
  5) zaloguj: admin@clashzone.local / Admin#123
===============================================================================
