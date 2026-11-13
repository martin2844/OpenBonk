package com.openbonk.dataprep;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Replaces prefab component bodies in a private incomplete diagnostic archive. */
public final class SteamPreloadDiagnosticObb {
 private static final String PARTIAL="bcca044c80134ebc2840978b43eeed75304606f838e005866d4234dbf58d1d30",PREFIX="assets/bin/Data/",COMMENT="OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE";
 private static final String BANDIT_DIRECT_PARTIAL="157d4d19dbf9c8bc53fb0ea9f9319a049bd0827f11251d962406016c2f5bb5e6";
 private static final String KNIGHT_DIRECT_PARTIAL="aab5e33bb29ad7456e0ff9dea90ad73ca353d231c4af0ac2b5ad87b417570f54";
 private static final String BATCH_DIRECT_PARTIAL="e26bf4ee16216801048e5e840e75c1aaeb8358317e6ca9f21d318a525a860aa9";
 private static final String BATCH2_DIRECT_PARTIAL="3f3a8536e799b25f65854aab0700717d865fad81b2df23cc41d49e36fcc578f3";
 private static final String BATCH3_DIRECT_PARTIAL="e811836cfb9dd8c98edcc8158f42339741ea86ab7f6526b585ec83a5593e4e5e";
 private static final String SHARED_DIRECT_PARTIAL="f09bab83a8ef95f76689fd4363fe7d525dc09f8b6c17aea4e7f95d9d3ed1e71c";
 private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
 private static String hashFile(File f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];try(InputStream in=new FileInputStream(f)){for(int n;(n=in.read(b))!=-1;)if(n>0)h.update(b,0,n);}return hex(h.digest());}
 private static final class Counted extends FilterOutputStream {long count;Counted(OutputStream o){super(o);}public void write(int b)throws IOException{out.write(b);count++;}public void write(byte[] b,int o,int n)throws IOException{out.write(b,o,n);count+=n;}}
 public static void assemble(File partial,File steam,File preloadPlan,File output)throws IOException{
  Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
  if(Files.isSymbolicLink(partial.toPath())||!partial.isFile())throw new IOException("incompatible diagnostic input");
  String partialHash=hashFile(partial);boolean noProbe=partialHash.equals("66a094ff88879e82d44a1663e8d71d223ad13187cfbdaafe9d7e013b4768b3a9");int inputCount=noProbe?1078:1081;if(!noProbe&&!partialHash.equals(PARTIAL)&&!partialHash.equals(BANDIT_DIRECT_PARTIAL)&&!partialHash.equals(KNIGHT_DIRECT_PARTIAL)&&!partialHash.equals(BATCH_DIRECT_PARTIAL)&&!partialHash.equals(BATCH2_DIRECT_PARTIAL)&&!partialHash.equals(BATCH3_DIRECT_PARTIAL)&&!partialHash.equals(SHARED_DIRECT_PARTIAL))throw new IOException("incompatible diagnostic input");
  Map<String,byte[]> source=SteamPreloadBodies.restore(partial,steam,preloadPlan),replacements=new HashMap<>();if(source.size()!=68)throw new IOException("expected preload assets");for(Map.Entry<String,byte[]> e:source.entrySet())if(!e.getKey().matches("[0-9a-f]{32}")||replacements.put(PREFIX+e.getKey(),e.getValue())!=null)throw new IOException("invalid preload entry");
  Path temp=Files.createTempFile(parent,".openbonk-preload-obb-",".tmp");boolean moved=false;
  try{
   byte[] buf=new byte[65536];Set<String> seen=new HashSet<>(),replaced=new HashSet<>();
   try(ZipFile input=new ZipFile(partial);Counted counter=new Counted(new BufferedOutputStream(Files.newOutputStream(temp)));ZipOutputStream out=new SteamStableZipOutputStream(counter)){
    if(input.size()!=inputCount||!COMMENT.equals(input.getComment()))throw new IOException("invalid diagnostic archive");out.setComment(COMMENT);Enumeration<? extends ZipEntry> iter=input.entries();
    while(iter.hasMoreElements()){
     ZipEntry old=iter.nextElement();String name=old.getName();if(!seen.add(name)||old.isDirectory()||old.getSize()<0||old.getSize()>64*1024*1024)throw new IOException("invalid input entry");byte[] value=replacements.get(name);if(value!=null&&(old.getMethod()!=ZipEntry.DEFLATED||!replaced.add(name)))throw new IOException("invalid preload entry method");
     ZipEntry next=new ZipEntry(name);next.setTime(SteamZipTime.fixed());next.setMethod(old.getMethod());
     if(old.getMethod()==ZipEntry.STORED){next.setSize(old.getSize());next.setCompressedSize(old.getSize());next.setCrc(old.getCrc());int nameSize=name.getBytes(StandardCharsets.UTF_8).length;int pad=(int)((4-((counter.count+30+nameSize+4)%4))%4);byte[] extra=new byte[4+pad];extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)pad;next.setExtra(extra);}else if(old.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unsupported ZIP method");out.putNextEntry(next);
     if(value!=null)out.write(value);else{long size=0;CRC32 crc=new CRC32();try(InputStream in=input.getInputStream(old)){for(int n;(n=in.read(buf))!=-1;){if(n==0)continue;if(n>old.getSize()-size)throw new IOException("entry too large");out.write(buf,0,n);crc.update(buf,0,n);size+=n;}}if(size!=old.getSize()||crc.getValue()!=old.getCrc())throw new IOException("entry differs");}out.closeEntry();
    }
    if(seen.size()!=inputCount||!replaced.equals(replacements.keySet()))throw new IOException("missing preload entries");
   }
   try(ZipFile result=new ZipFile(temp.toFile())){if(result.size()!=inputCount||!COMMENT.equals(result.getComment()))throw new IOException("invalid output archive");for(Map.Entry<String,byte[]> e:replacements.entrySet()){byte[] actual=entry(result,result.getEntry(e.getKey()),8*1024*1024);if(!MessageDigest.isEqual(hash(actual),hash(e.getValue())))throw new IOException("output preload differs");}}
   Files.move(temp,dest);moved=true;
  }finally{if(!moved)Files.deleteIfExists(temp);}
 }
 private SteamPreloadDiagnosticObb(){}
}
