package com.openbonk.dataprep;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Replaces localization entries in a private, incomplete diagnostic OBB. */
public final class SteamLocalizationDiagnosticObb {
 private static final String PARTIAL="350cde29c3ec668977195010254be7e5ad8614c358c837b0978d8eac732c9d4c",PLAN="1a7c9a580c8fe91b121d9eeb4a1f8eaf447bc20372d0642b61dde9219a285ce2",PREFIX="assets/aa/Android/",COMMENT="OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE";
 private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
 private static String hashFile(File f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];try(InputStream in=new FileInputStream(f)){for(int n;(n=in.read(b))!=-1;)if(n>0)h.update(b,0,n);}return hex(h.digest());}
 private static final class Counted extends FilterOutputStream{long count;Counted(OutputStream out){super(out);}public void write(int b)throws IOException{out.write(b);count++;}public void write(byte[] b,int o,int n)throws IOException{out.write(b,o,n);count+=n;}}
 public static void assemble(File partial,File bundles,File planFile,File output)throws IOException{
  Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent(),dir=bundles.toPath();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
  if(Files.isSymbolicLink(partial.toPath())||!partial.isFile()||Files.isSymbolicLink(dir)||!Files.isDirectory(dir,LinkOption.NOFOLLOW_LINKS))throw new IOException("incompatible diagnostic input");
  String partialHash=hashFile(partial);boolean noProbe=partialHash.equals("07b12691cbcc4ec640173bff9a3f9a10e0c8a994fd0c14b549e74acb1c4c20f9");
  if(!noProbe&&!partialHash.equals(PARTIAL))throw new IOException("incompatible diagnostic input");
  int inputCount=noProbe?1078:1081;
  byte[] p=file(planFile,65536);if(!hex(hash(p)).equals(PLAN))throw new IOException("incompatible localization OBB plan");Map<String,byte[]> replacements=new TreeMap<>();
  try(DataInputStream in=new DataInputStream(new ByteArrayInputStream(p))){
   if(!Arrays.equals(bytes(in,8),new byte[]{'O','B','L','C','O','0','0','1'})||in.readInt()!=18)throw new IOException("invalid locale OBB plan");
   for(int i=0;i<18;i++){int length=in.readInt();if(length<=0||length>256)throw new IOException("invalid locale name");String name=new String(bytes(in,length),StandardCharsets.US_ASCII);if(!name.matches("localization-[A-Za-z0-9_()\\-]+\\.bundle"))throw new IOException("invalid locale name");int size=in.readInt();if(size<=0||size>2*1024*1024)throw new IOException("invalid locale size");byte[] wanted=bytes(in,32);Path path=dir.resolve(name);if(Files.isSymbolicLink(path)||!Files.isRegularFile(path,LinkOption.NOFOLLOW_LINKS)||Files.size(path)!=size)throw new IOException("invalid locale bundle");byte[] value=file(path.toFile(),size);if(!MessageDigest.isEqual(hash(value),wanted)||replacements.put(PREFIX+name,value)!=null)throw new IOException("locale bundle differs");}
   if(in.read()!=-1)throw new IOException("trailing locale OBB plan");
  }
  Path temp=Files.createTempFile(parent,".openbonk-locale-obb-",".tmp");boolean moved=false;
  try{
   Set<String> seen=new HashSet<>(),removed=new HashSet<>();byte[] buffer=new byte[65536];
   try(ZipFile source=new ZipFile(partial);Counted counter=new Counted(new BufferedOutputStream(Files.newOutputStream(temp)));ZipOutputStream out=new SteamStableZipOutputStream(counter)){
    if(source.size()!=inputCount||!COMMENT.equals(source.getComment()))throw new IOException("invalid partial archive");out.setComment(COMMENT);Enumeration<? extends ZipEntry> entries=source.entries();
    while(entries.hasMoreElements()){
     ZipEntry old=entries.nextElement();String name=old.getName();if(old.isDirectory()||old.getSize()<0||old.getSize()>64*1024*1024)throw new IOException("invalid partial entry");if(replacements.containsKey(name)){if(!removed.add(name))throw new IOException("duplicate replaced locale");continue;}if(!seen.add(name))throw new IOException("duplicate partial entry");
     ZipEntry e=new ZipEntry(name);e.setTime(SteamZipTime.fixed());e.setMethod(old.getMethod());
     if(old.getMethod()==ZipEntry.STORED){e.setSize(old.getSize());e.setCompressedSize(old.getSize());e.setCrc(old.getCrc());int nameSize=name.getBytes(StandardCharsets.UTF_8).length;int pad=(int)((4-((counter.count+30+nameSize+4)%4))%4);byte[] extra=new byte[4+pad];extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)pad;e.setExtra(extra);}else if(old.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unsupported ZIP method");
     out.putNextEntry(e);long size=0;CRC32 crc=new CRC32();try(InputStream in=source.getInputStream(old)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>old.getSize()-size)throw new IOException("partial entry too large");out.write(buffer,0,n);crc.update(buffer,0,n);size+=n;}}if(size!=old.getSize()||crc.getValue()!=old.getCrc())throw new IOException("partial entry differs");out.closeEntry();
    }
    if(seen.size()!=inputCount-18||removed.size()!=18||!removed.equals(replacements.keySet()))throw new IOException("missing replaced locale");
    for(Map.Entry<String,byte[]> item:replacements.entrySet()){byte[] value=item.getValue();ZipEntry e=new ZipEntry(item.getKey());e.setTime(SteamZipTime.fixed());e.setMethod(ZipEntry.STORED);e.setSize(value.length);e.setCompressedSize(value.length);CRC32 crc=new CRC32();crc.update(value);e.setCrc(crc.getValue());int nameSize=item.getKey().getBytes(StandardCharsets.UTF_8).length;int pad=(int)((4-((counter.count+30+nameSize+4)%4))%4);byte[] extra=new byte[4+pad];extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)pad;e.setExtra(extra);out.putNextEntry(e);out.write(value);out.closeEntry();}
   }
   try(ZipFile result=new ZipFile(temp.toFile())){if(result.size()!=inputCount||!COMMENT.equals(result.getComment()))throw new IOException("invalid output archive");for(Map.Entry<String,byte[]> item:replacements.entrySet()){ZipEntry actualEntry=result.getEntry(item.getKey());if(actualEntry==null||actualEntry.getMethod()!=ZipEntry.STORED)throw new IOException("output locale must be stored");byte[] actual=entry(result,actualEntry,2*1024*1024);if(!MessageDigest.isEqual(hash(actual),hash(item.getValue())))throw new IOException("output locale differs");}}
   Files.move(temp,dest);moved=true;
  }finally{if(!moved)Files.deleteIfExists(temp);}
 }
 private SteamLocalizationDiagnosticObb(){}
}
