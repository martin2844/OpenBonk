package com.openbonk.dataprep;
import java.io.*;
import java.util.Arrays;
public final class SteamLocalizationTextCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=4&&a.length!=5)throw new IllegalArgumentException("steam plan output expected [count]");int count=a.length==5?Integer.parseInt(a[4]):17;File out=new File(a[2]),dir=out.getAbsoluteFile().getParentFile();String[] before=dir.list((d,n)->n.startsWith(".openbonk-locale-"));if(before==null)throw new IOException("missing output directory");Arrays.sort(before);
        try{SteamLocalizationText.restore(new File(a[0]),new File(a[1]),out);if(!a[3].equals("pass"))throw new AssertionError("invalid input accepted");String[] files=out.list((d,n)->n.endsWith(".bundle"));if(files==null||files.length!=count)throw new AssertionError("missing locales");}catch(IOException e){if(a[3].equals("pass")||!a[3].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
        String[] after=dir.list((d,n)->n.startsWith(".openbonk-locale-"));if(after==null)throw new IOException("missing output directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary files leaked");System.out.println("PASS: "+a[3]);
    }
}
