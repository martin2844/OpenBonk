package com.openbonk.dataprep;
import java.io.*;
import java.util.Arrays;

public final class SteamShared0MetadataCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=5)throw new IllegalArgumentException("base steam plan output expected");
        File out=new File(a[3]),dir=out.getAbsoluteFile().getParentFile();String[] before=dir.list((d,n)->n.startsWith(".openbonk-metadata-"));if(before==null)throw new IOException("missing output directory");Arrays.sort(before);
        try{SteamShared0Metadata.restore(new File(a[0]),new File(a[1]),new File(a[2]),out);if(!a[4].equals("pass"))throw new AssertionError("invalid input accepted");}
        catch(IOException e){if(a[4].equals("pass")||!a[4].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
        String[] after=dir.list((d,n)->n.startsWith(".openbonk-metadata-"));if(after==null)throw new IOException("missing output directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary file leaked");
        System.out.println("PASS: "+a[4]);
    }
}
