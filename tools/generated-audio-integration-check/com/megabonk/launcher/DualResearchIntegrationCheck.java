package com.megabonk.launcher;

import android.content.Context;
import java.io.File;
import java.io.InputStream;
import java.io.UncheckedIOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.security.MessageDigest;
import java.util.stream.Stream;

/** Exercise the packaged dual research selector and both preparation paths. */
public final class DualResearchIntegrationCheck {
    private static byte[] hash(Path file) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256"); byte[] buffer = new byte[65536];
        try (InputStream in = Files.newInputStream(file)) {
            for (int n; (n = in.read(buffer)) != -1;) if (n > 0) digest.update(buffer, 0, n);
        }
        return digest.digest();
    }
    public static void main(String[] args) throws Exception {
        if (args.length != 5) throw new IllegalArgumentException("assets cache linux-zip windows-zip expected-obb");
        Path cache = Paths.get(args[1]); Files.createDirectory(cache);
        Context context = new Context(Paths.get(args[0]), cache.toFile());
        byte[] expected = hash(Paths.get(args[4]));
        for (int i = 2; i <= 3; i++) {
            Path sentinel = cache.resolve("reclaimable-cache-sentinel");
            Files.write(sentinel, new byte[] { 1 });
            boolean[] checkedStaging = { false };
            LauncherDiagnosticGenerator generator = new LauncherDiagnosticGenerator(context, stage -> {
                if (checkedStaging[0] || !"Preparando texturas".equals(stage)) return;
                try (Stream<Path> files = Files.list(context.getFilesDir().toPath());
                     Stream<Path> cached = Files.list(cache)) {
                    if (!files.anyMatch(path -> path.getFileName().toString().startsWith(".openbonk-full-diagnostic-")))
                        throw new AssertionError("active preparation is not in private files");
                    if (cached.anyMatch(path -> !path.equals(sentinel)))
                        throw new AssertionError("active preparation leaked into cache");
                    Files.delete(sentinel); // Simulate reclaiming the only cache entry mid-import.
                    checkedStaging[0] = true;
                } catch (java.io.IOException e) { throw new UncheckedIOException(e); }
            });
            Path output = cache.getParent().resolve("generated-" + i + ".obb");
            generator.prepare(new File(args[i]), output.toFile());
            if (!checkedStaging[0]) throw new AssertionError("texture-stage cache test did not run");
            if (!java.util.Arrays.equals(expected, hash(output))) throw new AssertionError("generated OBB differs");
            Files.delete(output);
            try (Stream<Path> children = Files.list(cache)) {
                if (children.findAny().isPresent()) throw new AssertionError("temporary files retained");
            }
            try (Stream<Path> children = Files.list(context.getFilesDir().toPath())) {
                if (children.findAny().isPresent()) throw new AssertionError("private preparation files retained");
            }
        }
        System.out.println("PASS: packaged dual plans selected and both OBBs restored exactly");
    }
    private DualResearchIntegrationCheck() {}
}
