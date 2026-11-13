package com.openbonk.dataprep;
import java.io.*;
import java.nio.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Reconstructs the pinned final-map asset; remaining OBB content is separate. */
public final class SteamShared4MapAsset {
    private static final String BASE="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static boolean compatibleBaseHash(String hash){return hash.equals(BASE)||hash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");}
    private static final String PLAN="bb56a41427da05f65ef79da2245553b81968d978d18aae420d6e3c879433d25f";
    private static final String[] NAMES={"sharedassets4.assets","sharedassets4.assets.resS","level4"};
    private static final long[] LINUX={693868L,564048L,2701232L},WINDOWS={693876L,564048L,2701248L};
    private static final class Image {Texture source;int destination,size,width,height,mips;byte[] outputHash;}
    private static final class Audio {int offset;long oldOffset,oldSize,newOffset,newSize;}
    public static Map<String,byte[]> restore(File baseFile,File steamFile,File planFile)throws IOException{
        if(!baseFile.isFile()||!compatibleBaseHash(hex(hash(file(baseFile,16*1024*1024)))))throw new IOException("incompatible base");
        byte[] planBytes=file(planFile,1024*1024);if(!hex(hash(planBytes)).equals(PLAN))throw new IOException("incompatible map plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP");
        try(ZipFile base=new ZipFile(baseFile);ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','M','4','0','0','2'})||plan.readInt()!=56)throw new IOException("invalid map plan");
            byte[][] lh=new byte[3][],wh=new byte[3][];for(int i=0;i<3;i++)lh[i]=bytes(plan,32);for(int i=0;i<3;i++)wh[i]=bytes(plan,32);
            int assetSize=plan.readInt();byte[] baseHash=bytes(plan,32),resultHash=bytes(plan,32),targetHash=bytes(plan,32);
            if(assetSize!=1334816||!MessageDigest.isEqual(resultHash,targetHash))throw new IOException("invalid asset layout");
            List<Texture> ranges=new ArrayList<>(),all=new ArrayList<>();long previous=0;
            for(int i=0;i<56;i++){
                Texture t=new Texture();t.target=plan.readInt();t.size=plan.readInt();t.linuxFile=plan.readInt();t.linuxOffset=plan.readLong();t.windowsFile=plan.readInt();t.windowsOffset=plan.readLong();t.payloadHash=bytes(plan,32);
                if(t.target<previous||t.size<=0||t.target>assetSize-t.size||t.linuxFile<0||t.linuxFile>=3||t.windowsFile<0||t.windowsFile>=3||t.linuxOffset<0||t.windowsOffset<0||t.linuxOffset>LINUX[t.linuxFile]-t.size||t.windowsOffset>WINDOWS[t.windowsFile]-t.size)throw new IOException("invalid copy range");
                previous=(long)t.target+t.size;t.data=new byte[t.size];ranges.add(t);all.add(t);
            }
            if(plan.readInt()!=3)throw new IOException("invalid texture count");List<Image> images=new ArrayList<>();
            for(int i=0;i<3;i++){
                Image image=new Image();Texture t=new Texture();image.source=t;image.destination=plan.readInt();image.size=plan.readInt();t.linuxOffset=plan.readLong();t.windowsOffset=plan.readLong();t.size=plan.readInt();image.width=plan.readInt();image.height=plan.readInt();image.mips=plan.readInt();t.payloadHash=bytes(plan,32);image.outputHash=bytes(plan,32);t.linuxFile=t.windowsFile=1;
                if(image.destination<0||image.size<=0||image.destination>assetSize-image.size||t.size<=0||t.size>564048||t.linuxOffset<0||t.windowsOffset<0||t.linuxOffset>LINUX[1]-t.size||t.windowsOffset>WINDOWS[1]-t.size||image.width<=0||image.width>4096||image.height<=0||image.height>4096||image.mips<=0||image.mips>13)throw new IOException("invalid texture range");
                t.data=new byte[t.size];images.add(image);all.add(t);
            }
            if(plan.readInt()!=6)throw new IOException("invalid audio reference count");List<Audio> audio=new ArrayList<>();
            for(int i=0;i<6;i++){Audio r=new Audio();r.offset=plan.readInt();r.oldOffset=plan.readLong();r.oldSize=plan.readLong();r.newOffset=plan.readLong();r.newSize=plan.readLong();if(r.offset<0||r.offset>assetSize-16||r.oldOffset<0||r.oldSize<0||r.newOffset<0||r.newSize<=0||r.newOffset>1780640-r.newSize)throw new IOException("invalid audio reference");audio.add(r);}
            if(plan.read()!=-1)throw new IOException("trailing map plan data");
            ZipEntry[] sources=new ZipEntry[3];String common=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                for(int i=0;i<3;i++){String root=rootFor(n,NAMES[i]);if(root!=null){if(sources[i]!=null||common!=null&&!common.equals(root))throw new IOException("conflicting Steam roots");common=root;sources[i]=e;}}
            }
            if(sources[0]==null)throw new IOException("missing Steam asset");boolean windows;if(sources[0].getSize()==WINDOWS[0])windows=true;else if(sources[0].getSize()==LINUX[0])windows=false;else throw new IOException("unsupported Steam asset");
            for(int i=0;i<3;i++)collect(steam,sources[i],(windows?WINDOWS:LINUX)[i],(windows?wh:lh)[i],i,windows,all);
            byte[] first=entry(base,base.getEntry("assets/bin/Data/sharedassets4.assets.split0"),1048576),second=entry(base,base.getEntry("assets/bin/Data/sharedassets4.assets.split1"),1048576);
            if(first.length!=1048576||first.length+second.length!=assetSize)throw new IOException("base asset size differs");byte[] asset=Arrays.copyOf(first,assetSize);System.arraycopy(second,0,asset,first.length,second.length);
            if(!MessageDigest.isEqual(hash(asset),baseHash))throw new IOException("base asset differs");
            for(Texture t:ranges){if(t.filled!=t.size||!MessageDigest.isEqual(hash(t.data),t.payloadHash))throw new IOException("copy source differs");for(int i=t.target;i<t.target+t.size;i++)if(asset[i]!=0)throw new IOException("copy target not blank");System.arraycopy(t.data,0,asset,t.target,t.size);}
            for(Image image:images){Texture t=image.source;if(t.filled!=t.size||!MessageDigest.isEqual(hash(t.data),t.payloadHash))throw new IOException("texture source differs");for(int i=image.destination;i<image.destination+image.size;i++)if(asset[i]!=0)throw new IOException("texture target not blank");
                int src=0,dst=image.destination;for(int lv=0;lv<image.mips;lv++){int w=Math.max(1,image.width>>lv),h=Math.max(1,image.height>>lv),n=((w+3)/4)*((h+3)/4)*8;if(n>t.size-src||n>image.destination+image.size-dst)throw new IOException("invalid texture mip");byte[] converted=SteamConvertedTextures.convertMip(Arrays.copyOfRange(t.data,src,src+n),w,h,10,45);if(converted==null||converted.length!=n)throw new IOException("texture conversion failed");System.arraycopy(converted,0,asset,dst,n);src+=n;dst+=n;}
                if(src!=t.size||dst!=image.destination+image.size||!MessageDigest.isEqual(hash(Arrays.copyOfRange(asset,image.destination,dst)),image.outputHash))throw new IOException("texture result differs");
            }
            ByteBuffer buffer=ByteBuffer.wrap(asset).order(ByteOrder.LITTLE_ENDIAN);for(Audio r:audio){if(buffer.getLong(r.offset)!=r.oldOffset||buffer.getLong(r.offset+8)!=r.oldSize)throw new IOException("audio reference differs");buffer.putLong(r.offset,r.newOffset);buffer.putLong(r.offset+8,r.newSize);}
            if(!MessageDigest.isEqual(hash(asset),resultHash))throw new IOException("reconstructed map asset differs");
            Map<String,byte[]> result=new LinkedHashMap<>();result.put("sharedassets4.assets.split0",Arrays.copyOfRange(asset,0,1048576));result.put("sharedassets4.assets.split1",Arrays.copyOfRange(asset,1048576,asset.length));return result;
        }
    }
    private SteamShared4MapAsset(){}
}
