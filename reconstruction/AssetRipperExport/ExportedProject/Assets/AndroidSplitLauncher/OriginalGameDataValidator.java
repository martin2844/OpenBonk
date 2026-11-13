package com.megabonk.launcher;

import java.io.File;
import java.util.zip.ZipFile;

/** Validates a compatible local Steam ZIP before replacing pending data. */
final class OriginalGameDataValidator {
    static final class Problem {
        final String file; final boolean missing;
        Problem(String file, boolean missing) { this.file = file; this.missing = missing; }
    }

    // Diagnostic only: acceptance continues to use valid().
    static Problem problem(File file) {
        String linux = "StreamingAssets/aa/StandaloneLinux64/", windows = "StreamingAssets/aa/StandaloneWindows64/";
        String[] bundles = OriginalLocalizationManifest.BUNDLES;
        java.util.zip.ZipEntry asset = null, texture = null;
        java.util.zip.ZipEntry[] entries = new java.util.zip.ZipEntry[bundles.length];
        boolean[] isWindows = new boolean[bundles.length];
        try (ZipFile zip = new ZipFile(file)) {
            java.util.Enumeration<? extends java.util.zip.ZipEntry> all = zip.entries();
            java.util.HashSet<String> seen = new java.util.HashSet<>();
            while (all.hasMoreElements()) {
                java.util.zip.ZipEntry entry = all.nextElement(); String name = entry.getName();
                if (seen.size() >= 20000 || name.startsWith("/") || name.contains("\\") || name.contains(":") || !seen.add(name)) return null;
                for (String part : name.split("/")) if (part.equals(".") || part.equals("..")) return null;
                if (matches(name, "sharedassets1.assets")) { if (asset != null) return null; asset = entry; }
                if (matches(name, "sharedassets1.assets.resS")) { if (texture != null) return null; texture = entry; }
                for (int i = 0; i < bundles.length; i++) {
                    boolean win = matches(name, windows + bundles[i]);
                    if (win || matches(name, linux + bundles[i])) {
                        if (entries[i] != null) return null;
                        entries[i] = entry; isWindows[i] = win;
                    }
                }
            }
            if (asset == null || asset.isDirectory()) return new Problem("sharedassets1.assets", true);
            if (texture == null || texture.isDirectory()) return new Problem("sharedassets1.assets.resS", true);
            for (int i = 0; i < bundles.length; i++) if (entries[i] == null || entries[i].isDirectory()) return new Problem(bundles[i], true);
            long length = asset.getSize();
            if (length != 50251684L && length != 56239204L) return new Problem("sharedassets1.assets", false);
            if (texture.getSize() != 113115008L) return new Problem("sharedassets1.assets.resS", false);
            String assetHash = length == 50251684L ? "64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a" : "d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35";
            if (!contentValid(zip, asset, assetHash)) return new Problem("sharedassets1.assets", false);
            if (!contentValid(zip, texture, "43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea")) return new Problem("sharedassets1.assets.resS", false);
            for (int i = 0; i < bundles.length; i++) {
                if (entries[i].getSize() <= 0 || entries[i].getSize() > 8L * 1024 * 1024 ||
                    !contentValid(zip, entries[i], isWindows[i] ? OriginalLocalizationManifest.WINDOWS_SHA256[i] : OriginalLocalizationManifest.SHA256[i])) return new Problem(bundles[i], false);
            }
        } catch (Exception e) { return null; }
        return null;
    }

    private static boolean matches(String name, String suffix) {
        return name.equals(suffix) || name.equals("Megabonk_Data/" + suffix) || name.endsWith("/Megabonk_Data/" + suffix);
    }

