# Happy Headlines — Observability in one Seq instance

**Architecture:** all custom services send structured **logs** (Serilog) and
OpenTelemetry **traces** and **metrics** to Seq, including ArticleCacheWorker.
The old Zipkin collector and CacheDashboard webapp (port 7000) are removed.
Console output remains a local emergency fallback, not a separate indexed log server.

## 1. Start and access Seq

From the repository root:

```bash
cd HappyHeadlines
cp .env.example .env
# Edit .env and choose a strong value for SEQ_ADMIN_PASSWORD.
docker compose up --build -d
```

Open **http://localhost:5342**, log in as `admin` using the password from `.env`.
A named volume (`rps_seq_data`) persists Seq events and traces.

**Important for an existing installation:** `SEQ_FIRSTRUN_ADMINPASSWORD`
applies only when Seq initializes an *empty* data volume. If you previously ran
Seq with `SEQ_FIRSTRUN_NOAUTHENTICATION`, changing the environment variable
will not enable authentication on an existing volume. Enable authentication
in Seq Settings > Users. Do **not** delete the existing volume unless you
want to permanently delete its data.

The UI port is bound only to host loopback, `127.0.0.1:5342:80`.
Services send telemetry to Seq's **ingestion-only** Docker port, `seq:5341`.
This is a development configuration; production needs TLS, network restrictions,
secret management, retention policies, and backup/recovery procedures.

## 2. How the signals reach Seq

| Signal | Library | Destination from inside Docker |
|---|---|---|
| Structured logs | `ILogger` → Serilog Seq sink | `http://seq:5341` |
| Distributed traces | OpenTelemetry HTTP/protobuf | `http://seq:5341/ingest/otlp/v1/traces` |
| Metrics | OpenTelemetry HTTP/protobuf | `http://seq:5341/ingest/otlp/v1/metrics` |

A single shared `Monitoring` library configures the three signals. Each service
calls `builder.AddMonitoring("ServiceName")`; HTTP services also call
`app.UseHappyHeadlinesRequestLogging()`. The service name, environment and
W3C trace/span identifiers make events filterable and correlate logs with spans.

The optional `SEQ_API_KEY` can be placed in `HappyHeadlines/.env`. The Compose
file passes it to all applications as `Observability__SeqApiKey`. To enforce
authenticated ingestion, create an **ingest-only** key through Seq **Data > Ingestion**,
turn on requiring API keys, add it to `.env`, and restart the application containers.
Avoid putting API keys in checked-in configuration files.

## 3. Take screenshots for the assignment

Start the system, then run the smoke script from `HappyHeadlines/`:

```bash
bash ../scripts/observability-smoke.sh
```

### Screenshot A — Structured logging (Seq Events)

Filter `Service = 'PublisherService'`, then choose an event such as
`Queued article {ArticleId} for continent {Continent}`. Expand structured fields:
`ArticleId`, `Continent`, `Service`, `Environment` and the trace identifiers.
This proves the publishing boundary is logged without dumping article content.

For a fault scenario, stop ProfanityService (`docker compose stop profanity-service`),
submit a comment, and filter on `Service = 'CommentService'`. Find the circuit
breaker OPEN and pending moderation events. Restart ProfanityService afterwards.

### Screenshot B — Distributed tracing (Seq Traces / Events)

Find a trace containing the producer span `publish article`, then inspect the
consumer spans `process published article` and `process newsletter article`.
The publisher injects the trace context into the RabbitMQ message; both
subscribers extract it. The ArticleService branch includes `article.db.publish`.
The newsletter subscriber **currently only acknowledges/logs** receipt; actual
sending of email newsletters is not implemented and should not be claimed.

For synchronous fault isolation, send a comment and inspect `comment.create`
with the outgoing HTTP client span to ProfanityService, the incoming request
there, and `profanity.db.words` where applicable.

### Screenshot C — Metrics (Seq Metrics)

Look for `happyheadlines.cache.lookups` with `cache.name` and `result` tags.
The prior Redis counter dashboard was replaced by these genuine OTel metrics.
Also inspect `happyheadlines.workflow.events`, `System.Runtime`, and
`Microsoft.AspNetCore.Hosting` metrics. Seq requires **delta temporality**;
the shared library exports metrics every 10 seconds with that setting.

## 4. What to log and when (The Art of Scalability, Chapter 12)

- **Information** when an HTTP operation finishes: method, *route template*, status,
  duration; when an article is queued or persisted; when moderation changes state;
  when cache refresh completes.
- **Warning** for dependency timeouts, cache fallback, circuit breaker changes,
  deferred moderation, and anticipated partial failure.
- **Error** for unhandled request errors, publish failures, database exceptions,
  and background worker failures.
- **Never** log Authorization or cookie headers, query-string data, passwords,
  personal email addresses, user-authored comment text, profane words, or article
  bodies. Avoid high-cardinality IDs as metric tags; use them in isolated diagnostic
  events only where needed.
- Health poll requests are deliberately excluded from the request-event stream.

Recommended Seq *Signals* and alert rules: 5xx HTTP statuses, breaker OPEN,
article persistence failure, pending moderation backlog (requires a custom backlog
instrument), and p95 request latency. Configure retention and evaluate sampler
rates after measuring real traffic; this demo records 100% of traces.

## 5. Visual mapping to the system diagram

`Webapp → PublisherService → RabbitMQ (ArticleQueue)` is the primary publishing
path. The two branches from the queue are `ArticleService` (to continent-based
PostgreSQL databases) and `NewsletterService` (current event receipt/logging only).
`CommentService → ProfanityService → ProfanityDatabase` is a separate synchronous
moderation path and is protected with Polly's circuit breaker.

The project diagram also shows NewsletterService requesting ArticleService and
sending newsletters, but those behaviors are **not implemented as such** in the
inspected repository. The traces should tell the truth about the actual code, not
fabricate those paths for screenshots.

## 6. Validation and known tradeoffs

```bash
cd HappyHeadlines
dotnet restore HappyHeadlines.sln
dotnet build HappyHeadlines.sln --configuration Release
docker compose config
docker compose up --build -d
```

`ArticleCacheWorker` was also fixed to resolve the scoped article repository from
an explicit service scope instead of constructor-injecting it into a hosted singleton.

The exporter uses async batching but events can be lost on abrupt termination or
prolonged Seq outages. Seq is an **observability dependency, not a functional
application dependency**. Production systems require careful capacity and failure
planning, ingress authentication, resilient buffering, health checks and monitoring
of Seq itself.
