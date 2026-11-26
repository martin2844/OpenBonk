const { json, options, error, HttpError } = require('../../lib/http');
const { command, configured } = require('../../lib/redis');

module.exports = async function handler(req, res) {
  if (req.method === 'OPTIONS') return options(res);
  if (req.method !== 'GET') return error(res, 405, 'method_not_allowed', 'Use GET');
  if (!configured()) return error(res, 503, 'storage_not_configured', 'Set KV_REST_API_URL and KV_REST_API_TOKEN');
  try {
    await command('PING');
    return json(res, 200, { ok: true, storage: 'up' });
  } catch (cause) {
    if (cause instanceof HttpError) return error(res, cause.status, cause.code, cause.message);
    return error(res, 503, 'storage_down', 'Leaderboard storage is unavailable');
  }
};
