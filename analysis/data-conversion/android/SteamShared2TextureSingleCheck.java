package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;

public final class SteamShared2TextureSingleCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=6)throw new IllegalArgumentException("library base zip mesh-plan texture-plan output");System.load(a[0]);
        byte[] mesh=SteamShared2Meshes.restorePartial(new File(a[1]),new File(a[2]),new File(a[3]));
        byte[] result=SteamShared2Textures.restorePartial(mesh,new File(a[2]),new File(a[4]));
        Files.write(Paths.get(a[5]),result,StandardOpenOption.CREATE_NEW);System.out.println("PASS: 72 textures; incomplete asset bytes="+result.length);
    }
}
