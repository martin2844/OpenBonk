const { json, options, error, readJson, clientIp, hash, HttpError } = require('../../lib/http');
const { validateSubmission, leaderboardKey, entriesKey, rateKeys, RATE_LIMIT_PER_MINUTE, RATE_LIMIT_PER_IP_MINUTE } = require('../../lib/leaderboard');
const { pipeline } = require('../../lib/redis');

module.exports = async function handler(req, res) {
  if (req.method === 'OPTIONS') return options(res);
  if (req.method !== 'POST') return error(res, 405, 'method_not_allowed', 'Use POST');

  try {
    const body = await readJson(req);
    const submission = validateSubmission(body);
    const key = leaderboardKey(submission.board, submission.period);
    const entries = entriesKey(submission.board, submission.period);
    const keys = rateKeys(hash(clientIp(req)), hash(submission.member));

    const limits = await pipeline([
      ['INCR', keys.ip], ['EXPIRE', keys.ip, 90],
      ['INCR', keys.name], ['EXPIRE', keys.name, 90]
    ]);
    if (limits[0] > RATE_LIMIT_PER_IP_MINUTE || limits[2] > RATE_LIMIT_PER_MINUTE) {
      return error(res, 429, 'rate_limited', 'Too many submissions; try again later');
    }

    const entry = JSON.stringify({
      name: submission.name,
      version: submission.version,
      character: submission.character,
      map: submission.map,
      duration: submission.duration,
      mods: submission.mods,
      at: new Date().toISOString()
    });
    const writes = [
      ['ZADD', key, 'GT', submission.score, submission.member],
      ['HSET', entries, submission.member, entry]
    ];
    if (submission.period !== 'all') {
      writes.push(['EXPIRE', key, 14 * 86_400], ['EXPIRE', entries, 14 * 86_400]);
    }
    await pipeline(writes);

    const result = await pipeline([
      ['ZREVRANK', key, submission.member],
      ['ZSCORE', key, submission.member]
    ]);
    return json(res, 200, {
      ok: true,
      board: submission.board,
      period: submission.period,
      name: submission.name,
      rank: result[0] === null ? null : result[0] + 1,
      best: Number(result[1]),
      submitted: submission.score
    });
  } catch (cause) {
    if (cause instanceof HttpError) return error(res, cause.status, cause.code, cause.message);
    console.error('leaderboard submission failed', cause);
    return error(res, 500, 'internal_error', 'Could not record the score');
  }
};
