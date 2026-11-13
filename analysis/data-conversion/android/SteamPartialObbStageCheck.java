package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.zip.ZipFile;

public final class SteamPartialObbStageCheck {
    public static void main(String[] a)throws Exception{
        if(a.length!=6)throw new IllegalArgumentException("base linux-output windows-output bad-plan plan parent");
        File base=new File(a[0]),linux=new File(a[1]),windows=new File(a[2]),badPlan=new File(a[3]),plan=new File(a[4]),parent=new File(a[5]);
        File bad=new File(parent,"invalid.obb"),l=new File(parent,"linux-partial.obb"),w=new File(parent,"windows-partial.obb");
        try{SteamPartialObbStage.assembleDiagnostic(base,linux,badPlan,bad);throw new AssertionError("bad plan accepted");}catch(IOException expected){if(bad.exists())throw new AssertionError("partial output published");System.out.println("rejected bad plan: "+expected.getMessage());}
        try{SteamPartialObbStage.assembleDiagnostic(base,parent,plan,bad);throw new AssertionError("missing input accepted");}catch(IOException expected){if(bad.exists())throw new AssertionError("partial output published");System.out.println("rejected missing input: "+expected.getMessage());}
        SteamPartialObbStage.assembleDiagnostic(base,linux,plan,l);SteamPartialObbStage.assembleDiagnostic(base,windows,plan,w);
        if(!MessageDigest.isEqual(Files.readAllBytes(l.toPath()),Files.readAllBytes(w.toPath())))throw new AssertionError("archives differ");
        try(ZipFile z=new ZipFile(l)){if(z.size()!=946||!z.getComment().contains("INCOMPLETE"))throw new AssertionError("invalid partial archive");}
        try{SteamPartialObbStage.assembleDiagnostic(base,linux,plan,l);throw new AssertionError("existing output replaced");}catch(IOException expected){}
        File[] left=parent.listFiles((dir,n)->n.startsWith(".openbonk-partial-obb-"));if(left==null||left.length!=0)throw new AssertionError("temporary archive left behind");
        System.out.println("PASS: explicitly incomplete diagnostic OBB; 344 verified replacements; Linux/Windows archives match");
    }
}
