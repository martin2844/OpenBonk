package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Converts the last two textures in the diagnostic sharedassets0 asset. */
public final class SteamShared0FinalTextures {
    private static final String PLAN="7f18abca923fed82583d7a69648bce7c5b4243bdc981ef61e7231dd00ea39ab2";
    private static final int LENGTH=132028896;
    private static final class Image {int dst,header,pixels,width,mips,format;Texture head,image;byte[] objectHash;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static byte[] fileHash(RandomAccessFile f)throws IOException{MessageDigest h=digest();byte[] b=new byte[65536];f.seek(0);for(int n;(n=f.read(b))!=-1;)if(n>0)h.update(b,0,n);return h.digest();}
    public static void restore(File baseFile,File steamFile,File planFile,File output)throws IOException{
        byte[] planData=file(planFile,4096);if(!hex(hash(planData)).equals(PLAN))throw new IOException("incompatible final texture plan");Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        try(DataInputStream p=new DataInputStream(new ByteArrayInputStream(planData))){
            if(!Arrays.equals(bytes(p,8),new byte[]{'O','B','S','0','T','0','0','1'})||p.readInt()!=LENGTH||p.readInt()!=2)throw new IOException("invalid texture plan");byte[] baseHash=bytes(p,32),resultHash=bytes(p,32);
            long[][] sizes=new long[2][2];byte[][][] hashes=new byte[2][2][];for(int k=0;k<2;k++)for(int i=0;i<2;i++){sizes[k][i]=p.readLong();hashes[k][i]=bytes(p,32);}
            List<Image> images=new ArrayList<>();List<Texture> ranges=new ArrayList<>();long last=0;
            for(int i=0;i<2;i++){
                Image x=new Image();x.dst=p.readInt();x.header=p.readInt();x.pixels=p.readInt();x.width=p.readInt();x.mips=p.readInt();x.format=p.readInt();Texture h=new Texture(),v=new Texture();x.head=h;x.image=v;h.linuxOffset=p.readLong();h.windowsOffset=p.readLong();v.linuxOffset=p.readLong();v.windowsOffset=p.readLong();v.size=p.readInt();h.payloadHash=bytes(p,32);v.payloadHash=bytes(p,32);x.objectHash=bytes(p,32);h.size=x.header;h.linuxFile=h.windowsFile=0;v.linuxFile=v.windowsFile=1;
                long end=(long)x.dst+x.header+x.pixels+16;if(x.dst<last||end>LENGTH||x.header<80||x.header>512||x.pixels<=0||x.pixels>1024*1024||!(x.width==128||x.width==256)||x.mips<1||x.mips>9||!(x.format==4||x.format==10)||v.size<=0||v.size>1024*1024||h.linuxOffset<0||h.windowsOffset<0||v.linuxOffset<0||v.windowsOffset<0||h.linuxOffset>sizes[0][0]-h.size||h.windowsOffset>sizes[1][0]-h.size||v.linuxOffset>sizes[0][1]-v.size||v.windowsOffset>sizes[1][1]-v.size)throw new IOException("invalid texture range");last=end;h.data=new byte[h.size];v.data=new byte[v.size];ranges.add(h);ranges.add(v);images.add(x);
            }
            if(p.read()!=-1)throw new IOException("trailing texture plan");if(Files.isSymbolicLink(baseFile.toPath())||!Files.isRegularFile(baseFile.toPath(),LinkOption.NOFOLLOW_LINKS)||baseFile.length()!=LENGTH)throw new IOException("invalid base asset");
            Path temp=Files.createTempFile(parent,".openbonk-final-textures-",".tmp");boolean moved=false;
            try(RandomAccessFile work=new RandomAccessFile(temp.toFile(),"rw");InputStream base=Files.newInputStream(baseFile.toPath());ZipFile steam=new ZipFile(steamFile)){
                MessageDigest bh=digest();byte[] buffer=new byte[65536];long count=0;for(int n;(n=base.read(buffer))!=-1;){if(n==0)continue;if(n>LENGTH-count)throw new IOException("base too large");work.write(buffer,0,n);bh.update(buffer,0,n);count+=n;}if(count!=LENGTH||!MessageDigest.isEqual(bh.digest(),baseHash))throw new IOException("base asset differs");
                ZipEntry[] entries=new ZipEntry[2];String root=null;Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> all=steam.entries();String[] names={"sharedassets0.assets","sharedassets0.assets.resS"};
                while(all.hasMoreElements()){ZipEntry e=all.nextElement();String n=e.getName();if(seen.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!seen.add(n))throw new IOException("unsafe Steam ZIP");for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe Steam path");for(int i=0;i<2;i++){String r=rootFor(n,names[i]);if(r!=null){if(entries[i]!=null||root!=null&&!root.equals(r))throw new IOException("conflicting Steam roots");entries[i]=e;root=r;}}}
                if(entries[0]==null)throw new IOException("missing Steam asset");boolean windows;if(entries[0].getSize()==sizes[0][0])windows=false;else if(entries[0].getSize()==sizes[1][0])windows=true;else throw new IOException("unsupported Steam asset");int platform=windows?1:0;
                for(int i=0;i<2;i++)collect(steam,entries[i],sizes[platform][i],hashes[platform][i],i,windows,ranges);
                for(Image x:images){
                    if(x.head.filled!=x.head.size||x.image.filled!=x.image.size||!MessageDigest.isEqual(hash(x.head.data),x.head.payloadHash)||!MessageDigest.isEqual(hash(x.image.data),x.image.payloadHash))throw new IOException("texture source differs");
                    byte[] object=new byte[x.header+x.pixels+16];System.arraycopy(x.head.data,0,object,0,x.header);ByteBuffer h=ByteBuffer.wrap(object).order(ByteOrder.LITTLE_ENDIAN);int name=h.getInt(0),end=4+((name+3)&~3);if(name<0||name>256||end+24>x.header)throw new IOException("invalid texture header");h.putInt(end+12,x.pixels);h.putInt(end+20,45);h.putInt(x.header-4,x.pixels);
                    int input=0,out=x.header;for(int level=0;level<x.mips;level++){int w=Math.max(1,x.width>>level),blocks=((w+3)/4)*((w+3)/4),n=x.format==4?w*w*4:blocks*8,expected=blocks*8;if(n>x.image.size-input||expected>x.header+x.pixels-out)throw new IOException("invalid texture mip");byte[] converted=SteamConvertedTextures.convertMip(Arrays.copyOfRange(x.image.data,input,input+n),w,w,x.format,45);if(converted==null||converted.length!=expected)throw new IOException("texture conversion failed");System.arraycopy(converted,0,object,out,expected);input+=n;out+=expected;}
                    if(input!=x.image.size||out!=x.header+x.pixels||!MessageDigest.isEqual(hash(object),x.objectHash))throw new IOException("converted texture differs");work.seek(x.dst);byte[] old=new byte[object.length];work.readFully(old);for(byte b:old)if(b!=0)throw new IOException("texture target not blank");work.seek(x.dst);work.write(object);
                }
                if(work.length()!=LENGTH||!MessageDigest.isEqual(fileHash(work),resultHash))throw new IOException("complete asset differs");work.getFD().sync();Files.move(temp,dest);moved=true;
            }finally{if(!moved)Files.deleteIfExists(temp);}
        }
    }
    private SteamShared0FinalTextures(){}
}
