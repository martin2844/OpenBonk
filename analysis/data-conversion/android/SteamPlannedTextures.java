package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** Experimental plan-based texture reconstruction; not a distributable build. */
public final class SteamPlannedTextures {
    private static final long MAX_FILE = 1024L * 1024 * 1024, MAX_TOTAL = 256L * 1024 * 1024;
    private static final int MAX_ENTRIES = 20000, MAX_RECORDS = 5000, MAX_CLIP = 64 * 1024 * 1024, MAX_SPLIT = 1024 * 1024;
    private static final String PREFIX = "assets/bin/Data/";
    private static final class Texture {
        String source, inputHash, outputHash, decodedHash; long inputOffset, convertedOffset;
        int inputSize, outputSize, width, height, mips, sourceFormat, targetFormat;
    }
    private static final class Fragment {
        Texture texture; String entry; int targetOffset, convertedOffset, size;
    }
    private static MessageDigest sha() {
        try { return MessageDigest.getInstance("SHA-256"); }
        catch (NoSuchAlgorithmException e) { throw new AssertionError(e); }
    }
    private static String hex(byte[] bytes) {
        char[] chars = "0123456789abcdef".toCharArray(), result = new char[bytes.length * 2];
        for (int i = 0; i < bytes.length; i++) { result[i * 2] = chars[(bytes[i] & 255) >>> 4]; result[i * 2 + 1] = chars[bytes[i] & 15]; }
        return new String(result);
    }
    private static String checkedHash(String value) throws IOException {
        if (value == null || !value.matches("[0-9a-f]{64}")) throw new IOException("invalid checksum");
        return value;
    }
    private static byte[] readPlan(File file) throws IOException {
        if (!file.isFile() || file.length() <= 0 || file.length() > 2 * 1024 * 1024) throw new IOException("invalid plan size");
        try (InputStream in = new FileInputStream(file); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] b = new byte[8192]; int count = 0;
            for (int n; (n = in.read(b)) != -1;) { if (n == 0) continue; count += n; if (count > 2 * 1024 * 1024) throw new IOException("plan too large"); out.write(b, 0, n); }
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
    private static void checkName(String name) throws IOException {
        if (name.isEmpty() || name.startsWith("/") || name.indexOf('\\') >= 0 || name.indexOf(':') >= 0) throw new IOException("unsafe ZIP name");
        for (String part : name.split("/", -1)) if (part.isEmpty() || part.equals(".") || part.equals("..")) throw new IOException("unsafe ZIP path");
    }
    private static String rootFor(String name, String suffix) {
        if (name.equals(suffix)) return "";
        if (name.equals("Megabonk_Data/" + suffix) || name.endsWith("/Megabonk_Data/" + suffix)) return name.substring(0, name.length() - suffix.length());
        return null;
    }
    private static void copyExact(InputStream in, OutputStream out, long size, MessageDigest h) throws IOException {
        byte[] buffer = new byte[65536];
        while (size > 0) {
            int n = in.read(buffer, 0, (int)Math.min(buffer.length, size)); if (n < 0) throw new EOFException("truncated input"); if (n == 0) continue;
            if (out != null) out.write(buffer, 0, n); if (h != null) h.update(buffer, 0, n); size -= n;
        }
    }
    private static int mipSize(int fmt, int w, int h) throws IOException {
        long n;
        if (fmt == 1 || fmt == 3 || fmt == 4) n = (long)w * h * fmt;
        else if (fmt == 10 || fmt == 12 || fmt == 25 || fmt == 45 || fmt == 47) n = (long)((w + 3) / 4) * ((h + 3) / 4) * (fmt == 10 || fmt == 45 ? 8 : 16);
        else throw new IOException("unsupported texture format");
        if (n <= 0 || n > MAX_CLIP) throw new IOException("invalid mip size"); return (int)n;
    }
    public static void restore(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output) throws IOException {
        restoreInternal(templateFile, steamFile, planFile, expectedPlanHash, output, false);
    }
    public static void restoreCubemaps(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output) throws IOException {
        restoreInternal(templateFile, steamFile, planFile, expectedPlanHash, output, true);
    }
    private static void restoreInternal(File templateFile, File steamFile, File planFile, String expectedPlanHash, File output, boolean cubemaps) throws IOException {
        Path target = output.toPath().toAbsolutePath(), parent = target.getParent();
        if (parent == null || !Files.isDirectory(parent) || Files.exists(target, LinkOption.NOFOLLOW_LINKS)) throw new IOException("output must be new");
        byte[] planBytes = readPlan(planFile);
        if (!hex(sha().digest(planBytes)).equals(checkedHash(expectedPlanHash))) throw new IOException("plan checksum differs");
        Map<String, String> sourceHashes = new HashMap<>(); Map<String, List<Fragment>> byEntry = new HashMap<>(); List<Texture> textures = new ArrayList<>(); String templateHash;
        try {
            JSONObject plan = new JSONObject(new String(planBytes, "UTF-8"));
            String kind = cubemaps ? "experimental-converted-steam-cubemap-offsets" : "experimental-converted-steam-texture-offsets";
            if (plan.getInt("schema") != 1 || !plan.getString("kind").equals(kind)) throw new IOException("unsupported texture plan");
            templateHash = checkedHash(plan.getString("template_sha256")); JSONObject sources = plan.getJSONObject("sources");
            Iterator<String> keys = sources.keys();
            while (keys.hasNext()) { String name = keys.next(); if (!name.matches("sharedassets[12]\\.assets(?:\\.resS)?")) throw new IOException("invalid Steam source"); sourceHashes.put(name, checkedHash(sources.getString(name))); }
            if (sourceHashes.isEmpty() || sourceHashes.size() > 4) throw new IOException("invalid source count");
            JSONArray rows = plan.getJSONArray("records"); long total = 0;
            if (rows.length() == 0 || rows.length() > (cubemaps ? 32 : MAX_RECORDS)) throw new IOException("invalid texture count");
            for (int i = 0; i < rows.length(); i++) {
                JSONObject row = rows.getJSONObject(i); Texture t = new Texture();
                if (row.getInt("id") != i) throw new IOException("invalid texture id");
                t.source = row.getString("source"); t.inputHash = checkedHash(row.getString("source_sha256")); t.outputHash = checkedHash(row.getString("target_sha256"));
                t.inputOffset = row.getLong("source_offset"); t.inputSize = row.getInt("source_size"); t.outputSize = row.getInt("target_size");
                t.sourceFormat = row.getInt("source_format");
                if (cubemaps) { t.width = 512; t.height = 512; t.mips = 6; t.targetFormat = 45; t.decodedHash = checkedHash(row.getString("decoded_sha256")); }
                else { t.width = row.getInt("width"); t.height = row.getInt("height"); t.mips = row.getInt("mips"); t.targetFormat = row.getInt("target_format"); }
                if (!sourceHashes.containsKey(t.source) || t.inputOffset < 0 || t.inputSize <= 0 || t.inputSize > MAX_CLIP || t.inputOffset > MAX_FILE - t.inputSize ||
                    t.outputSize <= 0 || t.outputSize > MAX_CLIP || t.width <= 0 || t.width > 4096 || t.height <= 0 || t.height > 4096 || t.mips <= 0 || t.mips > 32 ||
                    !(cubemaps ? (t.sourceFormat == 12 || t.sourceFormat == 29) : (t.sourceFormat == 1 || t.sourceFormat == 3 || t.sourceFormat == 4 || t.sourceFormat == 10 || t.sourceFormat == 12 || t.sourceFormat == 25)) ||
                    !(t.targetFormat == 4 || t.targetFormat == 45 || t.targetFormat == 47)) throw new IOException("invalid texture metadata");
                if (cubemaps && (!t.source.equals("sharedassets1.assets.resS") || t.inputSize > 4 * 1024 * 1024 || t.outputSize != 6 * 131072)) throw new IOException("invalid cubemap metadata");
                t.convertedOffset = total; total += t.outputSize; if (total > MAX_TOTAL) throw new IOException("texture limit exceeded");
                JSONArray fs = row.getJSONArray("fragments"); int position = 0;
                if (fs.length() == 0 || fs.length() > 128) throw new IOException("invalid fragment count");
                for (int j = 0; j < fs.length(); j++) {
                    JSONObject f = fs.getJSONObject(j); Fragment fragment = new Fragment(); fragment.texture = t;
                    fragment.entry = f.getString("entry"); fragment.targetOffset = f.getInt("target_offset"); fragment.convertedOffset = f.getInt("converted_offset"); fragment.size = f.getInt("size");
                    if (!fragment.entry.matches(PREFIX + (cubemaps ? "sharedassets1" : "sharedassets[12]") + "\\.assets\\.split(?:0|[1-9][0-9]*)") || fragment.convertedOffset != position || fragment.size <= 0 ||
                        fragment.targetOffset < 0 || fragment.targetOffset > MAX_SPLIT - fragment.size || fragment.size > t.outputSize - position) throw new IOException("invalid texture fragment");
                    position += fragment.size; byEntry.computeIfAbsent(fragment.entry, k -> new ArrayList<>()).add(fragment);
                }
                if (position != t.outputSize) throw new IOException("incomplete texture fragments"); textures.add(t);
            }
            for (List<Fragment> list : byEntry.values()) {
                list.sort(Comparator.comparingInt(f -> f.targetOffset)); int end = 0;
                for (Fragment f : list) { if (f.targetOffset < end) throw new IOException("overlapping texture fragments"); end = f.targetOffset + f.size; }
            }
        } catch (JSONException e) { throw new IOException("invalid texture plan", e); }
        if (!fileHash(templateFile).equals(templateHash) || !steamFile.isFile() || steamFile.length() <= 0 || steamFile.length() > MAX_FILE) throw new IOException("invalid template or Steam ZIP");
        Path work = Files.createTempDirectory(parent, ".openbonk-textures-");
        try (ZipFile steam = new ZipFile(steamFile); ZipFile template = new ZipFile(templateFile)) {
            Map<String, ZipEntry> found = new HashMap<>(); Set<String> names = new HashSet<>(); String root = null;
            Enumeration<? extends ZipEntry> entries = steam.entries();
            while (entries.hasMoreElements()) {
                ZipEntry entry = entries.nextElement(); if (entry.isDirectory()) continue; String name = entry.getName(); checkName(name);
                if (names.size() >= MAX_ENTRIES || !names.add(name)) throw new IOException("duplicate or excessive Steam entries");
                for (String suffix : sourceHashes.keySet()) {
                    String candidate = rootFor(name, suffix); if (candidate == null) continue;
                    if (root != null && !root.equals(candidate)) throw new IOException("conflicting Steam roots"); root = candidate;
                    if (found.put(suffix, entry) != null) throw new IOException("duplicate Steam input");
                }
            }
            Map<String, Path> sourceFiles = new HashMap<>();
            for (String name : sourceHashes.keySet()) {
                ZipEntry entry = found.get(name);
                if (entry == null || entry.getSize() < 0 || entry.getSize() > MAX_FILE) throw new IOException("missing Steam input");
                Path file = work.resolve(name); MessageDigest hash = sha();
                try (InputStream in = steam.getInputStream(entry); OutputStream out = Files.newOutputStream(file)) {
                    copyExact(in, out, entry.getSize(), hash); if (in.read() != -1 || !hex(hash.digest()).equals(sourceHashes.get(name))) throw new IOException("Steam input checksum differs");
                }
                sourceFiles.put(name, file);
            }
            Path convertedPath = work.resolve("converted.bin");
            try (OutputStream out = new BufferedOutputStream(Files.newOutputStream(convertedPath))) {
                for (Texture t : textures) {
                    byte[] input = new byte[t.inputSize];
                    try (RandomAccessFile src = new RandomAccessFile(sourceFiles.get(t.source).toFile(), "r")) {
                        if (t.inputOffset + t.inputSize > src.length()) throw new IOException("texture source out of bounds");
                        src.seek(t.inputOffset); src.readFully(input);
                    }
                    if (!hex(sha().digest(input)).equals(t.inputHash)) throw new IOException("texture source checksum differs");
                    if (cubemaps) {
                        try { if (t.sourceFormat == 29) input = SteamCubemapResources.unpackCrunch(input); }
                        catch (UnsatisfiedLinkError e) { throw new IOException("cubemap decoder unavailable", e); }
                        if (input == null || input.length != 6 * 262144 || !hex(sha().digest(input)).equals(t.decodedHash)) throw new IOException("cubemap decode differs");
                    }
                    MessageDigest hash = sha(); int inputPos = 0, outputPos = 0;
                    for (int level = 0; level < t.mips; level++) {
                        int w = cubemaps ? t.width : Math.max(1, t.width >> level), h = cubemaps ? t.height : Math.max(1, t.height >> level);
                        int sourceFormat = cubemaps ? 12 : t.sourceFormat;
                        int n = mipSize(sourceFormat, w, h), wanted = mipSize(t.targetFormat, w, h);
                        if (n > input.length - inputPos || wanted > t.outputSize - outputPos) throw new IOException("invalid mip bounds");
                        byte[] mip;
                        try { mip = SteamConvertedTextures.convertMip(Arrays.copyOfRange(input, inputPos, inputPos + n), w, h, sourceFormat, t.targetFormat); }
                        catch (UnsatisfiedLinkError e) { throw new IOException("texture converter unavailable", e); }
                        if (mip == null || mip.length != wanted) throw new IOException("texture conversion failed");
                        out.write(mip); hash.update(mip); inputPos += n; outputPos += wanted;
                    }
                    if (inputPos != input.length || outputPos != t.outputSize || !hex(hash.digest()).equals(t.outputHash)) throw new IOException("converted texture checksum differs");
                }
            }
            Path temp = work.resolve("output.obb"); Set<String> seen = new HashSet<>(), restored = new HashSet<>(); long total = 0;
            try (RandomAccessFile converted = new RandomAccessFile(convertedPath.toFile(), "r"); ZipOutputStream out = new ZipOutputStream(new BufferedOutputStream(Files.newOutputStream(temp)))) {
                entries = template.entries();
                while (entries.hasMoreElements()) {
                    ZipEntry entry = entries.nextElement(); if (entry.isDirectory()) continue; String name = entry.getName(); checkName(name);
                    if (seen.size() >= MAX_ENTRIES || !seen.add(name) || entry.getSize() < 0 || entry.getSize() > MAX_FILE - total) throw new IOException("invalid template entry");
                    total += entry.getSize(); List<Fragment> fs = byEntry.get(name); byte[] patched = null; long crc = entry.getCrc();
                    if (fs != null) {
                        if (entry.getSize() > MAX_SPLIT) throw new IOException("texture split too large");
                        patched = new byte[(int)entry.getSize()];
                        try (DataInputStream in = new DataInputStream(template.getInputStream(entry))) { in.readFully(patched); if (in.read() != -1) throw new IOException("oversized split"); }
                        for (Fragment f : fs) {
                            if (f.targetOffset > patched.length - f.size) throw new IOException("texture fragment out of bounds");
                            for (int i = f.targetOffset; i < f.targetOffset + f.size; i++) if (patched[i] != 0) throw new IOException("texture template not blank");
                            converted.seek(f.texture.convertedOffset + f.convertedOffset); converted.readFully(patched, f.targetOffset, f.size);
                        }
                        CRC32 sum = new CRC32(); sum.update(patched); crc = sum.getValue(); restored.add(name);
                    }
                    if (crc < 0) throw new IOException("missing template CRC");
                    ZipEntry dest = new ZipEntry(name); dest.setTime(0); dest.setMethod(ZipEntry.STORED); dest.setSize(entry.getSize()); dest.setCompressedSize(entry.getSize()); dest.setCrc(crc); out.putNextEntry(dest);
                    if (patched != null) out.write(patched);
                    else try (InputStream in = template.getInputStream(entry)) { copyExact(in, out, entry.getSize(), null); if (in.read() != -1) throw new IOException("oversized entry"); }
                    out.closeEntry();
                }
            }
            if (!restored.equals(byEntry.keySet())) throw new IOException("missing texture template entries");
            Files.move(temp, target);
        } finally {
            try (java.util.stream.Stream<Path> paths = Files.walk(work)) {
                paths.sorted(Comparator.reverseOrder()).forEach(p -> { try { Files.deleteIfExists(p); } catch (IOException ignored) {} });
            }
        }
    }
    private SteamPlannedTextures() {}
}
