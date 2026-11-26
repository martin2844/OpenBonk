const { HttpError } = require('./http');

const BOARDS = new Set(['kills', 'kills_weekly']);
const MAX_NAME = 24;
const MAX_SCORE = 10_000_000;
const MAX_DURATION = 86_400;
const RATE_LIMIT_PER_MINUTE = 12;
const RATE_LIMIT_PER_IP_MINUTE = 30;

function boardPeriod(board, now = new Date()) {
  if (!BOARDS.has(board)) throw new HttpError(400, 'unknown_board', 'Unknown leaderboard');
  return board === 'kills_weekly' ? isoWeek(now) : 'all';
}

function leaderboardKey(board, period) {
  return `openbonk:lb:${board}:${period}`;
}

function entriesKey(board, period) {
  return `openbonk:entries:${board}:${period}`;
}

function rateKeys(ipHash, nameHash, now = new Date()) {
  const minute = Math.floor(now.getTime() / 60_000);
  return {
    ip: `openbonk:rl:ip:${ipHash}:${minute}`,
    name: `openbonk:rl:name:${nameHash}:${minute}`
  };
}

function validateSubmission(body, now = new Date()) {
  if (!body || typeof body !== 'object' || Array.isArray(body)) {
    throw new HttpError(400, 'invalid_body', 'A JSON object is required');
  }
  const board = typeof body.board === 'string' && body.board ? body.board : 'kills';
  const period = boardPeriod(board, now);
  const name = normalizeName(body.name);
  if (!name) throw new HttpError(400, 'invalid_name', 'Name is required');
  const score = body.score;
  if (!Number.isSafeInteger(score) || score < 0 || score > MAX_SCORE) {
    throw new HttpError(400, 'invalid_score', 'Score must be an integer between 0 and ' + MAX_SCORE);
  }
  const duration = body.duration === undefined ? 0 : body.duration;
  if (!Number.isSafeInteger(duration) || duration < 0 || duration > MAX_DURATION) {
    throw new HttpError(400, 'invalid_duration', 'Duration must be an integer between 0 and ' + MAX_DURATION);
  }
  const version = text(body.version, 64) || 'unknown';
  const character = text(body.character, 48);
  const map = text(body.map, 48);
  const mods = body.mods === true;
  if (duration > 0 && score > duration * 200 + 10_000) {
    throw new HttpError(422, 'implausible_score', 'Score is implausible for the submitted duration');
  }
  return {
    board,
    period,
    name,
    member: name.toLocaleLowerCase('en-US'),
    score,
    duration,
    version,
    character,
    map,
    mods
  };
}

function normalizeName(value) {
  if (typeof value !== 'string') return '';
  return value
    .normalize('NFKC')
    .replace(/[\u0000-\u001f\u007f-\u009f\u200b-\u200f\u202a-\u202e\u2060-\u206f]/g, '')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, MAX_NAME);
}

function text(value, max) {
  if (typeof value !== 'string') return '';
  return value.replace(/[\u0000-\u001f\u007f]/g, '').trim().slice(0, max);
}

function parseLimit(value) {
  const parsed = Number.parseInt(value, 10);
  if (!Number.isFinite(parsed)) return 50;
  return Math.max(1, Math.min(100, parsed));
}

function isoWeek(date) {
  const day = new Date(Date.UTC(date.getUTCFullYear(), date.getUTCMonth(), date.getUTCDate()));
  const weekday = day.getUTCDay() || 7;
  day.setUTCDate(day.getUTCDate() + 4 - weekday);
  const year = day.getUTCFullYear();
  const yearStart = new Date(Date.UTC(year, 0, 1));
  const week = Math.ceil((((day - yearStart) / 86_400_000) + 1) / 7);
  return `${year}-W${String(week).padStart(2, '0')}`;
}

module.exports = {
  BOARDS,
  MAX_NAME,
  MAX_SCORE,
  MAX_DURATION,
  RATE_LIMIT_PER_MINUTE,
  RATE_LIMIT_PER_IP_MINUTE,
  boardPeriod,
  leaderboardKey,
  entriesKey,
  rateKeys,
  validateSubmission,
  normalizeName,
  parseLimit,
  isoWeek
};
