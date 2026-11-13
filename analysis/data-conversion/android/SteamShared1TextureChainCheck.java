package com.openbonk.dataprep;
import java.io.*;
import java.util.Arrays;

public final class SteamShared1TextureChainCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=23)throw new IllegalArgumentException("library base steam 18-plans output expected");System.load(a[0]);File[] plans=new File[18];for(int i=0;i<18;i++)plans[i]=new File(a[3+i]);File out=new File(a[21]),dir=out.getAbsoluteFile().getParentFile();String[] before=dir.list((d,n)->n.startsWith(".openbonk-shared1-chain-"));if(before==null)throw new IOException("missing output directory");Arrays.sort(before);
        try{SteamShared1TextureChain.restore(new File(a[1]),new File(a[2]),plans,out);if(!a[22].equals("pass"))throw new AssertionError("invalid input accepted");}catch(IOException e){if(a[22].equals("pass")||!a[22].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
        String[] after=dir.list((d,n)->n.startsWith(".openbonk-shared1-chain-"));if(after==null)throw new IOException("missing output directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary files leaked");System.out.println("PASS: "+a[22]);
    }
}
