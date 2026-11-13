package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;

/** Chains the private sharedassets0 reconstruction stages, cleaning intermediates. */
public final class SteamShared0Complete {
    public static void restore(File apk,File steam,File fonts,File atlases,File metadata,File textures,File output)throws IOException{
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        Path temp=Files.createTempDirectory(parent,".openbonk-shared0-chain-"),a=temp.resolve("fonts"),b=temp.resolve("atlases"),c=temp.resolve("metadata");
        try{
            SteamShared0Fonts.restore(apk,steam,fonts,a.toFile());
            SteamShared0Atlases.restore(a.toFile(),steam,atlases,b.toFile());Files.delete(a);
            SteamShared0Metadata.restore(b.toFile(),steam,metadata,c.toFile());Files.delete(b);
            SteamShared0FinalTextures.restore(c.toFile(),steam,textures,output);
        }finally{Files.deleteIfExists(a);Files.deleteIfExists(b);Files.deleteIfExists(c);Files.deleteIfExists(temp);}
    }
    private SteamShared0Complete(){}
}
