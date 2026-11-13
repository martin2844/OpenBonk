package com.megabonk.launcher;

import java.io.File;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;

public final class SplitFileRecoveryCheck {
    private static void write(File f, String s) throws IOException { Files.write(f.toPath(), s.getBytes(StandardCharsets.UTF_8)); }
    private static String read(File f) throws IOException { return new String(Files.readAllBytes(f.toPath()), StandardCharsets.UTF_8); }
    private static void check(boolean value) { if (!value) throw new AssertionError(); }
    public static void main(String[] args) throws Exception {
        Path dir = Files.createTempDirectory("split-recovery-");
        File dest = dir.resolve("data").toFile(), backup = SplitFileRecovery.backupFor(dest), legacy = dir.resolve("legacy.previous").toFile();
        SplitFileRecovery.Validator valid = f -> { try { return read(f).startsWith("valid"); } catch (IOException e) { return false; } };
        try {
            SplitFileRecovery.recover(dest, valid);
            write(backup, "valid old");
            SplitFileRecovery.recover(dest, valid);
            check(read(dest).equals("valid old") && !backup.exists());
            write(backup, "valid old"); write(dest, "valid new");
            SplitFileRecovery.recover(dest, valid);
            check(read(dest).equals("valid new") && !backup.exists());
            write(backup, "valid old"); write(dest, "broken");
            try { SplitFileRecovery.recover(dest, valid); throw new AssertionError("expected error"); } catch (IOException expected) {}
            check(read(dest).equals("broken") && read(backup).equals("valid old"));
            check(dest.delete()); write(backup, "broken backup");
            try { SplitFileRecovery.recover(dest, valid); throw new AssertionError("expected error"); } catch (IOException expected) {}
            check(!dest.exists() && read(backup).equals("broken backup"));
            check(backup.delete()); write(legacy, "valid legacy");
            SplitFileRecovery.recover(dest, legacy, valid);
            check(read(dest).equals("valid legacy") && !legacy.exists());
            write(legacy, "valid legacy"); write(dest, "valid new");
            SplitFileRecovery.recover(dest, legacy, valid);
            check(read(dest).equals("valid new") && !legacy.exists());
            write(legacy, "valid legacy"); write(dest, "broken");
            try { SplitFileRecovery.recover(dest, legacy, valid); throw new AssertionError("expected error"); } catch (IOException expected) {}
            check(read(dest).equals("broken") && read(legacy).equals("valid legacy"));
            System.out.println("SplitFileRecoveryCheck passed");
        } finally { Files.deleteIfExists(dest.toPath()); Files.deleteIfExists(backup.toPath()); Files.deleteIfExists(legacy.toPath()); Files.deleteIfExists(dir); }
    }
}
