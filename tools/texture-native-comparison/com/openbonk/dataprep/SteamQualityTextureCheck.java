package com.openbonk.dataprep;

import java.io.*;
import java.security.MessageDigest;
import java.util.Arrays;

/** Local-only cross-device check; the fixture may contain private game data. */
public final class SteamQualityTextureCheck {
    public static void main(String[] args) throws Exception {
        if (args.length != 2) throw new IllegalArgumentException("library fixture");
        System.load(new File(args[0]).getCanonicalPath());
        if (SteamConvertedTextures.convertQualityMip(new byte[1], 2, 2, 4) != null ||
            SteamConvertedTextures.convertQualityMip(new byte[4], 0, 1, 4) != null)
            throw new AssertionError("invalid source accepted");
        MessageDigest aggregate = MessageDigest.getInstance("SHA-256"); int count;
        try (DataInputStream in = new DataInputStream(new BufferedInputStream(new FileInputStream(args[1])))) {
            byte[] magic = new byte[8]; in.readFully(magic);
            if (!Arrays.equals(magic, new byte[]{'O','B','H','Q','0','0','0','1'})) throw new IOException("invalid fixture");
            count = in.readInt(); if (count <= 0 || count > 1000) throw new IOException("invalid count");
            for (int i = 0; i < count; i++) {
                int w = in.readInt(), h = in.readInt(), format = in.readInt(), n = in.readInt();
                if (w < 1 || h < 1 || w > 2048 || h > 2048 || n < 1 || n > 64 * 1024 * 1024) throw new IOException("invalid fixture dimensions");
                byte[] input = new byte[n]; in.readFully(input);
                byte[] output = SteamConvertedTextures.convertQualityMip(input, w, h, format);
                if (output == null || output.length != ((w + 3) / 4) * ((h + 3) / 4) * 16) throw new IOException("conversion failed");
                aggregate.update(MessageDigest.getInstance("SHA-256").digest(output));
            }
            if (in.read() != -1) throw new IOException("trailing fixture data");
        }
        StringBuilder hex = new StringBuilder(); for (byte b : aggregate.digest()) hex.append(String.format("%02x", b & 255));
        System.out.println("PASS " + count + " " + hex);
    }
    private SteamQualityTextureCheck() {}
}
