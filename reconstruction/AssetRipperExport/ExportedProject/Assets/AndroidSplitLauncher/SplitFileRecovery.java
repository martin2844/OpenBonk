package com.megabonk.launcher;

import java.io.File;
import java.io.IOException;

/** Recovers an interrupted same-directory replacement without overwriting files. */
final class SplitFileRecovery {
    interface Validator { boolean valid(File file); }

    static void recover(File destination, Validator validator) throws IOException {
        recover(destination, backupFor(destination), validator);
    }

    static File backupFor(File destination) {
        return new File(destination.getParentFile(), destination.getName() + ".previous");
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
