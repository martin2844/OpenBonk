package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Restores 72 textures in a still-incomplete second-map asset. */
public final class SteamShared2Textures {
    private static final String PLAN="5b0e463c619b1b3e37fa6ba41c6e72f59d87d659f2b044d14993da75196b4ccd";
    private static final long[] LINUX={5824160L,10434976L},WINDOWS={5503192L,10434976L};
    private static final String[] NAMES={"sharedassets2.assets","sharedassets2.assets.resS"};
    private static final class Image{int dst,headerSize,pixelSize,w,h,mips,sourceFormat,targetFormat,lightmap;Texture header,pixels;byte[] objectHash;}
    public static byte[] restorePartial(byte[] baseAsset,File steamFile,File planFile)throws IOException{
        if(baseAsset==null||baseAsset.length!=22278296)throw new IOException("invalid base asset");
        byte[] planBytes=file(planFile,1024*1024);if(!hex(hash(planBytes)).equals(PLAN))throw new IOException("incompatible texture plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP");
        try(ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','T','2','0','0','1'})||plan.readInt()!=72)throw new IOException("invalid texture plan");
            byte[][] lh=new byte[2][],wh=new byte[2][];for(int i=0;i<2;i++)lh[i]=bytes(plan,32);for(int i=0;i<2;i++)wh[i]=bytes(plan,32);
            int size=plan.readInt();byte[] baseHash=bytes(plan,32),resultHash=bytes(plan,32),targetHash=bytes(plan,32);
            if(size!=baseAsset.length||!MessageDigest.isEqual(hash(baseAsset),baseHash)||MessageDigest.isEqual(resultHash,targetHash))throw new IOException("incompatible incomplete base asset");
            List<Image> images=new ArrayList<>();List<Texture> ranges=new ArrayList<>();long previous=0,total=0;
            for(int i=0;i<72;i++){
                Image image=new Image();image.dst=plan.readInt();image.headerSize=plan.readInt();image.pixelSize=plan.readInt();image.w=plan.readInt();image.h=plan.readInt();image.mips=plan.readInt();image.sourceFormat=plan.readInt();image.targetFormat=plan.readInt();image.lightmap=plan.readInt();
                Texture head=new Texture(),pix=new Texture();image.header=head;image.pixels=pix;head.linuxOffset=plan.readLong();head.windowsOffset=plan.readLong();pix.linuxOffset=plan.readLong();pix.windowsOffset=plan.readLong();pix.size=plan.readInt();head.payloadHash=bytes(plan,32);pix.payloadHash=bytes(plan,32);image.objectHash=bytes(plan,32);head.size=image.headerSize;head.linuxFile=head.windowsFile=0;pix.linuxFile=pix.windowsFile=1;
                long end=(long)image.dst+image.headerSize+image.pixelSize+16;
                if(image.dst<previous||image.headerSize<80||image.headerSize>512||image.pixelSize<=0||image.pixelSize>8*1024*1024||end>size||image.w<=0||image.w>4096||image.h<=0||image.h>4096||image.mips<=0||image.mips>13||!(image.sourceFormat==3||image.sourceFormat==4||image.sourceFormat==10||image.sourceFormat==12||image.sourceFormat==25)||!(image.targetFormat==4||image.targetFormat==45||image.targetFormat==47)||!(image.lightmap==0||image.lightmap==6)||pix.size<=0||pix.size>8*1024*1024||head.linuxOffset<0||head.windowsOffset<0||pix.linuxOffset<0||pix.windowsOffset<0||head.linuxOffset>LINUX[0]-head.size||head.windowsOffset>WINDOWS[0]-head.size||pix.linuxOffset>LINUX[1]-pix.size||pix.windowsOffset>WINDOWS[1]-pix.size)throw new IOException("invalid texture record");
                previous=end;total+=head.size+pix.size;if(total>32*1024*1024)throw new IOException("texture data limit exceeded");head.data=new byte[head.size];pix.data=new byte[pix.size];images.add(image);ranges.add(head);ranges.add(pix);
            }
            if(plan.read()!=-1)throw new IOException("trailing texture plan data");ZipEntry[] source=new ZipEntry[2];String common=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                for(int i=0;i<2;i++){String root=rootFor(n,NAMES[i]);if(root!=null){if(source[i]!=null||common!=null&&!common.equals(root))throw new IOException("conflicting roots");common=root;source[i]=e;}}
            }
            if(source[0]==null)throw new IOException("missing Steam asset");boolean windows;if(source[0].getSize()==WINDOWS[0])windows=true;else if(source[0].getSize()==LINUX[0])windows=false;else throw new IOException("unsupported Steam asset");
            for(int i=0;i<2;i++)collect(steam,source[i],(windows?WINDOWS:LINUX)[i],(windows?wh:lh)[i],i,windows,ranges);
            byte[] result=baseAsset.clone();ByteBuffer buffer=ByteBuffer.wrap(result).order(ByteOrder.LITTLE_ENDIAN);
            for(Image x:images){
                if(x.header.filled!=x.header.size||x.pixels.filled!=x.pixels.size||!MessageDigest.isEqual(hash(x.header.data),x.header.payloadHash)||!MessageDigest.isEqual(hash(x.pixels.data),x.pixels.payloadHash))throw new IOException("texture source differs");
                int end=x.dst+x.headerSize+x.pixelSize+16;for(int i=x.dst;i<end;i++)if(result[i]!=0)throw new IOException("texture target not blank");System.arraycopy(x.header.data,0,result,x.dst,x.headerSize);
                int nameLength=buffer.getInt(x.dst),nameEnd=4+((nameLength+3)&~3);if(nameLength<0||nameLength>256||nameEnd+80>x.headerSize)throw new IOException("invalid texture header");
                buffer.putInt(x.dst+nameEnd+12,x.pixelSize);buffer.putInt(x.dst+nameEnd+20,x.targetFormat);buffer.putInt(x.dst+nameEnd+76,x.lightmap);buffer.putInt(x.dst+x.headerSize-4,x.pixelSize);
                int in=0,out=x.dst+x.headerSize;
                for(int lv=0;lv<x.mips;lv++){
                    int w=Math.max(1,x.w>>lv),h=Math.max(1,x.h>>lv),block=((w+3)/4)*((h+3)/4),sourceSize=x.sourceFormat<=4?w*h*x.sourceFormat:block*(x.sourceFormat==10?8:16),expected=x.targetFormat==4?w*h*4:block*(x.targetFormat==45?8:16);
                    if(sourceSize>x.pixels.size-in||expected>x.dst+x.headerSize+x.pixelSize-out)throw new IOException("invalid texture mip");
                    byte[] converted=SteamConvertedTextures.convertMip(Arrays.copyOfRange(x.pixels.data,in,in+sourceSize),w,h,x.sourceFormat,x.targetFormat);
                    if(converted==null||converted.length!=expected)throw new IOException("native texture conversion failed");System.arraycopy(converted,0,result,out,expected);in+=sourceSize;out+=expected;
                }
                if(in!=x.pixels.size||out!=x.dst+x.headerSize+x.pixelSize||!MessageDigest.isEqual(hash(Arrays.copyOfRange(result,x.dst,end)),x.objectHash))throw new IOException("texture result differs");
            }
            if(!MessageDigest.isEqual(hash(result),resultHash))throw new IOException("partial texture asset differs");return result;
        }
    }
    private SteamShared2Textures(){}
}
