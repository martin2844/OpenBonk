package com.megabonk.launcher;

import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import org.json.JSONObject;

/** Exercises real API 3 validation and promotion with the example item. */
public final class ContentFolderCheck {
    private static byte[] wrap(Path original, String prefix, boolean extra, boolean corrupt) throws Exception {
        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        try (ZipFile input = new ZipFile(original.toFile()); ZipOutputStream output = new ZipOutputStream(bytes)) {
            if (!prefix.isEmpty()) { output.putNextEntry(new ZipEntry(prefix)); output.closeEntry(); }
            Enumeration<? extends ZipEntry> entries = input.entries();
            while (entries.hasMoreElements()) {
                ZipEntry entry = entries.nextElement();
                output.putNextEntry(new ZipEntry(prefix + entry.getName()));
                byte[] data; try (InputStream in = input.getInputStream(entry)) { data = in.readAllBytes(); }
                if (corrupt && entry.getName().equals("main.lua")) data[0] ^= 1;
                output.write(data); output.closeEntry();
            }
            if (extra) { output.putNextEntry(new ZipEntry("outside.txt")); output.write(1); output.closeEntry(); }
        }
        return bytes.toByteArray();
    }
    private static String install(byte[] zip, Path mods, RecoveryCheck.Prefs prefs) throws Exception {
        return LauncherModImporter.install(new ByteArrayInputStream(zip), mods.toFile(), prefs);
    }
    private static byte[] changeWeapon(byte[] zip, String effect, boolean addColor) throws Exception {
        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        try (ZipInputStream input = new ZipInputStream(new ByteArrayInputStream(zip)); ZipOutputStream output = new ZipOutputStream(bytes)) {
            for (ZipEntry entry; (entry = input.getNextEntry()) != null;) {
                byte[] data = input.readAllBytes();
                if (entry.getName().equals("manifest.json")) {
                    JSONObject manifest = new JSONObject(new String(data, java.nio.charset.StandardCharsets.UTF_8));
                    JSONObject weapon = manifest.getJSONObject("weapon");
                    weapon.put("effect", effect);
                    if (addColor) weapon.put("lightningColor", "#00AAFF");
                    data = manifest.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8);
                }
                output.putNextEntry(new ZipEntry(entry.getName())); output.write(data); output.closeEntry();
            }
        }
        return bytes.toByteArray();
    }
    private static void check(boolean value, String message) { if (!value) throw new AssertionError(message); }
    public static void main(String[] args) throws Exception {
        Path root = Files.createTempDirectory("openbonk-mod-folder-");
        Path mods = root.resolve("Mods"), original = Path.of(args[0]);
        RecoveryCheck.Prefs prefs = new RecoveryCheck.Prefs();
        String id = install(wrap(original, "", false, false), mods, prefs);
        byte[] before = Files.readAllBytes(mods.resolve(id).resolve("main.lua"));
        prefs.state.put(id, true);
        check(id.equals(install(wrap(original, "example-folder/", false, false), mods, prefs)), "folder import");
        check(Arrays.equals(before, Files.readAllBytes(mods.resolve(id).resolve("main.lua"))), "same script");
        check(!prefs.getBoolean(id, true), "update is disabled");
        prefs.state.put(id, true);
        for (byte[] invalid : new byte[][] {wrap(original, "folder/", true, false), wrap(original, "folder/", false, true)}) {
            try { install(invalid, mods, prefs); throw new AssertionError("invalid package accepted"); }
            catch (IOException expected) { }
            check(Arrays.equals(before, Files.readAllBytes(mods.resolve(id).resolve("main.lua"))), "failed update changed script");
            check(prefs.getBoolean(id, false), "failed update changed enabled state");
        }
        try (ZipFile input = new ZipFile(original.toFile())) {
            JSONObject manifest;
            try (InputStream in = input.getInputStream(input.getEntry("manifest.json"))) {
                manifest = new JSONObject(new String(in.readAllBytes(), java.nio.charset.StandardCharsets.UTF_8));
            }
            JSONObject weapon = manifest.optJSONObject("weapon");
            if (weapon != null && weapon.optString("effect").equals("arrow")) {
                byte[] valid = wrap(original, "", false, false);
                for (byte[] invalid : new byte[][] {changeWeapon(valid, "unrecognized", false), changeWeapon(valid, "arrow", true)}) {
                    try { install(invalid, mods, prefs); throw new AssertionError("invalid weapon accepted"); }
                    catch (IOException expected) { }
                    check(Arrays.equals(before, Files.readAllBytes(mods.resolve(id).resolve("main.lua"))), "invalid weapon update changed script");
                    check(prefs.getBoolean(id, false), "invalid weapon update changed enabled state");
                }
                System.out.println("PASS arrow package and invalid-effect/color update rejection");
            }
        }
        System.out.println("PASS root and wrapped packages, outside file and altered content rejection");
    }
}
