package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;

/** Local round-trip check; all supplied game data remains local. */
public final class SteamPlannedAnimationsCheck {
    private static byte[] hash(InputStream input) throws Exception {
        MessageDigest digest = MessageDigest.getInstance("SHA-256"); byte[] buffer = new byte[65536];
        try (InputStream in = input) { for (int n; (n = in.read(buffer)) != -1;) if (n > 0) digest.update(buffer, 0, n); }
        return digest.digest();
    }
    private static String hex(byte[] data) { StringBuilder out = new StringBuilder(); for (byte b : data) out.append(String.format("%02x", b & 255)); return out.toString(); }
    public static void main(String[] args) throws Exception {
        if (args.length != 6 || !(args[5].equals("pass") || args[5].equals("reject-plan") || args[5].equals("reject-input"))) throw new IllegalArgumentException("template steam-zip plan output expected-obb pass|reject-plan|reject-input");
        File output = new File(args[3]);
        String planHash = hex(hash(new FileInputStream(args[2])));
        if (args[5].equals("reject-plan")) planHash = "0000000000000000000000000000000000000000000000000000000000000000";
        try { SteamPlannedMeshes.restoreAnimations(new File(args[0]), new File(args[1]), new File(args[2]), planHash, output); }
        catch (IOException e) {
            if (!args[5].startsWith("reject-") || Files.exists(output.toPath(), LinkOption.NOFOLLOW_LINKS)) throw e;
            System.out.println("PASS: invalid input rejected without output"); return;
        }
        if (!args[5].equals("pass")) throw new AssertionError("invalid plan accepted");
        try (ZipFile actual = new ZipFile(output); ZipFile expected = new ZipFile(args[4])) {
            Set<String> names = new HashSet<>(); Enumeration<? extends ZipEntry> entries = expected.entries();
            while (entries.hasMoreElements()) {
                ZipEntry e = entries.nextElement(); if (e.isDirectory()) continue;
                if (!names.add(e.getName())) throw new AssertionError("duplicate expected entry");
                ZipEntry got = actual.getEntry(e.getName());
                if (got == null || !Arrays.equals(hash(expected.getInputStream(e)), hash(actual.getInputStream(got)))) throw new AssertionError("entry differs");
            }
            int actualCount = 0; Enumeration<? extends ZipEntry> actualEntries = actual.entries();
            while (actualEntries.hasMoreElements()) if (!actualEntries.nextElement().isDirectory()) actualCount++;
            if (actualCount != names.size()) throw new AssertionError("entry count differs");
            System.out.println("PASS: restored " + actualCount + " archive entries exactly");
        }
    }
    private SteamPlannedAnimationsCheck() {}
}
