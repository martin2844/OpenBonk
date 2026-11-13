package com.openbonk.dataprep;
import java.io.*;
import java.util.Arrays;
public final class SteamShared0CompleteCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=9)throw new IllegalArgumentException("library apk steam font-plan atlas-plan metadata-plan texture-plan output expected");System.load(a[0]);
        File output=new File(a[7]),dir=output.getAbsoluteFile().getParentFile();String[] before=dir.list((d,n)->n.startsWith(".openbonk-shared0-chain-"));if(before==null)throw new IOException("missing output directory");Arrays.sort(before);
        try{SteamShared0Complete.restore(new File(a[1]),new File(a[2]),new File(a[3]),new File(a[4]),new File(a[5]),new File(a[6]),output);if(!a[8].equals("pass"))throw new AssertionError("invalid input accepted");}
        catch(IOException e){if(a[8].equals("pass")||!a[8].equals(e.getMessage()))throw e;if(output.exists())throw new AssertionError("partial output published");}
        String[] after=dir.list((d,n)->n.startsWith(".openbonk-shared0-chain-"));if(after==null)throw new IOException("missing output directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("intermediate files leaked");System.out.println("PASS: "+a[8]);
    }
}
