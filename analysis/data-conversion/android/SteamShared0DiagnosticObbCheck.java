package com.openbonk.dataprep;
import java.io.*;
import java.util.zip.ZipFile;
public final class SteamShared0DiagnosticObbCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=3)throw new IllegalArgumentException("partial asset output");SteamShared0DiagnosticObb.assemble(new File(a[0]),new File(a[1]),new File(a[2]));
        try(ZipFile z=new ZipFile(a[2])){if(z.size()!=1072||!z.getComment().contains("INCOMPLETE"))throw new AssertionError("incorrect diagnostic archive");}
        System.out.println("PASS: shared0 added to incomplete diagnostic OBB");
    }
}
