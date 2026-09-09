# ArticleService

En REST-baseret ArticleService med to former for horisontal skalering (AKF scale cube):

- **x-axis split**: tre identiske instanser af ArticleService bag en load balancer.
- **z-axis split**: ArticleDatabase er delt op efter kontinent - én database pr. kontinent, plus én global database til verdensnyheder. I alt 8 databaser.

## Arkitektur

```
                         ┌────────────────────┐
 klient ──HTTP──► nginx  │  load balancer      │  (x-axis split, round robin)
                         └────────┬────────────┘
                    ┌─────────────┼─────────────┐
                    ▼             ▼             ▼
           article-service-1  -service-2   -service-3   (identiske instanser)
                    │             │             │
                    └─────────────┴─────────────┘
                                  │
              vælger database ud fra "continent" i requesten
                                  │
   ┌────────┬─────────┬────────┬────────┬───────────┬───────────┬─────────┬────────┐
   ▼        ▼         ▼        ▼        ▼           ▼           ▼         ▼
 Africa  Antarctica  Asia   Europe  NorthAmerica SouthAmerica Oceania   Global
                        (z-axis split, én database pr. kontinent)
```

### REST API'et

`ArticlesController` udstiller fire endpoints:

| Handling | Endpoint |
|---|---|
| Create | `POST /api/articles` |
| Read   | `GET /api/articles/{continent}/{id}` |
| Update | `PUT /api/articles/{continent}/{id}` |
| Delete | `DELETE /api/articles/{continent}/{id}` |

`continent` er en del af URL'en for Read/Update/Delete, fordi et `id` kun er unikt *inden for* én kontinent-database - ikke på tværs af alle otte. Man er derfor nødt til at fortælle serviceren hvilken database den skal kigge i.

### x-axis split - `docker-compose.yml` + `nginx.conf`

`article-service-1/2/3` er tre containere bygget af nøjagtig samme image/kode. `nginx.conf` samler dem i en `upstream`-gruppe og fordeler indkommende requests round-robin mellem dem. Klienter rammer altid nginx på port 8080, aldrig en instans direkte.

### z-axis split - `Data/ShardRouter.cs` + `Data/ArticleRepository.cs`

- `ShardRouter` bygger ved opstart én connection string pr. `Continent`-enum-værdi, ud fra `Shards:*`-værdierne i `docker-compose.yml`.
- `ArticleRepository` spørger altid routeren "hvilken database hører dette kontinent til?", åbner en kortlivet `ArticleDbContext` mod netop den database, og udfører sit arbejde der. Ingen enkelt request rører mere end én database.
- `database-initializer` er en ekstra container der kører **én gang**, opretter tabellen i alle 8 databaser, og lukker ned igen - før de tre rigtige instanser starter. Det forhindrer at de tre instanser kan komme til at oprette samme database samtidig.

### Projektstruktur

```
ArticleService/
  Controllers/ArticlesController.cs   REST-endpoints
  Models/Article.cs, Continent.cs     domænemodel
  Models/Dtos/                        request-objekter (Create/UpdateArticleRequest)
  Data/
    ArticleDbContext.cs               EF Core-gateway til én database
    IShardRouter.cs / ShardRouter.cs  kontinent -> connection string
    IArticleRepository.cs / ArticleRepository.cs   CRUD-operationer
    DatabaseInitializer.cs            opretter skema i alle 8 databaser
  Program.cs                          app-opstart + DI-opsætning
docker-compose.yml                    alle 12 containere (8 DB + 3 service + 1 load balancer)
nginx.conf                            load balancer-konfiguration
ArticleService.http                   færdige test-kald til Create/Read/Update/Delete
```

## Sådan kører du det

Kræver kun Docker (ingen lokal .NET SDK nødvendig - byggeriet sker inde i Docker).

```bash
docker compose up --build
```

Det starter alle 12 containere: 8 SQL Server-databaser, en engangs-initializer der opretter skemaet, de tre ArticleService-instanser, og nginx som load balancer på port **8080**.

Første opstart tager typisk et minuts tid, fordi SQL Server-containerne skal nå at blive klar, før initializeren kan oprette skemaet.

For at lukke alt ned igen (inkl. data i databaserne):

```bash
docker compose down -v
```

## Sådan tester du det

Åbn `ArticleService.http` i Rider (eller VS Code med REST Client-extension) og tryk "Run" over hvert kald - eller brug curl:

```bash
# Create
curl -X POST http://localhost:8080/api/articles \
  -H "Content-Type: application/json" \
  -d '{"title":"Første artikel","content":"Hej fra ArticleService","author":"John","continent":"Europe"}'

# Read (brug det id du fik tilbage fra Create)
curl http://localhost:8080/api/articles/Europe/1

# Update
curl -X PUT http://localhost:8080/api/articles/Europe/1 \
  -H "Content-Type: application/json" \
  -d '{"title":"Opdateret titel","content":"Ny tekst","author":"John"}'

# Delete
curl -X DELETE http://localhost:8080/api/articles/Europe/1
```

Der er også Swagger UI tilgængeligt fra hver enkelt instans (ikke gennem load balanceren) når `ASPNETCORE_ENVIRONMENT=Development`, f.eks. hvis du kører én instans direkte med `dotnet run` under `ArticleService/`.

### Verificér x-axis split

Se hvilken instans der svarede på hvert kald i load balancer-loggen:

```bash
docker logs dls_semesterproject-load-balancer-1
```

Loglinjerne viser `-> <ip>:80` for den instans der besvarede requesten - lav flere kald og se at IP'en skifter round-robin mellem de tre instanser.

### Verificér z-axis split

Slå direkte op i én af databasecontainerne og se at kun det rigtige kontinents artikler ligger der:

```bash
docker exec dls_semesterproject-article-db-europe-1 \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'ArticleService_Dev7!' \
  -d ArticleDatabase -Q "SELECT * FROM Articles;"
```
