package com.megabonk.launcher;

import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Host check for version-pinned localization bundle validation. */
public final class OriginalLocalizationManifestCheck {
    public static void main(String[] args) throws Exception {
        if (args.length != 2) throw new IllegalArgumentException("expected valid and modified ZIPs");
        if (OriginalLocalizationManifest.BUNDLES.length != 18 ||
            OriginalLocalizationManifest.BUNDLES.length != OriginalLocalizationManifest.SHA256.length)
            throw new AssertionError("unexpected manifest count");
        try (ZipFile good = new ZipFile(args[0]); ZipFile bad = new ZipFile(args[1])) {
            for (int i = 0; i < OriginalLocalizationManifest.BUNDLES.length; i++) {
                String name = OriginalLocalizationManifest.BUNDLES[i];
                ZipEntry entry = good.getEntry(name);
                if (!OriginalZipContentCheck.valid(good, entry, OriginalLocalizationManifest.SHA256[i]))
                    throw new AssertionError("valid bundle rejected: " + name);
                ZipEntry changed = bad.getEntry(name);
                if (i == 0) {
                    if (!OriginalZipContentCheck.valid(bad, changed, null))
                        throw new AssertionError("modified ZIP should retain a valid CRC");
                    if (OriginalZipContentCheck.valid(bad, changed, OriginalLocalizationManifest.SHA256[i]))
                        throw new AssertionError("modified content accepted");
                } else if (!OriginalZipContentCheck.valid(bad, changed, OriginalLocalizationManifest.SHA256[i])) {
                    throw new AssertionError("unmodified bundle rejected: " + name);
                }
            }
        }
        System.out.println("PASS: 18 exact bundles accepted; modified bundle with valid CRC rejected");
    }
}
