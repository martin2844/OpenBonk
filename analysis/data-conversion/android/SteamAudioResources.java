package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.*;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Experimental recovery of 63 Steam audio resources and their Android metadata. */
public final class SteamAudioResources {
    private static final String BASE_SHA="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static boolean compatibleBaseHash(String hash){return hash.equals(BASE_SHA)||hash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");}
    private static final String PLAN_SHA="ae6023707a925ce13f6b222adcc691906f0e161c4d47c026a7ddf0fdad4c206a";
    private static final long RESOURCE_SIZE=60845700L;
    private static final class Clip {
        String name;long linux,windows;int size,oldSize,templateSize,append,filled;byte[] templateHash,assetHash,audioHash,audio;
    }
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] hash(byte[] b){return digest().digest(b);}
    private static String hex(byte[] b){char[] d="0123456789abcdef".toCharArray(),r=new char[b.length*2];for(int i=0;i<b.length;i++){r[2*i]=d[(b[i]&255)>>>4];r[2*i+1]=d[b[i]&15];}return new String(r);}
    private static byte[] read(InputStream in,int limit)throws IOException{
        ByteArrayOutputStream out=new ByteArrayOutputStream();byte[] b=new byte[65536];int total=0;
        for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>limit-total)throw new IOException("input too large");out.write(b,0,n);total+=n;}return out.toByteArray();
    }
    private static byte[] file(File f,int limit)throws IOException{try(InputStream in=new FileInputStream(f)){return read(in,limit);}}
    private static byte[] entry(ZipFile zip,ZipEntry e,int limit)throws IOException{
        if(e==null||e.isDirectory()||e.getSize()<0||e.getSize()>limit)throw new IOException("invalid ZIP entry");
        try(InputStream in=zip.getInputStream(e)){byte[] b=read(in,limit);if(b.length!=e.getSize())throw new IOException("truncated ZIP entry");return b;}
    }
    private static byte[] bytes(DataInputStream in,int n)throws IOException{byte[] b=new byte[n];in.readFully(b);return b;}
    private static String rootFor(String name,String suffix){if(name.equals(suffix))return "";if(name.equals("Megabonk_Data/"+suffix)||name.endsWith("/Megabonk_Data/"+suffix))return name.substring(0,name.length()-suffix.length());return null;}
    private static byte[] digestEntry(ZipFile z,ZipEntry e,long expected)throws IOException{
        if(e==null||e.isDirectory()||e.getSize()!=expected)throw new IOException("invalid source entry");
        MessageDigest hash=digest();byte[] b=new byte[65536];long total=0;
        try(InputStream in=z.getInputStream(e)){for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>expected-total)throw new IOException("source too large");hash.update(b,0,n);total+=n;}}
        if(total!=expected)throw new IOException("truncated source");return hash.digest();
    }
    private static long get64(byte[] b,int off,boolean little)throws IOException{
        if(off<0||off>b.length-8)throw new IOException("invalid metadata offset");long n=0;
        for(int i=0;i<8;i++)n|=(long)(b[off+i]&255)<<(8*(little?i:7-i));return n;
    }
    private static void put64(byte[] b,int off,long value,boolean little){for(int i=0;i<8;i++)b[off+i]=(byte)(value>>>(8*(little?i:7-i)));}
    public static Map<String,byte[]> restore(File baseFile,File steamFile,File planFile)throws IOException{
        if(!baseFile.isFile()||baseFile.length()>16*1024*1024||!compatibleBaseHash(hex(hash(file(baseFile,16*1024*1024)))))throw new IOException("incompatible base OBB");
        byte[] planBytes=file(planFile,1024*1024);if(!hex(hash(planBytes)).equals(PLAN_SHA))throw new IOException("incompatible audio plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP size");
        try(ZipFile base=new ZipFile(baseFile);ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','A','U','0','0','1'})||plan.readInt()!=63)throw new IOException("invalid audio plan");
            byte[] linuxAsset=bytes(plan,32),linuxResource=bytes(plan,32),windowsAsset=bytes(plan,32),windowsResource=bytes(plan,32);
            List<Clip> clips=new ArrayList<>();Set<String> outputs=new HashSet<>();long totalAudio=0;
            for(int i=0;i<63;i++){
                Clip c=new Clip();c.name=new String(bytes(plan,32),"US-ASCII");c.linux=plan.readLong();c.windows=plan.readLong();c.size=plan.readInt();c.oldSize=plan.readInt();c.templateSize=plan.readInt();c.append=plan.readInt();
                c.templateHash=bytes(plan,32);c.assetHash=bytes(plan,32);c.audioHash=bytes(plan,32);
                if(!c.name.matches("[0-9a-f]{32}")||!outputs.add(c.name)||c.size<=0||c.size>8*1024*1024||c.oldSize<=0||c.templateSize<40||c.templateSize>4096||(c.append!=0&&c.append!=4)||c.linux<0||c.windows<0||c.linux>RESOURCE_SIZE-c.size||c.windows>RESOURCE_SIZE-c.size)throw new IOException("invalid audio plan record");
                totalAudio+=c.size;if(totalAudio>20*1024*1024)throw new IOException("audio limit exceeded");c.audio=new byte[c.size];clips.add(c);
            }
            if(plan.read()!=-1)throw new IOException("trailing audio plan data");
            ZipEntry asset=null,resource=null;String assetRoot=null,resourceRoot=null;Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String name=e.getName();
                if(seen.size()>=20000||name.startsWith("/")||name.indexOf('\\')>=0||name.indexOf(':')>=0||!seen.add(name))throw new IOException("unsafe or duplicate ZIP entry");
                for(String part:name.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                String root=rootFor(name,"sharedassets1.assets");if(root!=null){if(asset!=null)throw new IOException("multiple asset roots");asset=e;assetRoot=root;}
                root=rootFor(name,"sharedassets1.resource");if(root!=null){if(resource!=null)throw new IOException("multiple resource roots");resource=e;resourceRoot=root;}
            }
            if(asset==null||resource==null||!assetRoot.equals(resourceRoot))throw new IOException("missing or conflicting Steam data");
            boolean windows;if(asset.getSize()==50251684L)windows=true;else if(asset.getSize()==56239204L)windows=false;else throw new IOException("unsupported Steam asset");
            if(!MessageDigest.isEqual(digestEntry(steam,asset,asset.getSize()),windows?windowsAsset:linuxAsset))throw new IOException("Steam asset checksum differs");
            if(resource.isDirectory()||resource.getSize()!=RESOURCE_SIZE)throw new IOException("invalid resource size");
            MessageDigest resourceHash=digest();byte[] buf=new byte[65536];long offset=0;
            try(InputStream in=steam.getInputStream(resource)){
                for(int n;(n=in.read(buf))!=-1;){
                    if(n==0)continue;if(n>RESOURCE_SIZE-offset)throw new IOException("resource exceeds limit");resourceHash.update(buf,0,n);
                    for(Clip c:clips){long start=windows?c.windows:c.linux,from=Math.max(start,offset),to=Math.min(start+c.size,offset+n);if(from<to){int len=(int)(to-from);System.arraycopy(buf,(int)(from-offset),c.audio,(int)(from-start),len);c.filled+=len;}}
                    offset+=n;
                }
            }
            if(offset!=RESOURCE_SIZE||!MessageDigest.isEqual(resourceHash.digest(),windows?windowsResource:linuxResource))throw new IOException("Steam resource checksum differs");
            Map<String,byte[]> result=new LinkedHashMap<>();
            for(Clip c:clips){
                if(c.filled!=c.size||c.audio.length<4||c.audio[0]!='F'||c.audio[1]!='S'||c.audio[2]!='B'||c.audio[3]!='5'||!MessageDigest.isEqual(hash(c.audio),c.audioHash))throw new IOException("audio payload differs");
                byte[] original=entry(base,base.getEntry("assets/bin/Data/"+c.name),4096);
                if(original.length!=c.templateSize||!MessageDigest.isEqual(hash(original),c.templateHash)||get64(original,24,false)!=original.length||get64(original,original.length-12,true)!=c.oldSize)throw new IOException("audio template differs");
                byte[] patched=Arrays.copyOf(original,original.length+c.append);put64(patched,24,patched.length,false);put64(patched,original.length-12,c.size,true);
                if(!MessageDigest.isEqual(hash(patched),c.assetHash))throw new IOException("audio metadata differs");
                result.put(c.name,patched);result.put(c.name+".resource",c.audio);
            }
            return result;
        }
    }
    public static void restoreToDirectory(File base,File steam,File plan,File output)throws IOException{
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be a new directory");
        Map<String,byte[]> files=restore(base,steam,plan);Path staged=Files.createTempDirectory(parent,".openbonk-audio-");
        try{
            for(Map.Entry<String,byte[]> item:files.entrySet()){
                Path path=staged.resolve(item.getKey());Files.write(path,item.getValue(),StandardOpenOption.CREATE_NEW,StandardOpenOption.WRITE);
                if(!MessageDigest.isEqual(hash(file(path.toFile(),8*1024*1024)),hash(item.getValue())))throw new IOException("staged audio differs");
            }
            Files.move(staged,dest);
        }finally{if(Files.exists(staged,LinkOption.NOFOLLOW_LINKS)){for(String name:files.keySet())Files.deleteIfExists(staged.resolve(name));Files.deleteIfExists(staged);}}
    }
    private SteamAudioResources(){}
}
