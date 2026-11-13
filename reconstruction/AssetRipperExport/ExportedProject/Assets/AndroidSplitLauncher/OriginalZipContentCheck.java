package com.megabonk.launcher;

import java.io.InputStream;
import java.security.MessageDigest;
import java.util.zip.CRC32;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Checks the uncompressed bytes before a pending import replaces an older one. */
final class OriginalZipContentCheck {
    static boolean valid(ZipFile zip, ZipEntry entry, String expectedHash) throws Exception {
        if (entry == null || entry.getSize() < 0 || entry.getCrc() < 0) return false;
        MessageDigest digest = expectedHash == null ? null : MessageDigest.getInstance("SHA-256");
        CRC32 crc = new CRC32();
        long total = 0;
        try (InputStream input = zip.getInputStream(entry)) {
            byte[] buffer = new byte[65536];
            int count;
            while ((count = input.read(buffer)) != -1) {
                total += count;
                if (total > entry.getSize()) return false;
                crc.update(buffer, 0, count);
                if (digest != null) digest.update(buffer, 0, count);
            }
        }
        if (total != entry.getSize() || crc.getValue() != entry.getCrc()) return false;
        if (digest != null) {
            StringBuilder hash = new StringBuilder(64);
            for (byte b : digest.digest()) hash.append(String.format(java.util.Locale.ROOT, "%02x", b & 255));
            return hash.toString().equals(expectedHash);
        }
        return true;
    }
}
