package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.util.Enumeration;
import java.util.HashSet;
import java.util.Set;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Selects a research plan; subsequent stages still validate all source hashes. */
public final class SteamResearchPlatform {
    private static boolean matches(String name, String suffix) {
        return name.equals(suffix) || name.equals("Megabonk_Data/" + suffix) || name.endsWith("/Megabonk_Data/" + suffix);
    }
    public static String select(File file) throws IOException {
        String suffix = "/localization-assets-shared_assets_all.bundle";
        String linux = "StreamingAssets/aa/StandaloneLinux64" + suffix;
        String windows = "StreamingAssets/aa/StandaloneWindows64" + suffix;
        boolean hasLinux = false, hasWindows = false; int count = 0;
        Set<String> seen = new HashSet<>();
        try (ZipFile zip = new ZipFile(file)) {
            Enumeration<? extends ZipEntry> entries = zip.entries();
            while (entries.hasMoreElements()) {
                ZipEntry entry = entries.nextElement(); String name = entry.getName();
                if (++count > 20000 || !seen.add(name) || name.startsWith("/") || name.indexOf('\\') >= 0 || name.indexOf(':') >= 0)
                    throw new IOException("invalid Steam ZIP entry");
                if (entry.isDirectory()) continue;
                for (String part : name.split("/", -1)) if (part.isEmpty() || part.equals(".") || part.equals("..")) throw new IOException("invalid Steam ZIP path");
                if (matches(name, linux)) { if (hasLinux) throw new IOException("duplicate Linux input"); hasLinux = true; }
                if (matches(name, windows)) { if (hasWindows) throw new IOException("duplicate Windows input"); hasWindows = true; }
            }
        }
        if (hasLinux == hasWindows) throw new IOException("Steam ZIP platform is missing or ambiguous");
        return hasWindows ? "windows" : "linux";
    }
    private SteamResearchPlatform() {}
}
