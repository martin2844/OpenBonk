package com.openbonk.dataprep;
import java.io.*;
import java.util.Arrays;
public final class SteamShared1LayoutCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=4)throw new IllegalArgumentException("base plan output expected");File out=new File(a[2]),dir=out.getAbsoluteFile().getParentFile();String[] before=dir.list((d,n)->n.startsWith(".openbonk-shared1-"));if(before==null)throw new IOException("missing output directory");Arrays.sort(before);
        try{SteamShared1Layout.restore(new File(a[0]),new File(a[1]),out);if(!a[3].equals("pass"))throw new AssertionError("invalid input accepted");}catch(IOException e){if(a[3].equals("pass")||!a[3].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
        String[] after=dir.list((d,n)->n.startsWith(".openbonk-shared1-"));if(after==null)throw new IOException("missing output directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary file leaked");System.out.println("PASS: "+a[3]);
    }
}
