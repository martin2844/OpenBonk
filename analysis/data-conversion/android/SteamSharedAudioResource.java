package com.openbonk.dataprep;

import java.io.*;
import java.security.*;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Experimental reconstruction of 118 audio streams and the loading video. */
public final class SteamSharedAudioResource {
    private static final String PLAN="57a8feb4f98d9eb6692c208dd32d9f92556d52b41cb682936b03324945bd9883";
    private static final long RESOURCE_SIZE=60845700L;
    private static final class Clip {long target,linux,windows;int size,filled;byte[] hash;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static final class Storage implements Closeable {
        final byte[] memory; final RandomAccessFile file; final int length;
        Storage(int length,File target)throws IOException {
            this.length=length;
            if(target==null){memory=new byte[length];file=null;}
            else {
                Path p=target.toPath();
                if(Files.isSymbolicLink(p)||!Files.isRegularFile(p,LinkOption.NOFOLLOW_LINKS))throw new IOException("invalid temporary audio file");
                memory=null;file=new RandomAccessFile(target,"rw");file.setLength(0);file.setLength(length);
            }
        }
        void put(long offset,byte[] b,int start,int count)throws IOException {
            if(memory!=null)System.arraycopy(b,start,memory,(int)offset,count);
            else {file.seek(offset);file.write(b,start,count);}
        }
        int get(long offset)throws IOException {if(memory!=null)return memory[(int)offset]&255;file.seek(offset);return file.readUnsignedByte();}
        void set(long offset,int value)throws IOException {if(memory!=null)memory[(int)offset]=(byte)value;else{file.seek(offset);file.write(value);}}
        byte[] checksum(long offset,int size)throws IOException {
            MessageDigest h=digest();
            if(memory!=null)h.update(memory,(int)offset,size);
            else{file.seek(offset);byte[] b=new byte[65536];for(int left=size;left>0;){int n=Math.min(left,b.length);file.readFully(b,0,n);h.update(b,0,n);left-=n;}}
            return h.digest();
        }
        public void close()throws IOException{if(file!=null)file.close();}
    }
    static byte[] hashEntry(ZipFile zip,ZipEntry e,long size)throws IOException{
        if(e==null||e.isDirectory()||e.getSize()!=size)throw new IOException("invalid Steam asset");
        MessageDigest hash=digest();long count=0;byte[] buffer=new byte[65536];
        try(InputStream in=zip.getInputStream(e)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>size-count)throw new IOException("oversized source");hash.update(buffer,0,n);count+=n;}}
        if(count!=size)throw new IOException("truncated source");return hash.digest();
    }
    public static byte[] restore(File steamFile,File planFile)throws IOException {
        return restore(steamFile,planFile,null);
    }
    /** Writes to an existing private temporary file; the caller deletes it on failure. */
    public static void restoreToFile(File steamFile,File planFile,File output)throws IOException {
        if(output==null)throw new IOException("missing temporary audio file");
        restore(steamFile,planFile,output);
    }
    private static byte[] restore(File steamFile,File planFile,File output)throws IOException {
        byte[] planBytes=file(planFile,1024*1024);if(!hex(hash(planBytes)).equals(PLAN))throw new IOException("incompatible shared audio plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP");
        try(ZipFile zip=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','S','A','0','0','1'})||plan.readInt()!=118)throw new IOException("invalid shared audio plan");
            byte[] la=bytes(plan,32),lr=bytes(plan,32),wa=bytes(plan,32),wr=bytes(plan,32);
            int length=plan.readInt(),videoOffset=plan.readInt();byte[] expected=bytes(plan,32),videoHash=bytes(plan,32);
            if(length!=50848708||videoOffset!=50839968||length-videoOffset!=8740)throw new IOException("invalid audio resource size");
            List<Clip> clips=new ArrayList<>();long last=0;
            for(int i=0;i<118;i++){
                Clip c=new Clip();c.target=plan.readLong();c.linux=plan.readLong();c.windows=plan.readLong();c.size=plan.readInt();c.hash=bytes(plan,32);
                if(c.size<=0||c.size>8*1024*1024||c.target<last||c.target-last>=32||c.target>videoOffset-c.size||c.linux<0||c.windows<0||c.linux>RESOURCE_SIZE-c.size||c.windows>RESOURCE_SIZE-c.size)throw new IOException("invalid audio range");
                last=c.target+c.size;clips.add(c);
            }
            if(last!=videoOffset||plan.read()!=-1)throw new IOException("invalid audio layout");
            ZipEntry asset=null,resource=null;String common=null;Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> entries=zip.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();if(seen.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!seen.add(n))throw new IOException("unsafe ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                String ar=rootFor(n,"sharedassets1.assets"),rr=rootFor(n,"sharedassets1.resource"),root=ar!=null?ar:rr;
                if(root!=null){if(common!=null&&!common.equals(root))throw new IOException("conflicting roots");common=root;if(ar!=null){if(asset!=null)throw new IOException("duplicate asset");asset=e;}else{if(resource!=null)throw new IOException("duplicate resource");resource=e;}}
            }
            if(asset==null||resource==null)throw new IOException("missing Steam source");
            boolean windows;if(asset.getSize()==50251684L)windows=true;else if(asset.getSize()==56239204L)windows=false;else throw new IOException("unsupported Steam asset");
            if(!MessageDigest.isEqual(hashEntry(zip,asset,asset.getSize()),windows?wa:la)||resource.isDirectory()||resource.getSize()!=RESOURCE_SIZE)throw new IOException("Steam asset differs");
            try(Storage result=new Storage(length,output)){
            MessageDigest inputHash=digest();byte[] buffer=new byte[65536];long pos=0;int videoFilled=0;
            try(InputStream in=zip.getInputStream(resource)){
                for(int n;(n=in.read(buffer))!=-1;){
                    if(n==0)continue;if(n>RESOURCE_SIZE-pos)throw new IOException("resource too large");inputHash.update(buffer,0,n);
                    for(Clip c:clips){long start=windows?c.windows:c.linux,from=Math.max(pos,start),to=Math.min(pos+n,start+c.size);if(from<to){int len=(int)(to-from);result.put(c.target+from-start,buffer,(int)(from-pos),len);c.filled+=len;}}
                    long from=Math.max(pos,60836960L),to=Math.min(pos+n,RESOURCE_SIZE);if(from<to){int len=(int)(to-from);result.put(videoOffset+from-60836960L,buffer,(int)(from-pos),len);videoFilled+=len;}
                    pos+=n;
                }
            }
            if(pos!=RESOURCE_SIZE||videoFilled!=8740||!MessageDigest.isEqual(inputHash.digest(),windows?wr:lr))throw new IOException("Steam resource differs");
            if(windows)for(int i=0;i<7;i++){int p=videoOffset+252+i;if(result.get(p)!=0xb4)throw new IOException("unexpected video data");result.set(p,0x70);}
            if(!MessageDigest.isEqual(result.checksum(videoOffset,8740),videoHash))throw new IOException("video differs");
            for(Clip c:clips){if(c.filled!=c.size||result.get(c.target)!='F'||result.get(c.target+1)!='S'||result.get(c.target+2)!='B'||result.get(c.target+3)!='5'||!MessageDigest.isEqual(result.checksum(c.target,c.size),c.hash))throw new IOException("audio clip differs");}
            if(!MessageDigest.isEqual(result.checksum(0,length),expected))throw new IOException("reconstructed resource differs");return result.memory;
            }
        }
    }
    private SteamSharedAudioResource(){}
}
