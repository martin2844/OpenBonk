package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** Experimental restoration of generated offset-only audio plans. */
public final class SteamGeneratedAudio {
    private static final long MAX_ARCHIVE = 1024L * 1024 * 1024;
    private static final long MAX_AUDIO = 32L * 1024 * 1024;
    private static final int MAX_CLIP = 8 * 1024 * 1024;
    private static final int MAX_ENTRIES = 20000;

    private static final class Clip {
        String entry, hash;
        long offset;
        int size, filled;
        byte[] payload;
    }
    private static MessageDigest digest() {
        try { return MessageDigest.getInstance("SHA-256"); }
        catch (NoSuchAlgorithmException e) { throw new AssertionError(e); }
    }
    private static String hex(byte[] data) {
        char[] digits = "0123456789abcdef".toCharArray(), out = new char[data.length * 2];
        for (int i = 0; i < data.length; i++) { out[i * 2] = digits[(data[i] & 255) >>> 4]; out[i * 2 + 1] = digits[data[i] & 15]; }
        return new String(out);
    }
    private static String checkedHash(String value) throws IOException {
        if (value == null || !value.matches("[0-9a-f]{64}")) throw new IOException("invalid checksum");
        return value;
    }
    private static byte[] readSmall(File file, int limit) throws IOException {
        if (!file.isFile() || file.length() <= 0 || file.length() > limit) throw new IOException("invalid input size");
        try (InputStream in = new FileInputStream(file); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[8192]; int total = 0;
            for (int n; (n = in.read(buffer)) != -1;) { if (n == 0) continue; if (n > limit - total) throw new IOException("input too large"); out.write(buffer, 0, n); total += n; }
            return out.toByteArray();
        }
    }
    private static String fileHash(File file) throws IOException {
        if (!file.isFile() || file.length() <= 0 || file.length() > MAX_ARCHIVE) throw new IOException("invalid archive size");
        MessageDigest hash = digest(); long total = 0; byte[] buffer = new byte[65536];
        try (InputStream in = new FileInputStream(file)) {
            for (int n; (n = in.read(buffer)) != -1;) { if (n == 0) continue; total += n; if (total > MAX_ARCHIVE) throw new IOException("archive too large"); hash.update(buffer, 0, n); }
        }
        return hex(hash.digest());
    }
    private static void checkName(String name) throws IOException {
        if (name.isEmpty() || name.startsWith("/") || name.indexOf('\\') >= 0 || name.indexOf(':') >= 0) throw new IOException("unsafe ZIP name");
        for (String part : name.split("/", -1)) if (part.equals(".") || part.equals("..") || part.isEmpty()) throw new IOException("unsafe ZIP path");
    }
    private static String rootFor(String name, String suffix) {
        if (name.equals(suffix)) return "";
        if (name.equals("Megabonk_Data/" + suffix) || name.endsWith("/Megabonk_Data/" + suffix)) return name.substring(0, name.length() - suffix.length());
        return null;
    }
    private static void hashEntry(ZipFile zip, ZipEntry entry, String expected, List<Clip> clips) throws IOException {
        if (entry == null || entry.isDirectory() || entry.getSize() < 0 || entry.getSize() > MAX_ARCHIVE) throw new IOException("invalid Steam input");
        MessageDigest hash = digest(); byte[] buffer = new byte[65536]; long offset = 0, limit = entry.getSize();
        try (InputStream in = zip.getInputStream(entry)) {
            for (int n; (n = in.read(buffer)) != -1;) {
                if (n == 0) continue;
                if (n > limit - offset) throw new IOException("Steam input too large");
                hash.update(buffer, 0, n);
                if (clips != null) for (Clip clip : clips) {
                    long from = Math.max(offset, clip.offset), to = Math.min(offset + n, clip.offset + clip.size);
                    if (from < to) { int size = (int)(to - from); System.arraycopy(buffer, (int)(from - offset), clip.payload, (int)(from - clip.offset), size); clip.filled += size; }
                }
                offset += n;
            }
        }
        if (offset != limit || !hex(hash.digest()).equals(expected)) throw new IOException("Steam input checksum differs");
    }
    public static void restore(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output) throws IOException {
        Path target = output.toPath().toAbsolutePath(), parent = target.getParent();
        if (parent == null || !Files.isDirectory(parent) || Files.exists(target, LinkOption.NOFOLLOW_LINKS)) throw new IOException("output must be a new file");
        byte[] planBytes = readSmall(planFile, 1024 * 1024);
        if (!hex(digest().digest(planBytes)).equals(checkedHash(expectedPlanHash))) throw new IOException("plan checksum differs");
        JSONObject plan;
        try { plan = new JSONObject(new String(planBytes, "UTF-8")); }
        catch (JSONException e) { throw new IOException("invalid audio plan", e); }
        List<Clip> clips = new ArrayList<>(); Map<String, Clip> records = new HashMap<>();
        String assetHash, resourceHash, templateHash;
        try {
            if (plan.getInt("schema") != 1 || !plan.getString("kind").equals("experimental-steam-audio-offsets")) throw new IOException("unsupported audio plan");
            assetHash = checkedHash(plan.getString("asset_sha256")); resourceHash = checkedHash(plan.getString("resource_sha256")); templateHash = checkedHash(plan.getString("template_sha256"));
            JSONArray rows = plan.getJSONArray("records"); long total = 0;
            if (rows.length() == 0 || rows.length() > 1000) throw new IOException("invalid audio count");
            for (int i = 0; i < rows.length(); i++) {
                JSONObject row = rows.getJSONObject(i); Clip clip = new Clip(); clip.entry = row.getString("entry");
                clip.offset = row.getLong("source_offset"); clip.size = row.getInt("size"); clip.hash = checkedHash(row.getString("sha256"));
                if (!clip.entry.matches("assets/bin/Data/[0-9a-f]{32}\\.resource") || records.containsKey(clip.entry) || clip.offset < 0 || clip.size <= 0 || clip.size > MAX_CLIP || clip.offset > MAX_ARCHIVE - clip.size) throw new IOException("invalid audio record");
                total += clip.size; if (total > MAX_AUDIO) throw new IOException("audio limit exceeded");
                clip.payload = new byte[clip.size]; clips.add(clip); records.put(clip.entry, clip);
            }
        } catch (JSONException e) { throw new IOException("invalid audio plan fields", e); }
        if (!fileHash(templateFile).equals(templateHash) || !steamFile.isFile() || steamFile.length() <= 0 || steamFile.length() > MAX_ARCHIVE) throw new IOException("template or Steam ZIP differs");
        try (ZipFile steam = new ZipFile(steamFile); ZipFile template = new ZipFile(templateFile)) {
            ZipEntry asset = null, resource = null; String commonRoot = null; Set<String> steamNames = new HashSet<>();
            Enumeration<? extends ZipEntry> entries = steam.entries();
            while (entries.hasMoreElements()) {
                ZipEntry e = entries.nextElement(); String name = e.getName();
                if (e.isDirectory()) continue; checkName(name);
                if (steamNames.size() >= MAX_ENTRIES || !steamNames.add(name)) throw new IOException("duplicate or excessive Steam entries");
                for (String suffix : new String[]{"sharedassets1.assets", "sharedassets1.resource"}) {
                    String root = rootFor(name, suffix);
                    if (root == null) continue;
                    if (commonRoot != null && !commonRoot.equals(root)) throw new IOException("conflicting Steam roots");
                    commonRoot = root;
                    if (suffix.endsWith(".assets")) { if (asset != null) throw new IOException("duplicate Steam asset"); asset = e; }
                    else { if (resource != null) throw new IOException("duplicate Steam resource"); resource = e; }
                }
            }
            if (asset == null || resource == null) throw new IOException("missing Steam inputs");
            hashEntry(steam, asset, assetHash, null); hashEntry(steam, resource, resourceHash, clips);
            for (Clip clip : clips) if (clip.filled != clip.size || !hex(digest().digest(clip.payload)).equals(clip.hash)) throw new IOException("audio payload differs");
            Path temporary = Files.createTempFile(parent, ".openbonk-audio-", ".tmp"); boolean moved = false;
            try {
                Set<String> seen = new HashSet<>(), restored = new HashSet<>(); long total = 0;
                try (ZipOutputStream out = new ZipOutputStream(new BufferedOutputStream(Files.newOutputStream(temporary)))) {
                    Enumeration<? extends ZipEntry> all = template.entries(); byte[] buffer = new byte[65536];
                    while (all.hasMoreElements()) {
                        ZipEntry e = all.nextElement(); String name = e.getName();
                        if (e.isDirectory()) continue; checkName(name);
                        if (seen.size() >= MAX_ENTRIES || !seen.add(name) || e.getSize() < 0 || e.getSize() > MAX_ARCHIVE - total) throw new IOException("invalid template entry");
                        total += e.getSize(); Clip clip = records.get(name); ZipEntry dest = new ZipEntry(name); dest.setTime(0); out.putNextEntry(dest);
                        try (InputStream in = template.getInputStream(e)) {
                            long count = 0;
                            for (int n; (n = in.read(buffer)) != -1;) {
                                if (n == 0) continue; count += n; if (count > e.getSize()) throw new IOException("template entry too large");
                                if (clip == null) out.write(buffer, 0, n);
                                else for (int i = 0; i < n; i++) if (buffer[i] != 0) throw new IOException("audio template not blank");
                            }
                            if (count != e.getSize() || clip != null && count != clip.size) throw new IOException("template entry length differs");
                        }
                        if (clip != null) { out.write(clip.payload); restored.add(name); }
                        out.closeEntry();
                    }
                }
                if (!restored.equals(records.keySet())) throw new IOException("audio template missing entries");
                Files.move(temporary, target); moved = true;
            } finally { if (!moved) Files.deleteIfExists(temporary); }
        }
    }
    private SteamGeneratedAudio() {}
}
