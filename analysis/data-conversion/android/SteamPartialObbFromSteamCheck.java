package com.openbonk.dataprep;
import java.io.*;
import java.security.*;

public final class SteamPartialObbFromSteamCheck {
    private static byte[] hash(File f)throws Exception{MessageDigest d=MessageDigest.getInstance("SHA-256");try(InputStream in=new FileInputStream(f)){byte[] b=new byte[65536];for(int n;(n=in.read(b))!=-1;)if(n>0)d.update(b,0,n);}return d.digest();}
    public static void main(String[] a)throws Exception{
        if(a.length!=9)throw new IllegalArgumentException("base linux-prep windows-prep partial-plan linux-zip windows-zip corrupt-zip audio-plan parent");
        File base=new File(a[0]),lp=new File(a[1]),wp=new File(a[2]),plan=new File(a[3]),lz=new File(a[4]),wz=new File(a[5]),badZip=new File(a[6]),audioPlan=new File(a[7]),parent=new File(a[8]);
        File bad=new File(parent,"invalid.obb"),l=new File(parent,"linux.obb"),w=new File(parent,"windows.obb");
        try{SteamPartialObbStage.assembleFromSteamDiagnostic(base,lp,plan,badZip,audioPlan,bad);throw new AssertionError("corrupt ZIP accepted");}catch(IOException expected){if(bad.exists())throw new AssertionError("incomplete output published");System.out.println("rejected corrupt ZIP: "+expected.getMessage());}
        SteamPartialObbStage.assembleFromSteamDiagnostic(base,lp,plan,lz,audioPlan,l);SteamPartialObbStage.assembleFromSteamDiagnostic(base,wp,plan,wz,audioPlan,w);
        if(!MessageDigest.isEqual(hash(l),hash(w)))throw new AssertionError("platform archives differ");
        File[] leftovers=parent.listFiles((d,n)->n.startsWith(".openbonk-partial-obb-")||n.startsWith(".openbonk-shared-audio-"));if(leftovers==null||leftovers.length!=0)throw new AssertionError("temporary data remains");
        System.out.println("PASS: partial OBB plus 118 audio clips and video reconstructed from both Steam ZIPs");
    }
}
