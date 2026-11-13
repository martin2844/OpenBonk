package com.openbonk.dataprep;

import java.io.ByteArrayOutputStream;
import java.io.File;
import java.io.IOException;
import java.io.InputStream;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.Enumeration;
import java.util.HashSet;
import java.util.Set;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Extracts one pinned PC clip in memory; this does not construct an OBB. */
public final class SteamBufferingClip {
    private static final long RESOURCE_SIZE = 60845700L, CLIP_OFFSET = 60836960L;
    private static final int CLIP_SIZE = 8740, WINDOWS_PATCH = 252;
    private static final String LINUX_ASSET = "d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35";
    private static final String WINDOWS_ASSET = "64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a";
    private static final String LINUX_RESOURCE = "28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c";
    private static final String WINDOWS_RESOURCE = "29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d";
    private static final String CLIP_HASH = "020a724dccd35bfd6d2cbab07b40e6a8b4be19dc3433dbdf37f185f49f276be1";

    private static MessageDigest digest() {
        try { return MessageDigest.getInstance("SHA-256"); }
        catch (NoSuchAlgorithmException e) { throw new AssertionError(e); }
    }
    private static String hex(byte[] data) {
        char[] digits = "0123456789abcdef".toCharArray(), result = new char[data.length * 2];
        for (int i = 0; i < data.length; i++) { result[2*i] = digits[(data[i] & 255) >>> 4]; result[2*i+1] = digits[data[i] & 15]; }
        return new String(result);
    }
    private static String rootFor(String path, String filename) {
        if (path.equals(filename)) return "";
        if (path.equals("Megabonk_Data/" + filename) || path.endsWith("/Megabonk_Data/" + filename))
            return path.substring(0, path.length() - filename.length());
        return null;
    }
    private static String hashEntry(ZipFile zip, ZipEntry entry, long expectedSize, ByteArrayOutputStream clip) throws IOException {
        if (entry.isDirectory() || entry.getSize() != expectedSize) throw new IOException("unexpected entry size");
        MessageDigest hash = digest(); byte[] buffer = new byte[65536]; long count = 0;
        try (InputStream in = zip.getInputStream(entry)) {
            for (int n; (n = in.read(buffer)) != -1;) {
                if (n == 0) continue;
                if (n > expectedSize - count) throw new IOException("entry exceeds expected size");
                hash.update(buffer, 0, n);
                if (clip != null) {
                    long start = Math.max(count, CLIP_OFFSET), end = Math.min(count + n, CLIP_OFFSET + CLIP_SIZE);
                    if (start < end) clip.write(buffer, (int)(start - count), (int)(end - start));
                }
                count += n;
            }
        }
        if (count != expectedSize) throw new IOException("truncated entry");
        return hex(hash.digest());
    }

    public static byte[] extract(File archive) throws IOException {
        if (!archive.isFile() || archive.length() <= 0 || archive.length() > 1024L * 1024 * 1024)
            throw new IOException("unsupported archive size");
        try (ZipFile zip = new ZipFile(archive)) {
            ZipEntry asset = null, resource = null; String assetRoot = null, resourceRoot = null;
            Set<String> names = new HashSet<>(); Enumeration<? extends ZipEntry> entries = zip.entries();
            while (entries.hasMoreElements()) {
                ZipEntry entry = entries.nextElement(); String name = entry.getName();
                if (names.size() >= 20000 || name.startsWith("/") || name.indexOf('\\') >= 0 || name.indexOf(':') >= 0 || !names.add(name))
                    throw new IOException("unsafe or duplicate archive entry");
                for (String part : name.split("/")) if (part.equals(".") || part.equals("..") || part.isEmpty()) throw new IOException("unsafe archive path");
                String root = rootFor(name, "sharedassets1.assets");
                if (root != null) { if (asset != null) throw new IOException("multiple asset roots"); asset = entry; assetRoot = root; }
                root = rootFor(name, "sharedassets1.resource");
                if (root != null) { if (resource != null) throw new IOException("multiple resource roots"); resource = entry; resourceRoot = root; }
            }
            if (asset == null || resource == null || !assetRoot.equals(resourceRoot)) throw new IOException("missing or conflicting game data");
            boolean windows;
            if (asset.getSize() == 50251684L) windows = true;
            else if (asset.getSize() == 56239204L) windows = false;
            else throw new IOException("unsupported game asset");
            if (!hashEntry(zip, asset, asset.getSize(), null).equals(windows ? WINDOWS_ASSET : LINUX_ASSET))
                throw new IOException("incompatible game asset");
            ByteArrayOutputStream result = new ByteArrayOutputStream(CLIP_SIZE);
            if (!hashEntry(zip, resource, RESOURCE_SIZE, result).equals(windows ? WINDOWS_RESOURCE : LINUX_RESOURCE))
                throw new IOException("incompatible game resource");
            byte[] clip = result.toByteArray();
            if (clip.length != CLIP_SIZE) throw new IOException("short clip");
            if (windows) for (int i = 0; i < 7; i++) {
                if ((clip[WINDOWS_PATCH + i] & 255) != 0xb4) throw new IOException("unexpected video metadata");
                clip[WINDOWS_PATCH + i] = 0x70;
            }
            MessageDigest hash = digest(); hash.update(clip);
            if (!hex(hash.digest()).equals(CLIP_HASH)) throw new IOException("clip checksum differs");
            return clip;
        }
    }
    private SteamBufferingClip() {}
}
