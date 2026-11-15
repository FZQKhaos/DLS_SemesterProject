#!/usr/bin/env bash
# Run only against locally launched HappyHeadlines containers.
set -euo pipefail
: "${FEATURE_FLAGS_ADMIN_KEY:?Set FEATURE_FLAGS_ADMIN_KEY in your shell}"
API=${NEWSLETTER_URL:-http://localhost:8086}
SUB=${SUBSCRIBER_URL:-http://localhost:8087}
setflag() {
  curl --fail-with-body -sS -X PUT "$API/admin/features/subscriber-service" \
    -H "X-Admin-Key: $FEATURE_FLAGS_ADMIN_KEY" \
    -H 'Content-Type: application/json' -d "{\"enabled\":$1}"
  printf '\n'
}
setflag true
curl --fail-with-body -sS -X POST "$SUB/api/subscribers" \
  -H 'Content-Type: application/json' -d '{"email":"demo@example.com"}'
printf '\n'
setflag false
status=$(curl -sS -o /dev/null -w '%{http_code}' -X POST "$SUB/api/subscribers" \
  -H 'Content-Type: application/json' -d '{"email":"demo2@example.com"}')
[ "$status" = 404 ] || { echo "Expected 404 when disabled; got $status" >&2; exit 1; }
curl --fail-with-body -sS "$API/health" > /dev/null
echo "PASS: SubscriberService hidden while NewsletterService remains healthy"
