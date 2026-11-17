package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;

public final class LocalizationObbCheck {
    private static byte[] hash(InputStream in)throws Exception{MessageDigest d=MessageDigest.getInstance("SHA-256");try(InputStream x=in){byte[] b=new byte[65536];for(int n;(n=x.read(b))!=-1;)if(n>0)d.update(b,0,n);}return d.digest();}
    private static String hex(byte[] data){StringBuilder s=new StringBuilder();for(byte b:data)s.append(String.format("%02x",b&255));return s.toString();}
    public static void main(String[] a)throws Exception{
        if(a.length!=5 && a.length!=6)throw new IllegalArgumentException("template steam-zip plan output expected [reject-input]");
        File out=new File(a[3]);String planHash=hex(hash(new FileInputStream(a[2])));
        if(a.length==6){
            if(!a[5].equals("reject-input"))throw new IllegalArgumentException("invalid mode");
            try{SteamLocalizationObb.restore(new File(a[0]),new File(a[1]),new File(a[2]),planHash,out);throw new AssertionError("corrupt input accepted");}
            catch(IOException expected){if(out.exists())throw new AssertionError("output on failure");System.out.println("PASS: damaged Steam ZIP rejected without output");return;}
        }
        try{SteamLocalizationObb.restore(new File(a[0]),new File(a[1]),new File(a[2]),new String(new char[64]).replace('\0','0'),out);throw new AssertionError("bad plan hash accepted");}
        catch(IOException expected){if(out.exists())throw new AssertionError("output on failure");}
        SteamLocalizationObb.restore(new File(a[0]),new File(a[1]),new File(a[2]),planHash,out);
        try(ZipFile actual=new ZipFile(out);ZipFile expected=new ZipFile(a[4])){
            Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> it=expected.entries();
            while(it.hasMoreElements()){
                ZipEntry e=it.nextElement();if(e.isDirectory())continue;if(!names.add(e.getName()))throw new AssertionError("duplicate expected entry");
                ZipEntry got=actual.getEntry(e.getName());if(got==null||!Arrays.equals(hash(expected.getInputStream(e)),hash(actual.getInputStream(got))))throw new AssertionError("entry differs: "+e.getName());
            }
            int n=0;it=actual.entries();while(it.hasMoreElements())if(!it.nextElement().isDirectory())n++;
            if(n!=names.size())throw new AssertionError("entry count differs");System.out.println("PASS: restored "+n+" OBB entries; invalid plan hash rejected");
        }
    }
}
