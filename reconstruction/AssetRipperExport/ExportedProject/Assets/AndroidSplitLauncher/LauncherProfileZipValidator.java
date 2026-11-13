package com.megabonk.launcher;

import java.io.*;
import java.nio.ByteBuffer;
import java.nio.charset.CodingErrorAction;
import java.nio.charset.StandardCharsets;
import java.util.Base64;
import java.util.zip.*;
import javax.crypto.Cipher;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import org.json.JSONArray;
import org.json.JSONObject;
import org.json.JSONTokener;

/** Checks profile archive integrity and core fields before replacing a pending ZIP. */
final class LauncherProfileZipValidator {
    private static final long ZIP_LIMIT = 16L * 1024 * 1024, FILE_LIMIT = 8L * 1024 * 1024;
    // Same save format used by the game. This is a format key, not an account secret.
    private static final byte[] KEY = {(byte)0xd9,0x40,(byte)0x84,0x0d,0x5a,(byte)0xe7,(byte)0xc7,(byte)0x90,0x7b,0x09,0x24,0x37,(byte)0xbc,0x0c,0x5b,0x44,(byte)0xaa,(byte)0xf7,0x0e,0x27,0x3e,0x12,(byte)0xd0,(byte)0xfb,0x4d,(byte)0xa2,(byte)0xb8,(byte)0xc7,0x67,(byte)0xcc,(byte)0x91,0x1d};
    private static final byte[] IV = {0x37,(byte)0x86,0x4e,(byte)0xf1,0x5c,0x24,(byte)0xbc,0x0a,(byte)0xcb,(byte)0xc6,0x0e,0x39,0x78,(byte)0xef,0x1f,0x06};

    static boolean valid(File file) {
        if (!file.isFile() || file.length() <= 0 || file.length() > ZIP_LIMIT) return false;
        try (ZipFile zip = new ZipFile(file)) {
            ZipEntry progression = null, stats = null; String parent = null; int count = 0;
            java.util.Enumeration<? extends ZipEntry> entries = zip.entries();
            while (entries.hasMoreElements()) {
                ZipEntry entry = entries.nextElement();
                if (++count > 128) return false;
                String name = entry.getName();
                if (name.startsWith("/") || name.contains("\\") || name.contains(":")) return false;
                for (String part : name.split("/")) if (part.equals(".") || part.equals("..")) return false;
                if (entry.isDirectory()) continue;
                String leaf = name.substring(name.lastIndexOf('/') + 1);
                if (!leaf.equals("progression.json") && !leaf.equals("stats.json")) continue;
                if (entry.getSize() <= 0 || entry.getSize() > FILE_LIMIT || entry.getCrc() < 0) return false;
                String prefix = name.substring(0, name.length() - leaf.length());
                if (parent == null) parent = prefix; else if (!parent.equals(prefix)) return false;
                if (leaf.equals("progression.json")) { if (progression != null) return false; progression = entry; }
                else { if (stats != null) return false; stats = entry; }
            }
            if (progression == null || stats == null) return false;
            JSONObject p = decrypt(read(zip, progression)), s = decrypt(read(zip, stats));
            Object silver = p.opt("silver");
            return (silver instanceof Integer || silver instanceof Long) && p.opt("achievements") instanceof JSONArray &&
                optionalObject(p, "shopItems") && optionalObject(p, "characterProgression") && s.opt("stats") instanceof JSONObject &&
                optionalObject(s, "times") && optionalObject(s, "enemyLogs");
        } catch (Exception e) { return false; }
    }

    private static boolean optionalObject(JSONObject json, String name) {
        return !json.has(name) || json.opt(name) instanceof JSONObject;
    }

    private static String read(ZipFile zip, ZipEntry entry) throws IOException {
        ByteArrayOutputStream out = new ByteArrayOutputStream(); CRC32 crc = new CRC32(); long total = 0;
        try (InputStream in = zip.getInputStream(entry)) {
            byte[] buffer = new byte[65536];
            for (int n; (n = in.read(buffer)) != -1;) {
                if (n == 0) continue;
                if (n > FILE_LIMIT - total || n > entry.getSize() - total) throw new IOException("profile entry too large");
                total += n; crc.update(buffer, 0, n); out.write(buffer, 0, n);
            }
        }
        if (total != entry.getSize() || crc.getValue() != entry.getCrc()) throw new IOException("profile entry damaged");
        return StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT).decode(ByteBuffer.wrap(out.toByteArray())).toString();
    }

    private static JSONObject decrypt(String text) throws Exception {
        StringBuilder encoded = new StringBuilder(text.length());
        for (int i = 0; i < text.length(); i++) {
            char c = text.charAt(i); if (c != ' ' && c != '\t' && c != '\r' && c != '\n') encoded.append(c);
        }
        Cipher cipher = Cipher.getInstance("AES/CBC/PKCS5Padding");
        cipher.init(Cipher.DECRYPT_MODE, new SecretKeySpec(KEY, "AES"), new IvParameterSpec(IV));
        byte[] plain = cipher.doFinal(Base64.getDecoder().decode(encoded.toString()));
        String json = StandardCharsets.UTF_8.newDecoder().onMalformedInput(CodingErrorAction.REPORT).onUnmappableCharacter(CodingErrorAction.REPORT).decode(ByteBuffer.wrap(plain)).toString();
        JSONTokener tokener = new JSONTokener(json); Object parsed = tokener.nextValue();
        if (!(parsed instanceof JSONObject) || tokener.nextClean() != 0) throw new IOException("invalid profile JSON");
        return (JSONObject) parsed;
    }
    private LauncherProfileZipValidator() {}
}
