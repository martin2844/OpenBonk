const { createHash } = require('node:crypto');

const MAX_BODY_BYTES = 16 * 1024;

function json(res, status, body, extraHeaders = {}) {
  if (res.writableEnded) return;
  res.statusCode = status;
  res.setHeader('Content-Type', 'application/json; charset=utf-8');
  res.setHeader('Cache-Control', 'no-store');
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET,POST,OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  for (const [key, value] of Object.entries(extraHeaders)) res.setHeader(key, value);
  res.end(JSON.stringify(body));
}

function options(res) {
  json(res, 204, null);
}

function error(res, status, code, message) {
  json(res, status, { error: code, message });
}

async function readJson(req) {
  if (req.body && typeof req.body === 'object') {
    const encoded = JSON.stringify(req.body);
    if (Buffer.byteLength(encoded) > MAX_BODY_BYTES) throw new HttpError(413, 'body_too_large', 'Request body is too large');
    return req.body;
  }
  let raw = '';
  for await (const chunk of req) {
    raw += chunk;
    if (Buffer.byteLength(raw) > MAX_BODY_BYTES) throw new HttpError(413, 'body_too_large', 'Request body is too large');
  }
  if (!raw) throw new HttpError(400, 'empty_body', 'A JSON body is required');
  try {
    return JSON.parse(raw);
  } catch {
    throw new HttpError(400, 'invalid_json', 'The body is not valid JSON');
  }
}

function clientIp(req) {
  const forwarded = req.headers['x-forwarded-for'];
  const first = Array.isArray(forwarded) ? forwarded[0] : forwarded;
  return (first ? first.split(',')[0] : req.socket?.remoteAddress || 'unknown').trim();
}

function hash(value) {
  return createHash('sha256').update(process.env.IP_SALT || 'openbonk').update('|').update(value).digest('hex').slice(0, 32);
}

class HttpError extends Error {
  constructor(status, code, message) {
    super(message);
    this.status = status;
    this.code = code;
  }
}

module.exports = { json, options, error, readJson, clientIp, hash, HttpError, MAX_BODY_BYTES };
