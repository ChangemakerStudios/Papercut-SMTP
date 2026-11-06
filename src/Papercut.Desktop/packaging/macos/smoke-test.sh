#!/usr/bin/env bash
# Runs the packaged app from a portable zip and checks it receives mail. Doesn't cover the menu-bar icon or notifications.
# Usage: ./smoke-test.sh <releases dir>
set -euo pipefail

RELEASES="$1"
WEB="http://127.0.0.1:37410"
WORK="$(mktemp -d)"
export PAPERCUT_DATA_DIR="$WORK/data"
mkdir -p "$PAPERCUT_DATA_DIR" "$WORK/app"

ditto -x -k "$RELEASES"/*-Portable.zip "$WORK/app"
APP="$(find "$WORK/app" -maxdepth 2 -name '*.app' | head -1)"
[[ -n "$APP" ]] || { echo "no .app in the portable zip" >&2; exit 1; }

"$APP/Contents/MacOS/Papercut" &
PID=$!

finish() {
  status=$?
  kill "$PID" 2>/dev/null || true
  if [[ $status -ne 0 ]]; then
    echo "==> smoke test failed; logs:"
    find "$PAPERCUT_DATA_DIR/Logs" -type f -exec tail -n 40 {} + 2>/dev/null || true
  fi
}
trap finish EXIT

echo "==> waiting for the service"
for _ in $(seq 1 60); do
  kill -0 "$PID" 2>/dev/null || { echo "the app exited early" >&2; exit 1; }
  curl -fs -o /dev/null "$WEB/health" && break
  sleep 2
done
curl -fs -o /dev/null "$WEB/health"

echo "==> sending mail over SMTP"
python3 - <<'PY'
import smtplib
from email.message import EmailMessage

m = EmailMessage()
m["Subject"], m["From"], m["To"] = "macOS smoke test", "a@example.com", "b@example.com"
m.set_content("hello")

with smtplib.SMTP("127.0.0.1", 2525, timeout=30) as s:
    s.send_message(m)
PY

for _ in $(seq 1 15); do
  curl -fs "$WEB/api/messages" | grep -q "macOS smoke test" && { echo "==> mail received"; exit 0; }
  sleep 2
done

echo "the mail never appeared" >&2
exit 1
