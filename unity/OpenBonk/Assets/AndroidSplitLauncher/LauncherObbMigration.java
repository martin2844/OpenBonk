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
import org.json.JSONObject;

/** A narrowly pinned, transactional migration; the old OBB is never changed. */
final class LauncherObbMigration {
    private static final String OLD = "9011ea73423c8418db7f7d0895c515b98a23001e947fd227b83c221a04e37475";
    private static final String TARGET = "c3a42cbb1b38bdf89ee0e8175b81f2f70da4206e9a4862a67b11cda7cf3adf34";
    private static final String PATCH = "eb7a78b632370e696e0d87b754cb61ffa4c978c3bda9fb97f725a27e29ce7e48";
    private static final long TARGET_SIZE = 374365400L;
    private static final String V010 = "c367ae38db2ba09fc3ddb1de428bb1782664916905565bb68b1e850ac4ee09f5";
    private static final String V010_PATCH_SHA = "5a2f4b34cef6d0d288e8ca63d895a5515880d0649c3b5145f60612b9a7583907";
    private static final long V010_PATCH_SIZE = 70982639L;
    private static final String V010_PATCH_URL = "https://github.com/martin2844/OpenBonk/releases/download/v0.1.5-beta/data-0.1.0-to-0.1.4.zst";
    private static native int applyNative(String oldPath, String patchPath, String outputPath, long outputSize);

    static void migrate(Context context, File destination, Runnable onLegacyDownload) {
        if (destination == null || destination.exists()) return;
        if ("c7e714c4fe5be7d147e0225720dd4bc5d6d9f790432a016ab1ac99dedd67f60c".equals(SplitExpectedData.SHA256)) {
            String target = "c7e714c4fe5be7d147e0225720dd4bc5d6d9f790432a016ab1ac99dedd67f60c";
            migrateFrom(context, destination, 5, TARGET,
                "fbd1e512b49d1ebae2c48f42e7bf9b518ee41054603471ab6d28bca9908d63cd",
                "0.1.4-to-integration015.zst", target, 374365435L);
            if (!destination.exists()) migrateFrom(context, destination, 4, OLD,
                "e0f5c18554d46c1b211d8e2016b7c6ffb8b5f4c1d3b818f863a887d4afadc668",
                "0.1.3-to-integration015.zst", target, 374365435L);
            if (!destination.exists()) migrateFrom(context, destination, 3, OLD,
                "e0f5c18554d46c1b211d8e2016b7c6ffb8b5f4c1d3b818f863a887d4afadc668",
                "0.1.3-to-integration015.zst", target, 374365435L);
            if (!destination.exists()) migrateFrom(context, destination, 2,
                "c505401a92e8d9533c4d82237340a5d8946364169d8e64322af305d37ffc1afd",
                "1f14c666658bbe70a0e8b0f0a3027cd99700c35e532d95ee68301d5f04bea42e",
                "0.1.1-to-integration015.zst", target, 374365435L);
            if (!destination.exists()) {
                File intermediate = new File(destination.getParentFile(), "main.5." + context.getPackageName() + ".obb");
                boolean created = migrateLegacyRemote(context, intermediate, onLegacyDownload);
                if (created) {
                    migrateFrom(context, destination, 5, TARGET,
                        "fbd1e512b49d1ebae2c48f42e7bf9b518ee41054603471ab6d28bca9908d63cd",
                        "0.1.4-to-integration015.zst", target, 374365435L);
                    if (destination.exists()) intermediate.delete();
                }
            }
            return;
        }
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
        if (!destination.exists()) migrateLegacyRemote(context, destination, onLegacyDownload);
    }

    // The 0.1.0 delta is much larger than other migration patches. Download it
    // only for an exact source match; keep the original even on failure.
    private static boolean migrateLegacyRemote(Context context, File destination, Runnable onDownload) {
        if (destination.exists()) return false;
        File original = new File(destination.getParentFile(), "main.1." + context.getPackageName() + ".obb");
        if (!original.isFile()) return false;
        File patch = null, output = null;
        try {
            if (!V010.equals(digest(original))) return false;
            System.loadLibrary("openbonk_migration");
            if (onDownload != null) onDownload.run();
            patch = File.createTempFile("openbonk-migration-", ".patch", context.getCacheDir());
            JSONObject info = new JSONObject();
            info.put("url", V010_PATCH_URL); info.put("size", V010_PATCH_SIZE); info.put("sha256", V010_PATCH_SHA);
            LauncherOnline.download(info, patch, V010_PATCH_SIZE);
            output = File.createTempFile("openbonk-migration-", ".obb", destination.getParentFile());
            if (applyNative(original.getAbsolutePath(), patch.getAbsolutePath(), output.getAbsolutePath(), TARGET_SIZE) != 0 ||
                output.length() != TARGET_SIZE || !TARGET.equals(digest(output))) throw new IOException("migration output mismatch");
            if (destination.exists() || !output.renameTo(destination)) throw new IOException("cannot install migrated data");
            output = null;
            Log.i("MegabonkLauncher", "Migrated compatible 0.1.0 game data; original retained");
            return true;
        } catch (Exception | LinkageError e) {
            Log.w("MegabonkLauncher", "Legacy game data migration unavailable: " + e.getMessage());
            return false;
        } finally {
            if (patch != null) patch.delete();
            if (output != null) output.delete();
        }
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
