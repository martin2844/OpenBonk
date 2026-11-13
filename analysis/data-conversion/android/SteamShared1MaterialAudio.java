package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores material values and audio objects in an incomplete shared1 asset. */
public final class SteamShared1MaterialAudio {
    private static final String PLAN="fc96c214a73e702069425abe9bc75f5bdd825d9f7c73164c33c685527faafa3f";
    private static final int LENGTH=122379840;
    private static final class Item {int dst,size;long linux,windows,audioOffset,audioSize;byte[][] sourceHash;byte[] targetHash,data;int filled;boolean audio;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    public static void restore(File baseFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,131072);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible material audio plan");Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream p=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(p,8),new byte[]{'O','B','S','1','M','0','0','1'})||p.readInt()!=LENGTH||p.readInt()!=397||p.readInt()!=118)throw new IOException("invalid material audio plan");byte[] baseHash=bytes(p,32),resultHash=bytes(p,32);long[] sizes=new long[2];byte[][] hashes=new byte[2][];for(int i=0;i<2;i++){sizes[i]=p.readLong();hashes[i]=bytes(p,32);}
            List<Item> items=new ArrayList<>();long previous=0,total=0;
            for(int i=0;i<397;i++){Item x=new Item();x.dst=p.readInt();x.size=p.readInt();x.linux=p.readLong();x.windows=p.readLong();byte[] h=bytes(p,32);x.sourceHash=new byte[][]{h,h};x.targetHash=h;if(x.dst<previous||x.size<=0||x.size>65536||x.dst>LENGTH-x.size||x.linux<0||x.windows<0||x.linux>sizes[0]-x.size||x.windows>sizes[1]-x.size)throw new IOException("invalid material range");previous=(long)x.dst+x.size;total+=x.size;items.add(x);}
            previous=0;
            for(int i=0;i<118;i++){Item x=new Item();x.audio=true;x.dst=p.readInt();x.size=p.readInt();x.linux=p.readLong();x.windows=p.readLong();x.audioOffset=p.readLong();x.audioSize=p.readLong();x.sourceHash=new byte[][]{bytes(p,32),bytes(p,32)};x.targetHash=bytes(p,32);if(x.dst<previous||x.size<64||x.size>2048||x.dst>LENGTH-x.size||x.linux<0||x.windows<0||x.linux>sizes[0]-x.size||x.windows>sizes[1]-x.size||x.audioOffset<0||x.audioSize<=0||x.audioOffset>50839968L-x.audioSize)throw new IOException("invalid audio range");previous=(long)x.dst+x.size;total+=x.size;items.add(x);}
            if(p.read()!=-1||total>8*1024*1024)throw new IOException("invalid material audio data");if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");
            Path temp=Files.createTempFile(parent,".openbonk-shared1-material-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile steam=new ZipFile(steamFile)){
                byte[] buffer=new byte[65536];MessageDigest bh=digest();long count=0;for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-count)throw new IOException("base too large");work.write(buffer,0,n);bh.update(buffer,0,n);count+=n;}if(count!=LENGTH||!MessageDigest.isEqual(bh.digest(),baseHash))throw new IOException("base asset differs");
                Set<String> names=new HashSet<>();ZipEntry source=null;Enumeration<? extends ZipEntry> all=steam.entries();while(all.hasMoreElements()){ZipEntry e=all.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe Steam ZIP");for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");if(rootFor(n,"sharedassets1.assets")!=null){if(source!=null)throw new IOException("duplicate Steam asset");source=e;}}
                if(source==null||source.isDirectory())throw new IOException("missing Steam asset");boolean windows;if(source.getSize()==sizes[0])windows=false;else if(source.getSize()==sizes[1])windows=true;else throw new IOException("unsupported Steam asset");int platform=windows?1:0;MessageDigest sh=digest();long pos=0;
                for(Item x:items)x.data=new byte[x.size];
                try(InputStream in=steam.getInputStream(source)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>sizes[platform]-pos)throw new IOException("Steam asset too large");sh.update(buffer,0,n);for(Item x:items){long start=windows?x.windows:x.linux,from=Math.max(pos,start),to=Math.min(pos+n,start+x.size);if(from<to){int len=(int)(to-from);System.arraycopy(buffer,(int)(from-pos),x.data,(int)(from-start),len);x.filled+=len;}}pos+=n;}}
                if(pos!=sizes[platform]||!MessageDigest.isEqual(sh.digest(),hashes[platform]))throw new IOException("Steam asset differs");
                for(Item x:items){if(x.filled!=x.size||!MessageDigest.isEqual(hash(x.data),x.sourceHash[platform]))throw new IOException("source object differs");if(x.audio){ByteBuffer h=ByteBuffer.wrap(x.data).order(ByteOrder.LITTLE_ENDIAN);int name=h.getInt(0),end=4+((name+3)&~3);if(name<0||name>256||end+30>=x.size-20||x.data[end+30]!=1)throw new IOException("audio header differs");x.data[end+30]=0;h.putLong(x.size-20,x.audioOffset);h.putLong(x.size-12,x.audioSize);}
                    if(!MessageDigest.isEqual(hash(x.data),x.targetHash))throw new IOException("reconstructed object differs");work.seek(x.dst);byte[] old=new byte[x.size];work.readFully(old);for(byte b:old)if(b!=0)throw new IOException("target is not blank");work.seek(x.dst);work.write(x.data);}
                if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("partial shared1 asset differs");work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared1MaterialAudio(){}
}
