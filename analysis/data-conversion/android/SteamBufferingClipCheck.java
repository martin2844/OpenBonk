package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;
import java.security.MessageDigest;

/** Host/Android test driver: first two ZIPs must be valid, others rejected. */
public final class SteamBufferingClipCheck {
    public static void main(String[] args) throws Exception {
        if (args.length < 3) throw new IllegalArgumentException("two valid ZIPs and at least one invalid ZIP required");
        byte[] first = SteamBufferingClip.extract(new File(args[0]));
        byte[] second = SteamBufferingClip.extract(new File(args[1]));
        if (first.length != 8740 || !MessageDigest.isEqual(first, second)) throw new AssertionError("clip differs between platforms");
        for (int i = 2; i < args.length; i++) {
            try { SteamBufferingClip.extract(new File(args[i])); throw new AssertionError("invalid ZIP accepted: " + args[i]); }
            catch (IOException expected) { System.out.println("Rejected " + new File(args[i]).getName() + ": " + expected.getMessage()); }
        }
        System.out.println("PASS: pinned Windows/Linux video extraction and rejection of invalid ZIPs");
    }
}
