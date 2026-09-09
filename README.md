# Happy Headlines — ArticleService (C# / ASP.NET Core)

This project implements the ArticleService and ArticleDatabase part of the Happy Headlines assignment.
It follows the general technology/style of the supplied reference project — ASP.NET Core controllers,
Microsoft.Data.SqlClient, Docker and Docker Compose — while using new ArticleService-specific application logic.

## Requirements implemented

- REST-based ArticleService in **C# / ASP.NET Core (.NET 7)**
- Four CRUD operations: Create, Read, Update and Delete
- **X-axis split:** three identical stateless ArticleService containers
- **Nginx load balancer** in front of those three API replicas
- **Z-axis split:** eight separate Microsoft SQL database containers
  - Africa
  - Antarctica
  - Asia
  - Europe
  - North America
  - South America
  - Oceania
  - Global
- The region in the URL is the shard key

## Important startup design

Database/schema initialization is performed by **one dedicated one-shot container** named:

```text
database-initializer
```

It creates the `ArticleDatabase` and `dbo.Articles` table in each of the eight SQL shards and then exits.
Only after it exits successfully are these containers started:

```text
article-service-1
article-service-2
article-service-3
```

This is intentional. Running database creation as an `IHostedService` inside all three API replicas would make
all three replicas execute `CREATE DATABASE` at the same time. SQL Server can deadlock those concurrent system-
database operations and choose one or more API processes as deadlock victims. Once those API containers exit,
Nginx reports `host is unreachable`, `no live upstreams`, and HTTP 502 responses.

## Project structure

```text
ArticleService.sln
ArticleService/
  Controllers/
    ArticlesController.cs
  Configuration/
    ArticleRegions.cs
  Data/
    ArticleRepository.cs
    DatabaseInitializer.cs
    IArticleRepository.cs
    IShardConnectionFactory.cs
    ShardConnectionFactory.cs
  Models/
    Article.cs
    CreateArticleRequest.cs
    UpdateArticleRequest.cs
  Program.cs
  ArticleService.csproj
  Dockerfile
docker-compose.yml
nginx.conf
HappyHeadlines.http
```

## Run with Docker

If you previously ran an older version of this project, stop those containers first so Nginx does not retain
upstream containers from the failed run:

```bash
docker compose down --remove-orphans
```

You normally do **not** need to delete the database volumes.

Then rebuild and start:

```bash
docker compose up --build
```

A healthy startup should look roughly like this:

```text
database-initializer  ... Article shard europe is ready.
database-initializer  ... All ArticleDatabase shards are ready.
database-initializer exited with code 0
article-service-1     ... Now listening on: http://[::]:80
article-service-2     ... Now listening on: http://[::]:80
article-service-3     ... Now listening on: http://[::]:80
load-balancer         ...
```

The API is exposed through Nginx at:

```text
http://localhost:8080
```

Opening that URL redirects to Swagger:

```text
http://localhost:8080/swagger
```

## Useful checks

Show the container state:

```bash
docker compose ps -a
```

The expected state is:

- `database-initializer`: `Exited (0)`
- three `article-service-*` containers: `Up`
- `load-balancer`: `Up`
- eight `article-db-*` containers: `Up`

If initialization fails, inspect only that process first:

```bash
docker compose logs database-initializer
```

Then check an API replica:

```bash
docker compose logs article-service-1
```

Test the load balancer:

```bash
curl -i http://localhost:8080/health
```

## REST endpoints

The region is part of the route so the service can immediately choose the correct physical database.

```text
POST   /articles/{region}
GET    /articles/{region}/{id}
PUT    /articles/{region}/{id}
DELETE /articles/{region}/{id}
```

Valid regions:

```text
africa
antarctica
asia
europe
north-america
south-america
oceania
global
```

### Create

```bash
curl -i -X POST http://localhost:8080/articles/europe \
  -H "Content-Type: application/json" \
  -d '{
    "title": "A happy story",
    "body": "Something positive happened.",
    "author": "Happy Headlines"
  }'
```

Copy the returned `id`.

### Read

```bash
curl -i http://localhost:8080/articles/europe/ARTICLE_ID
```

### Update

```bash
curl -i -X PUT http://localhost:8080/articles/europe/ARTICLE_ID \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Updated happy story",
    "body": "Even more positive details were added.",
    "author": "Happy Headlines"
  }'
```

### Delete

```bash
curl -i -X DELETE http://localhost:8080/articles/europe/ARTICLE_ID
```

## Demonstrating the x-axis split

Every ArticleService replica adds this response header:

```text
X-Article-Service-Instance: article-service-1
```

Repeated requests through `localhost:8080` are distributed by Nginx between:

```text
article-service-1
article-service-2
article-service-3
```

For PowerShell:

```powershell
1..9 | ForEach-Object {
    (Invoke-WebRequest http://localhost:8080/health).Headers["X-Article-Service-Instance"]
}
```

For a Unix-style shell:

```bash
for i in 1 2 3 4 5 6 7 8 9; do
  curl -s -D - http://localhost:8080/health -o /dev/null \
    | grep X-Article-Service-Instance
done
```

## Demonstrating the z-axis split

The URL region is the database shard key.

```text
/articles/europe/...        -> article-db-europe
/articles/asia/...          -> article-db-asia
/articles/global/...        -> article-db-global
```

The service therefore does **not** search all eight databases for normal CRUD operations.
The caller supplies the article region, which lets ArticleService select the correct shard directly.

An article does not change shards in `PUT`. Moving an article to a different region should be handled as a
separate migration operation (or delete + create), because changing the shard key is a data-movement concern.

## How this maps to C4

At C4 Container level, ArticleService is still **one logical container**. The three copies are deployment
instances of that same container. Likewise, Article Database is one logical data responsibility that is
physically partitioned into eight shard databases for the z-axis split.

```text
Website / NewsletterService
          |
          v
    Nginx Load Balancer
       /    |    \
      v     v     v
     AS1   AS2   AS3
       \    |    /
        shard routing
  / / / / / / / /
 8 physical SQL databases
```
