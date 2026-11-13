package com.openbonk.dataprep;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Adds the reconstructed shared0 splits to an explicitly incomplete diagnostic OBB. */
public final class SteamShared0DiagnosticObb {
    private static final String PARTIAL="29d82e0ae3391a5c6402c17fac6e8d78aeea11875d52d05527439ee944e8150a",ASSET="ff3b5e27ca87d1a9186c485ab90e599fa02fb442347fb42c6ff214e14306933c",COMMENT="OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE";
    private static final int LENGTH=132028896,SPLIT=1048576;
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static String hashFile(File f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];try(InputStream in=new FileInputStream(f)){for(int n;(n=in.read(b))!=-1;)if(n>0)h.update(b,0,n);}return hex(h.digest());}
    private static final class Counted extends FilterOutputStream {long count;Counted(OutputStream o){super(o);}public void write(int b)throws IOException{out.write(b);count++;}public void write(byte[] b,int o,int n)throws IOException{out.write(b,o,n);count+=n;}}
    public static void assemble(File partial,File asset,File output)throws IOException{
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        if(!partial.isFile()||!asset.isFile()||asset.length()!=LENGTH||!hashFile(asset).equals(ASSET))throw new IOException("incompatible diagnostic input");
        String partialHash=hashFile(partial);
        boolean noProbe=partialHash.equals("e5bbcd2850557add611638a2df68265943a5f60ff06c975bf121bab2a496f661");
        if(!noProbe&&!partialHash.equals(PARTIAL))throw new IOException("incompatible diagnostic input");
        int inputCount=noProbe?943:946;
        Path temp=Files.createTempFile(parent,".openbonk-shared0-obb-",".tmp");boolean moved=false;
        try{
            Set<String> seen=new HashSet<>();byte[] buffer=new byte[65536];
            try(ZipFile source=new ZipFile(partial);InputStream shared=new FileInputStream(asset);Counted count=new Counted(new BufferedOutputStream(Files.newOutputStream(temp)));ZipOutputStream out=new SteamStableZipOutputStream(count)){
                if(source.size()!=inputCount||!COMMENT.equals(source.getComment()))throw new IOException("invalid partial archive");out.setComment(COMMENT);Enumeration<? extends ZipEntry> entries=source.entries();
                while(entries.hasMoreElements()){
                    ZipEntry old=entries.nextElement();String name=old.getName();if(!seen.add(name)||old.isDirectory()||old.getSize()<0||old.getSize()>64*1024*1024||name.startsWith("assets/bin/Data/sharedassets0.assets.split"))throw new IOException("invalid partial entry");
                    ZipEntry entry=new ZipEntry(name);entry.setTime(SteamZipTime.fixed());entry.setMethod(old.getMethod());
                    if(old.getMethod()==ZipEntry.STORED){entry.setSize(old.getSize());entry.setCompressedSize(old.getSize());entry.setCrc(old.getCrc());int nameSize=name.getBytes(StandardCharsets.UTF_8).length;int pad=(int)((4-((count.count+30+nameSize+4)%4))%4);byte[] extra=new byte[4+pad];extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)pad;entry.setExtra(extra);}else if(old.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unsupported ZIP method");
                    out.putNextEntry(entry);long size=0;CRC32 crc=new CRC32();try(InputStream in=source.getInputStream(old)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>old.getSize()-size)throw new IOException("partial entry too large");out.write(buffer,0,n);crc.update(buffer,0,n);size+=n;}}if(size!=old.getSize()||crc.getValue()!=old.getCrc())throw new IOException("partial entry differs");out.closeEntry();
                }
                if(seen.size()!=inputCount)throw new IOException("partial entry count differs");
                MessageDigest h=digest();for(int i=0;i<126;i++){String name="assets/bin/Data/sharedassets0.assets.split"+i;if(!seen.add(name))throw new IOException("duplicate asset split");ZipEntry e=new ZipEntry(name);e.setTime(SteamZipTime.fixed());e.setMethod(ZipEntry.DEFLATED);out.putNextEntry(e);int remaining=Math.min(SPLIT,LENGTH-i*SPLIT);while(remaining>0){int n=shared.read(buffer,0,Math.min(buffer.length,remaining));if(n<0)throw new IOException("asset truncated");if(n==0)continue;out.write(buffer,0,n);h.update(buffer,0,n);remaining-=n;}out.closeEntry();}
                if(shared.read()!=-1||!hex(h.digest()).equals(ASSET))throw new IOException("asset changed during assembly");
            }
            try(ZipFile result=new ZipFile(temp.toFile())){if(result.size()!=inputCount+126||!COMMENT.equals(result.getComment()))throw new IOException("diagnostic verification failed");MessageDigest h=digest();for(int i=0;i<126;i++){ZipEntry e=result.getEntry("assets/bin/Data/sharedassets0.assets.split"+i);int expected=Math.min(SPLIT,LENGTH-i*SPLIT);if(e==null||e.getSize()!=expected)throw new IOException("missing asset split");try(InputStream in=result.getInputStream(e)){int count=0;for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;h.update(buffer,0,n);count+=n;}if(count!=expected)throw new IOException("split size differs");}}if(!hex(h.digest()).equals(ASSET))throw new IOException("asset verification failed");}
            Files.move(temp,dest);moved=true;
        }finally{if(!moved)Files.deleteIfExists(temp);}
    }
    private SteamShared0DiagnosticObb(){}
}
