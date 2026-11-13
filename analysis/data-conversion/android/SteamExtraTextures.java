package com.openbonk.dataprep;

import java.io.*;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Private diagnostic; its plan includes Android-derived serialized bytes. */
public final class SteamExtraTextures {
    private static final String BASE="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static boolean compatibleBaseHash(String hash){return hash.equals(BASE)||hash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");}
    private static final String PLAN="7146afb527c39becc5b673e29dcc808747839994189372effc6fd779b5b01a19";
    private static final String DERIVED_PLAN="7d703359af8eb9973ee60ea97ff85248b83116cfa0dfc08ae0c1a55a4ef410b4";
    private static final String[] NAMES={"sharedassets1.assets","sharedassets1.assets.resS","sharedassets2.assets","sharedassets2.assets.resS","resources.assets","resources.assets.resS"};
    private static final long[] LINUX={56239204L,113115008L,5824160L,10434976L,4092292L,2621536L},WINDOWS={50251684L,113115008L,5503192L,10434976L,527348L,2621536L};
    private static final class Record {Texture t;int targetSize,w,h,mips,sourceFormat,targetFormat,suffix;byte[] patch;}

    public static Map<String,byte[]> restorePrivateDiagnostic(File baseFile,File steamFile,File planFile)throws IOException {
        return restoreInternal(baseFile,steamFile,planFile,false);
    }
    /** Derived fields and hashes only; independent distribution review remains pending. */
    public static Map<String,byte[]> restore(File baseFile,File steamFile,File planFile)throws IOException {
        return restoreInternal(baseFile,steamFile,planFile,true);
    }
    private static Map<String,byte[]> restoreInternal(File baseFile,File steamFile,File planFile,boolean derived)throws IOException {
        if(!baseFile.isFile()||!compatibleBaseHash(hex(hash(file(baseFile,16*1024*1024)))))throw new IOException("incompatible base");
        byte[] planBytes=file(planFile,1024*1024);if(!hex(hash(planBytes)).equals(derived?DERIVED_PLAN:PLAN))throw new IOException("incompatible extra texture plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP");
        try(ZipFile base=new ZipFile(baseFile);ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))) {
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','E','X','0','0',(byte)(derived?'2':'1')})||plan.readInt()!=25)throw new IOException("invalid extra plan");
            byte[][] lh=new byte[6][],wh=new byte[6][];for(int i=0;i<6;i++)lh[i]=bytes(plan,32);for(int i=0;i<6;i++)wh[i]=bytes(plan,32);
            List<Record> records=new ArrayList<>();List<Texture> textures=new ArrayList<>();Set<String> outputs=new HashSet<>();long total=0;
            for(int i=0;i<25;i++) {
                Record r=new Record();Texture t=new Texture();r.t=t;t.name=new String(bytes(plan,32),"US-ASCII");t.templateHash=bytes(plan,32);t.resultHash=bytes(plan,32);t.payloadHash=bytes(plan,32);
                t.target=plan.readInt();r.targetSize=plan.readInt();t.size=plan.readInt();r.w=plan.readInt();r.h=plan.readInt();r.mips=plan.readInt();r.sourceFormat=plan.readInt();r.targetFormat=plan.readInt();r.suffix=plan.readInt();
                t.linuxFile=plan.readInt();t.linuxOffset=plan.readLong();t.windowsFile=plan.readInt();t.windowsOffset=plan.readLong();int patches=plan.readInt();
                if(!t.name.matches("[0-9a-f]{32}")||!outputs.add(t.name)||t.target<0||t.target>65536||r.targetSize<=0||r.targetSize>8*1024*1024||t.size<=0||t.size>16*1024*1024||r.w<=0||r.w>4096||r.h<=0||r.h>4096||r.mips<=0||r.mips>13||!(r.sourceFormat==1||r.sourceFormat==4||r.sourceFormat==10||r.sourceFormat==12)||!(r.targetFormat==1||r.targetFormat==4)||r.suffix<0||r.suffix>65536||t.linuxFile<0||t.linuxFile>=6||t.windowsFile<0||t.windowsFile>=6||t.linuxOffset<0||t.windowsOffset<0||t.linuxOffset>LINUX[t.linuxFile]-t.size||t.windowsOffset>WINDOWS[t.windowsFile]-t.size||patches<0||patches>256||patches>plan.available()/5)throw new IOException("invalid extra texture record");
                if(derived&&patches!=0)throw new IOException("derived plan has header bytes");
                r.patch=bytes(plan,patches*5);total+=t.size;if(total>64*1024*1024)throw new IOException("source limit exceeded");t.data=new byte[t.size];records.add(r);textures.add(t);
            }
            if(plan.read()!=-1)throw new IOException("trailing plan data");
            ZipEntry[] sources=new ZipEntry[6];String common=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String n=e.getName();
                if(names.size()>=20000||n.startsWith("/")||n.indexOf('\\')>=0||n.indexOf(':')>=0||!names.add(n))throw new IOException("unsafe ZIP entry");
                for(String part:n.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                for(int i=0;i<6;i++){String root=rootFor(n,NAMES[i]);if(root!=null){if(sources[i]!=null||common!=null&&!common.equals(root))throw new IOException("conflicting roots");sources[i]=e;common=root;}}
            }
            if(sources[0]==null)throw new IOException("missing source");boolean windows;if(sources[0].getSize()==WINDOWS[0])windows=true;else if(sources[0].getSize()==LINUX[0])windows=false;else throw new IOException("unsupported source");
            for(int i=0;i<6;i++)collect(steam,sources[i],(windows?WINDOWS:LINUX)[i],(windows?wh:lh)[i],i,windows,textures);
            Map<String,byte[]> results=new LinkedHashMap<>();
            for(Record r:records){
                Texture t=r.t;byte[] template=entry(base,base.getEntry("assets/bin/Data/"+t.name),8*1024*1024);
                if(!MessageDigest.isEqual(hash(template),t.templateHash)||t.filled!=t.size||!MessageDigest.isEqual(hash(t.data),t.payloadHash)||t.target>template.length-r.suffix)throw new IOException("template or source differs");
                byte[] output=new byte[t.target+r.targetSize+r.suffix];System.arraycopy(template,0,output,0,t.target);System.arraycopy(template,template.length-r.suffix,output,output.length-r.suffix,r.suffix);
                int previous=-1;for(int p=0;p<r.patch.length;p+=5){int at=((r.patch[p]&255)<<24)|((r.patch[p+1]&255)<<16)|((r.patch[p+2]&255)<<8)|(r.patch[p+3]&255);byte value=r.patch[p+4];if(at<=previous||at>=t.target||output[at]==value)throw new IOException("invalid header patch");output[at]=value;previous=at;}
                if(derived && t.target!=0){
                    if(t.target<128||output.length<128+r.suffix||ByteBuffer.wrap(template).order(ByteOrder.BIG_ENDIAN).getLong(24)!=template.length)throw new IOException("invalid serialized header");
                    ByteBuffer sourceHeader=ByteBuffer.wrap(template).order(ByteOrder.LITTLE_ENDIAN),header=ByteBuffer.wrap(output).order(ByteOrder.LITTLE_ENDIAN);
                    if(sourceHeader.getInt(t.target-80)!=sourceHeader.getInt(t.target-4))throw new IOException("inconsistent image size");
                    ByteBuffer.wrap(output).order(ByteOrder.BIG_ENDIAN).putLong(24,output.length);
                    header.putInt(112,output.length-128-r.suffix);header.putInt(t.target-80,r.targetSize);header.putInt(t.target-72,r.targetFormat);header.putInt(t.target-4,r.targetSize);
                }else if(derived&&(r.targetFormat!=1||r.targetSize!=1||template.length!=output.length))throw new IOException("invalid single-pixel layout");
                int from=0,to=t.target;
                for(int lv=0;lv<r.mips;lv++){
                    int w=Math.max(1,r.w>>lv),h=Math.max(1,r.h>>lv),bw=(w+3)/4,bh=(h+3)/4;
                    int n=r.sourceFormat<=4?w*h*r.sourceFormat:bw*bh*(r.sourceFormat==10?8:16),want=w*h*(r.targetFormat==1?1:4);
                    if(n>t.size-from||want>t.target+r.targetSize-to)throw new IOException("invalid mip length");
                    byte[] mip=SteamConvertedTextures.convertMip(Arrays.copyOfRange(t.data,from,from+n),w,h,r.sourceFormat,r.targetFormat);
                    if(mip==null||mip.length!=want)throw new IOException("conversion failed");System.arraycopy(mip,0,output,to,want);from+=n;to+=want;
                }
                if(from!=t.size||to!=t.target+r.targetSize||!MessageDigest.isEqual(hash(output),t.resultHash))throw new IOException("converted texture differs");results.put(t.name,output);
            }
            return results;
        }
    }
    private SteamExtraTextures(){}
}
