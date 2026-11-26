const test = require('node:test');
const assert = require('node:assert/strict');
const {
  boardPeriod,
  leaderboardKey,
  entriesKey,
  rateKeys,
  validateSubmission,
  normalizeName,
  parseLimit,
  isoWeek
} = require('../lib/leaderboard');

test('normalizes player names', () => {
  assert.equal(normalizeName('  Fox\u0000  McCloud  '), 'Fox McCloud');
  assert.equal(normalizeName('\u200b'), '');
  assert.equal(normalizeName('a'.repeat(40)).length, 24);
});

test('validates and normalizes a submission', () => {
  const value = validateSubmission({
    board: 'kills',
    name: '  Fox  ',
    score: 1234,
    duration: 600,
    version: '0.1.27-beta',
    character: 'Fox',
    map: 'Forest'
  }, new Date('2026-11-26T12:00:00Z'));
  assert.equal(value.board, 'kills');
  assert.equal(value.period, 'all');
  assert.equal(value.name, 'Fox');
  assert.equal(value.member, 'fox');
  assert.equal(value.score, 1234);
  assert.equal(value.mods, false);
});

test('rejects bad submissions', () => {
  assert.throws(() => validateSubmission({ name: 'Fox', score: -1 }), /Score/);
  assert.throws(() => validateSubmission({ name: '', score: 10 }), /Name/);
  assert.throws(() => validateSubmission({ board: 'nope', name: 'Fox', score: 10 }), /Unknown leaderboard/);
  assert.throws(() => validateSubmission({ name: 'Fox', score: 20000, duration: 1 }), /implausible/);
});

test('uses weekly and all-time keys', () => {
  const now = new Date('2026-11-26T12:00:00Z');
  assert.equal(boardPeriod('kills', now), 'all');
  assert.equal(boardPeriod('kills_weekly', now), isoWeek(now));
  assert.match(boardPeriod('kills_weekly', now), /^2026-W\d{2}$/);
  assert.equal(leaderboardKey('kills', 'all'), 'openbonk:lb:kills:all');
  assert.equal(entriesKey('kills', 'all'), 'openbonk:entries:kills:all');
});

test('builds stable rate-limit keys', () => {
  const keys = rateKeys('ip', 'name', new Date('2026-11-26T12:00:00Z'));
  assert.match(keys.ip, /^openbonk:rl:ip:ip:\d+$/);
  assert.match(keys.name, /^openbonk:rl:name:name:\d+$/);
});

test('clamps the requested limit', () => {
  assert.equal(parseLimit(undefined), 50);
  assert.equal(parseLimit('0'), 1);
  assert.equal(parseLimit('500'), 100);
  assert.equal(parseLimit('25'), 25);
});
