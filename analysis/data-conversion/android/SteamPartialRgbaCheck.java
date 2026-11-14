package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;

/** Private local round-trip check for the generated texture plan. */
public final class SteamPartialRgbaCheck {
    private static byte[] hash(InputStream input) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256"); byte[] buffer = new byte[65536];
        try (InputStream in = input) { for (int n; (n = in.read(buffer)) != -1;) if (n > 0) digest.update(buffer, 0, n); }
        return digest.digest();
    }
    private static String hex(byte[] data) { StringBuilder s = new StringBuilder(); for (byte b : data) s.append(String.format("%02x", b & 255)); return s.toString(); }
    public static void main(String[] args) throws Exception {
        if (args.length != 7 || !(args[6].equals("pass") || args[6].equals("reject-plan") || args[6].equals("reject-input")))
            throw new IllegalArgumentException("library template steam-zip plan output expected-obb pass|reject-plan|reject-input");
        System.load(new File(args[0]).getCanonicalPath()); File output = new File(args[4]); String planHash = hex(hash(new FileInputStream(args[3])));
        if (args[6].equals("reject-plan")) planHash = "0000000000000000000000000000000000000000000000000000000000000000";
        try { SteamPlannedTextures.restorePartialRgba(new File(args[1]), new File(args[2]), new File(args[3]), planHash, output); }
        catch (IOException e) {
            if (!args[6].startsWith("reject-") || Files.exists(output.toPath(), LinkOption.NOFOLLOW_LINKS)) throw e;
            System.out.println("PASS: invalid input rejected without output"); return;
        }
        if (!args[6].equals("pass")) throw new AssertionError("invalid input accepted");
        try (ZipFile actual = new ZipFile(output); ZipFile expected = new ZipFile(args[5])) {
            Set<String> names = new HashSet<>(); Enumeration<? extends ZipEntry> entries = expected.entries();
            while (entries.hasMoreElements()) {
                ZipEntry e = entries.nextElement(); if (e.isDirectory()) continue;
                if (!names.add(e.getName())) throw new AssertionError("duplicate expected entry");
                ZipEntry got = actual.getEntry(e.getName());
                if (got == null || !Arrays.equals(hash(expected.getInputStream(e)), hash(actual.getInputStream(got)))) throw new AssertionError("archive entry differs");
            }
            int count = 0; Enumeration<? extends ZipEntry> entries2 = actual.entries();
            while (entries2.hasMoreElements()) if (!entries2.nextElement().isDirectory()) count++;
            if (count != names.size()) throw new AssertionError("archive entry count differs");
            System.out.println("PASS: restored " + count + " archive entries exactly");
        }
    }
    private SteamPartialRgbaCheck() {}
}
