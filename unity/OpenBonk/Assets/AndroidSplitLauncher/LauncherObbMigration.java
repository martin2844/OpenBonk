package com.megabonk.launcher;

import android.content.Context;
import android.util.Log;
import java.io.File;
import java.io.FileInputStream;
import java.io.FileOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.security.MessageDigest;
import java.util.Locale;

/** A narrowly pinned, transactional migration; the old OBB is never changed. */
final class LauncherObbMigration {
    private static final String OLD = "9011ea73423c8418db7f7d0895c515b98a23001e947fd227b83c221a04e37475";
    private static final String TARGET = "c3a42cbb1b38bdf89ee0e8175b81f2f70da4206e9a4862a67b11cda7cf3adf34";
    private static final String PATCH = "eb7a78b632370e696e0d87b754cb61ffa4c978c3bda9fb97f725a27e29ce7e48";
    private static final long TARGET_SIZE = 374365400L;
    private static native int applyNative(String oldPath, String patchPath, String outputPath, long outputSize);

    static void migrate(Context context, File destination) {
        if (destination == null || destination.exists()) return;
        if (OLD.equals(SplitExpectedData.SHA256)) {
            migrateFrom(context, destination, 2,
                "c505401a92e8d9533c4d82237340a5d8946364169d8e64322af305d37ffc1afd",
                "12ad4f8dbd7a6f62a79ddbcf6039f1f73af5d11cb9ed260b0cd424de6646c7c5",
                "0.1.1-to-0.1.2.zst", OLD, 374365413L);
            return;
        }
        if (!TARGET.equals(SplitExpectedData.SHA256)) return;
        migrateFrom(context, destination, 4, OLD, PATCH, "0.1.3-to-integration.zst", TARGET, TARGET_SIZE);
        if (!destination.exists()) migrateFrom(context, destination, 3, OLD, PATCH, "0.1.3-to-integration.zst", TARGET, TARGET_SIZE);
        if (!destination.exists()) migrateFrom(context, destination, 2,
            "c505401a92e8d9533c4d82237340a5d8946364169d8e64322af305d37ffc1afd",
            "2e2c7ea8ecf0e53cf58aa3cc05c232e7ea6454e05499a7dd8c1224b68e55a3c8",
            "0.1.1-to-integration.zst", TARGET, TARGET_SIZE);
    }

    private static void migrateFrom(Context context, File destination, int version, String oldHash, String patchHash, String patchName, String targetHash, long targetSize) {
        File original = new File(destination.getParentFile(), "main." + version + "." + context.getPackageName() + ".obb");
        if (!original.isFile()) return;
        File patch = null, output = null;
        try {
            if (!oldHash.equals(digest(original))) return;
            patch = File.createTempFile("openbonk-migration-", ".patch", context.getCacheDir());
            try (InputStream in = context.getAssets().open("openbonk-migrations/" + patchName);
                 FileOutputStream out = new FileOutputStream(patch)) {
                byte[] buffer = new byte[65536]; int n; long size = 0;
                while ((n = in.read(buffer)) != -1) { size += n; if (size > 8L * 1024 * 1024) throw new IOException("migration patch too large"); out.write(buffer, 0, n); }
                out.getFD().sync();
            }
            if (!patchHash.equals(digest(patch))) throw new IOException("migration patch mismatch");
            output = File.createTempFile("openbonk-migration-", ".obb", destination.getParentFile());
            System.loadLibrary("openbonk_migration");
            if (applyNative(original.getAbsolutePath(), patch.getAbsolutePath(), output.getAbsolutePath(), targetSize) != 0 ||
                output.length() != targetSize || !targetHash.equals(digest(output))) throw new IOException("migration output mismatch");
            if (destination.exists() || !output.renameTo(destination)) throw new IOException("cannot install migrated data");
            output = null;
            Log.i("MegabonkLauncher", "Migrated compatible game data from version " + version + "; original retained");
        } catch (Exception | LinkageError e) {
            // Import remains available; a bad patch or insufficient space must
            // not make the launcher unusable or remove the original OBB.
            Log.w("MegabonkLauncher", "Game data migration unavailable: " + e.getMessage());
        } finally {
            if (patch != null) patch.delete();
            if (output != null) output.delete();
        }
    }

    private static String digest(File file) throws Exception {
        MessageDigest hash = MessageDigest.getInstance("SHA-256");
        try (InputStream in = new FileInputStream(file)) {
            byte[] buffer = new byte[65536]; int n;
            while ((n = in.read(buffer)) != -1) hash.update(buffer, 0, n);
        }
        StringBuilder result = new StringBuilder(64);
        for (byte b : hash.digest()) result.append(String.format(Locale.ROOT, "%02x", b & 255));
        return result.toString();
    }
    private LauncherObbMigration() {}
}
