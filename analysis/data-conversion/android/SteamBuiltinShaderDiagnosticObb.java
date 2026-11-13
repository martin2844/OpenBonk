package com.openbonk.dataprep;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Replaces builtin shader objects in a private incomplete diagnostic archive. */
public final class SteamBuiltinShaderDiagnosticObb {
 private static final String PARTIAL="dac213eef5ec96dc370223059b50f1121ad29ab6277e2fd9eafbc80d39c626ec",PREFIX="assets/bin/Data/",COMMENT="OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE";
 private static final String BANDIT_DIRECT_PARTIAL="2594d13801384f1c5bd406bd94e0161f32ca6bb77627ef25f88d551338fadc6e";
 private static final String KNIGHT_DIRECT_PARTIAL="437be29101d07b30a33d29e3df5a2f7f36102509b3aa91276e2f9d47f3afb37e";
 private static final String BATCH_DIRECT_PARTIAL="7b5fadfe98a928b3ae4bbd25e12c83b97a3fdd4008e43fce94aa33ed0443e88e";
 private static final String BATCH2_DIRECT_PARTIAL="e5306c8b551003985a1dc39fc60bc3c79f5874d73152f8ec5c0764035e493a14";
 private static final String BATCH3_DIRECT_PARTIAL="733508cbb6d02bfeb0eccf3e427b90a0370ffd6f251fe27f9b08d2a816307802";
 private static final String SHARED_DIRECT_PARTIAL="86e77b1a9be32cef944bb80475a27ab568f36211ae190b0f3986d5b09d6242fa";
 private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
 private static String hashFile(File f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];try(InputStream in=new FileInputStream(f)){for(int n;(n=in.read(b))!=-1;)if(n>0)h.update(b,0,n);}return hex(h.digest());}
 private static final class Counted extends FilterOutputStream {long count;Counted(OutputStream o){super(o);}public void write(int b)throws IOException{out.write(b);count++;}public void write(byte[] b,int o,int n)throws IOException{out.write(b,o,n);count+=n;}}
 public static void assemble(File partial,File pack,File shaderPlan,File output)throws IOException{
  Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
  if(Files.isSymbolicLink(partial.toPath())||!partial.isFile())throw new IOException("incompatible diagnostic input");
  String partialHash=hashFile(partial);boolean noProbe=partialHash.equals("1c428223770af8f9ef67fc3c261fae8589b95712b2a45dcd1f33312e68650d50");int inputCount=noProbe?1078:1081;if(!noProbe&&!partialHash.equals(PARTIAL)&&!partialHash.equals(BANDIT_DIRECT_PARTIAL)&&!partialHash.equals(KNIGHT_DIRECT_PARTIAL)&&!partialHash.equals(BATCH_DIRECT_PARTIAL)&&!partialHash.equals(BATCH2_DIRECT_PARTIAL)&&!partialHash.equals(BATCH3_DIRECT_PARTIAL)&&!partialHash.equals(SHARED_DIRECT_PARTIAL))throw new IOException("incompatible diagnostic input");
  Map<String,byte[]> source=SteamBuiltinShaderResources.restore(partial,pack,shaderPlan),replacements=new HashMap<>();if(source.size()!=1)throw new IOException("expected builtin shader asset");for(Map.Entry<String,byte[]> e:source.entrySet())if(!e.getKey().matches("[0-9a-f]{32}")||replacements.put(PREFIX+e.getKey(),e.getValue())!=null)throw new IOException("invalid shader entry");
  Path temp=Files.createTempFile(parent,".openbonk-builtin-obb-",".tmp");boolean moved=false;
  try{
   byte[] buf=new byte[65536];Set<String> seen=new HashSet<>(),replaced=new HashSet<>();
   try(ZipFile input=new ZipFile(partial);Counted counter=new Counted(new BufferedOutputStream(Files.newOutputStream(temp)));ZipOutputStream out=new SteamStableZipOutputStream(counter)){
    if(input.size()!=inputCount||!COMMENT.equals(input.getComment()))throw new IOException("invalid diagnostic archive");out.setComment(COMMENT);Enumeration<? extends ZipEntry> iter=input.entries();
    while(iter.hasMoreElements()){
     ZipEntry old=iter.nextElement();String name=old.getName();if(!seen.add(name)||old.isDirectory()||old.getSize()<0||old.getSize()>64*1024*1024)throw new IOException("invalid input entry");byte[] value=replacements.get(name);if(value!=null&&(old.getMethod()!=ZipEntry.DEFLATED||!replaced.add(name)))throw new IOException("invalid shader entry method");
     ZipEntry next=new ZipEntry(name);next.setTime(SteamZipTime.fixed());next.setMethod(old.getMethod());
     if(old.getMethod()==ZipEntry.STORED){next.setSize(old.getSize());next.setCompressedSize(old.getSize());next.setCrc(old.getCrc());int nameSize=name.getBytes(StandardCharsets.UTF_8).length;int pad=(int)((4-((counter.count+30+nameSize+4)%4))%4);byte[] extra=new byte[4+pad];extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)pad;next.setExtra(extra);}else if(old.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unsupported ZIP method");out.putNextEntry(next);
     if(value!=null)out.write(value);else{long size=0;CRC32 crc=new CRC32();try(InputStream in=input.getInputStream(old)){for(int n;(n=in.read(buf))!=-1;){if(n==0)continue;if(n>old.getSize()-size)throw new IOException("entry too large");out.write(buf,0,n);crc.update(buf,0,n);size+=n;}}if(size!=old.getSize()||crc.getValue()!=old.getCrc())throw new IOException("entry differs");}out.closeEntry();
    }
    if(seen.size()!=inputCount||!replaced.equals(replacements.keySet()))throw new IOException("missing shader entries");
   }
   try(ZipFile result=new ZipFile(temp.toFile())){if(result.size()!=inputCount||!COMMENT.equals(result.getComment()))throw new IOException("invalid output archive");for(Map.Entry<String,byte[]> e:replacements.entrySet()){byte[] actual=entry(result,result.getEntry(e.getKey()),8*1024*1024);if(!MessageDigest.isEqual(hash(actual),hash(e.getValue())))throw new IOException("output shader differs");}}
   Files.move(temp,dest);moved=true;
  }finally{if(!moved)Files.deleteIfExists(temp);}
 }
 private SteamBuiltinShaderDiagnosticObb(){}
}
