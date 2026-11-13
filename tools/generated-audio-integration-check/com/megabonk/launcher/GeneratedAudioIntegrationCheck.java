package com.megabonk.launcher;
import android.content.Context;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.*;

/** Local integration check. The provided inputs contain game data. */
public final class GeneratedAudioIntegrationCheck {
    private static String sha(File file) throws Exception {
        MessageDigest md = MessageDigest.getInstance("SHA-256"); byte[] b = new byte[65536];
        try (InputStream in = new FileInputStream(file)) { for (int n; (n = in.read(b)) != -1;) if (n > 0) md.update(b, 0, n); }
        StringBuilder s = new StringBuilder(); for (byte value : md.digest()) s.append(String.format("%02x", value & 255)); return s.toString();
    }
    private static void require(boolean condition) { if (!condition) throw new AssertionError(); }
    public static void main(String[] args) throws Exception {
        if (args.length < 5 || args.length > 7) throw new IllegalArgumentException("new-dir template plan steam-zip expected-obb [packed-plan [texture-plan]]");
        Path root = Paths.get(args[0]); Files.createDirectory(root); Path assets = root.resolve("assets/openbonk-diagnostic"); Files.createDirectories(assets);
        Path cache = root.resolve("cache"); Files.createDirectory(cache); Path destination = root.resolve("data"); Files.createDirectory(destination);
        Files.write(assets.resolve("marker"), "OPENBONK_PRIVATE_DIAGNOSTIC_V1".getBytes(StandardCharsets.US_ASCII));
        Files.write(assets.resolve("generated-audio-mode"), (args.length == 7 ? "OPENBONK_GENERATED_AUDIO_RESEARCH_V3" : args.length == 6 ? "OPENBONK_GENERATED_AUDIO_RESEARCH_V2" : "OPENBONK_GENERATED_AUDIO_RESEARCH_V1").getBytes(StandardCharsets.US_ASCII));
        Files.copy(Paths.get(args[1]), assets.resolve("generated-audio-template.obb"));
        Files.copy(Paths.get(args[2]), assets.resolve("generated-audio-plan.json"));
        if (args.length >= 6) {
            Files.copy(Paths.get(args[5]), assets.resolve("packed-audio-plan.json"));
            Files.write(assets.resolve("packed-audio-plan.sha256"), sha(new File(args[5])).getBytes(StandardCharsets.US_ASCII));
        }
        if (args.length == 7) {
            Files.copy(Paths.get(args[6]), assets.resolve("texture-plan.json"));
            Files.write(assets.resolve("texture-plan.sha256"), sha(new File(args[6])).getBytes(StandardCharsets.US_ASCII));
        }
        Path planHash = assets.resolve("generated-audio-plan.sha256");
        Files.write(planHash, "0000000000000000000000000000000000000000000000000000000000000000".getBytes(StandardCharsets.US_ASCII));
        File obb = destination.resolve("game.obb").toFile(), steam = destination.resolve("steam.zip").toFile();
        Files.write(obb.toPath(), "old-obb".getBytes(StandardCharsets.US_ASCII)); Files.write(steam.toPath(), "old-steam".getBytes(StandardCharsets.US_ASCII));
        Context context = new Context(root.resolve("assets"), cache.toFile()); require(LauncherDiagnosticIntegration.available(context));
        String expectedHash = sha(new File(args[4]));
        SplitFileRecovery.Validator validObb = f -> { try { return sha(f).equals(expectedHash); } catch (Exception e) { return false; } };
        SplitFileRecovery.Validator validSteam = OriginalGameDataValidator::valid;
        if (args.length == 7) {
            Path textureHash = assets.resolve("texture-plan.sha256");
            Files.write(textureHash, "0000000000000000000000000000000000000000000000000000000000000000".getBytes(StandardCharsets.US_ASCII));
            try (InputStream in = new FileInputStream(args[3])) {
                try { LauncherPreparedSteamImport.install(in, cache.toFile(), obb, steam, validSteam, validObb, 1024L * 1024 * 1024, LauncherDiagnosticIntegration.create(context, null)); throw new AssertionError("invalid texture plan accepted"); }
                catch (IOException expected) { require(new String(Files.readAllBytes(obb.toPath()), StandardCharsets.US_ASCII).equals("old-obb")); require(new String(Files.readAllBytes(steam.toPath()), StandardCharsets.US_ASCII).equals("old-steam")); }
            }
            Files.write(textureHash, sha(new File(args[6])).getBytes(StandardCharsets.US_ASCII));
        }
        if (args.length >= 6) {
            Path packedHash = assets.resolve("packed-audio-plan.sha256");
            Files.write(packedHash, "0000000000000000000000000000000000000000000000000000000000000000".getBytes(StandardCharsets.US_ASCII));
            try (InputStream in = new FileInputStream(args[3])) {
                try { LauncherPreparedSteamImport.install(in, cache.toFile(), obb, steam, validSteam, validObb, 1024L * 1024 * 1024, LauncherDiagnosticIntegration.create(context, null)); throw new AssertionError("invalid packed plan accepted"); }
                catch (IOException expected) { require(new String(Files.readAllBytes(obb.toPath()), StandardCharsets.US_ASCII).equals("old-obb")); require(new String(Files.readAllBytes(steam.toPath()), StandardCharsets.US_ASCII).equals("old-steam")); }
            }
            Files.write(packedHash, sha(new File(args[5])).getBytes(StandardCharsets.US_ASCII));
        }
        try (InputStream in = new FileInputStream(args[3])) {
            try { LauncherPreparedSteamImport.install(in, cache.toFile(), obb, steam, validSteam, validObb, 1024L * 1024 * 1024, LauncherDiagnosticIntegration.create(context, null)); throw new AssertionError("invalid plan accepted"); }
            catch (IOException expected) { require(new String(Files.readAllBytes(obb.toPath()), StandardCharsets.US_ASCII).equals("old-obb")); require(new String(Files.readAllBytes(steam.toPath()), StandardCharsets.US_ASCII).equals("old-steam")); }
        }
        Files.write(planHash, sha(new File(args[2])).getBytes(StandardCharsets.US_ASCII));
        List<String> stages = new ArrayList<>();
        try (InputStream in = new FileInputStream(args[3])) { LauncherPreparedSteamImport.install(in, cache.toFile(), obb, steam, validSteam, validObb, 1024L * 1024 * 1024, LauncherDiagnosticIntegration.create(context, stages::add)); }
        require(validObb.valid(obb) && validSteam.valid(steam) && stages.contains("Preparando audio") && (args.length != 7 || stages.contains("Preparando texturas")));
        try (java.util.stream.Stream<Path> children = Files.list(cache)) { require(!children.findAny().isPresent()); }
        System.out.println("PASS: launcher discovery, invalid-plan rollback, progress, validated prepared-pair install and cleanup");
    }
    private GeneratedAudioIntegrationCheck() {}
}
