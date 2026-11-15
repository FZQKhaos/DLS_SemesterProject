#!/usr/bin/env bash
# Run from HappyHeadlines/ after the compose stack starts.
set -euo pipefail
ID="seq-demo-$(date +%s)"
printf 'Publishing demo article %s\n' "$ID"
curl -fsS -X POST http://localhost:8085/Publisher \
  -H 'Content-Type: application/json' \
  -d "{\"id\":\"$ID\",\"title\":\"Observability demo\",\"body\":\"A simple test article.\",\"continent\":\"europe\"}"
printf '\nPublishing is asynchronous; inspect Seq Events and Traces after subscribers process the message.\n'
printf 'Seq URL: http://localhost:5342\n'
printf '\nCreating demo comment (should call ProfanityService)\n'
curl -fsS -X POST http://localhost:6002/api/comments \
  -H 'Content-Type: application/json' \
  -d "{\"articleId\":\"$ID\",\"author\":\"demonstrator\",\"body\":\"An ordinary test comment.\"}"
printf '\nInspect HTTP, dependency, DB, and RabbitMQ spans in Seq.\n'
