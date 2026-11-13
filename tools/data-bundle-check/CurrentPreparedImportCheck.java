package com.megabonk.launcher;

import com.openbonk.dataprep.SteamDiagnosticInputCheck;
import com.openbonk.dataprep.SteamFullDiagnosticPreparation;
import com.openbonk.dataprep.SteamVideoStorage;
import java.io.*;
import java.nio.file.*;
import java.nio.file.attribute.BasicFileAttributes;
import java.security.MessageDigest;
import java.util.zip.ZipFile;

/** End-to-end host check of the current private launcher data pipeline. */
public final class CurrentPreparedImportCheck {
    private static final String OBB_HASH = "c367ae38db2ba09fc3ddb1de428bb1782664916905565bb68b1e850ac4ee09f5";
    private static String hash(File file) throws Exception {
        MessageDigest d = MessageDigest.getInstance("SHA-256"); byte[] b = new byte[65536];
        try (InputStream in = new FileInputStream(file)) { for (int n; (n = in.read(b)) != -1;) if (n > 0) d.update(b, 0, n); }
        StringBuilder result = new StringBuilder(); for (byte value : d.digest()) result.append(String.format(java.util.Locale.ROOT, "%02x", value & 255)); return result.toString();
    }
    private static void clean(Path root) throws IOException {
        if (!Files.exists(root, LinkOption.NOFOLLOW_LINKS)) return;
        Files.walkFileTree(root, new SimpleFileVisitor<Path>() {
            @Override public FileVisitResult visitFile(Path file, BasicFileAttributes attrs) throws IOException { Files.delete(file); return FileVisitResult.CONTINUE; }
            @Override public FileVisitResult postVisitDirectory(Path dir, IOException error) throws IOException { if (error != null) throw error; Files.delete(dir); return FileVisitResult.CONTINUE; }
        });
    }
    public static void main(String[] args) throws Exception {
        if (args.length != 8) throw new IllegalArgumentException("texture-lib shared0 base-obb steam plans parent pass|missing FILE");
        System.load(args[0]);
        File shared0 = new File(args[1]), base = new File(args[2]), source = new File(args[3]), plans = new File(args[4]);
        Path work = Files.createTempDirectory(new File(args[5]).toPath(), ".openbonk-current-import-test-");
        try {
            File cache = work.resolve("cache").toFile(), obb = work.resolve("data.obb").toFile(), steam = work.resolve("steam.zip").toFile();
            if (!cache.mkdir()) throw new IOException("cannot create cache");
            byte[] oldObb = "old obb".getBytes(java.nio.charset.StandardCharsets.UTF_8), oldSteam = "old steam".getBytes(java.nio.charset.StandardCharsets.UTF_8);
            Files.write(obb.toPath(), oldObb); Files.write(steam.toPath(), oldSteam);
            SplitFileRecovery.Validator validObb = f -> { try (ZipFile zip = new ZipFile(f)) { return hash(f).equals(OBB_HASH) && zip.size() == 1078; } catch (Exception e) { return false; } };
            LauncherPreparedSteamImport.Generator generator = (input, output) -> {
                Path intermediate = cache.toPath().resolve("intermediate.obb");
                try {
                    SteamFullDiagnosticPreparation.prepare(shared0, base, input, plans, intermediate.toFile(), label -> System.out.println("STAGE: " + label));
                    SteamVideoStorage.prepare(intermediate.toFile(), output);
                } finally { Files.deleteIfExists(intermediate); }
            };
            if (args[6].equals("missing")) {
                try (InputStream input = new FileInputStream(source)) {
                    LauncherPreparedSteamImport.install(input, cache, obb, steam, OriginalGameDataValidator::valid, validObb, 1024L * 1024 * 1024, generator);
                    throw new AssertionError("missing data accepted");
                } catch (SteamDiagnosticInputCheck.Failure expected) {
                    if (!expected.missing || !expected.file.equals(args[7])) throw expected;
                }
                if (!java.util.Arrays.equals(oldObb, Files.readAllBytes(obb.toPath())) || !java.util.Arrays.equals(oldSteam, Files.readAllBytes(steam.toPath()))) throw new AssertionError("old data changed");
            } else if (args[6].equals("pass")) {
                String inputHash = hash(source);
                try (InputStream input = new FileInputStream(source)) {
                    LauncherPreparedSteamImport.install(input, cache, obb, steam, OriginalGameDataValidator::valid, validObb, 1024L * 1024 * 1024, generator);
                }
                if (!validObb.valid(obb) || !OriginalGameDataValidator.valid(steam) || !inputHash.equals(hash(steam))) throw new AssertionError("installed data differs");
            } else throw new IllegalArgumentException("unknown mode");
            String[] left = cache.list(); if (left == null || left.length != 0) throw new AssertionError("temporary files remain");
            if (SplitFileRecovery.backupFor(obb).exists() || SplitFileRecovery.backupFor(steam).exists() || work.resolve("steam.zip.bundle.transaction").toFile().exists()) throw new AssertionError("transaction files remain");
            System.out.println("PASS: current prepared import " + args[6]);
        } finally { clean(work); }
    }
}
