package com.megabonk.launcher;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.LinkOption;

/** Stages a Steam ZIP and a locally generated OBB before publishing them as a pair. */
final class LauncherPreparedSteamImport {
    interface Generator { void prepare(File steamZip, File newObb) throws IOException; }
    interface RejectionReason { String explain(File steamZip); }

    static void install(InputStream selected, File cache, File obb, File steam,
                        SplitFileRecovery.Validator steamValidator,
                        SplitFileRecovery.Validator obbValidator,
                        long zipLimit, Generator generator) throws IOException {
        install(selected, cache, obb, steam, steamValidator, obbValidator, zipLimit, generator, null);
    }

    static void install(InputStream selected, File cache, File obb, File steam,
                        SplitFileRecovery.Validator steamValidator,
                        SplitFileRecovery.Validator obbValidator,
                        long zipLimit, Generator generator, RejectionReason rejectionReason) throws IOException {
        if (selected == null || generator == null || zipLimit <= 0 ||
            Files.isSymbolicLink(cache.toPath()) || !Files.isDirectory(cache.toPath(), LinkOption.NOFOLLOW_LINKS))
            throw new IOException("invalid preparation input");
        File stagedZip = File.createTempFile("openbonk-steam-import-", ".zip", cache);
        File generated = null;
        try {
            try (FileOutputStream out = new FileOutputStream(stagedZip)) {
                byte[] buffer = new byte[65536]; long total = 0; int n;
                while ((n = selected.read(buffer)) != -1) {
                    if (n > zipLimit - total) throw new IOException("ZIP too large");
                    out.write(buffer, 0, n); total += n;
                }
                if (total == 0) throw new IOException("empty ZIP");
                out.getFD().sync();
            }
            if (!steamValidator.valid(stagedZip)) {
                String reason = rejectionReason == null ? null : rejectionReason.explain(stagedZip);
                throw new IOException(reason == null ? "incompatible ZIP" : reason);
            }
            generated = File.createTempFile("openbonk-generated-obb-", ".tmp", cache);
            if (!generated.delete()) throw new IOException("cannot reserve output");
            try { generator.prepare(stagedZip, generated); }
            catch (LinkageError e) { throw new IOException("native converter unavailable", e); }
            if (!obbValidator.valid(generated)) throw new IOException("generated OBB differs");
            LauncherDataBundle.installPrepared(generated, stagedZip, obb, steam, obbValidator, steamValidator);
        } finally {
            if (generated != null) generated.delete();
            stagedZip.delete();
        }
    }
    private LauncherPreparedSteamImport() {}
}
