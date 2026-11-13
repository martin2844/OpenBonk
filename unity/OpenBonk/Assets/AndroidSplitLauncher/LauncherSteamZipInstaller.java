package com.megabonk.launcher;

import java.io.File;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;

/** Validates the complete ZIP before replacing an existing pending import. */
final class LauncherSteamZipInstaller {
    static void install(InputStream input, File staged, File destination, File legacyBackup,
                        SplitFileRecovery.Validator validator, long limit) throws IOException {
        File backup = SplitFileRecovery.backupFor(destination);
        try {
            if (staged.exists() && !staged.delete()) throw new IOException("temporary file unavailable");
            try (FileOutputStream out = new FileOutputStream(staged)) {
                byte[] buffer = new byte[65536]; long total = 0; int n;
                while ((n = input.read(buffer)) != -1) {
                    total += n;
                    if (total > limit) throw new IOException("ZIP too large");
                    out.write(buffer, 0, n);
                }
            }
            if (!validator.valid(staged)) throw new IOException("incompatible ZIP");
            SplitFileRecovery.recover(destination, validator);
            SplitFileRecovery.recover(destination, legacyBackup, validator);
            if (backup.exists()) throw new IOException("previous ZIP backup exists");
            boolean hadOld = destination.exists();
            if (hadOld && !destination.renameTo(backup)) throw new IOException("cannot preserve previous ZIP");
            if (!staged.renameTo(destination)) {
                if (hadOld) backup.renameTo(destination);
                throw new IOException("cannot prepare ZIP");
            }
            if (hadOld) backup.delete();
        } catch (IOException | RuntimeException e) { staged.delete(); throw e; }
    }

    private LauncherSteamZipInstaller() {}
}
