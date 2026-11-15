# Compulsory assignment 2 - implementation and runbook

Patch baseline: `Compulsory-assignment-#2` at tree `99b36edda5360a1b811a15061c2aa2d187123942`.

## CD (assignment scope)

`.github/workflows/workflow.yml` builds .NET 8 solution on PRs and main pushes;
its image matrix includes `subscriber-service` and every previously present app or worker.
On **main push**, it publishes nine images to GHCR with immutable *run-number*
tags. The assignment asks for image delivery, **not automatic production deployment**.
Verify in GitHub > Actions > `Build and publish` and GitHub Packages after merging.

## Recovery and swimlanes

- SubscriberService: POST `/api/subscribers` `{ "email": "a@example.com" }`
  and DELETE `/api/subscribers/{email}`; only it reads/writes `subscriber-db`.
- `rabbitmq` carries the existing `ArticlePublished` pub/sub event; the new
  SubscriberService independently subscribes to it and reads the subscribers table.
- **Dedicated** `subscriber-rabbitmq` broker / durable `SubscriberQueue` subscription
  carries `SubscriberNotification` events (welcome or an article for each subscriber).
- NewsletterService alone consumes that queue and sends SMTP messages **only when SMTP
  is configured**. Otherwise it logs the fact that no email was sent.
- No synchronous SubscriberService-to-NewsletterService or Newsletter-to-Subscriber
  REST call. SubscriberService outage does not block article publishing. Newsletter
  subscriber-broker worker reconnects without blocking its article consumer.
- Both services still share an infrastructure-level Redis feature flag and the article
  bus; this is pragmatic fault containment, not perfectly isolated swimlanes by
  the strict definition in *The Art of Scalability* chapter 21.

### Runtime release toggle (without redeploy/restart)

Set a strong random key in your shell before `docker compose up`:

```bash
export FEATURE_FLAGS_ADMIN_KEY='REPLACE_WITH_RANDOM_SECRET'
docker compose -f HappyHeadlines/compose.yaml up --build -d
```

Compose requires the `FEATURE_FLAGS_ADMIN_KEY` environment variable; do not commit
secrets. Flag defaults **disabled** until explicitly enabled. Example:

```bash
curl -X PUT http://localhost:8086/admin/features/subscriber-service \
  -H 'Content-Type: application/json' \
  -H "X-Admin-Key: $FEATURE_FLAGS_ADMIN_KEY" \
  -d '{"enabled":true}'
curl -X POST http://localhost:8087/api/subscribers \
  -H 'Content-Type: application/json' \
  -d '{"email":"demo@example.com"}'
curl -X PUT http://localhost:8086/admin/features/subscriber-service \
  -H 'Content-Type: application/json' \
  -H "X-Admin-Key: $FEATURE_FLAGS_ADMIN_KEY" \
  -d '{"enabled":false}'
# POST now returns 404; publication/CommentService continue functioning.
```

Read flag state using `GET` at the same admin endpoint with the admin header.
Each newsletter event and subscriber API request reads current Redis state. Redis
unavailable => subscribers feature disabled, without disabling core articles.

For actual email delivery, configure `Smtp__Host`, `Smtp__Port`, `Smtp__From`,
`Smtp__Username`, `Smtp__Password`, `Smtp__EnableSsl` for `newsletter-service`.
No SMTP credentials/provider are supplied by this repository; please configure
an approved mail transport before claiming messages were delivered.

### Operational limitations (next hardening steps)

- Publish to subscriber queue occurs **after** DB commit and there is no
  transactional outbox yet; temporary queue failure can lose a welcome notification.
- Toggle-off currently **drops** subscriber notifications that were already enqueued
  and consumed. Change the worker to park/retry messages if the business requires
  delayed processing after re-enable.
- Subscriber queue can redeliver messages; sending email needs an idempotent
  delivery ledger for exactly-once business effect.
- NewsletterService has no local subscriber projection; SubscriberService creates
  per-recipient messages asynchronously on article publications.
- Versioned images are pushed by main; Compose still builds locally.
- End-to-end Docker validation and energy measurements must be performed on a host
  with Docker and .NET SDK (these were not available in the authoring environment).

## Sustainable changes and reproducible verification

See [sustainability-before-after.md](sustainability-before-after.md).
Run `python3 scripts/verify-gaf.py` for a *deterministic work-proxy comparison*,
then perform energy measurements with identical hardware and identical workload
on the baseline and patched branch. Do not label work proxies as energy savings.
