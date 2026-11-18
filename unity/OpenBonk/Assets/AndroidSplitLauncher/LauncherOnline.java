package com.megabonk.launcher;

import android.content.Context;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.pm.Signature;
import java.io.*;
import java.net.URL;
import java.security.MessageDigest;
import java.util.Arrays;
import java.util.HashSet;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;
import javax.net.ssl.HttpsURLConnection;
import org.json.JSONObject;
import org.json.JSONArray;

/** Public metadata and downloads only; game data and profiles are never uploaded. */
final class LauncherOnline {
    static final String ROOT = "https://raw.githubusercontent.com/martin2844/OpenBonk/main/";
    static final String CATALOG = ROOT + "mods/catalog/index.json", UPDATES = ROOT + "updates/android.json";
    static final String RELEASES = "https://api.github.com/repos/martin2844/OpenBonk/releases?per_page=10";
    private static HttpsURLConnection connect(String address) throws Exception {
        for (int redirect = 0; redirect < 6; redirect++) {
            URL url = new URL(address);
            String host = url.getHost().toLowerCase(java.util.Locale.ROOT);
            if (!url.getProtocol().equals("https") || url.getUserInfo() != null || url.getPort() != -1 ||
                !(host.equals("github.com") || host.equals("api.github.com") || host.equals("raw.githubusercontent.com") || host.equals("release-assets.githubusercontent.com") || host.equals("objects.githubusercontent.com")))
                throw new IOException("untrusted download URL");
            HttpsURLConnection c = (HttpsURLConnection) url.openConnection();
            c.setInstanceFollowRedirects(false); c.setConnectTimeout(15000); c.setReadTimeout(30000);
            c.setRequestProperty("Accept", host.equals("api.github.com") ? "application/vnd.github+json" : "application/octet-stream");
            c.setRequestProperty("User-Agent", "OpenBonk-Android");
            int code = c.getResponseCode();
            if (code >= 300 && code < 400) {
                String location = c.getHeaderField("Location"); c.disconnect();
                if (location == null) throw new IOException("invalid redirect");
                address = new URL(url, location).toString(); continue;
            }
            if (code != 200) { c.disconnect(); throw new IOException("HTTP " + code); }
            return c;
        }
        throw new IOException("too many redirects");
    }
    static JSONObject metadata(String address) throws Exception {
        HttpsURLConnection c = connect(address);
        try (InputStream in = c.getInputStream(); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] b = new byte[4096]; int n, total = 0;
            while ((n = in.read(b)) != -1) { total += n; if (total > 262144) throw new IOException("metadata too large"); out.write(b, 0, n); }
            JSONObject result = new JSONObject(new String(out.toByteArray(), "UTF-8"));
            if (result.optInt("schema") != 1) throw new IOException("unsupported metadata version");
            return result;
        } finally { c.disconnect(); }
    }
    static JSONArray releases() throws Exception {
        HttpsURLConnection c = connect(RELEASES);
        try (InputStream in = c.getInputStream(); ByteArrayOutputStream out = new ByteArrayOutputStream()) {
            byte[] b = new byte[4096]; int n, total = 0;
            while ((n = in.read(b)) != -1) { total += n; if (total > 524288) throw new IOException("release history too large"); out.write(b, 0, n); }
            return new JSONArray(new String(out.toByteArray(), "UTF-8"));
        } finally { c.disconnect(); }
    }
    static void download(JSONObject info, File dest, long limit) throws Exception {
        long size = info.getLong("size"); String expected = info.getString("sha256");
        if (size <= 0 || size > limit || !expected.matches("[0-9a-f]{64}")) throw new IOException("invalid download metadata");
        HttpsURLConnection c = connect(info.getString("url"));
        boolean valid = false;
        try (InputStream in = c.getInputStream(); FileOutputStream out = new FileOutputStream(dest)) {
            MessageDigest digest = MessageDigest.getInstance("SHA-256"); byte[] b = new byte[32768]; int n; long total = 0;
            while ((n = in.read(b)) != -1) { total += n; if (total > size) throw new IOException("download exceeds expected size"); digest.update(b, 0, n); out.write(b, 0, n); }
            StringBuilder hex = new StringBuilder(); for (byte v : digest.digest()) hex.append(String.format(java.util.Locale.ROOT, "%02x", v & 255));
            if (total != size || !expected.equals(hex.toString())) throw new IOException("download checksum mismatch");
            out.getFD().sync(); valid = true;
        } finally { c.disconnect(); if (!valid) dest.delete(); }
    }
    static void checkModId(File zip, String expected) throws Exception {
        if (!expected.matches("[a-zA-Z0-9_-]{1,64}")) throw new IOException("invalid mod ID");
        try (ZipFile f = new ZipFile(zip)) {
            ZipEntry e = f.getEntry("manifest.json");
            if (e == null) e = f.getEntry(expected + "/manifest.json");
            if (e == null) throw new IOException("missing mod manifest");
            JSONObject manifest = new JSONObject(new String(LauncherModImporter.readEntry(f, e, 65536), "UTF-8"));
            if (!expected.equals(manifest.optString("id"))) throw new IOException("catalog and mod IDs differ");
        }
    }
    @SuppressWarnings("deprecation") static void checkApk(Context ctx, File file, long version) throws Exception {
        PackageManager pm = ctx.getPackageManager();
        PackageInfo old = pm.getPackageInfo(ctx.getPackageName(), PackageManager.GET_SIGNATURES);
        PackageInfo next = pm.getPackageArchiveInfo(file.getAbsolutePath(), PackageManager.GET_SIGNATURES);
        if (next == null || !ctx.getPackageName().equals(next.packageName) || next.versionCode != version || next.versionCode <= old.versionCode || old.signatures == null || next.signatures == null)
            throw new IOException("update package or version does not match");
        HashSet<Signature> a = new HashSet<>(Arrays.asList(old.signatures)), b = new HashSet<>(Arrays.asList(next.signatures));
        if (!a.equals(b)) throw new IOException("update signing certificate differs");
    }
}
