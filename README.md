# DLS SemesterProject

Fire mikroservices, bygget op uge for uge:

- **ArticleService** (uge 1) - REST CRUD for artikler, x-axis + z-axis split. Se afsnittet [ArticleService](#articleservice) nedenfor.
- **CommentService** og **ProfanityService** (uge 2) - to fejl-isolerede services (swimlanes) der kommunikerer direkte med hinanden, med circuit breaker. Se afsnittet [CommentService & ProfanityService](#commentservice--profanityservice) nedenfor.

## ArticleService

En REST-baseret ArticleService med to former for horisontal skalering (AKF scale cube):

- **x-axis split**: tre identiske instanser af ArticleService bag en load balancer.
- **z-axis split**: ArticleDatabase er delt op efter kontinent - én database pr. kontinent, plus én global database til verdensnyheder. I alt 8 databaser.

### Arkitektur

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

`ArticlesController` udstiller de fire krævede CRUD-endpoints, plus ét ekstra "list"-endpoint som gør API'et nemmere at browser-teste:

| Handling | Endpoint |
|---|---|
| Create | `POST /api/articles` |
| Read   | `GET /api/articles/{continent}/{id}` |
| Update | `PUT /api/articles/{continent}/{id}` |
| Delete | `DELETE /api/articles/{continent}/{id}` |
| List (bonus) | `GET /api/articles/{continent}` |

`continent` er en del af URL'en for Read/Update/Delete/List, fordi et `id` kun er unikt *inden for* én kontinent-database - ikke på tværs af alle otte. Man er derfor nødt til at fortælle serviceren hvilken database den skal kigge i.

> Bemærk: `GET /api/articles` (uden continent) findes ikke og giver `405 Method Not Allowed`, hvis man taster URL'en direkte i browseren - browseren sender GET, men den adresse er kun mappet til `POST` (Create). Brug `GET /api/articles/{continent}` for at liste artikler.

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
    DatabaseInitializer.cs            opretter skema + seed-data i alle 8 databaser
  Program.cs                          app-opstart + DI-opsætning
docker-compose.yml                    alle 12 containere (8 DB + 3 service + 1 load balancer)
nginx.conf                            load balancer-konfiguration
ArticleService.http                   færdige test-kald til Create/Read/Update/Delete
```

### Sådan kører du det

Kræver kun Docker (ingen lokal .NET SDK nødvendig - byggeriet sker inde i Docker).

```bash
docker compose up --build
```

Det starter alle 12 containere: 8 SQL Server-databaser, en engangs-initializer der opretter skemaet, de tre ArticleService-instanser, og nginx som load balancer på port **8080**.

Første opstart tager typisk et minuts tid, fordi SQL Server-containerne skal nå at blive klar, før initializeren kan oprette skemaet.

Initializeren opretter samtidig 5 små test-artikler ("Artikel 1" - "Artikel 5"), spredt ud over Europe, Asia, Africa, NorthAmerica og Global, så der er noget at kigge på med det samme. Seedningen sker kun hvis en databases tabel er tom, så den bliver ikke duplikeret ved genstart.

For at lukke alt ned igen (inkl. data i databaserne):

```bash
docker compose down -v
```

### Sådan tester du det

Nemmest: åbn `ArticleService.http` i Rider (eller VS Code med REST Client-extension) og tryk "Run" over hvert kald - så slipper du for shell-syntaks helt.

Vil du hellere bruge terminalen, så vær opmærksom på at **bash og PowerShell har forskellig syntaks** - `curl` i PowerShell er faktisk et alias for `Invoke-WebRequest`, som ikke forstår `-X`/`-H`/`-d`, og `\` er ikke linjefortsættelse i PowerShell (det er backtick `` ` ``).

**Git Bash / macOS / Linux:**
```bash
# Create
curl -X POST http://localhost:8080/api/articles \
  -H "Content-Type: application/json" \
  -d '{"title":"Første artikel","content":"Hej fra ArticleService","author":"John","continent":"Europe"}'

# List (viser bl.a. de seedede test-artikler)
curl http://localhost:8080/api/articles/Europe

# Read (brug det id du fik tilbage fra Create eller List)
curl http://localhost:8080/api/articles/Europe/1

# Update
curl -X PUT http://localhost:8080/api/articles/Europe/1 \
  -H "Content-Type: application/json" \
  -d '{"title":"Opdateret titel","content":"Ny tekst","author":"John"}'

# Delete
curl -X DELETE http://localhost:8080/api/articles/Europe/1
```

**PowerShell (Windows-terminalen):** brug enten `curl.exe` (den rigtige curl, ikke aliaset) med backtick til linjefortsættelse, eller `Invoke-RestMethod`:

```powershell
# Create
Invoke-RestMethod -Uri http://localhost:8080/api/articles -Method Post `
  -ContentType "application/json" `
  -Body '{"title":"Foerste artikel","content":"Hej fra ArticleService","author":"John","continent":"Europe"}'

# List
Invoke-RestMethod -Uri http://localhost:8080/api/articles/Europe

# Read
Invoke-RestMethod -Uri http://localhost:8080/api/articles/Europe/1

# Update
Invoke-RestMethod -Uri http://localhost:8080/api/articles/Europe/1 -Method Put `
  -ContentType "application/json" `
  -Body '{"title":"Opdateret titel","content":"Ny tekst","author":"John"}'

# Delete
Invoke-RestMethod -Uri http://localhost:8080/api/articles/Europe/1 -Method Delete
```

Der er også Swagger UI tilgængeligt fra hver enkelt instans (ikke gennem load balanceren) når `ASPNETCORE_ENVIRONMENT=Development`, f.eks. hvis du kører én instans direkte med `dotnet run` under `ArticleService/`.

#### Verificér x-axis split

Se hvilken instans der svarede på hvert kald i load balancer-loggen:

```bash
docker logs dls_semesterproject-load-balancer-1
```

Loglinjerne viser `-> <ip>:80` for den instans der besvarede requesten - lav flere kald og se at IP'en skifter round-robin mellem de tre instanser.

#### Verificér z-axis split

Slå direkte op i én af databasecontainerne og se at kun det rigtige kontinents artikler ligger der:

```bash
docker exec dls_semesterproject-article-db-europe-1 \
  /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P 'ArticleService_Dev7!' \
  -d ArticleDatabase -Q "SELECT * FROM Articles;"
```

## CommentService & ProfanityService

To selvstændige services: **CommentService** gemmer kommentarer til artikler, **ProfanityService**
tjekker tekst for bandeord. De kommunikerer **direkte** med hinanden over HTTP - ingen gateway
eller UI sidder imellem - og er hver især **fejl-isoleret (swimlanes)**: egen database, egen
container, egen connection pool. CommentService har desuden en **circuit breaker** der tager over,
hvis ProfanityService ikke svarer.

### Arkitektur

```
 klient ──HTTP──► CommentService ──HTTP (direkte, ingen gateway)──► ProfanityService
                        │                                                 │
                  CommentDatabase                                ProfanityDatabase
```

Der er bevidst **ingen** pil fra klienten til ProfanityService, og ingen gateway/UI mellem de to
services - CommentService kalder ProfanityService direkte, sådan som opgavebeskrivelsens
illustration viser det.

### Fault isolation - swimlanes

De to services er isoleret fra hinanden på flere niveauer, så en fejl i den ene ikke synker den anden:

- **Egne databaser**: `CommentDatabase` og `ProfanityDatabase` er to forskellige SQL Server-containere
  (`comment-db` / `profanity-db`) med hver sin volume og hver sit login. Ingen service kan læse eller
  skrive i den andens database.
- **Egne containere**: `comment-service` og `profanity-service` er separate Docker-images/processer,
  der kan bygges, deployes og genstartes uafhængigt af hinanden - se `docker-compose.yml`, hvor
  `comment-service` bevidst **ikke** har `depends_on: profanity-service`. De er ikke bundet sammen af
  opstartsrækkefølge; CommentService skal virke uanset om ProfanityService er oppe.
- **Egen "swimlane" af ressourcer i CommentService**: kaldet til ProfanityService går gennem sin
  helt egen, navngivne `HttpClient` (se `CommentService/Program.cs`, `AddHttpClient<IProfanityServiceClient, ...>`)
  med sin egen connection pool og sin egen korte timeout (3s). Det er selve pointen med en swimlane:
  hvis ProfanityService bliver langsom eller dør, er det kun *den* ressource-pulje der rammes - det
  sluger ikke forbindelser eller tråde som CommentService ellers skulle bruge til sin egen database
  eller til at besvare andre requests.

### Circuit breaker - `CommentService/Program.cs` + `Clients/ProfanityServiceClient.cs`

`CommentService`'s HttpClient til ProfanityService har en Polly circuit breaker koblet på:

- Efter **3 fejlende kald i træk** (timeout, connection refused, eller 5xx-svar) **åbner** kredsløbet.
- I **30 sekunder** derefter fejler ethvert kald til ProfanityService **med det samme** - uden
  overhovedet at forsøge at ramme netværket - i stedet for at vente timeout'en ud hver gang.
- Derefter går kredsløbet i **half-open** og slipper ét prøve-kald igennem; lykkes det, **lukker**
  kredsløbet igen og kald går som normalt; fejler det, åbner det igen i 30 sekunder mere.

Dette er hvor circuit breakeren "tager over": `ProfanityServiceClient.CheckAsync` fanger den
resulterende `BrokenCircuitException` (og alm. netværksfejl) og falder tilbage til at lade
kommentaren igennem **utjekket** i stedet for at fejle requesten - markeret med
`Comment.ProfanityChecked = false`, så man bagefter kan se hvilke kommentarer der aldrig blev
verificeret. En nede-periode for ProfanityService degraderer altså moderationen, den tager ikke
CommentService ned med sig.

### REST API'erne

**CommentService** (`CommentsController`):

| Handling | Endpoint |
|---|---|
| Create | `POST /api/comments` |
| List for artikel | `GET /api/comments/article/{articleId}` |
| Read | `GET /api/comments/{id}` |
| Delete | `DELETE /api/comments/{id}` |

`POST /api/comments` kalder ProfanityService, før kommentaren gemmes. Indeholder teksten et
bandeord (og ProfanityService kunne rent faktisk kontaktes), afvises kaldet med `400 Bad Request`.

**ProfanityService** (`ProfanityController`) - det interne API CommentService taler direkte med:

| Handling | Endpoint |
|---|---|
| Tjek tekst for bandeord | `POST /api/profanity/check` |
| List bandeordsliste (bonus/demo) | `GET /api/profanity/words` |
| Tilføj bandeord (bonus/demo) | `POST /api/profanity/words` |

`ProfanityDatabase` seedes ved opstart med et lille demo-ordforråd: `idiot`, `stupid`, `dumb`, `moron`.

### Sådan kører og tester du det

Indgår i samme `docker-compose.yml` som ArticleService:

```bash
docker compose up --build
```

- CommentService: **http://localhost:8081** (Swagger på `/swagger` når man kører den lokalt med `dotnet run`)
- ProfanityService: **http://localhost:8082**

Brug `CommentService.http` og `ProfanityService.http` til at teste kaldene enkeltvis (åbn i Rider/VS Code, tryk "Run").

#### Demonstrér circuit breakeren

```bash
# 1. Stop ProfanityService, men lad CommentService og dens database køre videre
docker compose stop profanity-service

# 2. Post et par kommentarer i træk (fx via CommentService.http, eller curl):
curl -X POST http://localhost:8081/api/comments \
  -H "Content-Type: application/json" \
  -d '{"articleId":1,"author":"John","text":"Endnu en kommentar"}'
```

De første kald venter hver ~3 sekunder på timeout'en, mens de tæller op mod bruddet; se i
CommentService-loggen (`docker logs dls_semesterproject-comment-service-1`) hvordan der efter det
3. fejlede kald logges `ProfanityService circuit breaker OPEN...`, hvorefter efterfølgende kald
besvares **øjeblikkeligt** med kommentaren gemt og `"profanityChecked": false`. Kør
`docker compose start profanity-service` igen, vent 30 sekunder (breakerens `durationOfBreak`), og se
`HALF-OPEN` og til sidst `CLOSED` i loggen, mens kommentarer igen bliver tjekket for bandeord.

### Projektstruktur

```
CommentService/
  Controllers/CommentsController.cs         REST-endpoints
  Models/Comment.cs, Dtos/                   domænemodel + request-objekt
  Clients/
    IProfanityServiceClient.cs / ProfanityServiceClient.cs   direkte HTTP-kald + fallback
    Dtos/                                    wire-format til ProfanityService
  Data/
    CommentDbContext.cs, ICommentRepository.cs / CommentRepository.cs
    DatabaseInitializer.cs
  Program.cs                                 DI-opsætning + circuit breaker-policy
ProfanityService/
  Controllers/ProfanityController.cs         REST-endpoints
  Models/BannedWord.cs, Dtos/
  Data/
    ProfanityDbContext.cs, IBannedWordRepository.cs / BannedWordRepository.cs
    ProfanityChecker.cs                      selve match-logikken
    DatabaseInitializer.cs                   skema + seed-ordliste
  Program.cs
CommentService.http / ProfanityService.http  færdige test-kald
```
