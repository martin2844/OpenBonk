package com.megabonk.launcher;

import java.io.*;
import java.nio.file.Files;
import java.security.MessageDigest;

/** Exercises the launcher's staging and preservation with a real Steam ZIP. */
public final class SteamZipInstallerCheck {
    private static byte[] hash(File file) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256");
        try (InputStream in = new FileInputStream(file)) {
            byte[] buf = new byte[65536]; int n;
            while ((n = in.read(buf)) != -1) digest.update(buf, 0, n);
        }
        return digest.digest();
    }
    private static void require(boolean ok) { if (!ok) throw new AssertionError(); }
    public static void main(String[] args) throws Exception {
        File input = new File(args[0]), dir = Files.createTempDirectory("steam-stage-").toFile();
        File stage = new File(dir, "stage"), dest = new File(dir, "pending"), legacy = new File(dir, "legacy");
        try {
            try (InputStream in = new FileInputStream(input)) {
                LauncherSteamZipInstaller.install(in, stage, dest, legacy, OriginalGameDataValidator::valid, 1024L * 1024 * 1024);
            }
            byte[] expected = hash(input);
            require(MessageDigest.isEqual(expected, hash(dest)) && !stage.exists());
            try (InputStream in = new ByteArrayInputStream(new byte[]{1,2,3})) {
                LauncherSteamZipInstaller.install(in, stage, dest, legacy, OriginalGameDataValidator::valid, 1024);
                throw new AssertionError("invalid ZIP accepted");
            } catch (IOException expectedFailure) { }
            require(MessageDigest.isEqual(expected, hash(dest)) && !stage.exists());
            try (InputStream in = new ByteArrayInputStream(new byte[]{1,2,3})) {
                LauncherSteamZipInstaller.install(in, stage, dest, legacy, OriginalGameDataValidator::valid, 2);
                throw new AssertionError("oversized ZIP accepted");
            } catch (IOException expectedFailure) { }
            require(MessageDigest.isEqual(expected, hash(dest)) && !stage.exists());
            require(dest.renameTo(legacy));
            try (InputStream in = new FileInputStream(input)) {
                LauncherSteamZipInstaller.install(in, stage, dest, legacy, OriginalGameDataValidator::valid, 1024L * 1024 * 1024);
            }
            require(MessageDigest.isEqual(expected, hash(dest)) && !legacy.exists());
            System.out.println("PASS: Steam ZIP staging, invalid/oversized preservation and legacy recovery");
        } finally {
            stage.delete(); dest.delete(); legacy.delete(); SplitFileRecovery.backupFor(dest).delete(); dir.delete();
        }
    }
}
