package com.openbonk.dataprep;
import java.io.*;
import java.security.*;

public final class SteamThreeAudioObbCheck {
    private static byte[] hash(File f)throws Exception{MessageDigest d=MessageDigest.getInstance("SHA-256");try(InputStream in=new FileInputStream(f)){byte[] b=new byte[65536];for(int n;(n=in.read(b))!=-1;)if(n>0)d.update(b,0,n);}return d.digest();}
    public static void main(String[] a)throws Exception{
        if(a.length!=11)throw new IllegalArgumentException("base linux-prep windows-prep partial-plan linux-zip windows-zip audio1 audio2 audio4 foreign-plan parent");
        File base=new File(a[0]),lp=new File(a[1]),wp=new File(a[2]),plan=new File(a[3]),lz=new File(a[4]),wz=new File(a[5]),p1=new File(a[6]),p2=new File(a[7]),p4=new File(a[8]),foreign=new File(a[9]),parent=new File(a[10]),bad=new File(parent,"invalid.obb"),l=new File(parent,"linux.obb"),w=new File(parent,"windows.obb");
        try{SteamPartialObbStage.assembleThreeAudioFromSteamDiagnostic(base,lp,plan,lz,p1,p2,foreign,bad);throw new AssertionError("bad third plan accepted");}catch(IOException expected){if(bad.exists())throw new AssertionError("incomplete output published");System.out.println("rejected bad third plan: "+expected.getMessage());}
        SteamPartialObbStage.assembleThreeAudioFromSteamDiagnostic(base,lp,plan,lz,p1,p2,p4,l);SteamPartialObbStage.assembleThreeAudioFromSteamDiagnostic(base,wp,plan,wz,p1,p2,p4,w);
        if(!MessageDigest.isEqual(hash(l),hash(w)))throw new AssertionError("platform archives differ");
        File[] leftovers=parent.listFiles((d,n)->n.startsWith(".openbonk-partial-obb-")||n.startsWith(".openbonk-shared-audio-"));if(leftovers==null||leftovers.length!=0)throw new AssertionError("temporary data remains");
        System.out.println("PASS: partial OBB with three shared audio resources matches across Steam platforms");
    }
}
