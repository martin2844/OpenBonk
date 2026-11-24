package com.megabonk.launcher;

import java.io.IOException;

/** Checks that rejected mod packages get an actionable message instead of raw ZIP errors. */
public final class ModImportMessageCheck {
    public static void main(String[] args) {
        check("package too large", false, "128 MB");
        check("package too large", true, "128 MB");
        check("not a zip", false, "not a ZIP");
        check("not a zip", true, "no es un ZIP");
        check("pc mod package", false, "BepInEx");
        check("pc mod package", true, "BepInEx");
        check("unsupported mod package", false, "manifest.json and main.lua");
        check("manifest.json and main.lua required", false, "manifest.json and main.lua");
        check("zip END header not found", false, "damaged or incomplete");
        check("invalid CEN header", true, "dañado o incompleto");
        check("storage unavailable", false, "Free some space");
        String custom = LauncherImportError.describe(new IOException("content hash mismatch"), false);
        if (!custom.contains("damaged")) throw new AssertionError("damaged content message: " + custom);
        String passthrough = LauncherImportError.describe(new IOException("unexpected custom failure"), false);
        if (!passthrough.equals("unexpected custom failure")) throw new AssertionError("unknown message changed: " + passthrough);
        System.out.println("ModImportMessageCheck PASS");
    }

    private static void check(String message, boolean spanish, String expected) {
        String result = LauncherImportError.describe(new IOException(message), spanish);
        if (!result.contains(expected)) throw new AssertionError(message + " (" + spanish + ") => " + result);
    }

    private ModImportMessageCheck() {}
}
