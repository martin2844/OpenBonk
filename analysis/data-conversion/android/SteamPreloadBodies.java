package com.openbonk.dataprep;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores private prefab component templates using pinned Steam object bodies. */
public final class SteamPreloadBodies {
 private static final String PLAN="a3806e3c3881190075a2ae4c7b9f25fb97098cb3b4a7fe67dc7fa08999ad60c4";
 private static final String[] NAMES={"sharedassets1.assets","sharedassets2.assets"};
 private static final class Row{String name;int length;byte[] before,after;List<Texture> ranges=new ArrayList<>();}
 public static Map<String,byte[]> restore(File partial,File steamFile,File planFile)throws IOException{
  byte[] raw=file(planFile,1024*1024);if(!hex(hash(raw)).equals(PLAN))throw new IOException("incompatible preload plan");
  try(DataInputStream p=new DataInputStream(new ByteArrayInputStream(raw));ZipFile steam=new ZipFile(steamFile);ZipFile base=new ZipFile(partial)){
   if(!Arrays.equals(bytes(p,8),new byte[]{'O','B','P','R','E','0','0','1'})||p.readInt()!=68)throw new IOException("invalid preload plan");
   long[][] sizes=new long[2][2];byte[][][] hashes=new byte[2][2][];for(int k=0;k<2;k++)for(int i=0;i<2;i++){sizes[k][i]=p.readLong();hashes[k][i]=bytes(p,32);if(sizes[k][i]<=0||sizes[k][i]>256L*1024*1024)throw new IOException("invalid source size");}
   List<Row> rows=new ArrayList<>();List<Texture> ranges=new ArrayList<>();Set<String> unique=new HashSet<>();long total=0;
   for(int i=0;i<68;i++){
    Row row=new Row();row.name=new String(bytes(p,32),StandardCharsets.US_ASCII);row.length=p.readInt();int count=p.readInt();row.before=bytes(p,32);row.after=bytes(p,32);
    if(!row.name.matches("[0-9a-f]{32}")||!unique.add(row.name)||row.length<=0||row.length>8*1024*1024||count<=0||count>10000)throw new IOException("invalid preload file");int end=0;
    for(int j=0;j<count;j++){
     Texture t=new Texture();t.target=p.readInt();t.size=p.readInt();t.linuxFile=t.windowsFile=p.readInt();t.linuxOffset=p.readLong();t.windowsOffset=p.readLong();t.payloadHash=bytes(p,32);
     if(t.target<end||t.size<=0||t.target>row.length-t.size||t.linuxFile<0||t.linuxFile>1||t.linuxOffset<0||t.windowsOffset<0||t.linuxOffset>sizes[0][t.linuxFile]-t.size||t.windowsOffset>sizes[1][t.windowsFile]-t.size)throw new IOException("invalid preload range");end=t.target+t.size;total+=t.size;if(total>16*1024*1024)throw new IOException("preload data too large");t.data=new byte[t.size];ranges.add(t);row.ranges.add(t);
    }rows.add(row);
   }
   if(p.read()!=-1||ranges.size()!=635)throw new IOException("invalid preload inventory");
   ZipEntry[] source=new ZipEntry[2];String common=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
   while(entries.hasMoreElements()){
    ZipEntry e=entries.nextElement();String name=e.getName();if(names.size()>=20000||name.startsWith("/")||name.indexOf('\\')>=0||name.indexOf(':')>=0||!names.add(name))throw new IOException("unsafe Steam ZIP");for(String part:name.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");
    for(int i=0;i<2;i++){String root=rootFor(name,NAMES[i]);if(root!=null){if(source[i]!=null||common!=null&&!common.equals(root))throw new IOException("conflicting Steam roots");source[i]=e;common=root;}}
   }
   if(source[0]==null||source[1]==null)throw new IOException("missing Steam asset");boolean windows;if(source[0].getSize()==sizes[0][0])windows=false;else if(source[0].getSize()==sizes[1][0])windows=true;else throw new IOException("unsupported Steam asset");int platform=windows?1:0;
   for(int i=0;i<2;i++)collect(steam,source[i],sizes[platform][i],hashes[platform][i],i,windows,ranges);
   Map<String,byte[]> result=new LinkedHashMap<>();for(Row row:rows){byte[] value=entry(base,base.getEntry("assets/bin/Data/"+row.name),8*1024*1024);if(value.length!=row.length||!MessageDigest.isEqual(hash(value),row.before))throw new IOException("preload template differs");for(Texture t:row.ranges){if(t.filled!=t.size||!MessageDigest.isEqual(hash(t.data),t.payloadHash))throw new IOException("preload source differs");for(int n=t.target;n<t.target+t.size;n++)if(value[n]!=0)throw new IOException("preload range not blank");System.arraycopy(t.data,0,value,t.target,t.size);}if(!MessageDigest.isEqual(hash(value),row.after))throw new IOException("preload result differs");result.put(row.name,value);}return result;
  }
 }
 private SteamPreloadBodies(){}
}
