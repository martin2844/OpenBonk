package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.util.Arrays;

/** Private diagnostic pipeline; plans and shader pack require distribution review. */
public final class SteamShared1Complete {
    public static void restore(File baseObb, File steam, File[] plans, File shaderPack, File output) throws IOException {
        if (plans == null || plans.length != 23) throw new IOException("expected 23 preparation plans");
        Path dest=output.toPath().toAbsolutePath(), parent=dest.getParent();
        if(parent==null || !Files.isDirectory(parent) || Files.exists(dest,LinkOption.NOFOLLOW_LINKS)) throw new IOException("output must be new");
        Path temp=Files.createTempDirectory(parent,".openbonk-shared1-complete-"), previous=null, next;
        try {
            next=temp.resolve("textures");
            SteamShared1TextureChain.restore(baseObb,steam,Arrays.copyOfRange(plans,0,18),next.toFile()); previous=next;
            next=temp.resolve("shaders"); SteamShared1Shaders.restore(previous.toFile(),shaderPack,plans[18],next.toFile()); Files.delete(previous); previous=next;
            next=temp.resolve("animation"); SteamShared1AnimationTerrain.restore(previous.toFile(),steam,plans[19],next.toFile()); Files.delete(previous); previous=next;
            next=temp.resolve("cubemaps"); SteamCubemapResources.restore(previous.toFile(),steam,plans[20],next.toFile()); Files.delete(previous); previous=next;
            next=temp.resolve("outline"); SteamShared1OutlineMeshes.restore(previous.toFile(),steam,plans[21],next.toFile()); Files.delete(previous); previous=next;
            SteamShared1FinalObjects.restore(previous.toFile(),steam,plans[22],dest.toFile());
        } finally {
            try(DirectoryStream<Path> entries=Files.newDirectoryStream(temp)){for(Path p:entries)Files.deleteIfExists(p);}
            Files.deleteIfExists(temp);
        }
    }
    private SteamShared1Complete(){}
}
