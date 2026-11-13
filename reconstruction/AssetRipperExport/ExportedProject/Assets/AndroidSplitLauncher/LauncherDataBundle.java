package com.megabonk.launcher;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.FileVisitResult;
import java.nio.file.SimpleFileVisitor;
import java.nio.file.attribute.BasicFileAttributes;
import java.util.*;
import java.util.zip.*;

/** Installs a locally prepared OBB and optional original-data ZIP together. */
final class LauncherDataBundle {
    static final String OBB = "openbonk-data.obb", STEAM = "steam-data.zip";
    private static final long OBB_LIMIT = 512L * 1024 * 1024;
    private static final long STEAM_LIMIT = 1024L * 1024 * 1024;

    private static File transaction(File steam) {
        return new File(steam.getParentFile(), steam.getName() + ".bundle.transaction");
    }

    // A marker means the pair did not commit. Restore each prior file before
    // allowing the ordinary single-file recovery or cleaning up staged data.
    static void recoverTransaction(File obb, File steam) throws IOException {
        File marker = transaction(steam);
        if (!marker.exists()) return;
        int oldObb, oldSteam;
        try (InputStream in = new FileInputStream(marker)) {
            oldObb = in.read(); oldSteam = in.read();
            if ((oldObb != '0' && oldObb != '1') || (oldSteam != '0' && oldSteam != '1') || in.read() != -1)
                throw new IOException("invalid data bundle transaction marker");
        }
        rollback(obb, oldObb == '1');
        rollback(steam, oldSteam == '1');
        if (!marker.delete()) throw new IOException("cannot clear data bundle transaction");
    }

    private static void rollback(File target, boolean existed) throws IOException {
        File backup = SplitFileRecovery.backupFor(target);
        if (backup.exists()) {
            if (!existed || !backup.isFile()) throw new IOException("invalid data bundle backup");
            if (target.exists() && (!target.isFile() || !target.delete())) throw new IOException("cannot remove uncommitted data");
            if (!backup.renameTo(target)) throw new IOException("cannot restore previous data");
        } else if (!existed) {
            if (target.exists() && (!target.isFile() || !target.delete())) throw new IOException("cannot remove uncommitted data");
        } else if (!target.isFile()) {
            throw new IOException("previous data is missing");
        }
    }

    private static void begin(File steam, boolean hadObb, boolean hadSteam) throws IOException {
        File marker = transaction(steam);
        File stage = new File(marker.getParentFile(), marker.getName() + ".partial");
        if (marker.exists()) throw new IOException("previous data bundle transaction exists");
        clearStage(stage);
        try {
            try (FileOutputStream out = new FileOutputStream(stage)) {
                out.write(hadObb ? '1' : '0'); out.write(hadSteam ? '1' : '0');
                out.getFD().sync();
            }
            if (!stage.renameTo(marker)) throw new IOException("cannot start data bundle transaction");
        } finally {
            stage.delete();
        }
    }

    static boolean looksLikeBundle(File file) {
        try (ZipFile zip = new ZipFile(file)) {
            return zip.getEntry(OBB) != null || zip.getEntry(STEAM) != null;
        } catch (IOException ignored) { return false; }
    }

    // Call while holding the launcher's data-operation lock, after backup recovery.
    static void cleanupPartials(File obb, File cache, File steam) throws IOException {
        if (obb != null) {
            clearStage(new File(obb.getParentFile(), obb.getName() + ".partial"));
            clearStage(new File(obb.getParentFile(), obb.getName() + ".bundle.partial"));
        }
        clearStage(new File(steam.getParentFile(), steam.getName() + ".bundle.partial"));
        clearStage(new File(steam.getParentFile(), steam.getName() + ".bundle.transaction.partial"));
        clearStage(new File(cache, "megabonk-original-texts.partial"));
        clearStage(new File(cache, "megabonk-profile.partial"));
        clearAbandonedPreparations(cache);
    }

    // The caller holds the data lock and waits for the import gate to become idle.
    private static void clearAbandonedPreparations(File cache) throws IOException {
        File[] files = cache.listFiles();
        if (files == null) throw new IOException("cannot inspect preparation cache");
        for (File file : files) {
            String name = file.getName();
            if (!name.startsWith("openbonk-steam-import-") && !name.startsWith("openbonk-generated-obb-") &&
                !name.startsWith(".openbonk-full-diagnostic-")) continue;
            Path root = file.toPath();
            Files.walkFileTree(root, new SimpleFileVisitor<Path>() {
                @Override public FileVisitResult visitFile(Path path, BasicFileAttributes attrs) throws IOException {
                    Files.delete(path); return FileVisitResult.CONTINUE;
                }
                @Override public FileVisitResult postVisitDirectory(Path path, IOException error) throws IOException {
                    if (error != null) throw error;
                    Files.delete(path); return FileVisitResult.CONTINUE;
                }
            });
        }
    }

