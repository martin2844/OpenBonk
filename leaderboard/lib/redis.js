const { HttpError } = require('./http');

function config() {
  const url = process.env.KV_REST_API_URL || process.env.UPSTASH_REDIS_REST_URL;
  const token = process.env.KV_REST_API_TOKEN || process.env.UPSTASH_REDIS_REST_TOKEN;
  if (!url || !token) {
    throw new HttpError(503, 'storage_not_configured', 'Leaderboard storage is not configured');
  }
  return { url: url.replace(/\/+$/, ''), token };
}

function configured() {
  try {
    config();
    return true;
  } catch {
    return false;
  }
}

async function pipeline(commands) {
  const { url, token } = config();
  const response = await fetch(`${url}/pipeline`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(commands)
  });
  if (!response.ok) {
    throw new HttpError(502, 'storage_error', 'Leaderboard storage returned HTTP ' + response.status);
  }
  const body = await response.json();
  if (!Array.isArray(body)) throw new HttpError(502, 'storage_error', 'Invalid leaderboard storage response');
  for (const item of body) {
    if (item && item.error) throw new HttpError(502, 'storage_error', String(item.error));
  }
  return body.map((item) => item.result);
}

async function command(...args) {
  const [result] = await pipeline([args]);
  return result;
}

module.exports = { pipeline, command, configured };
