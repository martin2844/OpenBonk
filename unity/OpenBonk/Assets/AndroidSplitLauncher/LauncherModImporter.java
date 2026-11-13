package com.megabonk.launcher;

import android.content.SharedPreferences;
import java.io.*;
import java.nio.ByteBuffer;
import java.nio.charset.CodingErrorAction;
import java.nio.charset.StandardCharsets;
import java.util.Enumeration;
import java.util.UUID;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;
import java.util.zip.CRC32;
import org.json.JSONObject;

/** Installs the same deliberately small package format as the Unity importer. */
final class LauncherModImporter {
    private static final int ZIP_LIMIT = 128 * 1024 * 1024, LEGACY_ZIP_LIMIT = 1024 * 1024, FILE_LIMIT = 65536;
    static String install(InputStream input, File root, SharedPreferences prefs) throws Exception {
        if (!root.isDirectory() && !root.mkdirs()) throw new IOException("storage unavailable");
        File temporary = new File(root, ".zip-" + UUID.randomUUID());
        File staging = new File(root, ".import-" + UUID.randomUUID());
        try {
            try (FileOutputStream out = new FileOutputStream(temporary)) { copy(input, out, ZIP_LIMIT); }
            try (ZipFile candidate = new ZipFile(temporary)) {
                ZipEntry entry = candidate.getEntry("manifest.json");
                String contentPrefix = "";
                if (entry == null) {
                    Enumeration<? extends ZipEntry> candidates = candidate.entries();
                    while (candidates.hasMoreElements()) {
                        ZipEntry possible = candidates.nextElement();
                        String name = possible.getName();
                        int slash = name.indexOf('/');
                        if (!possible.isDirectory() && slash > 0 && name.substring(slash + 1).equals("manifest.json") && validId(name.substring(0, slash))) {
                            if (entry != null) throw new IOException("multiple content manifests");
                            entry = possible; contentPrefix = name.substring(0, slash + 1);
                        }
                    }
                }
                if (entry != null && entry.getSize() > 0 && entry.getSize() <= FILE_LIMIT) {
                    byte[] bytes = readEntry(candidate, entry, FILE_LIMIT);
                    JSONObject json = new JSONObject(utf8(bytes));
                    if (json.optInt("api", 0) == 3) return LauncherContentImporter.install(candidate, root, prefs, contentPrefix);
                }
            }
            if (temporary.length() > LEGACY_ZIP_LIMIT) throw new IOException("package too large");
            byte[] manifest = null, script = null;
            String prefix = null, directory = null;
            int count = 0;
            try (ZipFile zip = new ZipFile(temporary)) {
                Enumeration<? extends ZipEntry> entries = zip.entries();
                while (entries.hasMoreElements()) {
                    ZipEntry entry = entries.nextElement();
                    if (++count > 3) throw new IOException("unexpected files");
                    String name = entry.getName();
                    if (name.isEmpty() || name.startsWith("/") || name.contains("\\")) throw new IOException("invalid path");
                    if (entry.isDirectory()) {
                        String dir = name.substring(0, name.length() - 1);
                        if (!validId(dir) || directory != null) throw new IOException("invalid directory");
                        directory = dir; continue;
                    }
                    String[] parts = name.split("/", -1);
                    if (parts.length > 2 || (parts.length == 2 && !validId(parts[0]))) throw new IOException("invalid path");
                    String nextPrefix = parts.length == 2 ? parts[0] : "";
                    if (prefix == null) prefix = nextPrefix;
                    else if (!prefix.equals(nextPrefix)) throw new IOException("files must share a directory");
                    byte[] bytes = readEntry(zip, entry, FILE_LIMIT);
                    String leaf = parts[parts.length - 1];
                    if (leaf.equals("manifest.json") && manifest == null) manifest = bytes;
                    else if (leaf.equals("main.lua") && script == null) script = bytes;
                    else throw new IOException("unexpected or duplicate file");
                }
            }
            if (manifest == null || script == null || (directory != null && !directory.equals(prefix))) throw new IOException("manifest.json and main.lua required");
            JSONObject json = new JSONObject(utf8(manifest));
            String id = json.optString("id", "");
            if (!validId(id) || !SplitDataLauncher.validModHooks(json) || (!prefix.isEmpty() && !prefix.equals(id))) throw new IOException("unsupported manifest or package ID");
            if (utf8(script).indexOf('\0') >= 0) throw new IOException("invalid script");
            File destination = new File(root, id);
            if (!staging.mkdir()) throw new IOException("cannot stage mod");
            try (FileOutputStream out = new FileOutputStream(new File(staging, "manifest.json"))) { out.write(manifest); out.getFD().sync(); }
            try (FileOutputStream out = new FileOutputStream(new File(staging, "main.lua"))) { out.write(script); out.getFD().sync(); }
            LauncherModRecovery.install(root, id, staging, prefs);
            return id;
        } finally {
            temporary.delete();
            if (staging.isDirectory()) {
                new File(staging, "manifest.json").delete(); new File(staging, "main.lua").delete(); staging.delete();
            }
        }
    }
    private static void copy(InputStream in, OutputStream out, int limit) throws IOException {
        byte[] buffer = new byte[4096]; int n, total = 0;
        while ((n = in.read(buffer)) != -1) { total += n; if (total > limit) throw new IOException("package too large"); out.write(buffer, 0, n); }
    }
    static byte[] readEntry(ZipFile zip, ZipEntry entry, int limit) throws IOException {
        long expected = entry.getSize();
        if (expected < 0 || expected > limit || entry.getCrc() < 0) throw new IOException("invalid ZIP entry size");
        CRC32 crc = new CRC32(); long total = 0;
        try (InputStream in = zip.getInputStream(entry); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[4096]; int n;
            while ((n = in.read(buffer)) != -1) {
                if (n == 0) continue;
                if (n > limit - total || n > expected - total) throw new IOException("ZIP entry too large");
                total += n; crc.update(buffer, 0, n); out.write(buffer, 0, n);
            }
            if (total != expected || crc.getValue() != entry.getCrc()) throw new IOException("ZIP entry damaged");
            return out.toByteArray();
        }
    }
    private static String utf8(byte[] bytes) throws Exception {
        return StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT).decode(ByteBuffer.wrap(bytes)).toString();
    }
    private static boolean validId(String id) {
        if (id == null || !id.matches("[A-Za-z0-9][A-Za-z0-9._-]{0,63}")) return false;
        return true;
    }
}
