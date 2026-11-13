package com.openbonk.dataprep;
import java.io.*;
import java.util.Arrays;
public final class SteamCubemapResourcesCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=6)throw new IllegalArgumentException("library base steam plan output expected");System.load(a[0]);File out=new File(a[4]),dir=out.getAbsoluteFile().getParentFile();String[] before=dir.list((d,n)->n.startsWith(".openbonk-shared1-cubemap-"));if(before==null)throw new IOException("missing output directory");Arrays.sort(before);
        try{SteamCubemapResources.restore(new File(a[1]),new File(a[2]),new File(a[3]),out);if(!a[5].equals("pass"))throw new AssertionError("invalid input accepted");}catch(IOException e){if(a[5].equals("pass")||!a[5].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
        String[] after=dir.list((d,n)->n.startsWith(".openbonk-shared1-cubemap-"));if(after==null)throw new IOException("missing output directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary file leaked");System.out.println("PASS: "+a[5]);
    }
}
