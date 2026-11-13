package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** Experimental offset-based mesh buffer restoration. Not a release build. */
public final class SteamPlannedMeshes {
    private static final long MAX_FILE = 1024L * 1024 * 1024, MAX_TOTAL = 256L * 1024 * 1024;
    private static final int MAX_SPLIT = 1024 * 1024, MAX_OBJECT_ENTRY = 32 * 1024 * 1024, MAX_ENTRIES = 20000, MAX_ROWS = 10000;
    private static final class Fragment { String entry, source, hash, maskedHash, originalHash; byte[] mask; long sourceOffset; int targetOffset, size; }
    private static MessageDigest sha() {
        try { return MessageDigest.getInstance("SHA-256"); } catch (NoSuchAlgorithmException e) { throw new AssertionError(e); }
    }
    private static String hex(byte[] bytes) {
        char[] digits = "0123456789abcdef".toCharArray(), out = new char[bytes.length * 2];
        for (int i = 0; i < bytes.length; i++) { out[i * 2] = digits[(bytes[i] & 255) >>> 4]; out[i * 2 + 1] = digits[bytes[i] & 15]; }
        return new String(out);
    }
    private static String checkedHash(String value) throws IOException {
        if (value == null || !value.matches("[0-9a-f]{64}")) throw new IOException("invalid checksum"); return value;
    }
    private static byte[] planBytes(File file) throws IOException {
        if (!file.isFile() || file.length() <= 0 || file.length() > 8 * 1024 * 1024) throw new IOException("invalid plan size");
        try (InputStream in = new FileInputStream(file); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] b = new byte[8192]; int total = 0;
            for (int n; (n = in.read(b)) != -1;) { if (n == 0) continue; total += n; if (total > 8 * 1024 * 1024) throw new IOException("plan too large"); out.write(b, 0, n); }
            return out.toByteArray();
        }
    }
    private static String fileHash(File file) throws IOException {
        if (!file.isFile() || file.length() <= 0 || file.length() > MAX_FILE) throw new IOException("invalid input size");
        MessageDigest h = sha(); byte[] b = new byte[65536]; long total = 0;
        try (InputStream in = new FileInputStream(file)) {
            for (int n; (n = in.read(b)) != -1;) { if (n == 0) continue; total += n; if (total > MAX_FILE) throw new IOException("input too large"); h.update(b, 0, n); }
        }
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
    private static void copyExact(InputStream in, OutputStream out, long size, MessageDigest digest) throws IOException {
        byte[] buffer = new byte[65536];
        while (size > 0) {
            int n = in.read(buffer, 0, (int)Math.min(buffer.length, size)); if (n < 0) throw new EOFException("truncated input"); if (n == 0) continue;
            if (out != null) out.write(buffer, 0, n); if (digest != null) digest.update(buffer, 0, n); size -= n;
        }
    }
    public static void restore(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output) throws IOException {
        restoreInternal(templateFile, steamFile, planFile, expectedPlanHash, output, 0);
    }
    public static void restoreAnimations(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output) throws IOException {
        restoreInternal(templateFile, steamFile, planFile, expectedPlanHash, output, 1);
    }
    public static void restoreExactObjects(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output) throws IOException {
        restoreInternal(templateFile, steamFile, planFile, expectedPlanHash, output, 2);
    }
    private static void restoreInternal(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output, int mode) throws IOException {
        boolean animations = mode == 1, objects = mode == 2;
        Path target = output.toPath().toAbsolutePath(), parent = target.getParent();
        if (parent == null || !Files.isDirectory(parent) || Files.exists(target, LinkOption.NOFOLLOW_LINKS)) throw new IOException("output must be new");
        byte[] data = planBytes(planFile);
        if (!hex(sha().digest(data)).equals(checkedHash(expectedPlanHash))) throw new IOException("plan checksum differs");
        String templateHash; Map<String, String> sourceHashes = new HashMap<>(); Map<String, List<Fragment>> byEntry = new HashMap<>();
        try {
            JSONObject plan = new JSONObject(new String(data, "UTF-8"));
            String kind = animations ? "experimental-animation-fragment-mask" : objects ? "experimental-exact-steam-object-offsets" : "experimental-steam-mesh-buffer-offsets";
            if (plan.getInt("schema") != 1 || !plan.getString("kind").equals(kind)) throw new IOException("unsupported asset plan");
            templateHash = checkedHash(plan.getString("template_sha256")); JSONObject sources = plan.getJSONObject("sources");
            Iterator<String> keys = sources.keys();
            while (keys.hasNext()) {
                String name = keys.next(); if (!name.matches(objects ? "(?:sharedassets[0-9]+\\.assets|level[0-9]+|resources\\.assets|StreamingAssets/aa/StandaloneLinux64/localization-(?:assets-shared_assets_all|string-tables-[a-z0-9()_-]+_assets_all)\\.bundle)" : "(?:sharedassets[0-9]+\\.assets|level[0-9]+)(?:\\.resS)?")) throw new IOException("invalid source name");
                if (animations && !(name.equals("sharedassets1.assets") || name.equals("sharedassets2.assets"))) throw new IOException("invalid animation source");
                sourceHashes.put(name, checkedHash(sources.getString(name)));
            }
            if (sourceHashes.isEmpty() || sourceHashes.size() > 32) throw new IOException("invalid source count");
            JSONArray rows = plan.getJSONArray("records"); long total = 0;
            if (rows.length() <= 0 || rows.length() > MAX_ROWS) throw new IOException("invalid mesh record count");
            for (int i = 0; i < rows.length(); i++) {
                JSONObject row = rows.getJSONObject(i); Fragment f = new Fragment();
                f.entry = row.getString("entry"); f.source = row.getString("source");
                if (animations) { f.maskedHash = checkedHash(row.getString("masked_sha256")); f.originalHash = checkedHash(row.getString("original_sha256")); }
                else f.hash = checkedHash(row.getString("sha256"));
                f.targetOffset = row.getInt("target_offset"); f.sourceOffset = row.getLong("source_offset"); f.size = row.getInt("size");
                String pattern = animations ? "assets/bin/Data/(?:[0-9a-f]{32}|(?:sharedassets[0-9]+\\.assets|level[0-9]+|resources\\.assets)(?:\\.split(?:0|[1-9][0-9]*))?)" : objects ?
                        "(?:assets/bin/Data/(?:[0-9a-f]{32}|(?:sharedassets[0-9]+\\.assets|level[0-9]+|resources\\.assets)(?:\\.split(?:0|[1-9][0-9]*))?)|assets/aa/StandaloneLinux64/localization-(?:assets-shared_assets_all|string-tables-[a-z0-9()_-]+_assets_all)\\.bundle)" :
                        "assets/bin/Data/sharedassets[12]\\.assets\\.split(?:0|[1-9][0-9]*)";
                int entryLimit = (objects || animations) && !f.entry.matches(".*\\.split(?:0|[1-9][0-9]*)") ? MAX_OBJECT_ENTRY : MAX_SPLIT;
                if (!f.entry.matches(pattern) || !sourceHashes.containsKey(f.source) ||
                        f.size <= 0 || f.size > entryLimit || f.targetOffset < 0 || f.targetOffset > entryLimit - f.size || f.sourceOffset < 0 || f.sourceOffset > MAX_FILE - f.size) throw new IOException("invalid mesh fragment");
                if (animations) {
                    try { f.mask = Base64.getDecoder().decode(row.getString("mask")); }
                    catch (IllegalArgumentException e) { throw new IOException("invalid animation mask", e); }
                    if (f.mask.length != (f.size + 7) / 8 || ((f.size & 7) != 0 && ((f.mask[f.mask.length - 1] & 255) >>> (f.size & 7)) != 0)) throw new IOException("invalid animation mask size");
                    int count = 0; for (byte b : f.mask) count += Integer.bitCount(b & 255);
                    if (count <= 0 || count != row.getInt("masked_bytes")) throw new IOException("invalid animation mask count");
                }
                total += f.size; if (total > MAX_TOTAL) throw new IOException("mesh limit exceeded");
                byEntry.computeIfAbsent(f.entry, k -> new ArrayList<>()).add(f);
            }
            for (List<Fragment> fs : byEntry.values()) {
                fs.sort(Comparator.comparingInt(f -> f.targetOffset)); int end = 0;
                for (Fragment f : fs) { if (f.targetOffset < end) throw new IOException("overlapping mesh fragments"); end = f.targetOffset + f.size; }
            }
        } catch (JSONException e) { throw new IOException("invalid mesh plan", e); }
        if (!fileHash(templateFile).equals(templateHash) || !steamFile.isFile() || steamFile.length() <= 0 || steamFile.length() > MAX_FILE) throw new IOException("invalid template or Steam ZIP");
        Path work = Files.createTempDirectory(parent, ".openbonk-meshes-");
        try (ZipFile steam = new ZipFile(steamFile); ZipFile template = new ZipFile(templateFile)) {
            Map<String, ZipEntry> found = new HashMap<>(); Set<String> names = new HashSet<>(); String root = null;
            Enumeration<? extends ZipEntry> entries = steam.entries();
            while (entries.hasMoreElements()) {
                ZipEntry e = entries.nextElement(); if (e.isDirectory()) continue; String name = e.getName(); safeName(name);
                if (names.size() >= MAX_ENTRIES || !names.add(name)) throw new IOException("duplicate or excessive Steam entries");
                for (String suffix : sourceHashes.keySet()) {
                    String candidate = rootFor(name, suffix); if (candidate == null) continue;
                    if (root != null && !root.equals(candidate)) throw new IOException("conflicting Steam roots"); root = candidate;
                    if (found.put(suffix, e) != null) throw new IOException("duplicate Steam input");
                }
            }
            Map<String, Path> files = new HashMap<>();
            for (String name : sourceHashes.keySet()) {
                ZipEntry e = found.get(name); if (e == null || e.getSize() < 0 || e.getSize() > MAX_FILE) throw new IOException("missing Steam input");
                Path path = work.resolve(name); Files.createDirectories(path.getParent()); MessageDigest h = sha();
                try (InputStream in = steam.getInputStream(e); OutputStream out = Files.newOutputStream(path)) {
                    copyExact(in, out, e.getSize(), h); if (in.read() != -1 || !hex(h.digest()).equals(sourceHashes.get(name))) throw new IOException("Steam input checksum differs");
                }
                files.put(name, path);
            }
            Path temp = work.resolve("output.obb"); Set<String> seen = new HashSet<>(), restored = new HashSet<>(); long total = 0;
            try (ZipOutputStream out = new ZipOutputStream(new BufferedOutputStream(Files.newOutputStream(temp)))) {
                entries = template.entries();
                while (entries.hasMoreElements()) {
                    ZipEntry entry = entries.nextElement(); if (entry.isDirectory()) continue; String name = entry.getName(); safeName(name);
                    if (seen.size() >= MAX_ENTRIES || !seen.add(name) || entry.getSize() < 0 || entry.getSize() > MAX_FILE - total) throw new IOException("invalid template entry");
                    total += entry.getSize(); List<Fragment> fs = byEntry.get(name); byte[] patched = null; long crc = entry.getCrc();
                    if (fs != null) {
                        long entryLimit = (objects || animations) && !name.matches(".*\\.split(?:0|[1-9][0-9]*)") ? MAX_OBJECT_ENTRY : MAX_SPLIT;
                        if (entry.getSize() > entryLimit) throw new IOException("asset entry too large");
                        patched = new byte[(int)entry.getSize()];
                        try (DataInputStream in = new DataInputStream(template.getInputStream(entry))) { in.readFully(patched); if (in.read() != -1) throw new IOException("oversized split"); }
                        for (Fragment f : fs) {
                            if (f.targetOffset > patched.length - f.size) throw new IOException("mesh target out of bounds");
                            if (animations) {
                                MessageDigest h = sha(); h.update(patched, f.targetOffset, f.size);
                                if (!hex(h.digest()).equals(f.maskedHash)) throw new IOException("animation template differs");
                            } else for (int i = f.targetOffset; i < f.targetOffset + f.size; i++) if (patched[i] != 0) throw new IOException("mesh template not blank");
                            byte[] sourceBytes = animations ? new byte[f.size] : null;
                            try (RandomAccessFile source = new RandomAccessFile(files.get(f.source).toFile(), "r")) {
                                if (f.sourceOffset + f.size > source.length()) throw new IOException("mesh source out of bounds");
                                source.seek(f.sourceOffset);
                                if (animations) source.readFully(sourceBytes); else source.readFully(patched, f.targetOffset, f.size);
                            }
                            if (animations) for (int i = 0; i < f.size; i++) if ((f.mask[i / 8] & (1 << (i % 8))) != 0) {
                                if (patched[f.targetOffset + i] != 0 || sourceBytes[i] == 0) throw new IOException("invalid masked animation byte");
                                patched[f.targetOffset + i] = sourceBytes[i];
                            }
                            MessageDigest h = sha(); h.update(patched, f.targetOffset, f.size);
                            if (!hex(h.digest()).equals(animations ? f.originalHash : f.hash)) throw new IOException("asset fragment checksum differs");
                        }
                        CRC32 sum = new CRC32(); sum.update(patched); crc = sum.getValue(); restored.add(name);
                    }
                    if (crc < 0) throw new IOException("missing template CRC");
                    ZipEntry dest = new ZipEntry(name); dest.setTime(0); dest.setMethod(ZipEntry.STORED); dest.setSize(entry.getSize()); dest.setCompressedSize(entry.getSize()); dest.setCrc(crc); out.putNextEntry(dest);
                    if (patched != null) out.write(patched);
                    else try (InputStream in = template.getInputStream(entry)) { copyExact(in, out, entry.getSize(), null); if (in.read() != -1) throw new IOException("oversized ZIP entry"); }
                    out.closeEntry();
                }
            }
            if (!restored.equals(byEntry.keySet())) throw new IOException("missing planned mesh entries");
            Files.move(temp, target);
        } finally {
            try (java.util.stream.Stream<Path> paths = Files.walk(work)) {
                paths.sorted(Comparator.reverseOrder()).forEach(p -> { try { Files.deleteIfExists(p); } catch (IOException ignored) {} });
            }
        }
    }
    private SteamPlannedMeshes() {}
}
