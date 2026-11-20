package com.megabonk.launcher;

import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.*;
import java.util.function.Consumer;
import java.util.zip.*;
import org.json.*;

/** Run with a valid Nimbo content ZIP; bundle bytes remain untouched. */
public class StreetProfileCheck {
    static JSONObject street(JSONObject manifest) { return manifest.getJSONObject("map").getJSONArray("stageEnvironments").getJSONObject(0).getJSONObject("streetscape"); }
    static JSONObject environment(JSONObject manifest) { return manifest.getJSONObject("map").getJSONArray("stageEnvironments").getJSONObject(0); }
    static Path rewrite(Path source, Path destination, Consumer<JSONObject> mutation) throws Exception {
        try (ZipFile zip = new ZipFile(source.toFile()); ZipOutputStream out = new ZipOutputStream(Files.newOutputStream(destination))) {
            for (ZipEntry entry : Collections.list(zip.entries())) {
                byte[] bytes;
                try (var input = zip.getInputStream(entry)) { bytes = input.readAllBytes(); }
                if (entry.getName().equals("manifest.json")) {
                    var manifest = new JSONObject(new String(bytes, StandardCharsets.UTF_8));
                    mutation.accept(manifest); bytes = manifest.toString().getBytes(StandardCharsets.UTF_8);
                }
                out.putNextEntry(new ZipEntry(entry.getName())); out.write(bytes); out.closeEntry();
            }
        }
        return destination;
    }
    static String install(Path zip, Path root, RecoveryCheck.Prefs prefs) throws Exception {
        try (var input = Files.newInputStream(zip)) { return LauncherModImporter.install(input, root.toFile(), prefs); }
    }
    public static void main(String[] args) throws Exception {
        Path scratch = Files.createTempDirectory("street-profile-check-");
        Path root = Files.createDirectory(scratch.resolve("Mods"));
        Path valid = rewrite(Path.of(args[0]), scratch.resolve("valid.zip"), manifest -> {
            manifest.put("api", 4);
            var environment = environment(manifest); environment.remove("rooftops");
            environment.put("streetscape", new JSONObject().put("tileWidth", 26).put("stepHeight", 6).put("coverage", .95)
                .put("frontages", new JSONArray().put("NimboRamenFront").put("NimboGachaCorner")));
            environment.put("moaiVisual", "MewtwoStatue");
        });
        var prefs = new RecoveryCheck.Prefs(); String id = install(valid, root, prefs);
        Path installed = root.resolve(id).resolve("manifest.json");
        byte[] before = Files.readAllBytes(installed); prefs.state.put(id, true);
        if (!LauncherModIntegrity.valid(root.resolve(id).toFile(), id)) throw new AssertionError("API4 integrity/recovery support");
        List<Consumer<JSONObject>> invalid = Arrays.asList(
            m -> m.put("api", 3),
            m -> street(m).put("tileWidth", "26"),
            m -> street(m).put("tileWidth", 26.000001),
            m -> street(m).put("tileWidth", 19),
            m -> street(m).put("tileWidth", 41),
            m -> street(m).put("stepHeight", 0),
            m -> street(m).put("stepHeight", 11),
            m -> street(m).put("stepHeight", "6"),
            m -> street(m).put("coverage", "0.95"),
            m -> street(m).put("coverage", -1),
            m -> street(m).put("coverage", 1.001),
            m -> street(m).remove("coverage"),
            m -> street(m).put("frontages", new JSONArray()),
            m -> street(m).put("frontages", new JSONArray().put("NimboRamenFront").put("NimboRamenFront")),
            m -> street(m).put("frontages", new JSONArray().put(1)),
            m -> street(m).put("frontages", new JSONArray().put(" ")),
            m -> street(m).put("frontages", new JSONArray().put("\u2000\u00a0")),
            m -> environment(m).put("streetscape", JSONObject.NULL),
            m -> environment(m).put("streetscape", true),
            m -> environment(m).put("replaceDecorations", false),
            m -> environment(m).put("rooftops", new JSONObject().put("roofFraction", .7).put("bridgeFraction", .3).put("riseFraction", .06)),
            m -> environment(m).remove("architectureMaterial"),
            m -> environment(m).put("moaiVisual", 123),
            m -> environment(m).put("moaiVisual", new JSONObject().put("prefab", "MewtwoStatue")),
            m -> environment(m).put("moaiVisual", "M".repeat(121)),
            m -> m.getJSONObject("map").put("stageDurations", JSONObject.NULL),
            m -> m.getJSONObject("map").put("stageDurations", "600,540,480"),
            m -> m.getJSONObject("map").put("stageDurations", new JSONArray()),
            m -> m.getJSONObject("map").put("stageDurations", new JSONArray().put(600).put(540)),
            m -> m.getJSONObject("map").put("stageDurations", new JSONArray().put(600).put(540).put(480).put(420)),
            m -> m.getJSONObject("map").put("stageDurations", new JSONArray().put("600").put(540).put(480)),
            m -> m.getJSONObject("map").put("stageDurations", new JSONArray().put(true).put(540).put(480)),
            m -> m.getJSONObject("map").put("stageDurations", new JSONArray().put(600).put(59).put(480)),
            m -> m.getJSONObject("map").put("stageDurations", new JSONArray().put(600).put(540).put(1801)));
        for (var mutation : invalid) {
            boolean rejected = false;
            try { install(rewrite(valid, scratch.resolve("invalid.zip"), mutation), root, prefs); }
            catch (IOException expected) { rejected = true; }
            if (!rejected || !Arrays.equals(before, Files.readAllBytes(installed)) || !prefs.getBoolean(id, false))
                throw new AssertionError("invalid profile changed installed data or enabled state");
        }
        // Isolate the new visual from streets, creatures and architecture: it
        // must independently request the bundle, just like other visual fields.
        Path moaiOnly = rewrite(valid, scratch.resolve("moai-only.zip"), m -> {
            var map = m.getJSONObject("map");
            map.remove("objects"); map.remove("enemies"); map.remove("encounters");
            map.put("stageEnvironments", new JSONArray().put(new JSONObject().put("stage", 1).put("moaiVisual", "MewtwoStatue")));
        });
        Path moaiRoot = Files.createDirectories(scratch.resolve("moai-only-app").resolve("Mods"));
        var moaiPrefs = new RecoveryCheck.Prefs();
        if (!install(moaiOnly, moaiRoot, moaiPrefs).equals(id) || !LauncherModIntegrity.valid(moaiRoot.resolve(id).toFile(), id))
            throw new AssertionError("standalone moai visual with bundle must import");
        Path moaiManifest = moaiRoot.resolve(id).resolve("manifest.json");
        byte[] moaiBefore = Files.readAllBytes(moaiManifest); moaiPrefs.state.put(id, true);
        boolean bundleRejected = false;
        try { install(rewrite(moaiOnly, scratch.resolve("moai-no-bundle.zip"), m -> m.getJSONObject("map").remove("bundle")), moaiRoot, moaiPrefs); }
        catch (IOException expected) { bundleRejected = "map bundle must match declared assets".equals(expected.getMessage()); }
        if (!bundleRejected || !Arrays.equals(moaiBefore, Files.readAllBytes(moaiManifest)) || !moaiPrefs.getBoolean(id, false))
            throw new AssertionError("standalone moai visual must require a bundle without changing installed state");
        install(rewrite(valid, scratch.resolve("legacy.zip"), m -> {
            m.put("api", 3);
            var environments = m.getJSONObject("map").getJSONArray("stageEnvironments");
            for (int i=0;i<environments.length();i++) environments.getJSONObject(i).remove("streetscape");
        }), root, prefs);
        System.out.println("PASS: API4 street ZIP dispatch/integrity; " + invalid.size() + " malformed updates preserve installed bytes/state; standalone moai visual requires its bundle; API3 still imports. Scratch: " + scratch);
    }
}
