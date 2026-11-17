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
            System.out.println("PASS unlock hook validation, ZIP import and enable/disable preferences");
        } finally {
            try (var paths = Files.walk(temporary)) {
                for (Path path : (Iterable<Path>) paths.sorted(java.util.Comparator.reverseOrder())::iterator) Files.deleteIfExists(path);
            }
        }
    }
}
