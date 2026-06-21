# ClashZone — uruchomienie w Dockerze

Aplikacja ASP.NET Core (.NET 9) + baza Microsoft SQL Server, uruchamiane przez Docker Compose.

## Wymagania
- Docker Desktop (Windows/macOS) lub Docker Engine + plugin Compose (Linux)
- Połączenie z internetem przy pierwszym uruchomieniu (pobranie obrazów bazowych)

## Uruchomienie
W katalogu z plikiem `docker-compose.yml`:

```bash
docker compose up --build
```

Pierwszy start trwa dłużej (pobranie obrazów + budowa aplikacji + utworzenie bazy
przez DbInitializer). Gdy w logach pojawi się `Now listening on: http://[::]:8080`,
aplikacja jest gotowa.

Otwórz w przeglądarce: **http://localhost:8080**

## Zatrzymanie
```bash
docker compose down            # zatrzymuje kontenery (dane bazy zostają)
docker compose down -v         # zatrzymuje i KASUJE dane bazy (czysty start)
```

## Najczęstsze problemy
- **`web` zakończył się błędem przy pierwszym starcie** — baza nie zdążyła wstać.
  Uruchom ponownie: `docker compose up` (kontener `web` połączy się już z gotową bazą).
- **Port 8080 lub 1433 zajęty** — zmień mapowanie portów w `docker-compose.yml`
  (np. `"8081:8080"`).

## Dane / konfiguracja
- Hasło SA bazy oraz connection string są ustawione w `docker-compose.yml`
  (sekcja `environment`). To dane przykładowe do uruchomienia lokalnego.
