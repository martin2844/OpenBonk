package com.megabonk.launcher;
import java.io.*;
import java.nio.file.*;
import java.util.zip.*;
import org.json.JSONObject;

public final class UnlockImportCheck {
    private static void require(boolean ok, String what) { if (!ok) throw new AssertionError(what); }
    public static void main(String[] args) throws Exception {
        Path packageRoot = Paths.get(args[0]);
        JSONObject manifest = new JSONObject(Files.readString(packageRoot.resolve("manifest.json")));
        require(LauncherModHookValidation.validModHooks(manifest), "unlock hook accepted");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","unlock_all_bad")), "unknown hook rejected");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hooks",new org.json.JSONArray().put("unlock_all"))), "wrong API rejected");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","collect_spawned_xp")), "XP pickup hook accepted");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","auto_open_pots")), "auto pots hook accepted");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","item_lab")), "item lab hook accepted");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","chest_tracker")), "chest tracker hook accepted");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","minimap_zoom")), "minimap zoom accepted");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","run_metrics")), "run metrics accepted");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","unlimited_offer_tools")), "offer tools accepted");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","retry_on_death")), "retry hook accepted");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hooks",new org.json.JSONArray().put("retry_on_death"))), "retry hook requires API 1");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hook","unlimited_offer_tools")), "offer tools wrong API rejected");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hook","run_metrics")), "run metrics wrong API rejected");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","toggle_everything")), "toggle everything accepted");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hook","toggle_everything")), "toggle everything wrong API rejected");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hook","minimap_zoom")), "minimap zoom wrong API rejected");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hook","chest_tracker")), "chest tracker wrong API rejected");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hook","item_lab")), "item lab wrong API rejected");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hook","auto_open_pots")), "auto pots wrong API rejected");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","starting_tome:Luck")), "starting tome accepted");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","starting_tome:13")), "numeric tome rejected");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","starting_tome:luck")), "wrong case rejected");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hook","starting_tome:Luck")), "wrong tome API rejected");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",1).put("hook","damage_multiplier")), "legacy hook still accepted");
        require(LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hooks",new org.json.JSONArray().put("stat_add:Luck"))), "stat hook still accepted");
        require(!LauncherModHookValidation.validModHooks(new JSONObject().put("api",2).put("hooks",new org.json.JSONArray().put("stat_add:Luck").put("stat_add:Luck"))), "duplicate stat hooks rejected");
        ByteArrayOutputStream buffer = new ByteArrayOutputStream();
        try (ZipOutputStream zip = new ZipOutputStream(buffer)) {
            for (String name : new String[]{"manifest.json", "main.lua"}) {
                zip.putNextEntry(new ZipEntry(name)); zip.write(Files.readAllBytes(packageRoot.resolve(name))); zip.closeEntry();
            }
        }
        Path temporary = Files.createTempDirectory("unlock-import-");
        try {
            Path root = Files.createDirectory(temporary.resolve("Mods"));
            var prefs = new RecoveryCheck.Prefs();
            require(LauncherModImporter.install(new ByteArrayInputStream(buffer.toByteArray()), root.toFile(), prefs).equals("unlock-everything"), "install");
            require(!prefs.getBoolean("unlock-everything", true), "import defaults disabled");
            require(Files.exists(root.resolve("unlock-everything/main.lua")), "script installed");
            prefs.edit().putBoolean("unlock-everything", true).commit();
            require(prefs.getBoolean("unlock-everything", false), "enable choice");
            prefs.edit().putBoolean("unlock-everything", false).commit();
            require(!prefs.getBoolean("unlock-everything", true), "disable choice");
            Path instant = Paths.get(args[1]);
            ByteArrayOutputStream instantBuffer = new ByteArrayOutputStream();
            try (ZipOutputStream instantZip = new ZipOutputStream(instantBuffer)) {
                for (String name : new String[]{"manifest.json", "main.lua"}) {
                    instantZip.putNextEntry(new ZipEntry(name)); instantZip.write(Files.readAllBytes(instant.resolve(name))); instantZip.closeEntry();
                }
            }
            require(LauncherModImporter.install(new ByteArrayInputStream(instantBuffer.toByteArray()), root.toFile(), prefs).equals("instant-collect"), "instant XP package install");
            require(!prefs.getBoolean("instant-collect", true), "instant XP import defaults disabled");
            Path lucky = Paths.get(args[2]);
            ByteArrayOutputStream luckyBuffer = new ByteArrayOutputStream();
            try (ZipOutputStream luckyZip = new ZipOutputStream(luckyBuffer)) {
                for (String name : new String[]{"manifest.json", "main.lua"}) {
                    luckyZip.putNextEntry(new ZipEntry(name)); luckyZip.write(Files.readAllBytes(lucky.resolve(name))); luckyZip.closeEntry();
                }
            }
            require(LauncherModImporter.install(new ByteArrayInputStream(luckyBuffer.toByteArray()), root.toFile(), prefs).equals("lucky-tome"), "starting tome package import");
            require(!prefs.getBoolean("lucky-tome", true), "starting tome initially disabled");
            prefs.edit().putBoolean("lucky-tome", true).commit();
            require(prefs.getBoolean("lucky-tome", false), "starting tome enabled");
            prefs.edit().putBoolean("lucky-tome", false).commit();
            require(!prefs.getBoolean("lucky-tome", true), "starting tome disabled");
            Path pots = Paths.get(args[3]);
            ByteArrayOutputStream potsBuffer = new ByteArrayOutputStream();
            try (ZipOutputStream potsZip = new ZipOutputStream(potsBuffer)) {
                for (String name : new String[]{"manifest.json", "main.lua"}) {
                    potsZip.putNextEntry(new ZipEntry(name)); potsZip.write(Files.readAllBytes(pots.resolve(name))); potsZip.closeEntry();
                }
            }
            require(LauncherModImporter.install(new ByteArrayInputStream(potsBuffer.toByteArray()), root.toFile(), prefs).equals("auto-pots"), "auto pots package import");
            require(!prefs.getBoolean("auto-pots", true), "auto pots initially disabled");
            prefs.edit().putBoolean("auto-pots", true).commit();
            require(prefs.getBoolean("auto-pots", false), "auto pots enabled");
            prefs.edit().putBoolean("auto-pots", false).commit();
            require(!prefs.getBoolean("auto-pots", true), "auto pots disabled");
            Path lab = Paths.get(args[4]);
            ByteArrayOutputStream labBuffer = new ByteArrayOutputStream();
            try (ZipOutputStream labZip = new ZipOutputStream(labBuffer)) {
                for (String name : new String[]{"manifest.json", "main.lua"}) {
                    labZip.putNextEntry(new ZipEntry(name)); labZip.write(Files.readAllBytes(lab.resolve(name))); labZip.closeEntry();
                }
            }
            require(LauncherModImporter.install(new ByteArrayInputStream(labBuffer.toByteArray()), root.toFile(), prefs).equals("item-lab"), "item lab package import");
            require(!prefs.getBoolean("item-lab", true), "item lab initially disabled");
            Path metrics = Paths.get(args[5]);
            ByteArrayOutputStream metricsBuffer = new ByteArrayOutputStream();
            try (ZipOutputStream metricsZip = new ZipOutputStream(metricsBuffer)) {
                for (String name : new String[]{"manifest.json", "main.lua"}) {
                    metricsZip.putNextEntry(new ZipEntry(name)); metricsZip.write(Files.readAllBytes(metrics.resolve(name))); metricsZip.closeEntry();
                }
            }
            require(LauncherModImporter.install(new ByteArrayInputStream(metricsBuffer.toByteArray()), root.toFile(), prefs).equals("run-metrics"), "run metrics package import");
            require(!prefs.getBoolean("run-metrics", true), "run metrics initially disabled");
            prefs.edit().putBoolean("run-metrics", true).commit();
            require(prefs.getBoolean("run-metrics", false), "run metrics enabled");
            prefs.edit().putBoolean("run-metrics", false).commit();
            require(!prefs.getBoolean("run-metrics", true), "run metrics disabled");
            Path tools = Paths.get(args[6]);
            ByteArrayOutputStream toolsBuffer = new ByteArrayOutputStream();
            try (ZipOutputStream toolsZip = new ZipOutputStream(toolsBuffer)) {
                for (String name : new String[]{"manifest.json", "main.lua"}) {
                    toolsZip.putNextEntry(new ZipEntry(name)); toolsZip.write(Files.readAllBytes(tools.resolve(name))); toolsZip.closeEntry();
                }
            }
            require(LauncherModImporter.install(new ByteArrayInputStream(toolsBuffer.toByteArray()), root.toFile(), prefs).equals("offer-tools"), "offer tools package import");
            require(!prefs.getBoolean("offer-tools", true), "offer tools initially disabled");
            prefs.edit().putBoolean("offer-tools", true).commit();
            require(prefs.getBoolean("offer-tools", false), "offer tools enabled");
            prefs.edit().putBoolean("offer-tools", false).commit();
            require(!prefs.getBoolean("offer-tools", true), "offer tools disabled");
            System.out.println("PASS startup hook validation, ZIP imports and enable/disable preferences");
        } finally {
            try (var paths = Files.walk(temporary)) {
                for (Path path : (Iterable<Path>) paths.sorted(java.util.Comparator.reverseOrder())::iterator) Files.deleteIfExists(path);
            }
        }
    }
}
