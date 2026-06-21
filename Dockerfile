# ---- Etap 1: build i publish ----
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Najpierw same .csproj -> lepszy cache warstwy restore
COPY ClashZone/ClashZone.csproj ClashZone/
COPY ClashZone.App/ClashZone.App.csproj ClashZone.App/
COPY DataAccess/ClashZone.DataAccess.csproj DataAccess/
RUN dotnet restore ClashZone/ClashZone.csproj

# Reszta kodu i publikacja
COPY ClashZone/ ClashZone/
COPY ClashZone.App/ ClashZone.App/
COPY DataAccess/ DataAccess/
RUN dotnet publish ClashZone/ClashZone.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Etap 2: obraz runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Aplikacja słucha po HTTP na 8080 wewnątrz kontenera
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "ClashZone.dll"]
