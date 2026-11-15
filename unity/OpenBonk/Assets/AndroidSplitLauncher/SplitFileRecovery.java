package com.megabonk.launcher;

import java.io.File;
import java.io.IOException;
import java.nio.file.Files;

/** Recovers an interrupted same-directory replacement without overwriting files. */
final class SplitFileRecovery {
    interface Validator { boolean valid(File file); }

    static void recover(File destination, Validator validator) throws IOException {
        recover(destination, backupFor(destination), validator);
    }

    static File backupFor(File destination) {
        return new File(destination.getParentFile(), destination.getName() + ".previous");
    }

    // Move an old queued request without replacing a newer request or backup.
    static void migrate(File legacy, File destination, Validator validator) throws IOException {
        if (Files.isSymbolicLink(legacy.toPath()) || Files.isSymbolicLink(destination.toPath()) ||
            Files.isSymbolicLink(backupFor(destination).toPath())) throw new IOException("Invalid queued Steam ZIP path");
        if (!legacy.exists()) return;
        if (!legacy.isFile() || !validator.valid(legacy)) throw new IOException("Queued Steam ZIP needs attention");
        if (destination.exists() || backupFor(destination).exists())
            throw new IOException("Both queued Steam ZIPs need attention");
        if (!legacy.renameTo(destination)) throw new IOException("Could not move queued Steam ZIP");
    }

    static void recover(File destination, File backup, Validator validator) throws IOException {
        if (!backup.exists()) return;
        if (!destination.exists()) {
            if (!validator.valid(backup) || !backup.renameTo(destination))
                throw new IOException("Could not restore the previous file");
        } else if (validator.valid(destination)) {
            if (!backup.delete()) throw new IOException("Could not clear the previous file backup");
        } else {
            // Keep both files for recovery: an invalid destination is not proof
            // that the backup can safely be discarded or overwritten.
            throw new IOException("Both the current file and its backup need attention");
        }
    }
}
