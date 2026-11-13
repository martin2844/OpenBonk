package com.openbonk.dataprep;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** Private local research stage; scene replacements and inputs are not distributable. */
public final class SteamRuntimeRebase {
 private static final String SOURCE="632b8fca82f42c8971e5484a32d704568b345c6baa304a9254435f848ac94a29";
 private static final String TARGET="3b5ddc2fa06a97bf04c6ccdf442a0454b49ec02fa86b32917b74d827ee4f79e7";
 private static final String PLAN="3a0621e1de16a038e0d41f77fb839d7c04ee2a963f0bdfd1917eb0b68ddcd1b1";
 private static final String VIDEO_RESOURCE="assets/bin/Data/sharedassets1.resource";
 private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
 private static String hex(byte[] b){StringBuilder s=new StringBuilder();for(byte x:b)s.append(String.format(Locale.ROOT,"%02x",x&255));return s.toString();}
 private static String hash(byte[] b){return hex(digest().digest(b));}
 private static String hashFile(File f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];try(InputStream in=new FileInputStream(f)){for(int n;(n=in.read(b))!=-1;)if(n>0)h.update(b,0,n);}return hex(h.digest());}
 private static byte[] read(ZipFile z,ZipEntry e,int limit)throws IOException{
  if(e==null||e.isDirectory()||e.getSize()<0||e.getSize()>limit)throw new IOException("invalid bounded ZIP entry");
  ByteArrayOutputStream out=new ByteArrayOutputStream((int)e.getSize());copy(z,e,out);return out.toByteArray();
 }
 private static void copy(ZipFile z,ZipEntry e,OutputStream out)throws IOException{
  CRC32 crc=new CRC32();long size=0;byte[] b=new byte[65536];try(InputStream in=z.getInputStream(e)){for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>e.getSize()-size)throw new IOException("entry too large");out.write(b,0,n);crc.update(b,0,n);size+=n;}}
  if(size!=e.getSize()||crc.getValue()!=e.getCrc())throw new IOException("ZIP entry mismatch");
 }
 private static final class Record {String name,sourceHash,targetHash,patchName,patchHash;int sourceSize,targetSize,patchSize;}
 private static final class Counted extends FilterOutputStream {long count;Counted(OutputStream o){super(o);}public void write(int x)throws IOException{out.write(x);count++;}public void write(byte[] b,int o,int n)throws IOException{out.write(b,o,n);count+=n;}}
 private static byte[] apply(byte[] source,byte[] patch,int expected)throws IOException{
  if(patch.length==0||expected<0||expected>8*1024*1024)throw new IOException("invalid patch size");
  try(DataInputStream in=new DataInputStream(new ByteArrayInputStream(patch))){
   int mode=in.readUnsignedByte();
   if(mode==0){if(patch.length-1!=expected)throw new IOException("replacement size mismatch");byte[] b=new byte[expected];in.readFully(b);return b;}
   if(mode!=1||source.length!=expected)throw new IOException("invalid patch mode");
   int count=in.readInt();if(count<0||count>source.length)throw new IOException("invalid patch count");
   byte[] result=source.clone();int last=0;
   for(int i=0;i<count;i++){int offset=in.readInt(),size=in.readInt();if(offset<last||size<=0||offset>result.length-size||size>in.available())throw new IOException("invalid patch range");in.readFully(result,offset,size);last=offset+size;}
   if(in.read()!=-1)throw new IOException("extra patch data");return result;
  }
 }
 public static void rebase(File source,File plan,File output)throws IOException{
  if(!source.isFile()||!hashFile(source).equals(SOURCE)||!plan.isFile()||!hashFile(plan).equals(PLAN))throw new IOException("incompatible research input");
  Path target=output.toPath().toAbsolutePath(),parent=target.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(target,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
  Path temp=Files.createTempFile(parent,".openbonk-runtime-rebase-",".tmp");boolean moved=false;
  try(ZipFile input=new ZipFile(source);ZipFile patches=new ZipFile(plan)){
   Map<String,Record> records=new HashMap<>();Set<String> patchNames=new HashSet<>();
   try{
    JSONObject manifest=new JSONObject(new String(read(patches,patches.getEntry("manifest.json"),1024*1024),StandardCharsets.UTF_8));
    if(manifest.getInt("version")!=1||!SOURCE.equals(manifest.getString("source_obb_sha256"))||!TARGET.equals(manifest.getString("target_reference_obb_sha256")))throw new IOException("invalid manifest");
    JSONArray rows=manifest.getJSONArray("entries");if(rows.length()!=222||patches.size()!=223)throw new IOException("invalid plan count");
    for(int i=0;i<rows.length();i++){
     JSONObject j=rows.getJSONObject(i);Record r=new Record();r.name=j.getString("entry");r.sourceSize=j.getInt("source_size");r.sourceHash=j.getString("source_sha256");r.targetSize=j.getInt("target_size");r.targetHash=j.getString("target_sha256");r.patchName=j.getString("patch");r.patchSize=j.getInt("patch_size");r.patchHash=j.getString("patch_sha256");
     if(!r.name.startsWith("assets/bin/Data/")||!r.patchName.matches("records/[0-9]{4}\\.bin")||r.sourceSize<0||r.sourceSize>8*1024*1024||r.targetSize<0||r.targetSize>8*1024*1024||r.patchSize<=0||r.patchSize>8*1024*1024||records.put(r.name,r)!=null||!patchNames.add(r.patchName))throw new IOException("invalid plan record");
    }
   }catch(JSONException e){throw new IOException("invalid plan JSON",e);}
   if(input.size()!=1081)throw new IOException("unexpected OBB inventory");
   Set<String> seen=new HashSet<>(),done=new HashSet<>();byte[] buffer=new byte[65536];
   try(Counted counted=new Counted(new BufferedOutputStream(Files.newOutputStream(temp)));ZipOutputStream out=new SteamStableZipOutputStream(counted)){
    out.setComment("OPENBONK PRIVATE RUNTIME RESEARCH");Enumeration<? extends ZipEntry> entries=input.entries();
    while(entries.hasMoreElements()){
     ZipEntry old=entries.nextElement();String name=old.getName();if(!seen.add(name)||old.isDirectory()||old.getSize()<0||old.getSize()>64L*1024*1024)throw new IOException("invalid input entry");
     Record r=records.get(name);byte[] modified=null;
     if(r!=null){if(old.getMethod()!=ZipEntry.DEFLATED||old.getSize()!=r.sourceSize||!done.add(name))throw new IOException("invalid source record");byte[] before=read(input,old,8*1024*1024),patch=read(patches,patches.getEntry(r.patchName),8*1024*1024);if(patch.length!=r.patchSize||!hash(before).equals(r.sourceHash)||!hash(patch).equals(r.patchHash))throw new IOException("source or patch differs");modified=apply(before,patch,r.targetSize);if(!hash(modified).equals(r.targetHash))throw new IOException("target entry differs");}
     boolean storeVideo=name.equals(VIDEO_RESOURCE);
     if(storeVideo&&(old.getMethod()!=ZipEntry.DEFLATED||modified!=null))throw new IOException("unexpected video resource");
     ZipEntry next=new ZipEntry(name);next.setTime(SteamZipTime.fixed());next.setMethod(storeVideo?ZipEntry.STORED:old.getMethod());
     if(next.getMethod()==ZipEntry.STORED){next.setSize(old.getSize());next.setCompressedSize(old.getSize());next.setCrc(old.getCrc());int length=name.getBytes(StandardCharsets.UTF_8).length;int pad=(int)((4-((counted.count+30+length+4)%4))%4);byte[] extra=new byte[4+pad];extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)pad;next.setExtra(extra);}else if(old.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unsupported ZIP method");
     out.putNextEntry(next);if(modified!=null)out.write(modified);else copy(input,old,out);out.closeEntry();
    }
    if(seen.size()!=1081||!done.equals(records.keySet()))throw new IOException("missing records");
   }
   try(ZipFile result=new ZipFile(temp.toFile())){
    if(result.size()!=input.size()||result.getEntry(VIDEO_RESOURCE)==null||result.getEntry(VIDEO_RESOURCE).getMethod()!=ZipEntry.STORED)throw new IOException("output inventory or video storage differs");Set<String> checked=new HashSet<>();Enumeration<? extends ZipEntry> entries=result.entries();
    while(entries.hasMoreElements()){ZipEntry e=entries.nextElement(),old=input.getEntry(e.getName());if(old==null||!checked.add(e.getName()))throw new IOException("unexpected output entry");Record r=records.get(e.getName());if(r!=null){if(!hash(read(result,e,8*1024*1024)).equals(r.targetHash))throw new IOException("output record differs");}else if(e.getSize()!=old.getSize()||e.getCrc()!=old.getCrc())throw new IOException("unchanged entry differs");}
   }
   try(FileOutputStream sync=new FileOutputStream(temp.toFile(),true)){sync.getFD().sync();}
   Files.move(temp,target);moved=true;
  }finally{if(!moved)Files.deleteIfExists(temp);}
 }
 private SteamRuntimeRebase(){}
}
