package com.megabonk.launcher;

import java.io.*;
import java.nio.ByteBuffer;
import java.nio.charset.CodingErrorAction;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.security.MessageDigest;
import java.util.HashSet;
import java.util.Locale;
import org.json.JSONArray;
import org.json.JSONObject;

/** Bounded checks used before choosing which interrupted mod version to keep. */
final class LauncherModIntegrity {
    private static final long MAX_FILE = 64L * 1024 * 1024, MAX_TOTAL = 128L * 1024 * 1024;
    static boolean valid(File directory, String id) {
        try {
            if (Files.isSymbolicLink(directory.toPath()) || !Files.isDirectory(directory.toPath(), LinkOption.NOFOLLOW_LINKS)) return false;
            File manifestFile = new File(directory, "manifest.json"), script = new File(directory, "main.lua");
            if (!regular(manifestFile) || manifestFile.length() <= 0 || manifestFile.length() > 65536 || !regular(script) || script.length() > 65536) return false;
            String text = StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT)
                    .decode(ByteBuffer.wrap(read(manifestFile, 65536))).toString();
            JSONObject manifest = new JSONObject(text);
            if (!id.equals(manifest.optString("id", ""))) return false;
            int api = manifest.optInt("api", 0);
            if (api == 1 || api == 2) return true; // Legacy manifests do not include file hashes.
            if (api != 3 || !"content".equals(manifest.optString("kind", ""))) return false;
            JSONArray files = manifest.optJSONArray("files");
            if (files == null || files.length() < 2 || files.length() > 64) return false;
            String root = directory.getCanonicalPath() + File.separator;
            HashSet<String> paths = new HashSet<>(); long total = 0; boolean hasScript = false;
            for (int i = 0; i < files.length(); i++) {
                JSONObject row = files.optJSONObject(i); if (row == null) return false;
                String path = row.optString("path", ""), hash = row.optString("sha256", "");
                long size = row.optLong("size", -1);
                if (path.isEmpty() || path.startsWith("/") || path.contains("\\") || path.contains(":") ||
                        !paths.add(path.toLowerCase(Locale.ROOT)) || !hash.matches("[a-fA-F0-9]{64}") || size <= 0 || size > MAX_FILE || size > MAX_TOTAL - total) return false;
                for (String part : path.split("/", -1)) if (part.isEmpty() || part.equals(".") || part.equals("..")) return false;
                File file = new File(directory, path);
                if (!regular(file) || !file.getCanonicalPath().startsWith(root) || file.length() != size || !digest(file).equalsIgnoreCase(hash)) return false;
                total += size; if (path.equals("main.lua")) hasScript = true;
            }
            return hasScript;
        } catch (Exception e) { return false; }
    }
    private static boolean regular(File file) { return !Files.isSymbolicLink(file.toPath()) && Files.isRegularFile(file.toPath(), LinkOption.NOFOLLOW_LINKS); }
    private static byte[] read(File file, int limit) throws IOException {
        try (InputStream in = new FileInputStream(file); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] buffer = new byte[4096]; int n;
            while ((n = in.read(buffer)) != -1) { if (out.size() + n > limit) throw new IOException("file too large"); out.write(buffer, 0, n); }
            return out.toByteArray();
        }
    }
    private static String digest(File file) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        try (InputStream in = new FileInputStream(file)) { byte[] buf = new byte[65536]; int n; while ((n = in.read(buf)) != -1) if (n > 0) digest.update(buf, 0, n); }
        StringBuilder result = new StringBuilder(64); for (byte b : digest.digest()) result.append(String.format(Locale.ROOT, "%02x", b & 255)); return result.toString();
    }
    private LauncherModIntegrity() {}
}
