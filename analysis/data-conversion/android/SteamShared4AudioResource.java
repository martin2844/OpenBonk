package com.openbonk.dataprep;
import java.io.File;
import java.io.IOException;

/** Experimental reconstruction of the third shared Steam audio resource. */
public final class SteamShared4AudioResource {
    public static byte[] restore(File steam,File plan)throws IOException{return SteamShared2AudioResource.restoreShared(steam,plan,true);}
    private SteamShared4AudioResource(){}
}