    private static boolean contentValid(ZipFile zip, java.util.zip.ZipEntry entry, String hash) {
        try { return OriginalZipContentCheck.valid(zip, entry, hash); } catch (Exception e) { return false; }
    }
    // Classification for a helpful picker message only. This does not verify
    // the archive and must never be used to authorize an import.
    static boolean looksLikeSteamZip(File file) {
        boolean assets = false, textures = false;
        try (ZipFile zip = new ZipFile(file)) {
            java.util.Enumeration<? extends java.util.zip.ZipEntry> entries = zip.entries();
            int count = 0;
            while (entries.hasMoreElements()) {
                if (++count > 20000) return false;
                java.util.zip.ZipEntry entry = entries.nextElement();
                if (entry.isDirectory()) continue;
                String name = entry.getName();
                if (name.equals("sharedassets1.assets") || name.equals("Megabonk_Data/sharedassets1.assets") || name.endsWith("/Megabonk_Data/sharedassets1.assets")) assets = true;
                if (name.equals("sharedassets1.assets.resS") || name.equals("Megabonk_Data/sharedassets1.assets.resS") || name.endsWith("/Megabonk_Data/sharedassets1.assets.resS")) textures = true;
            }
            return assets && textures;
        } catch (Exception e) { return false; }
    }

    static boolean valid(File file) {
        String prefix = "StreamingAssets/aa/StandaloneLinux64/";
        String windowsPrefix = "StreamingAssets/aa/StandaloneWindows64/";
        String[] bundles = OriginalLocalizationManifest.BUNDLES;
        String[] bundleHashes = OriginalLocalizationManifest.SHA256;
        String[] windowsHashes = OriginalLocalizationManifest.WINDOWS_SHA256;
        if (bundles.length != bundleHashes.length || bundles.length != windowsHashes.length) return false;
        int[] found = new int[bundles.length]; int count = 0, assets = 0, textureData = 0;
        java.util.zip.ZipEntry assetEntry = null, textureEntry = null;
        java.util.zip.ZipEntry[] bundleEntries = new java.util.zip.ZipEntry[bundles.length];
        boolean[] windowsBundle = new boolean[bundles.length];
        try (ZipFile zip = new ZipFile(file)) {
            java.util.Enumeration<? extends java.util.zip.ZipEntry> entries = zip.entries();
            while (entries.hasMoreElements()) {
                java.util.zip.ZipEntry entry = entries.nextElement();
                if (++count > 20000) return false;
                String name = entry.getName();
                if (name.startsWith("/") || name.contains("\\") || name.contains(":")) return false;
                for (String part : name.split("/")) if (part.equals(".") || part.equals("..")) return false;
                if (name.equals("sharedassets1.assets") || name.equals("Megabonk_Data/sharedassets1.assets") || name.endsWith("/Megabonk_Data/sharedassets1.assets")) {
                    if ((entry.getSize() != 56239204L && entry.getSize() != 50251684L) || ++assets != 1) return false;
                    assetEntry = entry;
                }
                if (name.equals("sharedassets1.assets.resS") || name.equals("Megabonk_Data/sharedassets1.assets.resS") || name.endsWith("/Megabonk_Data/sharedassets1.assets.resS")) {
                    if (entry.getSize() != 113115008L || ++textureData != 1) return false;
                    textureEntry = entry;
                }
                for (int i = 0; i < bundles.length; i++) {
                    String expected = prefix + bundles[i];
                    String windowsExpected = windowsPrefix + bundles[i];
                    boolean fromWindows = name.equals(windowsExpected) || name.equals("Megabonk_Data/" + windowsExpected) || name.endsWith("/Megabonk_Data/" + windowsExpected);
                    if (fromWindows || name.equals(expected) || name.equals("Megabonk_Data/" + expected) || name.endsWith("/Megabonk_Data/" + expected)) {
                        if (entry.getSize() <= 0 || entry.getSize() > 8L * 1024 * 1024 || ++found[i] != 1) return false;
                        bundleEntries[i] = entry;
                        windowsBundle[i] = fromWindows;
                    }
                }
            }
            for (int n : found) if (n != 1) return false;
            if (assets != 1 || textureData != 1) return false;
            String assetHash = assetEntry.getSize() == 50251684L
                ? "64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a"
                : "d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35";
            if (!OriginalZipContentCheck.valid(zip, assetEntry, assetHash) ||
                !OriginalZipContentCheck.valid(zip, textureEntry, "43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea")) return false;
            for (int i = 0; i < bundleEntries.length; i++)
                if (!OriginalZipContentCheck.valid(zip, bundleEntries[i], windowsBundle[i] ? windowsHashes[i] : bundleHashes[i])) return false;
            return true;
        } catch (Exception e) { return false; }
    }

    private OriginalGameDataValidator() {}
}
