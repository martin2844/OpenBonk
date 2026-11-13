package com.megabonk.launcher;

import java.io.File;

/** Exercises the same full-ZIP validator used by the Android launcher. */
public final class OriginalGameDataValidatorCheck {
    public static void main(String[] args) {
        if (args.length != 3) throw new IllegalArgumentException("expected valid, modified and duplicate ZIPs");
        if (!OriginalGameDataValidator.valid(new File(args[0]))) throw new AssertionError("valid ZIP rejected");
        if (OriginalGameDataValidator.valid(new File(args[1]))) throw new AssertionError("modified bundle accepted");
        if (OriginalGameDataValidator.valid(new File(args[2]))) throw new AssertionError("duplicate bundle accepted");
        System.out.println("PASS: valid full ZIP accepted; modified and duplicate bundles rejected");
    }
}
