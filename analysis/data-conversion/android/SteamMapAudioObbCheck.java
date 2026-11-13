package com.openbonk.dataprep;
import java.io.*;
import java.security.*;

public final class SteamMapAudioObbCheck {
    private static byte[] hash(File f)throws Exception{MessageDigest d=MessageDigest.getInstance("SHA-256");try(InputStream in=new FileInputStream(f)){byte[] b=new byte[65536];for(int n;(n=in.read(b))!=-1;)if(n>0)d.update(b,0,n);}return d.digest();}
    public static void main(String[] a)throws Exception{
        if(a.length!=13)throw new IllegalArgumentException("library base linux-prep windows-prep partial-plan linux-zip windows-zip audio1 audio2 audio4 map foreign-plan parent");System.load(a[0]);
        File base=new File(a[1]),lp=new File(a[2]),wp=new File(a[3]),plan=new File(a[4]),lz=new File(a[5]),wz=new File(a[6]),p1=new File(a[7]),p2=new File(a[8]),p4=new File(a[9]),map=new File(a[10]),foreign=new File(a[11]),parent=new File(a[12]),bad=new File(parent,"invalid.obb"),l=new File(parent,"linux.obb"),w=new File(parent,"windows.obb");
        try{SteamPartialObbStage.assembleMapAndAudioDiagnostic(base,lp,plan,lz,p1,p2,p4,foreign,bad);throw new AssertionError("bad map plan accepted");}catch(IOException expected){if(bad.exists())throw new AssertionError("incomplete output published");System.out.println("rejected bad map plan: "+expected.getMessage());}
        SteamPartialObbStage.assembleMapAndAudioDiagnostic(base,lp,plan,lz,p1,p2,p4,map,l);SteamPartialObbStage.assembleMapAndAudioDiagnostic(base,wp,plan,wz,p1,p2,p4,map,w);
        if(!MessageDigest.isEqual(hash(l),hash(w)))throw new AssertionError("platform archives differ");
        File[] left=parent.listFiles((d,n)->n.startsWith(".openbonk-partial-obb-")||n.startsWith(".openbonk-shared-audio-"));if(left==null||left.length!=0)throw new AssertionError("temporary data remains");
        System.out.println("PASS: partial OBB with final-map asset and three audio resources matches across Steam platforms");
    }
}
