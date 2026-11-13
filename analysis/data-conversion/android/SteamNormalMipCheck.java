package com.openbonk.dataprep;
import java.nio.file.*;
public final class SteamNormalMipCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=4)throw new IllegalArgumentException("library source rgba etc1");System.load(a[0]);byte[] src=Files.readAllBytes(Paths.get(a[1]));
        byte[] rgba=SteamConvertedTextures.convertNormalMip(src,32,32,4),etc=SteamConvertedTextures.convertNormalMip(src,32,32,34);
        if(rgba==null||rgba.length!=4096||etc==null||etc.length!=512)throw new AssertionError("normal conversion failed");
        Files.write(Paths.get(a[2]),rgba,StandardOpenOption.CREATE_NEW);Files.write(Paths.get(a[3]),etc,StandardOpenOption.CREATE_NEW);System.out.println("PASS: normal probe completed");
    }
}
