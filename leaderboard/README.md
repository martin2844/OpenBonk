# OpenBonk leaderboard server

Best-effort high-score API for the Android APK, designed for **Vercel
Functions + Upstash Redis**. No accounts, no integrity token, no client secret.
Scores are client-reported; the server only rejects malformed or implausible
submissions and rate-limits spam.

## Deploy on Vercel

1. Import the `OpenBonk` repository into Vercel and set **Root Directory** to
   `leaderboard`.
2. In the Vercel project, open **Storage**, add an **Upstash Redis** (or Vercel
   KV) database and connect it to the project.
   The integration provides `KV_REST_API_URL` and `KV_REST_API_TOKEN`. The
   server also accepts `UPSTASH_REDIS_REST_URL` and
   `UPSTASH_REDIS_REST_TOKEN`.
3. Deploy. The functions are under `/api/v1/`.

Optional environment variable:

- `IP_SALT` — a random string used when hashing IPs for rate limits. Set it if
  you want hashes to remain stable across deployments; otherwise a default is
  used.

Health check:

```sh
curl https://YOUR-PROJECT.vercel.app/api/v1/health
```

## Submit a score

```sh
curl -X POST https://YOUR-PROJECT.vercel.app/api/v1/scores \
  -H 'Content-Type: application/json' \
  -d '{
    "board": "kills",
    "name": "Fox",
    "score": 1234,
    "duration": 900,
    "version": "0.1.27-beta",
    "character": "Fox",
    "map": "Forest",
    "mods": false
  }'
```

Response:

```json
{
  "ok": true,
  "board": "kills",
  "period": "all",
  "name": "Fox",
  "rank": 12,
  "best": 1234,
  "submitted": 1234
}
```

## Read a leaderboard

```sh
curl 'https://YOUR-PROJECT.vercel.app/api/v1/leaderboard?board=kills&limit=50'
curl 'https://YOUR-PROJECT.vercel.app/api/v1/leaderboard?board=kills_weekly&limit=50'
```

Response:

```json
{
  "board": "kills",
  "period": "all",
  "limit": 50,
  "entries": [
    {
      "rank": 1,
      "name": "Fox",
      "score": 1234,
      "version": "0.1.27-beta",
      "character": "Fox",
      "map": "Forest",
      "duration": 900,
      "mods": false,
      "at": "2026-11-26T12:00:00.000Z"
    }
  ]
}
```

## Boards and storage

- `kills` — all-time, key `openbonk:lb:kills:all`
- `kills_weekly` — ISO week (UTC), key `openbonk:lb:kills_weekly:2026-W48`,
  expires after 14 days

Scores are kept in a Redis sorted set; display metadata is kept in a hash.
`ZADD GT` means a worse submission does not lower a player's existing best.
Names are normalized (NFKC, whitespace, controls removed) and deduplicated by
lowercase name, so two players using the same name share one entry. That is a
casual leaderboard trade-off; there are no accounts.

Limits:

- score: integer `0..10,000,000`
- duration: `0..86,400` seconds
- name: 1–24 characters after normalization
- rate: 12 submissions/minute per name and 30/minute per IP hash
- body: 16 KB

## Security limits

This is **not** tamper-proof. The game is client-authoritative and offline, the
APK is public, and there is deliberately no Play Integrity or account token. A
determined player can submit any valid-looking score. What this server does:

- rejects malformed and obviously implausible submissions
- rate-limits spam
- keeps a player's highest submitted score
- separates all-time and weekly boards
- never trusts a client timestamp

If competitive integrity matters later, add signed run tickets, replay/event
logs, Play Integrity, or separate vanilla/modded boards. Do not describe this
API as an anti-cheat guarantee.

## Client call

The Android client can use `UnityWebRequest` with the same JSON. Keep the URL in
one build-config constant; do not embed a secret in the APK.

## Local development

```sh
cd leaderboard
export KV_REST_API_URL='https://...'
export KV_REST_API_TOKEN='...'
npx vercel dev
npm test
```

No npm dependencies are required for the deployed functions; `vercel` is only
used for local development.
