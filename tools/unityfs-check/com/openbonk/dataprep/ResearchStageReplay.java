package com.openbonk.dataprep;
import java.io.File;
import java.nio.file.Files;
import java.security.MessageDigest;

/** Runs one existing stage for a private multi-stage integration check. */
public final class ResearchStageReplay {
    public static void main(String[] a)throws Exception{
        if(a.length!=6)throw new IllegalArgumentException("stage library template steam plan output");
        StringBuilder h=new StringBuilder();for(byte b:MessageDigest.getInstance("SHA-256").digest(Files.readAllBytes(new File(a[4]).toPath())))h.append(String.format("%02x",b&255));
        String stage=a[0],hash=h.toString();File base=new File(a[2]),steam=new File(a[3]),plan=new File(a[4]),out=new File(a[5]);
        if(stage.equals("cubemap")||stage.equals("texture"))System.load(new File(a[1]).getCanonicalPath());
        switch(stage){
            case "object":SteamPlannedMeshes.restoreExactObjects(base,steam,plan,hash,out);break;
            case "animation":SteamPlannedMeshes.restoreAnimations(base,steam,plan,hash,out);break;
            case "cubemap":SteamPlannedTextures.restoreCubemaps(base,steam,plan,hash,out);break;
            case "mesh":SteamPlannedMeshes.restore(base,steam,plan,hash,out);break;
            case "texture":SteamPlannedTextures.restore(base,steam,plan,hash,out);break;
            case "packed":SteamPackedAudio.restore(base,steam,plan,hash,out);break;
            case "audio":SteamGeneratedAudio.restore(base,steam,plan,hash,out);break;
            default:throw new IllegalArgumentException("unsupported stage");
        }
        System.out.println("PASS: "+stage+" stage restored");
    }
}
