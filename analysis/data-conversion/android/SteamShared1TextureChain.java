package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;

/** Builds an incomplete shared1 diagnostic asset; other object types remain blank. */
public final class SteamShared1TextureChain {
    public static void restore(File baseObb,File steam,File[] plans,File output)throws IOException{
        if(plans==null||plans.length!=18)throw new IOException("expected 18 preparation plans");
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        Path temp=Files.createTempDirectory(parent,".openbonk-shared1-chain-"),previous=null,next=null;
        try{
            next=temp.resolve("stage-0");SteamShared1Layout.restore(baseObb,plans[0],next.toFile());previous=next;
            next=temp.resolve("stage-1");SteamShared1MaterialAudio.restore(previous.toFile(),steam,plans[1],next.toFile());Files.delete(previous);previous=next;
            for(int i=2;i<6;i++){next=temp.resolve("stage-"+i);SteamShared1Meshes.restore(previous.toFile(),steam,plans[i],next.toFile());Files.delete(previous);previous=next;}
            for(int i=6;i<18;i++){next=i==17?dest:temp.resolve("stage-"+i);SteamShared1ExactTextures.restore(previous.toFile(),steam,plans[i],next.toFile());Files.delete(previous);previous=next;}
        }finally{
            try(DirectoryStream<Path> entries=Files.newDirectoryStream(temp)){for(Path p:entries)Files.deleteIfExists(p);}Files.deleteIfExists(temp);
        }
    }
    private SteamShared1TextureChain(){}
}
