package com.megabonk.launcher;

import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.util.Arrays;
import java.util.zip.ZipFile;
import org.json.*;

/** Actual release files, with the launcher's current enable helper extracted by the runner. */
public class ActualStreetPackageCheck {
    private static byte[] manifest(Path zip) throws Exception {
        try (var archive = new ZipFile(zip.toFile()); var input = archive.getInputStream(archive.getEntry("manifest.json"))) { return input.readAllBytes(); }
    }
    private static String install(Path zip, Path mods, RecoveryCheck.Prefs prefs) throws Exception {
        try (var input = Files.newInputStream(zip)) { return LauncherModImporter.install(input, mods.toFile(), prefs); }
    }
    private static void require(boolean value, String message) { if (!value) throw new AssertionError(message); }
    public static void main(String[] args) throws Exception {
        var oldZip = Path.of(args[0]); var newZip = Path.of(args[1]);
        byte[] before = manifest(oldZip), after = manifest(newZip);
        var legacy = new JSONObject(new String(before, StandardCharsets.UTF_8));
        var release = new JSONObject(new String(after, StandardCharsets.UTF_8));
        String id = release.getString("id");
        require(legacy.getInt("api") == 3 && release.getInt("api") == 4, "actual API3/API4 files");
        require(legacy.getString("id").equals(id) && release.getString("version").equals("2.0.0"), "matching update identity/version");
        var map = release.getJSONObject("map"); require(!map.has("objects"), "legacy scattered objects omitted");
        var stages = map.getJSONArray("stageEnvironments"); require(stages.length() == 3, "three neighborhoods");
        for (int i = 0; i < 3; i++) {
            var stage = stages.getJSONObject(i); var profile = stage.getJSONObject("streetscape");
            require(stage.getInt("stage") == i + 1 && profile.getInt("tileWidth") == 26 + i * 2 && profile.getInt("stepHeight") == 6 + i &&
                Math.abs(profile.getDouble("coverage") - (.95 - .05 * i)) < 1e-8 && profile.getJSONArray("frontages").length() >= 1 && profile.getJSONArray("frontages").length() <= 8,
                "exact final profile " + (i + 1));
        }
        var scratch = Files.createTempDirectory("actual-street-release-check-");
        for (boolean originallyEnabled : new boolean[] { false, true }) {
            var app = Files.createDirectory(scratch.resolve(originallyEnabled ? "enabled" : "disabled"));
            var mods = Files.createDirectory(app.resolve("Mods")); var prefs = new RecoveryCheck.Prefs();
            require(install(oldZip, mods, prefs).equals(id), "actual 1.4 import");
            require(LauncherModIntegrity.valid(mods.resolve(id).toFile(), id), "API3 integrity");
            prefs.state.put(id, originallyEnabled);
            require(install(newZip, mods, prefs).equals(id), "actual 2.0 update");
            require(Arrays.equals(after, Files.readAllBytes(mods.resolve(id).resolve("manifest.json"))), "exact release manifest installed");
            require(LauncherModIntegrity.valid(mods.resolve(id).toFile(), id), "all actual API4 file hashes validated");
            require(Arrays.equals(before, Files.readAllBytes(app.resolve("ModsHistory").resolve(id).resolve("manifest.json"))), "1.4 retained for recovery");
            require(LauncherModIntegrity.valid(app.resolve("ModsHistory").resolve(id).toFile(), id), "retained API3 integrity");
            // Invoke the exact current activity methods, not a reimplementation.
            var activity = new SplitDataLauncher(prefs);
            var enable = SplitDataLauncher.class.getDeclaredMethod("enableContentMod", java.io.File.class, String.class);
            enable.setAccessible(true); enable.invoke(activity, app.toFile(), id);
            require(prefs.getBoolean(id, false), "2.0 enabled after update from " + originallyEnabled);
            require(LauncherModRecovery.recover(mods.toFile()) == 0 && LauncherModIntegrity.valid(mods.resolve(id).toFile(), id), "new API4 survives recovery");
            LauncherModRecovery.restorePrevious(mods.toFile(), id, prefs);
            require(Arrays.equals(before, Files.readAllBytes(mods.resolve(id).resolve("manifest.json"))) && LauncherModIntegrity.valid(mods.resolve(id).toFile(), id), "restoring API3 remains valid");
        }
        System.out.println("PASS actual1.4/API3→2.0/API4: exact profiles, final ZIP hashes, both old enabled states auto-enable, previous-version recovery and API3 restore. Scratch: " + scratch);
    }
}
