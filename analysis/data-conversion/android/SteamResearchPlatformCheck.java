package com.openbonk.dataprep;

import java.io.File;
import java.io.IOException;

/** Standalone selector check for private research ZIPs. */
public final class SteamResearchPlatformCheck {
    public static void main(String[] args) throws Exception {
        if (args.length != 2) throw new IllegalArgumentException("ZIP linux|windows|reject");
        try {
            String result = SteamResearchPlatform.select(new File(args[0]));
            if (!result.equals(args[1])) throw new AssertionError("unexpected platform");
        } catch (IOException e) {
            if (!"reject".equals(args[1])) throw e;
            System.out.println("PASS: invalid or ambiguous ZIP rejected"); return;
        }
        System.out.println("PASS: " + args[1] + " selected");
    }
    private SteamResearchPlatformCheck() {}
}
