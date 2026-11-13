package com.openbonk.dataprep;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;

/** Private diagnostic: preserve prepared data while storing Unity video uncompressed. */
public final class SteamVideoStorage {
 private static final String SOURCE="632b8fca82f42c8971e5484a32d704568b345c6baa304a9254435f848ac94a29";
 private static final String VIDEO="assets/bin/Data/sharedassets1.resource";
 private static final class Counted extends FilterOutputStream {
  long count; Counted(OutputStream out){super(out);}
  public void write(int b)throws IOException{out.write(b);count++;}
  public void write(byte[] b,int off,int len)throws IOException{out.write(b,off,len);count+=len;}
 }
 private static String hash(File file)throws IOException {
  try { MessageDigest d=MessageDigest.getInstance("SHA-256"); byte[] b=new byte[65536];
   try(InputStream in=new FileInputStream(file)){for(int n;(n=in.read(b))!=-1;)if(n>0)d.update(b,0,n);}
   StringBuilder s=new StringBuilder();for(byte x:d.digest())s.append(String.format(Locale.ROOT,"%02x",x&255));return s.toString();
  } catch(NoSuchAlgorithmException e){throw new AssertionError(e);}
 }
 private static void copy(ZipFile input,ZipEntry e,OutputStream out)throws IOException {
  CRC32 crc=new CRC32();long total=0;byte[] b=new byte[65536];
  try(InputStream in=input.getInputStream(e)){for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>e.getSize()-total)throw new IOException("entry too large");out.write(b,0,n);crc.update(b,0,n);total+=n;}}
  if(total!=e.getSize()||crc.getValue()!=e.getCrc())throw new IOException("entry differs");
 }
 public static void prepare(File source,File output)throws IOException {
  if(!source.isFile())throw new IOException("incompatible prepared data");
  String sourceHash=hash(source);boolean noProbe=sourceHash.equals("b74cbadedf8a03eeb2cdeb4a6db9eda66dddd4ed4aa457e9ea80508d2e1aa230");
  if(!noProbe&&!SOURCE.equals(sourceHash))throw new IOException("incompatible prepared data");
  int inputCount=noProbe?1078:1081;
  Path target=output.toPath().toAbsolutePath(),parent=target.getParent();
  if(parent==null||!Files.isDirectory(parent)||Files.exists(target,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
  Path temp=Files.createTempFile(parent,".openbonk-video-",".tmp");boolean moved=false;
  try(ZipFile input=new ZipFile(source)) {
   if(input.size()!=inputCount)throw new IOException("unexpected inventory");
   Set<String> seen=new HashSet<>();
   try(Counted counted=new Counted(new BufferedOutputStream(Files.newOutputStream(temp))); ZipOutputStream out=new SteamStableZipOutputStream(counted)) {
    out.setComment("OPENBONK PRIVATE VIDEO STORAGE");
    Enumeration<? extends ZipEntry> entries=input.entries();
    while(entries.hasMoreElements()){
     ZipEntry old=entries.nextElement();String name=old.getName();
     if(!seen.add(name)||old.isDirectory()||old.getSize()<0||old.getSize()>64L*1024*1024)throw new IOException("invalid input entry");
     boolean video=name.equals(VIDEO);if(video&&old.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unexpected video entry");
     ZipEntry next=new ZipEntry(name);next.setTime(SteamZipTime.fixed());next.setMethod(video?ZipEntry.STORED:old.getMethod());
     if(next.getMethod()==ZipEntry.STORED){next.setSize(old.getSize());next.setCompressedSize(old.getSize());next.setCrc(old.getCrc());int length=name.getBytes(StandardCharsets.UTF_8).length;int pad=(int)((4-((counted.count+30+length+4)%4))%4);byte[] extra=new byte[4+pad];extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)pad;next.setExtra(extra);}
     else if(next.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unsupported ZIP method");
     out.putNextEntry(next);copy(input,old,out);out.closeEntry();
    }
    if(seen.size()!=inputCount||!seen.contains(VIDEO))throw new IOException("missing video entry");
   }
   try(ZipFile result=new ZipFile(temp.toFile())){
    if(result.size()!=input.size()||result.getEntry(VIDEO)==null||result.getEntry(VIDEO).getMethod()!=ZipEntry.STORED)throw new IOException("invalid output inventory");
    Set<String> checked=new HashSet<>();Enumeration<? extends ZipEntry> entries=result.entries();
    while(entries.hasMoreElements()){ZipEntry e=entries.nextElement(),old=input.getEntry(e.getName());if(old==null||!checked.add(e.getName())||e.getSize()!=old.getSize()||e.getCrc()!=old.getCrc())throw new IOException("output entry differs");}
   }
   try(FileOutputStream sync=new FileOutputStream(temp.toFile(),true)){sync.getFD().sync();}
   Files.move(temp,target);moved=true;
  } finally {if(!moved)Files.deleteIfExists(temp);}
 }
 private SteamVideoStorage(){}
}
