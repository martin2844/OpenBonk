package com.openbonk.dataprep;

/** Exercises bit selection across byte and mip boundaries. */
public final class SteamTextureChoiceCheck {
    public static void main(String[] args) {
        byte[] mask = { (byte) 0x80, 0x01, 0x00, (byte) 0x80 };
        if (SteamPlannedTextures.hasSelectedBytes(mask, 0, 7)) throw new AssertionError("empty prefix");
        if (!SteamPlannedTextures.hasSelectedBytes(mask, 7, 1)) throw new AssertionError("last bit");
        if (!SteamPlannedTextures.hasSelectedBytes(mask, 8, 1)) throw new AssertionError("first bit");
        if (SteamPlannedTextures.hasSelectedBytes(mask, 9, 22)) throw new AssertionError("empty interior");
        if (!SteamPlannedTextures.hasSelectedBytes(mask, 30, 2)) throw new AssertionError("last byte");
        if (SteamPlannedTextures.hasSelectedBytes(mask, 8, 0)) throw new AssertionError("empty range");
        System.out.println("PASS: texture choice ranges");
    }
}
