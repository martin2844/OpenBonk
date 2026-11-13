package com.openbonk.dataprep;

/** JNI declaration for standalone native conversion checks. */
public final class SteamCubemapResources {
    static native byte[] unpackCrunch(byte[] input);
    private SteamCubemapResources() {}
}
