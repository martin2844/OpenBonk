const { json, options, error, HttpError } = require('../../lib/http');
const { boardPeriod, leaderboardKey, entriesKey, parseLimit } = require('../../lib/leaderboard');
const { pipeline } = require('../../lib/redis');

module.exports = async function handler(req, res) {
  if (req.method === 'OPTIONS') return options(res);
  if (req.method !== 'GET') return error(res, 405, 'method_not_allowed', 'Use GET');

  try {
    const query = new URL(req.url, 'http://localhost').searchParams;
    const board = query.get('board') || 'kills';
    const period = boardPeriod(board);
    const limit = parseLimit(query.get('limit'));
    const key = leaderboardKey(board, period);
    const entries = entriesKey(board, period);

    const [flat] = await pipeline([['ZREVRANGE', key, 0, limit - 1, 'WITHSCORES']]);
    const names = [];
    const scores = [];
    for (let i = 0; i < flat.length; i += 2) {
      names.push(flat[i]);
      scores.push(Number(flat[i + 1]));
    }

    let metadata = [];
    if (names.length) {
      [metadata] = await pipeline([['HMGET', entries, ...names]]);
    }

    const rows = names.map((member, index) => {
      let entry = {};
      try {
        entry = JSON.parse(metadata[index] || '{}');
      } catch {
        entry = {};
      }
      return {
        rank: index + 1,
        name: typeof entry.name === 'string' ? entry.name : member,
        score: scores[index],
        version: entry.version || null,
        character: entry.character || null,
        map: entry.map || null,
        duration: entry.duration || 0,
        mods: entry.mods === true,
        at: entry.at || null
      };
    });

    return json(res, 200, { board, period, limit, entries: rows }, {
      'Cache-Control': 'public, s-maxage=15, stale-while-revalidate=30'
    });
  } catch (cause) {
    if (cause instanceof HttpError) return error(res, cause.status, cause.code, cause.message);
    console.error('leaderboard read failed', cause);
    return error(res, 500, 'internal_error', 'Could not read the leaderboard');
  }
};
