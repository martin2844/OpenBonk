package com.megabonk.launcher;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.util.zip.*;

/** Host-side checks of staged replacement and recovery with synthetic data. */
public final class BundleCheck {
    private static void write(File f, String s) throws IOException {
        Files.write(f.toPath(), s.getBytes(StandardCharsets.UTF_8));
    }
    private static String read(File f) throws IOException {
        return new String(Files.readAllBytes(f.toPath()), StandardCharsets.UTF_8);
    }
    private static void bundle(File file, String obb, String steam, boolean extra) throws IOException {
        try (ZipOutputStream zip = new ZipOutputStream(new FileOutputStream(file))) {
            for (String[] item : new String[][] {{LauncherDataBundle.OBB, obb}, {LauncherDataBundle.STEAM, steam}}) {
                zip.putNextEntry(new ZipEntry(item[0]));
                zip.write(item[1].getBytes(StandardCharsets.UTF_8)); zip.closeEntry();
            }
            if (extra) { zip.putNextEntry(new ZipEntry("../other")); zip.write(1); zip.closeEntry(); }
        }
    }
    private static void require(boolean value) { if (!value) throw new AssertionError(); }
    private static void interrupted(File root, int oldMask, int step) throws Exception {
        File obb = new File(root, "pair.obb"), steam = new File(root, "pair.zip");
        File obbBackup = SplitFileRecovery.backupFor(obb), steamBackup = SplitFileRecovery.backupFor(steam);
        File marker = new File(root, "pair.zip.bundle.transaction");
        boolean oldObb = (oldMask & 1) != 0, oldSteam = (oldMask & 2) != 0;
        if (oldObb) write(obb, "old obb");
        if (oldSteam) write(steam, "old steam");
        write(marker, (oldObb ? "1" : "0") + (oldSteam ? "1" : "0"));
        if (step >= 1 && oldObb) require(obb.renameTo(obbBackup));
        if (step >= 2 && oldSteam) require(steam.renameTo(steamBackup));
        if (step >= 3) write(obb, "new obb");
        if (step >= 4) write(steam, "new steam");
        LauncherDataBundle.recoverTransaction(obb, steam);
        require(!marker.exists() && !obbBackup.exists() && !steamBackup.exists());
        require(oldObb ? read(obb).equals("old obb") : !obb.exists());
        require(oldSteam ? read(steam).equals("old steam") : !steam.exists());
        LauncherDataBundle.recoverTransaction(obb, steam);
        obb.delete(); steam.delete();
    }
    public static void main(String[] args) throws Exception {
        File root = Files.createTempDirectory("openbonk-bundle-").toFile();
        File obb = new File(root, "data.obb"), steam = new File(root, "steam.zip"), zip = new File(root, "bundle.zip");
        SplitFileRecovery.Validator obbValidator = f -> { try { return read(f).equals("new obb"); } catch (IOException e) { return false; } };
        SplitFileRecovery.Validator steamValidator = f -> { try { return read(f).equals("new steam"); } catch (IOException e) { return false; } };
        try {
            for (int mask = 0; mask < 4; mask++)
                for (int step = 0; step <= 4; step++) interrupted(root, mask, step);
            // After commit, a crash during backup deletion must retain both new files.
            write(obb, "new obb"); write(steam, "new steam");
            write(SplitFileRecovery.backupFor(obb), "old obb");
            write(SplitFileRecovery.backupFor(steam), "old steam");
            LauncherDataBundle.recoverTransaction(obb, steam);
            SplitFileRecovery.recover(obb, obbValidator); SplitFileRecovery.recover(steam, steamValidator);
            require(read(obb).equals("new obb") && read(steam).equals("new steam"));
            File corrupt = new File(root, "steam.zip.bundle.transaction");
            write(obb, "protected obb"); write(steam, "protected steam");
            write(corrupt, "x");
            try { LauncherDataBundle.recoverTransaction(obb, steam); throw new AssertionError("accepted invalid marker"); }
            catch (IOException expected) {
                require(corrupt.exists() && read(obb).equals("protected obb") && read(steam).equals("protected steam"));
            }
            require(corrupt.delete());
            write(obb, "old obb"); write(steam, "old steam");
            bundle(zip, "new obb", "broken", false);
            try { LauncherDataBundle.install(zip, obb, steam, obbValidator, steamValidator); throw new AssertionError("accepted invalid steam"); }
            catch (IOException expected) { require(read(obb).equals("old obb") && read(steam).equals("old steam")); }
            bundle(zip, "new obb", "new steam", true);
            try { LauncherDataBundle.install(zip, obb, steam, obbValidator, steamValidator); throw new AssertionError("accepted unsafe entry"); }
            catch (IOException expected) { require(read(obb).equals("old obb") && read(steam).equals("old steam")); }
            bundle(zip, "new obb", "new steam", false);
            LauncherDataBundle.install(zip, obb, steam, obbValidator, steamValidator);
            require(obbValidator.valid(obb) && steamValidator.valid(steam));
            File backup = SplitFileRecovery.backupFor(obb);
            require(obb.renameTo(backup));
            SplitFileRecovery.recover(obb, obbValidator);
            require(obbValidator.valid(obb) && !backup.exists());
            require(obb.renameTo(backup));
            bundle(zip, "new obb", "broken", false);
            try { LauncherDataBundle.install(zip, obb, steam, obbValidator, steamValidator); throw new AssertionError("accepted invalid steam"); }
            catch (IOException expected) { require(!obb.exists() && backup.exists() && obbValidator.valid(backup)); }
            SplitFileRecovery.recover(obb, obbValidator);
            File obbPart = new File(root, "data.obb.partial"), obbBundlePart = new File(root, "data.obb.bundle.partial");
            File steamPart = new File(root, "steam.zip.bundle.partial"), profilePart = new File(root, "megabonk-profile.partial");
            for (File part : new File[] {obbPart, obbBundlePart, steamPart, profilePart}) write(part, "partial");
            File staleZip = new File(root, "openbonk-steam-import-abandoned.zip"), staleOutput = new File(root, "openbonk-generated-obb-abandoned.tmp");
            File staleDir = new File(root, ".openbonk-full-diagnostic-abandoned"), nested = new File(staleDir, "nested");
            write(staleZip, "partial"); write(staleOutput, "partial"); require(nested.mkdirs()); write(new File(nested, "partial"), "partial");
            Files.createSymbolicLink(new File(staleDir, "protected-link").toPath(), obb.toPath());
            LauncherDataBundle.cleanupPartials(obb, root, steam);
            require(!obbPart.exists() && !obbBundlePart.exists() && !steamPart.exists() && !profilePart.exists());
            require(!staleZip.exists() && !staleOutput.exists() && !staleDir.exists());
            require(obbValidator.valid(obb) && steamValidator.valid(steam));
            File preparedObb = new File(root, "prepared.obb"), preparedSteam = new File(root, "prepared.zip");
            write(preparedObb, "new obb"); write(preparedSteam, "broken");
            try { LauncherDataBundle.installPrepared(preparedObb, preparedSteam, obb, steam, obbValidator, steamValidator); throw new AssertionError("accepted invalid prepared ZIP"); }
            catch (IOException expected) { require(obbValidator.valid(obb) && steamValidator.valid(steam)); }
            require(read(preparedObb).equals("new obb") && read(preparedSteam).equals("broken") && !obbBundlePart.exists() && !steamPart.exists());
            write(preparedSteam, "new steam");
            LauncherDataBundle.installPrepared(preparedObb, preparedSteam, obb, steam, obbValidator, steamValidator);
            require(obbValidator.valid(obb) && steamValidator.valid(steam) && preparedObb.isFile() && preparedSteam.isFile());
            try { LauncherDataBundle.installPrepared(obb, preparedSteam, obb, steam, obbValidator, steamValidator); throw new AssertionError("accepted overlapping destination"); }
            catch (IOException expected) { require(obbValidator.valid(obb) && steamValidator.valid(steam)); }
            File symlink = new File(root, "prepared-link"); Files.createSymbolicLink(symlink.toPath(), preparedObb.toPath());
            try { LauncherDataBundle.installPrepared(symlink, preparedSteam, obb, steam, obbValidator, steamValidator); throw new AssertionError("accepted symlink"); }
            catch (IOException expected) { require(obbValidator.valid(obb) && steamValidator.valid(steam)); }
            require(symlink.delete());
            System.out.println("PASS: bundle and prepared-pair validation, 20 interruption states, corrupt marker and recovery");
        } finally {
            File[] files = root.listFiles();
            if (files != null) for (File f : files) f.delete();
            root.delete();
        }
    }
}
