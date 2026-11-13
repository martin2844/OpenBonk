package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores geometry or exact mesh metadata ranges in an incomplete shared1 asset. */
public final class SteamShared1Meshes {
    private static final String PLAN="7ac1084c785c3a015821b3a155aa9e4f39bee48b3a10911e91c05b5df304d0c5";
    private static final String METADATA_PLAN="f61b4d6fdf91bcf99a3b786f5b89470f04944433be4a455c1895f4199a763068";
    private static final String FLAG_PLAN="ff751ce7421ef240f6a5a3909646942f735e7c642a0243e995e89e36c711ed8d";
    private static final String STREAM_PLAN="fa485e91f9bd394392c7e7ec0aeefa341b924c3a7cb6e74e4180c4b15d442400";
    private static final int LENGTH=122379840;private static final String[] NAMES={"sharedassets1.assets","sharedassets1.assets.resS","level1","level1.resS"};
    private static final class Range {int dst,size,linuxFile,windowsFile,filled;long linux,windows;byte[] hash,data;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    private static void commit(Range r,RandomAccessFile work)throws IOException{
        if(!MessageDigest.isEqual(hash(r.data),r.hash))throw new IOException("mesh source differs");work.seek(r.dst);byte[] old=new byte[65536];int left=r.size;
        while(left>0){int n=Math.min(left,old.length);work.readFully(old,0,n);for(int i=0;i<n;i++)if(old[i]!=0)throw new IOException("mesh target not blank");left-=n;}
        work.seek(r.dst);work.write(r.data);r.data=null;
    }
    public static void restore(File baseFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,131072);String planHash=hex(hash(planData));boolean metadata=planHash.equals(METADATA_PLAN),flagsPlan=planHash.equals(FLAG_PLAN),streamPlan=planHash.equals(STREAM_PLAN);if(!metadata&&!flagsPlan&&!streamPlan&&!planHash.equals(PLAN))throw new IOException("incompatible mesh plan");Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream p=new DataInputStream(new ByteArrayInputStream(planData))){
            int rangeCount=streamPlan?168:flagsPlan?158:metadata?1012:840;if(!Arrays.equals(bytes(p,8),new byte[]{'O','B','S','1',(byte)(streamPlan?'K':flagsPlan?'J':metadata?'H':'G'),'0','0','1'})||p.readInt()!=LENGTH||p.readInt()!=rangeCount)throw new IOException("invalid mesh plan");byte[] baseHash=bytes(p,32),resultHash=bytes(p,32);long[][] sizes=new long[2][4];byte[][][] hashes=new byte[2][4][];for(int k=0;k<2;k++)for(int i=0;i<4;i++){sizes[k][i]=p.readLong();hashes[k][i]=bytes(p,32);}
            List<Range> ranges=new ArrayList<>();long previous=0,total=0;
            for(int i=0;i<rangeCount;i++){Range r=new Range();r.dst=p.readInt();r.size=p.readInt();r.linuxFile=p.readInt();r.linux=p.readLong();r.windowsFile=p.readInt();r.windows=p.readLong();r.hash=bytes(p,32);
                if(r.dst<previous||r.size<=0||r.size>8*1024*1024||r.dst>LENGTH-r.size||r.linuxFile<0||r.linuxFile>=4||r.windowsFile<0||r.windowsFile>=4||r.linux<0||r.windows<0||r.linux>sizes[0][r.linuxFile]-r.size||r.windows>sizes[1][r.windowsFile]-r.size)throw new IOException("invalid mesh range");previous=(long)r.dst+r.size;total+=r.size;ranges.add(r);}
            int[] flags=new int[flagsPlan?158:0];if(flagsPlan){if(p.readInt()!=flags.length)throw new IOException("invalid mesh flag count");int prior=-1;for(int i=0;i<flags.length;i++){flags[i]=p.readInt();if(flags[i]<=prior||flags[i]>=LENGTH)throw new IOException("invalid mesh flag");prior=flags[i];}}
            int[] offsets=new int[streamPlan?84:0],lengths=new int[offsets.length];if(streamPlan){if(p.readInt()!=offsets.length)throw new IOException("invalid vertex size count");int prior=-1;for(int i=0;i<offsets.length;i++){offsets[i]=p.readInt();lengths[i]=p.readInt();if(offsets[i]<=prior||offsets[i]>LENGTH-4||lengths[i]<=0||lengths[i]>8*1024*1024)throw new IOException("invalid vertex size");prior=offsets[i];}}
            if(p.read()!=-1||total>64*1024*1024)throw new IOException("invalid mesh data");if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");
            Path temp=Files.createTempFile(parent,".openbonk-shared1-mesh-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile steam=new ZipFile(steamFile)){
                byte[] buffer=new byte[65536];MessageDigest bh=digest();long count=0;for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-count)throw new IOException("base too large");work.write(buffer,0,n);bh.update(buffer,0,n);count+=n;}if(count!=LENGTH||!MessageDigest.isEqual(bh.digest(),baseHash))throw new IOException("base asset differs");
                ZipEntry[] entries=new ZipEntry[4];String root=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> all=steam.entries();
                while(all.hasMoreElements()){ZipEntry e=all.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe Steam ZIP");for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");for(int i=0;i<4;i++){String r=rootFor(n,NAMES[i]);if(r!=null){if(entries[i]!=null||root!=null&&!root.equals(r))throw new IOException("conflicting Steam roots");entries[i]=e;root=r;}}}
                if(entries[0]==null)throw new IOException("missing Steam asset");boolean windows;if(entries[0].getSize()==sizes[0][0])windows=false;else if(entries[0].getSize()==sizes[1][0])windows=true;else throw new IOException("unsupported Steam asset");int platform=windows?1:0;
                for(int file=0;file<4;file++){
                    ZipEntry source=entries[file];if(source==null||source.isDirectory()||source.getSize()!=sizes[platform][file])throw new IOException("invalid mesh source file");MessageDigest sourceHash=digest();long pos=0;
                    try(InputStream in=steam.getInputStream(source)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>sizes[platform][file]-pos)throw new IOException("mesh source too large");sourceHash.update(buffer,0,n);
                        for(Range r:ranges){if((windows?r.windowsFile:r.linuxFile)!=file)continue;long start=windows?r.windows:r.linux,from=Math.max(pos,start),to=Math.min(pos+n,start+r.size);if(from<to){if(r.data==null)r.data=new byte[r.size];int len=(int)(to-from);System.arraycopy(buffer,(int)(from-pos),r.data,(int)(from-start),len);r.filled+=len;if(r.filled==r.size)commit(r,work);}}pos+=n;}}
                    if(pos!=sizes[platform][file]||!MessageDigest.isEqual(sourceHash.digest(),hashes[platform][file]))throw new IOException("mesh source file differs");
                }
                for(Range r:ranges)if(r.filled!=r.size||r.data!=null)throw new IOException("incomplete mesh range");for(int flag:flags){work.seek(flag);if(work.readUnsignedByte()!=0)throw new IOException("mesh flag not blank");work.seek(flag);work.write(1);}for(int i=0;i<offsets.length;i++){work.seek(offsets[i]);if(work.readInt()!=0)throw new IOException("vertex size not blank");work.seek(offsets[i]);int n=lengths[i];work.write(n&255);work.write((n>>>8)&255);work.write((n>>>16)&255);work.write((n>>>24)&255);}if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("partial shared1 asset differs");work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared1Meshes(){}
}
