package com.megabonk.launcher;

import android.content.Context;
import java.io.File;
import java.io.InputStream;
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
        LauncherDiagnosticGenerator generator = new LauncherDiagnosticGenerator(context, null);
        byte[] expected = hash(Paths.get(args[4]));
        for (int i = 2; i <= 3; i++) {
            Path output = cache.getParent().resolve("generated-" + i + ".obb");
            generator.prepare(new File(args[i]), output.toFile());
            if (!java.util.Arrays.equals(expected, hash(output))) throw new AssertionError("generated OBB differs");
            Files.delete(output);
            try (Stream<Path> children = Files.list(cache)) {
                if (children.findAny().isPresent()) throw new AssertionError("temporary files retained");
            }
        }
        System.out.println("PASS: packaged dual plans selected and both OBBs restored exactly");
    }
    private DualResearchIntegrationCheck() {}
}
