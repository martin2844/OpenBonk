package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores one diagnostic OBB entry from the user's verified Steam resources. */
public final class SteamDiagnosticBaseRebuild {
 private static final String TEMPLATE="29af2d72a6982f2cf8905020498127a384a467ef1c56ddeb57dd80e17961055c";
 private static final String ORIGINAL="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
 private static final String NO_PROBE_TEMPLATE="3f6f96ef96a2b976d6862d37f7b5c4a06c15eb4ef5bcbd8efa49af17da8aeb76";
 private static final String NO_PROBE_RESTORED="f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003";
 private static final String HEADER="62fd958961b088c275cf6f605ebc3463e4904c2b9917a122dcf4e35ee72a98f3";
 private static final String ENTRY="20c812b6e2a3d3248ca0552901d6588b5ab785083d376ccba16254d0c7e08a29";
 private static final String[] RESOURCE_HASH={"a71923a420e792cd46c107eb8e607443ceb9357ef988e4efb73caafa3d35a366","142439d97de6546370e03b3c09831c4401383e056816aee19966465ef861e0db"};
 private static final String[] FONT_HASH={"621e2fcb9b92e7d33f30af136730ecf3cd5609f574ddc9ccc39bc50e7b409283","d7a255bc40ee15d86f3793297b1ce80938608604f0cac91f9b1cbe477aa69094"};
 private static final String FONT_TARGET="dfda6bad9f6b823e039ab9279b29df2f144033802a97a2440effebbe8f5f5631";
 private static final int OFFSET=848444, COMPRESSED=8502, HEADER_SIZE=384, FONT_SIZE=28796;
 public static void restore(File template,File steam,File output)throws IOException{
  Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
  if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
  byte[] base=file(template,4*1024*1024);
  String templateHash=hex(hash(base));
  boolean noProbe=base.length==2724266&&templateHash.equals(NO_PROBE_TEMPLATE);
  if(!noProbe&&(base.length!=3243821||!templateHash.equals(TEMPLATE)))throw new IOException("base template differs");
  byte[] header=Arrays.copyOfRange(base,OFFSET,OFFSET+HEADER_SIZE);
  if(!hex(hash(header)).equals(HEADER))throw new IOException("font header differs");
  byte[] resources;
  try(ZipFile zip=new ZipFile(steam)){
   ZipEntry match=null;Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> entries=zip.entries();
   while(entries.hasMoreElements()){
    ZipEntry e=entries.nextElement();String n=e.getName();
    if(seen.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!seen.add(n))throw new IOException("unsafe Steam ZIP");
    for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");
    if(rootFor(n,"resources.assets")!=null){if(match!=null)throw new IOException("duplicate resources asset");match=e;}
   }
   if(match==null||match.isDirectory())throw new IOException("missing resources asset");
   resources=entry(zip,match,5*1024*1024);
  }
  int platform,offset;
  if(resources.length==4092292){platform=0;offset=4054976;}else if(resources.length==527348){platform=1;offset=490032;}else throw new IOException("unsupported resources asset");
  if(!hex(hash(resources)).equals(RESOURCE_HASH[platform]))throw new IOException("resources asset differs");
  byte[] font=Arrays.copyOfRange(resources,offset,offset+FONT_SIZE);
  if(!hex(hash(font)).equals(FONT_HASH[platform]))throw new IOException("font source differs");
  int[] positions={20,172,176,17444,17448,28148,28152},values={56,2,1,3,1,4,1};
  for(int i=0;i<positions.length;i++)font[positions[i]]=(byte)values[i];
  if(!hex(hash(font)).equals(FONT_TARGET))throw new IOException("font reconstruction differs");
  byte[] value=new byte[HEADER_SIZE+FONT_SIZE];System.arraycopy(header,0,value,0,HEADER_SIZE);System.arraycopy(font,0,value,HEADER_SIZE,FONT_SIZE);
  if(!hex(hash(value)).equals(ENTRY))throw new IOException("font entry differs");
  Deflater deflater=new SteamStableDeflater(6);ByteArrayOutputStream compressed=new ByteArrayOutputStream();
  try{deflater.setInput(value);deflater.finish();byte[] buf=new byte[4096];while(!deflater.finished()){int n=deflater.deflate(buf);if(n==0)throw new IOException("cannot compress font entry");compressed.write(buf,0,n);if(compressed.size()>COMPRESSED)throw new IOException("compressed font differs");}}finally{deflater.end();}
  byte[] packed=compressed.toByteArray();if(packed.length!=COMPRESSED)throw new IOException("compressed font differs");
  System.arraycopy(packed,0,base,OFFSET,COMPRESSED);
  if(!MessageDigest.isEqual(hash(base),unhex(noProbe?NO_PROBE_RESTORED:ORIGINAL)))throw new IOException("restored OBB differs");
  Path temp=Files.createTempFile(parent,".openbonk-base-obb-",".tmp");boolean moved=false;
  try{Files.write(temp,base);Files.move(temp,dest);moved=true;}finally{if(!moved)Files.deleteIfExists(temp);}
 }
 private static byte[] unhex(String s){byte[] b=new byte[s.length()/2];for(int i=0;i<b.length;i++)b[i]=(byte)Integer.parseInt(s.substring(i*2,i*2+2),16);return b;}
 private SteamDiagnosticBaseRebuild(){}
}
