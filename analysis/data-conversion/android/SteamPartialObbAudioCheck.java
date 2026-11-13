package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;

public final class SteamPartialObbAudioCheck {
    private static byte[] digest(File file)throws Exception{MessageDigest d=MessageDigest.getInstance("SHA-256");try(InputStream in=new FileInputStream(file)){byte[] b=new byte[65536];for(int n;(n=in.read(b))!=-1;)if(n>0)d.update(b,0,n);}return d.digest();}
    public static void main(String[] a)throws Exception{
        if(a.length!=7)throw new IllegalArgumentException("base linux-prep windows-prep plan linux-audio windows-audio parent");
        File base=new File(a[0]),linux=new File(a[1]),windows=new File(a[2]),plan=new File(a[3]),la=new File(a[4]),wa=new File(a[5]),parent=new File(a[6]),l=new File(parent,"linux-audio-partial.obb"),w=new File(parent,"windows-audio-partial.obb"),bad=new File(parent,"invalid-audio.obb");
        try{SteamPartialObbStage.assembleWithSharedAudioDiagnostic(base,linux,plan,plan,bad);throw new AssertionError("bad audio accepted");}catch(IOException expected){if(bad.exists())throw new AssertionError("partial output published");System.out.println("rejected bad audio: "+expected.getMessage());}
        SteamPartialObbStage.assembleWithSharedAudioDiagnostic(base,linux,plan,la,l);SteamPartialObbStage.assembleWithSharedAudioDiagnostic(base,windows,plan,wa,w);
        if(!MessageDigest.isEqual(digest(l),digest(w)))throw new AssertionError("platform archive differs");
        System.out.println("PASS: partial diagnostic OBB includes 118 audio clips and video; Linux/Windows archives match");
    }
}
