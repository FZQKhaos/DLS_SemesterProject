#!/usr/bin/env bash
set -euo pipefail

COMMENT_URL="${COMMENT_URL:-http://localhost:6000}"

printf '\n1) Stopping ProfanityService...\n'
docker compose stop profanity-service

printf '\n2) Posting four comments. Early calls wait for the timeout; after the threshold the breaker fails fast.\n'
for i in 1 2 3 4; do
  echo "--- request $i ---"
  curl -sS -i -X POST "$COMMENT_URL/api/comments" \
    -H 'Content-Type: application/json' \
    -d "{\"articleId\":\"circuit-demo\",\"author\":\"tester\",\"body\":\"comment $i with badword\"}" | head -n 12
  echo
 done

printf '\n3) Pending comments are stored but hidden from the normal public read.\n'
curl -sS "$COMMENT_URL/api/comments/article/circuit-demo"; echo
curl -sS "$COMMENT_URL/api/comments/article/circuit-demo?includePending=true"; echo

printf '\n4) Starting ProfanityService again. The background worker will publish pending comments after the breaker recovers.\n'
docker compose start profanity-service
printf 'Wait roughly 30-45 seconds, then run:\n'
printf 'curl -sS %s/api/comments/article/circuit-demo\n' "$COMMENT_URL"