    static void install(File bundle, File obb, File steam, SplitFileRecovery.Validator obbValidator,
                        SplitFileRecovery.Validator steamValidator) throws IOException {
        File obbStage = new File(obb.getParentFile(), obb.getName() + ".bundle.partial");
        File steamStage = new File(steam.getParentFile(), steam.getName() + ".bundle.partial");
        File obbBackup = SplitFileRecovery.backupFor(obb), steamBackup = SplitFileRecovery.backupFor(steam);
        try {
            recoverTransaction(obb, steam);
            clearStage(obbStage); clearStage(steamStage);
            try (ZipFile zip = new ZipFile(bundle)) {
                Map<String, ZipEntry> entries = new HashMap<>();
                Enumeration<? extends ZipEntry> all = zip.entries();
                while (all.hasMoreElements()) {
                    ZipEntry entry = all.nextElement();
                    if (entry.isDirectory() || (!OBB.equals(entry.getName()) && !STEAM.equals(entry.getName()))
                            || entries.put(entry.getName(), entry) != null)
                        throw new IOException("invalid data bundle entries");
                }
                if (entries.size() != 2) throw new IOException("incomplete data bundle");
                copy(zip, entries.get(OBB), obbStage, OBB_LIMIT);
                copy(zip, entries.get(STEAM), steamStage, STEAM_LIMIT);
            }
            commitStages(obb, steam, obbStage, steamStage, obbValidator, steamValidator);
        } catch (IOException e) {
            try { recoverTransaction(obb, steam); } catch (IOException recovery) { e.addSuppressed(recovery); }
            throw e;
        } finally {
            obbStage.delete(); steamStage.delete();
        }
    }

    /** Publish locally prepared files as a pair after validating both staged copies. */
    static void installPrepared(File preparedObb, File preparedSteam, File obb, File steam,
                                SplitFileRecovery.Validator obbValidator,
                                SplitFileRecovery.Validator steamValidator) throws IOException {
        File obbStage = new File(obb.getParentFile(), obb.getName() + ".bundle.partial");
        File steamStage = new File(steam.getParentFile(), steam.getName() + ".bundle.partial");
        File[] protectedFiles = {obb, steam, obbStage, steamStage, SplitFileRecovery.backupFor(obb),
                                 SplitFileRecovery.backupFor(steam), transaction(steam)};
        File first = preparedObb.getCanonicalFile(), second = preparedSteam.getCanonicalFile();
        if (first.equals(second)) throw new IOException("prepared inputs must be separate");
        for (File f : protectedFiles) if (first.equals(f.getCanonicalFile()) || second.equals(f.getCanonicalFile()))
            throw new IOException("prepared inputs overlap destination");
        try {
            recoverTransaction(obb, steam);
            clearStage(obbStage); clearStage(steamStage);
            copyFile(preparedObb, obbStage, OBB_LIMIT);
            copyFile(preparedSteam, steamStage, STEAM_LIMIT);
            commitStages(obb, steam, obbStage, steamStage, obbValidator, steamValidator);
        } catch (IOException | RuntimeException e) {
            try { recoverTransaction(obb, steam); } catch (IOException recovery) { e.addSuppressed(recovery); }
            throw e;
        } finally { obbStage.delete(); steamStage.delete(); }
    }

    private static void commitStages(File obb, File steam, File obbStage, File steamStage,
                                     SplitFileRecovery.Validator obbValidator,
                                     SplitFileRecovery.Validator steamValidator) throws IOException {
        if (!obbValidator.valid(obbStage) || !steamValidator.valid(steamStage))
            throw new IOException("incompatible or damaged data bundle");
        if ((obb.exists() && !obb.isFile()) || (steam.exists() && !steam.isFile()))
            throw new IOException("data destination is not a file");
        SplitFileRecovery.recover(obb, obbValidator); SplitFileRecovery.recover(steam, steamValidator);
        File obbBackup = SplitFileRecovery.backupFor(obb), steamBackup = SplitFileRecovery.backupFor(steam);
        if (obbBackup.exists() || steamBackup.exists()) throw new IOException("previous data backup exists");
        boolean hadObb = obb.exists(), hadSteam = steam.exists();
        begin(steam, hadObb, hadSteam);
        if (hadObb && !obb.renameTo(obbBackup)) throw new IOException("cannot preserve previous data");
        if (hadSteam && !steam.renameTo(steamBackup)) throw new IOException("cannot preserve previous ZIP");
        if (!obbStage.renameTo(obb)) throw new IOException("cannot install data");
        if (!steamStage.renameTo(steam)) throw new IOException("cannot install ZIP");
        if (!transaction(steam).delete()) throw new IOException("cannot commit data bundle");
        if (hadObb) obbBackup.delete(); if (hadSteam) steamBackup.delete();
    }

    private static void copyFile(File source, File target, long limit) throws IOException {
        if (Files.isSymbolicLink(source.toPath()) || !Files.isRegularFile(source.toPath(), LinkOption.NOFOLLOW_LINKS))
            throw new IOException("invalid prepared file");
        long size = Files.size(source.toPath());
        if (size <= 0 || size > limit) throw new IOException("invalid prepared file size");
        try (InputStream in = new FileInputStream(source); FileOutputStream out = new FileOutputStream(target)) {
            byte[] buffer = new byte[65536]; long total = 0; int n;
            while ((n = in.read(buffer)) != -1) { if (n > size - total) throw new IOException("prepared file changed"); out.write(buffer, 0, n); total += n; }
            if (total != size) throw new IOException("prepared file changed"); out.getFD().sync();
        }
    }

    private static void clearStage(File file) throws IOException {
        if (file.exists() && (!file.isFile() || !file.delete())) throw new IOException("cannot clear temporary data");
    }

    private static void copy(ZipFile zip, ZipEntry entry, File target, long limit) throws IOException {
        long size = entry.getSize();
        if (size <= 0 || size > limit) throw new IOException("invalid data bundle size");
        try (InputStream in = zip.getInputStream(entry); FileOutputStream out = new FileOutputStream(target)) {
            byte[] buffer = new byte[65536]; long total = 0; int n;
            while ((n = in.read(buffer)) != -1) {
                total += n;
                if (total > size || total > limit) throw new IOException("oversized data bundle entry");
                out.write(buffer, 0, n);
            }
            if (total != size) throw new IOException("incomplete data bundle entry");
        }
    }
    private LauncherDataBundle() {}
}
