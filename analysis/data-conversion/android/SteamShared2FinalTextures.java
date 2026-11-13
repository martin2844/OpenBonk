package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores two font atlases and one gamepad texture, completing sharedassets2. */
public final class SteamShared2FinalTextures {
    private static final String PLAN="73bf5085969992885b116e1c037119ce3d496b13c28653002caa4d750111352e";
    private static final class Image{int dst,headerSize,pixelSize,w,h,mips,sourceFormat,targetFormat,lightmap,ignore;Texture header,pixels;byte[] objectHash;}
    public static byte[] restore(byte[] baseAsset,File steamFile,File planFile)throws IOException{
        if(baseAsset==null||baseAsset.length!=22278296)throw new IOException("invalid base asset");byte[] planBytes=file(planFile,1024*1024);
        if(!hex(hash(planBytes)).equals(PLAN))throw new IOException("incompatible final texture plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP");
        try(ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','F','2','0','0','1'})||plan.readInt()!=3)throw new IOException("invalid final texture plan");
            byte[][] lh=new byte[4][],wh=new byte[4][];for(int i=0;i<4;i++)lh[i]=bytes(plan,32);for(int i=0;i<4;i++)wh[i]=bytes(plan,32);
            int size=plan.readInt();byte[] baseHash=bytes(plan,32),resultHash=bytes(plan,32);
            if(size!=baseAsset.length||!MessageDigest.isEqual(hash(baseAsset),baseHash))throw new IOException("incompatible incomplete base asset");
            List<Image> images=new ArrayList<>();List<Texture> ranges=new ArrayList<>();long previous=0,total=0;
            for(int i=0;i<3;i++){
                Image x=new Image();x.dst=plan.readInt();x.headerSize=plan.readInt();x.pixelSize=plan.readInt();x.w=plan.readInt();x.h=plan.readInt();x.mips=plan.readInt();x.sourceFormat=plan.readInt();x.targetFormat=plan.readInt();x.lightmap=plan.readInt();x.ignore=plan.readInt();
                Texture head=new Texture(),pix=new Texture();x.header=head;x.pixels=pix;head.linuxFile=head.windowsFile=plan.readInt();head.linuxOffset=plan.readLong();head.windowsOffset=plan.readLong();pix.linuxFile=pix.windowsFile=plan.readInt();pix.linuxOffset=plan.readLong();pix.windowsOffset=plan.readLong();pix.size=plan.readInt();head.payloadHash=bytes(plan,32);pix.payloadHash=bytes(plan,32);x.objectHash=bytes(plan,32);head.size=x.headerSize;
                long end=(long)x.dst+x.headerSize+x.pixelSize+16;
                if(x.dst<previous||x.headerSize<80||x.headerSize>512||x.pixelSize<=0||x.pixelSize>8*1024*1024||end>size||x.w<=0||x.w>4096||x.h<=0||x.h>4096||x.mips<=0||x.mips>13||!(x.sourceFormat==1||x.sourceFormat==12)||x.targetFormat!=47||!(x.lightmap==0||x.lightmap==6)||!(x.ignore==0||x.ignore==1)||pix.size<=0||pix.size>8*1024*1024||head.linuxFile<0||head.linuxFile>=4||pix.linuxFile<0||pix.linuxFile>=4||head.linuxOffset<0||head.windowsOffset<0||pix.linuxOffset<0||pix.windowsOffset<0||head.linuxOffset>LINUX_SIZES[head.linuxFile]-head.size||head.windowsOffset>WINDOWS_SIZES[head.windowsFile]-head.size||pix.linuxOffset>LINUX_SIZES[pix.linuxFile]-pix.size||pix.windowsOffset>WINDOWS_SIZES[pix.windowsFile]-pix.size)throw new IOException("invalid final texture record");
                previous=end;total+=head.size+pix.size;if(total>16*1024*1024)throw new IOException("texture limit exceeded");head.data=new byte[head.size];pix.data=new byte[pix.size];images.add(x);ranges.add(head);ranges.add(pix);
            }
            if(plan.read()!=-1)throw new IOException("trailing texture plan data");ZipEntry[] source=new ZipEntry[4];String common=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                for(int i=0;i<4;i++){String root=rootFor(n,SOURCE_NAMES[i]);if(root!=null){if(source[i]!=null||common!=null&&!common.equals(root))throw new IOException("conflicting roots");common=root;source[i]=e;}}
            }
            if(source[0]==null)throw new IOException("missing Steam asset");boolean windows;if(source[0].getSize()==WINDOWS_SIZES[0])windows=true;else if(source[0].getSize()==LINUX_SIZES[0])windows=false;else throw new IOException("unsupported Steam asset");
            for(int i=0;i<4;i++)collect(steam,source[i],(windows?WINDOWS_SIZES:LINUX_SIZES)[i],(windows?wh:lh)[i],i,windows,ranges);
            byte[] result=baseAsset.clone();
            for(Image x:images){
                if(x.header.filled!=x.header.size||x.pixels.filled!=x.pixels.size||!MessageDigest.isEqual(hash(x.header.data),x.header.payloadHash)||!MessageDigest.isEqual(hash(x.pixels.data),x.pixels.payloadHash))throw new IOException("texture source differs");
                byte[] object=new byte[x.headerSize+x.pixelSize+16];System.arraycopy(x.header.data,0,object,0,x.headerSize);ByteBuffer b=ByteBuffer.wrap(object).order(ByteOrder.LITTLE_ENDIAN);int nameLength=b.getInt(0),nameEnd=4+((nameLength+3)&~3);
                if(nameLength<0||nameLength>256||nameEnd+80>x.headerSize)throw new IOException("invalid texture header");b.putInt(nameEnd+12,x.pixelSize);b.putInt(nameEnd+20,x.targetFormat);object[nameEnd+30]=(byte)x.ignore;b.putInt(nameEnd+76,x.lightmap);b.putInt(x.headerSize-4,x.pixelSize);
                int in=0,out=x.headerSize;
                for(int lv=0;lv<x.mips;lv++){
                    int w=Math.max(1,x.w>>lv),h=Math.max(1,x.h>>lv),blocks=((w+3)/4)*((h+3)/4),sourceSize=x.sourceFormat==1?w*h:blocks*16,expected=blocks*16;
                    if(sourceSize>x.pixels.size-in||expected>x.headerSize+x.pixelSize-out)throw new IOException("invalid texture mip");
                    byte[] converted=SteamConvertedTextures.convertMip(Arrays.copyOfRange(x.pixels.data,in,in+sourceSize),w,h,x.sourceFormat,47);
                    if(converted==null||converted.length!=expected)throw new IOException("texture conversion failed");System.arraycopy(converted,0,object,out,expected);in+=sourceSize;out+=expected;
                }
                if(in!=x.pixels.size||out!=x.headerSize+x.pixelSize||!MessageDigest.isEqual(hash(object),x.objectHash))throw new IOException("final texture differs");
                for(int i=0;i<object.length;i++)if(result[x.dst+i]!=0&&result[x.dst+i]!=object[i])throw new IOException("retained texture metadata differs");System.arraycopy(object,0,result,x.dst,object.length);
            }
            if(!MessageDigest.isEqual(hash(result),resultHash))throw new IOException("completed shared2 asset differs");return result;
        }
    }
    private SteamShared2FinalTextures(){}
}
