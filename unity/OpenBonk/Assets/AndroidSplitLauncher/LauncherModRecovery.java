package com.megabonk.launcher;

import android.content.SharedPreferences;
import java.io.File;
import java.io.IOException;
import java.util.UUID;
import java.util.ArrayList;
import java.util.Collections;

/** Same-filesystem mod replacement with recovery after an interrupted rename. */
final class LauncherModRecovery {
    private static final String BACKUP = ".previous-";
    static synchronized int recover(File root) throws IOException {
        if (!root.exists()) return 0;
        int repaired = 0;
        File[] files = root.listFiles();
        if (files == null) throw new IOException("cannot inspect mod storage");
        for (File backup : files) {
            String name = backup.getName();
            if (!name.startsWith(BACKUP)) continue;
            String id = name.substring(BACKUP.length());
            if (!id.matches("[A-Za-z0-9][A-Za-z0-9._-]{0,63}") || !backup.isDirectory()) throw new IOException("invalid mod backup");
            File dest = new File(root, id);
            if (!dest.exists()) {
                if (LauncherModIntegrity.valid(backup, id)) {
                    if (!backup.renameTo(dest)) throw new IOException("cannot restore previous mod");
                } else { quarantine(root, id, backup); repaired++; }
            } else {
                boolean currentValid = LauncherModIntegrity.valid(dest, id), backupValid = LauncherModIntegrity.valid(backup, id);
                if (currentValid && !backupValid) { quarantine(root, id, backup); repaired++; }
                else if (!backupValid) {
                    File damaged = quarantine(root, id, dest);
                    try { quarantine(root, id, backup); }
                    catch (IOException failure) {
                        if (!damaged.renameTo(dest)) throw new IOException("mod recovery interrupted; both versions retained", failure);
                        throw failure;
                    }
                    repaired++;
                }
                else if (currentValid) savePrevious(root, id, backup);
                else {
                    File damaged = quarantine(root, id, dest);
                    if (!backup.renameTo(dest)) {
                        if (!damaged.renameTo(dest)) throw new IOException("mod recovery interrupted; both versions retained");
                        throw new IOException("cannot restore previous mod");
                    }
                    repaired++;
                }
            }
        }
        return repaired;
    }
    private static File quarantine(File root, String id, File source) throws IOException {
        File history = historyRoot(root);
        if (!history.isDirectory() && !history.mkdirs()) throw new IOException("cannot create mod history");
        File damaged = new File(history, ".damaged-" + id + "-" + UUID.randomUUID());
        if (!source.renameTo(damaged)) throw new IOException("cannot preserve damaged mod");
        return damaged;
    }
    static synchronized void install(File root, String id, File staging, SharedPreferences prefs) throws IOException {
        recover(root);
        File dest = new File(root, id), backup = new File(root, BACKUP + id);
        if (dest.exists() && !dest.isDirectory()) throw new IOException("invalid existing mod");
        boolean had = prefs.contains(id), old = prefs.getBoolean(id, false), moved = false;
        if (!prefs.edit().putBoolean(id, false).commit()) throw new IOException("cannot save mod state");
        try {
            if (dest.exists()) {
                if (!dest.renameTo(backup)) throw new IOException("cannot back up existing mod");
                moved = true;
            }
            if (!staging.renameTo(dest)) throw new IOException("cannot install mod");
        } catch (IOException failure) {
            boolean restored = !moved || backup.renameTo(dest);
            SharedPreferences.Editor restore = prefs.edit();
            if (had) restore.putBoolean(id, old); else restore.remove(id);
            boolean stateRestored = restore.commit();
            if (!restored || !stateRestored) throw new IOException("mod update interrupted; previous files retained for recovery", failure);
            throw failure;
        }
        // Keep one previous version outside the active Mods directory. A failed
        // move leaves the transaction backup for recover() on the next launch.
        if (moved) try { savePrevious(root, id, backup); } catch (IOException ignored) { }
    }
    private static File historyRoot(File root) { return new File(root.getParentFile(), "ModsHistory"); }
    private static void savePrevious(File root, String id, File backup) throws IOException {
        File history = historyRoot(root);
        if (!history.isDirectory() && !history.mkdirs()) throw new IOException("cannot create mod history");
        File old = new File(history, id);
        if (old.exists()) deleteTree(old);
        if (!backup.renameTo(old)) throw new IOException("cannot retain previous mod");
    }
    static synchronized boolean hasPrevious(File root, String id) {
        if (!id.matches("[A-Za-z0-9][A-Za-z0-9._-]{0,63}")) return false;
        File old = new File(historyRoot(root), id);
        File manifest = new File(old, "manifest.json");
        return old.isDirectory() && manifest.isFile() && manifest.length() > 0 && new File(old, "main.lua").isFile();
    }
    static synchronized ArrayList<String> previousIds(File root) throws IOException {
        ArrayList<String> result = new ArrayList<>(); File history = historyRoot(root);
        if (!history.exists()) return result;
        File[] entries = history.listFiles(); if (entries == null) throw new IOException("cannot inspect mod history");
        for (File entry : entries) if (hasPrevious(root, entry.getName())) result.add(entry.getName());
        Collections.sort(result); return result;
    }
    static synchronized void restorePrevious(File root, String id, SharedPreferences prefs) throws IOException {
        recover(root);
        if (!hasPrevious(root, id)) throw new IOException("no previous mod version");
        File dest = new File(root, id), backup = new File(root, BACKUP + id), old = new File(historyRoot(root), id);
        if (!LauncherModIntegrity.valid(old, id)) throw new IOException("previous mod is damaged");
        if (dest.exists() && !dest.isDirectory()) throw new IOException("current mod unavailable");
        boolean had = prefs.contains(id), wasEnabled = prefs.getBoolean(id, false);
        if (!prefs.edit().putBoolean(id, false).commit()) throw new IOException("cannot save mod state");
        boolean moved = false;
        try {
            if (dest.exists()) {
                if (!dest.renameTo(backup)) throw new IOException("cannot back up current mod");
                moved = true;
            }
            if (!old.renameTo(dest)) throw new IOException("cannot restore previous mod");
        } catch (IOException failure) {
            boolean restored = !moved || backup.renameTo(dest);
            SharedPreferences.Editor restore = prefs.edit();
            if (had) restore.putBoolean(id, wasEnabled); else restore.remove(id);
            if (!restore.commit() || !restored) throw new IOException("mod restoration interrupted; files retained for recovery", failure);
            throw failure;
        }
        if (moved) try { savePrevious(root, id, backup); } catch (IOException ignored) { }
    }
    private static void deleteTree(File file) throws IOException {
        if (file.isDirectory()) {
            File[] children = file.listFiles();
            if (children == null) throw new IOException("cannot inspect mod backup");
            for (File child : children) deleteTree(child);
        }
        if (!file.delete() && file.exists()) throw new IOException("cannot remove mod backup");
    }
}
