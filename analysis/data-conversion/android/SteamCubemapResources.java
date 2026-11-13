package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Converts seven pinned six-face Steam cubemaps for a private diagnostic. */
public final class SteamCubemapResources {
    private static final String PLAN="e7fd277acda57efac892a1921d85ae35fd947d1d8a10f648923636f4a4ed62be";private static final int LENGTH=122379840,PIXELS=786432,FACE=262144;
    static native byte[] unpackCrunch(byte[] input);
    private static final class Cube {int dst,header,format,lightmap,ignore;Texture head,image;byte[] decodedHash,objectHash;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    public static void restore(File baseFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,8192);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible cubemap plan");Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream p=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(p,8),new byte[]{'O','B','S','1','C','U','B','1'})||p.readInt()!=LENGTH||p.readInt()!=7)throw new IOException("invalid cubemap plan");byte[] baseHash=bytes(p,32),resultHash=bytes(p,32);long[][] sizes=new long[2][2];byte[][][] hashes=new byte[2][2][];for(int k=0;k<2;k++)for(int i=0;i<2;i++){sizes[k][i]=p.readLong();hashes[k][i]=bytes(p,32);}
            List<Cube> cubes=new ArrayList<>();List<Texture> ranges=new ArrayList<>();long previous=0,total=0;
            for(int i=0;i<7;i++){
                Cube x=new Cube();x.dst=p.readInt();x.header=p.readInt();x.format=p.readInt();x.lightmap=p.readInt();x.ignore=p.readInt();Texture h=new Texture(),v=new Texture();x.head=h;x.image=v;h.linuxOffset=p.readLong();h.windowsOffset=p.readLong();v.linuxOffset=p.readLong();v.windowsOffset=p.readLong();v.size=p.readInt();h.payloadHash=bytes(p,32);v.payloadHash=bytes(p,32);x.decodedHash=bytes(p,32);x.objectHash=bytes(p,32);h.size=x.header;h.linuxFile=h.windowsFile=0;v.linuxFile=v.windowsFile=1;long end=(long)x.dst+x.header+PIXELS+92;
                if(x.dst<previous||end>LENGTH||x.header<80||x.header>512||!(x.format==12||x.format==29)||!(x.lightmap==0||x.lightmap==6)||!(x.ignore==0||x.ignore==1)||v.size<=0||v.size>4*1024*1024||h.linuxOffset<0||h.windowsOffset<0||v.linuxOffset<0||v.windowsOffset<0||h.linuxOffset>sizes[0][0]-h.size||h.windowsOffset>sizes[1][0]-h.size||v.linuxOffset>sizes[0][1]-v.size||v.windowsOffset>sizes[1][1]-v.size)throw new IOException("invalid cubemap range");previous=end;total+=h.size+v.size;h.data=new byte[h.size];v.data=new byte[v.size];ranges.add(h);ranges.add(v);cubes.add(x);
            }
            if(p.read()!=-1||total>16*1024*1024)throw new IOException("invalid cubemap data");if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");
            Path temp=Files.createTempFile(parent,".openbonk-shared1-cubemap-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile steam=new ZipFile(steamFile)){
                byte[] buffer=new byte[65536];MessageDigest bh=digest();long count=0;for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-count)throw new IOException("base too large");work.write(buffer,0,n);bh.update(buffer,0,n);count+=n;}if(count!=LENGTH||!MessageDigest.isEqual(bh.digest(),baseHash))throw new IOException("base asset differs");
                ZipEntry[] entries=new ZipEntry[2];String root=null;String[] names={"sharedassets1.assets","sharedassets1.assets.resS"};Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> all=steam.entries();
                while(all.hasMoreElements()){ZipEntry e=all.nextElement();String n=e.getName();if(seen.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!seen.add(n))throw new IOException("unsafe Steam ZIP");for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");for(int i=0;i<2;i++){String r=rootFor(n,names[i]);if(r!=null){if(entries[i]!=null||root!=null&&!root.equals(r))throw new IOException("conflicting Steam roots");entries[i]=e;root=r;}}}
                if(entries[0]==null)throw new IOException("missing Steam asset");boolean windows;if(entries[0].getSize()==sizes[0][0])windows=false;else if(entries[0].getSize()==sizes[1][0])windows=true;else throw new IOException("unsupported Steam asset");int platform=windows?1:0;for(int i=0;i<2;i++)collect(steam,entries[i],sizes[platform][i],hashes[platform][i],i,windows,ranges);
                for(Cube x:cubes){
                    if(x.head.filled!=x.head.size||x.image.filled!=x.image.size||!MessageDigest.isEqual(hash(x.head.data),x.head.payloadHash)||!MessageDigest.isEqual(hash(x.image.data),x.image.payloadHash))throw new IOException("cubemap source differs");
                    byte[] decoded=x.format==29?unpackCrunch(x.image.data):x.image.data;if(decoded==null||decoded.length!=6*FACE||!MessageDigest.isEqual(hash(decoded),x.decodedHash))throw new IOException("cubemap decode differs");
                    byte[] header=x.head.data.clone();ByteBuffer h=ByteBuffer.wrap(header).order(ByteOrder.LITTLE_ENDIAN);int name=h.getInt(0),end=4+((name+3)&~3);if(name<0||name>256||end+80>x.header||h.getInt(end)!=0)throw new IOException("invalid cubemap header");h.putInt(end,1);h.putInt(end+12,131072);h.putInt(end+20,45);header[end+30]=(byte)x.ignore;h.putInt(end+76,x.lightmap);h.putInt(x.header-4,PIXELS);
                    byte[] pixels=new byte[PIXELS];for(int face=0;face<6;face++){byte[] encoded=SteamConvertedTextures.convertMip(Arrays.copyOfRange(decoded,face*FACE,(face+1)*FACE),512,512,12,45);if(encoded==null||encoded.length!=131072)throw new IOException("cubemap conversion failed");System.arraycopy(encoded,0,pixels,face*131072,131072);}
                    byte[] tail=new byte[92];tail[16]=6;MessageDigest oh=digest();oh.update(header);oh.update(pixels);oh.update(tail);if(!MessageDigest.isEqual(oh.digest(),x.objectHash))throw new IOException("converted cubemap differs");
                    work.seek(x.dst);int left=x.header+PIXELS+tail.length;while(left>0){int n=Math.min(left,buffer.length);work.readFully(buffer,0,n);for(int j=0;j<n;j++)if(buffer[j]!=0)throw new IOException("cubemap target not blank");left-=n;}work.seek(x.dst);work.write(header);work.write(pixels);work.write(tail);
                }
                if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("partial shared1 asset differs");work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamCubemapResources(){}
}
