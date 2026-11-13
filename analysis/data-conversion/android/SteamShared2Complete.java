package com.openbonk.dataprep;
import java.io.File;
import java.io.IOException;

/** Reconstructs sharedassets2; the surrounding OBB remains incomplete. */
public final class SteamShared2Complete {
    public static byte[] restore(File base,File steam,File meshPlan,File texturePlan,File animationPlan,File finalTexturePlan)throws IOException{
        byte[] asset=SteamShared2Meshes.restorePartial(base,steam,meshPlan);
        asset=SteamShared2Textures.restorePartial(asset,steam,texturePlan);
        asset=SteamShared2Animations.restorePartial(asset,steam,animationPlan);
        return SteamShared2FinalTextures.restore(asset,steam,finalTexturePlan);
    }
    private SteamShared2Complete(){}
}
