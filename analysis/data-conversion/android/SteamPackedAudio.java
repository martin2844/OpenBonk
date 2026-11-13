package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** Local research restoration of packed Steam audio. Not a distributable build. */
public final class SteamPackedAudio {
    private static final long MAX_FILE = 1024L * 1024 * 1024, MAX_TOTAL = 256L * 1024 * 1024;
    private static final int MAX_ENTRIES = 20000, MAX_RECORDS = 2000, MAX_CLIP = 8 * 1024 * 1024;
    private static final String PREFIX = "assets/bin/Data/";
    private static final String[] SOURCES = {"sharedassets1.assets", "sharedassets1.resource", "sharedassets2.assets", "sharedassets2.resource"};

    private static final class Row {
        String entry, source, hash;
        long target, offset;
        int size;
    }
    private static MessageDigest sha() {
        try { return MessageDigest.getInstance("SHA-256"); }
        catch (NoSuchAlgorithmException ex) { throw new AssertionError(ex); }
    }
    private static String hex(byte[] data) {
        StringBuilder s = new StringBuilder(data.length * 2);
        for (byte b : data) s.append(String.format(Locale.ROOT, "%02x", b & 255));
        return s.toString();
    }
    private static String checkHash(String value) throws IOException {
        if (value == null || !value.matches("[0-9a-f]{64}")) throw new IOException("invalid checksum");
        return value;
    }
    private static byte[] small(File file) throws IOException {
        if (!file.isFile() || file.length() <= 0 || file.length() > 1024 * 1024) throw new IOException("invalid plan size");
        try (InputStream in = new FileInputStream(file); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] b = new byte[8192]; int count = 0;
            for (int n; (n = in.read(b)) != -1;) { if (n == 0) continue; count += n; if (count > 1024 * 1024) throw new IOException("plan too large"); out.write(b, 0, n); }
            return out.toByteArray();
        }
    }
    private static String hash(File f) throws IOException {
        if (!f.isFile() || f.length() <= 0 || f.length() > MAX_FILE) throw new IOException("invalid input size");
        MessageDigest h = sha(); byte[] b = new byte[65536]; long count = 0;
        try (InputStream in = new FileInputStream(f)) { for (int n; (n = in.read(b)) != -1;) { if (n == 0) continue; count += n; if (count > MAX_FILE) throw new IOException("input too large"); h.update(b, 0, n); } }
        return hex(h.digest());
    }
    private static void safeName(String name) throws IOException {
        if (name.isEmpty() || name.startsWith("/") || name.indexOf('\\') >= 0 || name.indexOf(':') >= 0) throw new IOException("unsafe ZIP name");
        for (String part : name.split("/", -1)) if (part.isEmpty() || part.equals(".") || part.equals("..")) throw new IOException("unsafe ZIP path");
    }
    private static String rootFor(String name, String suffix) {
        if (name.equals(suffix)) return "";
        if (name.equals("Megabonk_Data/" + suffix) || name.endsWith("/Megabonk_Data/" + suffix)) return name.substring(0, name.length() - suffix.length());
        return null;
    }
    private static void copyExact(InputStream in, OutputStream out, long length, boolean zero, MessageDigest digest) throws IOException {
        byte[] buffer = new byte[65536];
        while (length > 0) {
            int n = in.read(buffer, 0, (int)Math.min(buffer.length, length));
            if (n < 0) throw new EOFException("truncated input");
            if (n == 0) continue;
            if (zero) for (int i = 0; i < n; i++) if (buffer[i] != 0) throw new IOException("template audio is not blank");
            if (out != null) out.write(buffer, 0, n);
            if (digest != null) digest.update(buffer, 0, n);
            length -= n;
        }
    }
    private static void verifySource(ZipFile zip, ZipEntry entry, String expected, Path extraction) throws IOException {
        if (entry == null || entry.isDirectory() || entry.getSize() < 0 || entry.getSize() > MAX_FILE) throw new IOException("invalid Steam input");
        MessageDigest digest = sha();
        try (InputStream in = zip.getInputStream(entry); OutputStream out = extraction == null ? null : Files.newOutputStream(extraction)) {
            copyExact(in, out, entry.getSize(), false, digest);
            if (in.read() != -1 || !hex(digest.digest()).equals(expected)) throw new IOException("Steam input differs");
        }
    }
    public static void restore(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output) throws IOException {
        Path target = output.toPath().toAbsolutePath(), parent = target.getParent();
        if (parent == null || !Files.isDirectory(parent) || Files.exists(target, LinkOption.NOFOLLOW_LINKS)) throw new IOException("output must be new");
        byte[] bytes = small(planFile);
        if (!hex(sha().digest(bytes)).equals(checkHash(expectedPlanHash))) throw new IOException("plan checksum differs");
        Map<String, String> hashes = new HashMap<>(); Map<String, List<Row>> byEntry = new HashMap<>();
        String templateHash;
        try {
            JSONObject plan = new JSONObject(new String(bytes, "UTF-8"));
            if (plan.getInt("schema") != 1 || !plan.getString("kind").equals("experimental-packed-steam-audio-offsets")) throw new IOException("unsupported plan");
            templateHash = checkHash(plan.getString("template_sha256")); JSONObject sources = plan.getJSONObject("sources");
            for (String name : SOURCES) hashes.put(name, checkHash(sources.getString(name)));
            JSONArray rows = plan.getJSONArray("records"); long total = 0;
            if (rows.length() == 0 || rows.length() > MAX_RECORDS) throw new IOException("invalid record count");
            for (int i = 0; i < rows.length(); i++) {
                JSONObject r = rows.getJSONObject(i); Row row = new Row();
                row.entry = r.getString("entry"); row.source = r.getString("source"); row.hash = checkHash(r.getString("sha256"));
                row.target = r.getLong("target_offset"); row.offset = r.getLong("source_offset"); row.size = r.getInt("size");
                if (!(row.entry.equals(PREFIX + "sharedassets1.resource") || row.entry.equals(PREFIX + "sharedassets2.resource")) ||
                    !(row.source.equals("sharedassets1.resource") || row.source.equals("sharedassets2.resource")) ||
                    row.target < 0 || row.offset < 0 || row.size <= 0 || row.size > MAX_CLIP || row.target > MAX_FILE - row.size || row.offset > MAX_FILE - row.size) throw new IOException("invalid plan range");
                total += row.size; if (total > MAX_TOTAL) throw new IOException("audio limit exceeded");
                byEntry.computeIfAbsent(row.entry, k -> new ArrayList<>()).add(row);
            }
            for (List<Row> list : byEntry.values()) {
                list.sort(Comparator.comparingLong(r -> r.target)); long end = 0;
                for (Row row : list) { if (row.target < end) throw new IOException("overlapping ranges"); end = row.target + row.size; }
            }
        } catch (JSONException e) { throw new IOException("invalid plan", e); }
        if (!hash(templateFile).equals(templateHash) || !steamFile.isFile() || steamFile.length() <= 0 || steamFile.length() > MAX_FILE) throw new IOException("invalid template or Steam ZIP");
        Path work = Files.createTempDirectory(parent, ".openbonk-packed-");
        try (ZipFile steam = new ZipFile(steamFile); ZipFile template = new ZipFile(templateFile)) {
            Map<String, ZipEntry> found = new HashMap<>(); Set<String> names = new HashSet<>(); String root = null;
            Enumeration<? extends ZipEntry> en = steam.entries();
            while (en.hasMoreElements()) {
                ZipEntry e = en.nextElement(); if (e.isDirectory()) continue; String name = e.getName(); safeName(name);
                if (names.size() >= MAX_ENTRIES || !names.add(name)) throw new IOException("duplicate or excessive Steam entries");
                for (String suffix : SOURCES) {
                    String candidate = rootFor(name, suffix); if (candidate == null) continue;
                    if (root != null && !root.equals(candidate)) throw new IOException("conflicting Steam roots");
                    root = candidate; if (found.put(suffix, e) != null) throw new IOException("duplicate Steam input");
                }
            }
            Map<String, Path> extracted = new HashMap<>();
            for (String name : SOURCES) {
                Path dest = name.endsWith(".resource") ? work.resolve(name) : null;
                verifySource(steam, found.get(name), hashes.get(name), dest);
                if (dest != null) extracted.put(name, dest);
            }
            Map<String, Path> patched = new HashMap<>(); Map<String, Long> checksums = new HashMap<>();
            for (Map.Entry<String, List<Row>> item : byEntry.entrySet()) {
                ZipEntry entry = template.getEntry(item.getKey());
                if (entry == null || entry.isDirectory() || entry.getSize() < 0 || entry.getSize() > MAX_FILE) throw new IOException("missing template resource");
                Path dest = work.resolve("patched-" + patched.size()); CRC32 crc = new CRC32();
                try (InputStream in = template.getInputStream(entry); OutputStream file = Files.newOutputStream(dest); CheckedOutputStream out = new CheckedOutputStream(file, crc)) {
                    long pos = 0;
                    for (Row row : item.getValue()) {
                        if (row.target + row.size > entry.getSize()) throw new IOException("template range exceeds entry");
                        copyExact(in, out, row.target - pos, false, null); copyExact(in, null, row.size, true, null);
                        MessageDigest digest = sha();
                        try (RandomAccessFile src = new RandomAccessFile(extracted.get(row.source).toFile(), "r")) {
                            if (row.offset + row.size > src.length()) throw new IOException("source range exceeds entry");
                            src.seek(row.offset); byte[] buffer = new byte[65536]; int left = row.size;
                            while (left > 0) { int n = src.read(buffer, 0, Math.min(buffer.length, left)); if (n < 0) throw new EOFException(); out.write(buffer, 0, n); digest.update(buffer, 0, n); left -= n; }
                        }
                        if (!hex(digest.digest()).equals(row.hash)) throw new IOException("clip hash differs");
                        pos = row.target + row.size;
                    }
                    copyExact(in, out, entry.getSize() - pos, false, null); if (in.read() != -1) throw new IOException("oversized template resource");
                }
                patched.put(item.getKey(), dest); checksums.put(item.getKey(), crc.getValue());
            }
            Path temp = work.resolve("output.obb"); Set<String> seen = new HashSet<>(), restored = new HashSet<>(); long total = 0;
            try (ZipOutputStream out = new ZipOutputStream(new BufferedOutputStream(Files.newOutputStream(temp)))) {
                en = template.entries();
                while (en.hasMoreElements()) {
                    ZipEntry e = en.nextElement(); if (e.isDirectory()) continue; String name = e.getName(); safeName(name);
                    if (seen.size() >= MAX_ENTRIES || !seen.add(name) || e.getSize() < 0 || e.getSize() > MAX_FILE - total) throw new IOException("invalid template entry");
                    total += e.getSize(); Path patch = patched.get(name); long crc = patch == null ? e.getCrc() : checksums.get(name);
                    if (crc < 0) throw new IOException("missing CRC");
                    ZipEntry dest = new ZipEntry(name); dest.setTime(0); dest.setMethod(ZipEntry.STORED); dest.setSize(e.getSize()); dest.setCompressedSize(e.getSize()); dest.setCrc(crc); out.putNextEntry(dest);
                    try (InputStream in = patch == null ? template.getInputStream(e) : Files.newInputStream(patch)) { copyExact(in, out, e.getSize(), false, null); if (in.read() != -1) throw new IOException("oversized entry"); }
                    if (patch != null) restored.add(name); out.closeEntry();
                }
            }
            if (!restored.equals(byEntry.keySet())) throw new IOException("missing planned resource");
            Files.move(temp, target);
        } finally {
            try (java.util.stream.Stream<Path> paths = Files.walk(work)) {
                paths.sorted(Comparator.reverseOrder()).forEach(p -> { try { Files.deleteIfExists(p); } catch (IOException ignored) {} });
            }
        }
    }
    private SteamPackedAudio() {}
}
