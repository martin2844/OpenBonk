package com.megabonk.launcher;

import java.io.File;
import java.io.FileOutputStream;
import java.nio.file.Files;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;
import java.util.zip.ZipOutputStream;

/** A support hint must not make an unsupported archive acceptable. */
public final class SteamZipDiagnosticCheck {
    private static void require(boolean value) { if (!value) throw new AssertionError(); }

    private static void entry(ZipOutputStream out, String name, byte[] bytes) throws Exception {
        out.putNextEntry(new ZipEntry(name)); out.write(bytes); out.closeEntry();
    }

    public static void main(String[] args) throws Exception {
        File dir = Files.createTempDirectory("openbonk-zip-diagnostic-").toFile();
        File archive = new File(dir, "input.zip");
        try {
            try (ZipOutputStream out = new ZipOutputStream(new FileOutputStream(archive))) {
                entry(out, "Megabonk_Data/sharedassets1.assets", new byte[] { 'a', 'b', 'c' });
                entry(out, "Megabonk_Data/sharedassets1.assets.resS", new byte[] { 1 });
                for (String name : OriginalLocalizationManifest.BUNDLES)
                    entry(out, "Megabonk_Data/StreamingAssets/aa/StandaloneLinux64/" + name, new byte[] { 1 });
            }
            require(!OriginalGameDataValidator.valid(archive));
            OriginalGameDataValidator.Problem problem = OriginalGameDataValidator.problem(archive);
            require(problem != null && !problem.missing && problem.size == 3);
            require("ba7816bf8f01cfea".equals(problem.hashPrefix));
            String english = OriginalGameDataValidator.describeProblem(problem, false);
            String spanish = OriginalGameDataValidator.describeProblem(problem, true);
            require(english.contains("3 bytes") && english.contains(problem.hashPrefix) && english.contains("not the ZIP"));
            require(spanish.contains(problem.hashPrefix) && spanish.contains("no el ZIP"));
            try (ZipFile zip = new ZipFile(archive)) {
                require(OriginalZipContentCheck.diagnosticHashPrefix(zip, zip.getEntry("Megabonk_Data/sharedassets1.assets"), 2) == null);
            }
            // A corrupt CRC must not be reported as a valid file fingerprint.
            byte[] bytes = Files.readAllBytes(archive.toPath()); boolean altered = false;
            for (int i = 0; i + 20 < bytes.length; i++) {
                if (bytes[i] == 'P' && bytes[i + 1] == 'K' && bytes[i + 2] == 1 && bytes[i + 3] == 2) {
                    bytes[i + 16] ^= 1; altered = true; break;
                }
            }
            require(altered); Files.write(archive.toPath(), bytes);
            try (ZipFile zip = new ZipFile(archive)) {
                require(OriginalZipContentCheck.diagnosticHashPrefix(zip, zip.getEntry("Megabonk_Data/sharedassets1.assets"), 128) == null);
            }
            require(OriginalGameDataValidator.describeProblem(new OriginalGameDataValidator.Problem("x", true), false).contains("Missing file"));
            System.out.println("PASS: unsupported ZIP remains rejected and gives a bounded, local fingerprint");
        } finally { archive.delete(); dir.delete(); }
    }
    private SteamZipDiagnosticCheck() {}
}
