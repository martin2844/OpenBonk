package com.openbonk.dataprep;

import java.io.*;
import java.security.MessageDigest;
import java.util.*;

/** Standalone local-only check; fixtures contain game data. */
public final class SteamConvertedTextures {
    static native byte[] convertMip(byte[] source, int width, int height, int sourceFormat, int targetFormat);
    static native byte[] convertQualityMip(byte[] source, int width, int height, int sourceFormat);
    static native byte[] convertNormalMip(byte[] source, int width, int height, int targetFormat);
    private static final class Counts { int total, equal, failed; }
    public static void main(String[] args) throws Exception {
        if (args.length != 2) throw new IllegalArgumentException("native-library fixture-file");
        System.load(new File(args[0]).getCanonicalPath());
        Map<String, Counts> groups = new TreeMap<>(); int total = 0, equal = 0, failed = 0;
        try (DataInputStream in = new DataInputStream(new BufferedInputStream(new FileInputStream(args[1])))) {
            byte[] magic = new byte[8]; in.readFully(magic);
            if (!Arrays.equals(magic, new byte[]{'O','B','T','X','0','0','0','1'})) throw new IOException("invalid fixture");
            int count = in.readInt(); if (count <= 0 || count > 50000) throw new IOException("invalid fixture count");
            for (int i = 0; i < count; i++) {
                int width = in.readInt(), height = in.readInt(), src = in.readInt(), dst = in.readInt(), length = in.readInt();
                if (width <= 0 || height <= 0 || width > 4096 || height > 4096 || length <= 0 || length > 64 * 1024 * 1024) throw new IOException("invalid fixture dimensions");
                byte[] expected = new byte[32], payload = new byte[length]; in.readFully(expected); in.readFully(payload);
                byte[] actual = convertMip(payload, width, height, src, dst);
                Counts c = groups.computeIfAbsent(src + "->" + dst, k -> new Counts()); c.total++; total++;
                if (actual == null) { c.failed++; failed++; }
                else if (MessageDigest.isEqual(expected, MessageDigest.getInstance("SHA-256").digest(actual))) { c.equal++; equal++; }
            }
            if (in.read() != -1) throw new IOException("trailing fixture data");
        }
        for (Map.Entry<String, Counts> e : groups.entrySet()) {
            Counts c = e.getValue(); System.out.println(e.getKey() + ": " + c.equal + "/" + c.total + " exact, " + c.failed + " failed");
        }
        System.out.println("total: " + equal + "/" + total + " exact, " + failed + " failed");
    }
    private SteamConvertedTextures() {}
}
