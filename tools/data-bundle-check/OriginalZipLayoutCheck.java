package com.megabonk.launcher;

import java.io.File;

/** Validate real local ZIP fixtures without Android dependencies. */
public final class OriginalZipLayoutCheck {
    public static void main(String[] args) {
        for (String path : args) {
            if (!OriginalGameDataValidator.valid(new File(path)))
                throw new AssertionError("Android validator rejected " + path);
        }
        System.out.println("Android Steam ZIP layouts: PASS");
    }
}
