package com.openbonk.dataprep;
import java.io.*;
import java.util.Arrays;
public final class SteamShared1CompleteCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=29)throw new IllegalArgumentException("library base steam shader-pack 23-plans output expected");
        System.load(a[0]); File[] plans=new File[23];for(int i=0;i<23;i++)plans[i]=new File(a[4+i]);
        File out=new File(a[27]),dir=out.getAbsoluteFile().getParentFile();
        String[] before=dir.list((d,n)->n.startsWith(".openbonk-shared1-complete-"));if(before==null)throw new IOException("missing output directory");Arrays.sort(before);
        try{SteamShared1Complete.restore(new File(a[1]),new File(a[2]),plans,new File(a[3]),out);if(!a[28].equals("pass"))throw new AssertionError("invalid input accepted");}
        catch(IOException e){if(a[28].equals("pass")||!a[28].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
        String[] after=dir.list((d,n)->n.startsWith(".openbonk-shared1-complete-"));if(after==null)throw new IOException("missing output directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary files leaked");System.out.println("PASS: "+a[28]);
    }
}
