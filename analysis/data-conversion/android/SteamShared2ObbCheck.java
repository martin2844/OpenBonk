package com.openbonk.dataprep;
import java.io.File;
import java.util.zip.ZipFile;

public final class SteamShared2ObbCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=14)throw new IllegalArgumentException("library base prep partial steam audio1 audio2 audio4 map mesh texture animation final output");
        System.load(a[0]);
        SteamPartialObbStage.assembleShared2AndMapDiagnostic(new File(a[1]),new File(a[2]),new File(a[3]),new File(a[4]),new File(a[5]),new File(a[6]),new File(a[7]),new File(a[8]),new File(a[9]),new File(a[10]),new File(a[11]),new File(a[12]),new File(a[13]));
        try(ZipFile zip=new ZipFile(a[13])){if(zip.size()!=946||!zip.getComment().contains("INCOMPLETE"))throw new AssertionError("invalid diagnostic archive");}
        System.out.println("PASS: reconstructed shared2, map and audio in incomplete diagnostic OBB");
    }
}
