package com.openbonk.dataprep;

import java.io.*;
import java.security.MessageDigest;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Experimental ZIP-to-converted-texture stage, not yet part of the launcher. */
public final class SteamConvertedTextures {
    private static final String BASE="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static boolean compatibleBaseHash(String hash){return hash.equals(BASE)||hash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");}
    private static final String PLAN="5cbf43e71686f86f1660232db9beab57e29da05b7991c72605329b22d6772fd8";
    static native byte[] convertMip(byte[] source,int width,int height,int sourceFormat,int targetFormat);
    static native byte[] convertQualityMip(byte[] source,int width,int height,int sourceFormat);
    static native byte[] convertNormalMip(byte[] source,int width,int height,int targetFormat);
    private static final class Converted {Texture t;int targetSize,width,height,mips,sourceFormat,targetFormat;}

    public static Map<String,byte[]> restore(File baseFile,File steamFile,File planFile)throws IOException {
        if(!baseFile.isFile() || !compatibleBaseHash(hex(hash(file(baseFile,16*1024*1024))))) throw new IOException("incompatible base");
        byte[] planBytes=file(planFile,1024*1024);
        if(!hex(hash(planBytes)).equals(PLAN)) throw new IOException("incompatible converted texture plan");
        if(!steamFile.isFile() || steamFile.length()<=0 || steamFile.length()>1024L*1024*1024) throw new IOException("invalid Steam ZIP");
        try(ZipFile base=new ZipFile(baseFile); ZipFile steam=new ZipFile(steamFile); DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))) {
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','C','V','0','0','1'}) || plan.readInt()!=53) throw new IOException("invalid plan");
            byte[][] linux=new byte[4][], windows=new byte[4][];
            for(int i=0;i<4;i++) linux[i]=bytes(plan,32);
            for(int i=0;i<4;i++) windows[i]=bytes(plan,32);
            List<Converted> records=new ArrayList<>(); List<Texture> textures=new ArrayList<>(); Set<String> outputs=new HashSet<>(); long total=0;
            for(int i=0;i<53;i++) {
                Converted c=new Converted(); Texture t=new Texture(); c.t=t;
                t.name=new String(bytes(plan,32),"US-ASCII");t.templateHash=bytes(plan,32);t.resultHash=bytes(plan,32);t.payloadHash=bytes(plan,32);
                t.target=plan.readInt(); c.targetSize=plan.readInt();t.size=plan.readInt();c.width=plan.readInt();c.height=plan.readInt();c.mips=plan.readInt();c.sourceFormat=plan.readInt();c.targetFormat=plan.readInt();
                t.linuxFile=plan.readInt();t.linuxOffset=plan.readLong();t.windowsFile=plan.readInt();t.windowsOffset=plan.readLong();
                if(!t.name.matches("[0-9a-f]{32}")||!outputs.add(t.name)||t.target<0||c.targetSize<=0||c.targetSize>8*1024*1024||t.size<=0||t.size>16*1024*1024||c.width<=0||c.width>4096||c.height<=0||c.height>4096||c.mips<=0||c.mips>13||!(c.sourceFormat==3||c.sourceFormat==4||c.sourceFormat==10||c.sourceFormat==12)||!(c.targetFormat==4||c.targetFormat==45||c.targetFormat==47)||t.linuxFile<0||t.linuxFile>=4||t.windowsFile<0||t.windowsFile>=4||t.linuxOffset<0||t.windowsOffset<0||t.linuxOffset>LINUX_SIZES[t.linuxFile]-t.size||t.windowsOffset>WINDOWS_SIZES[t.windowsFile]-t.size) throw new IOException("invalid texture record");
                total+=t.size;if(total>64*1024*1024)throw new IOException("source limit exceeded");
                t.data=new byte[t.size];records.add(c);textures.add(t);
            }
            if(plan.read()!=-1) throw new IOException("trailing plan data");
            ZipEntry[] sources=new ZipEntry[4]; String commonRoot=null; Set<String> names=new HashSet<>(); Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()) {
                ZipEntry e=entries.nextElement();String name=e.getName();
                if(names.size()>=20000||name.startsWith("/")||name.indexOf('\\')>=0||name.indexOf(':')>=0||!names.add(name))throw new IOException("unsafe ZIP entry");
                for(String part:name.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                for(int i=0;i<4;i++){String root=rootFor(name,SOURCE_NAMES[i]);if(root!=null){if(sources[i]!=null||commonRoot!=null&&!commonRoot.equals(root))throw new IOException("conflicting roots");sources[i]=e;commonRoot=root;}}
            }
            if(sources[0]==null)throw new IOException("missing Steam asset");
            boolean isWindows;if(sources[0].getSize()==WINDOWS_SIZES[0])isWindows=true;else if(sources[0].getSize()==LINUX_SIZES[0])isWindows=false;else throw new IOException("unsupported Steam asset");
            for(int i=0;i<4;i++) collect(steam,sources[i],(isWindows?WINDOWS_SIZES:LINUX_SIZES)[i],(isWindows?windows:linux)[i],i,isWindows,textures);
            Map<String,byte[]> result=new LinkedHashMap<>();
            for(Converted c:records) {
                Texture t=c.t;byte[] template=entry(base,base.getEntry("assets/bin/Data/"+t.name),8*1024*1024);
                if(!MessageDigest.isEqual(hash(template),t.templateHash)||t.filled!=t.size||!MessageDigest.isEqual(hash(t.data),t.payloadHash)||t.target>template.length-c.targetSize)throw new IOException("texture template or payload differs");
                for(int i=t.target;i<t.target+c.targetSize;i++)if(template[i]!=0)throw new IOException("target not blank");
                int pos=0,dest=t.target;
                for(int level=0;level<c.mips;level++) {
                    int w=Math.max(1,c.width>>level),h=Math.max(1,c.height>>level),bw=(w+3)/4,bh=(h+3)/4;
                    int n=c.sourceFormat<=4?w*h*c.sourceFormat:bw*bh*(c.sourceFormat==10?8:16);
                    int want=c.targetFormat==4?w*h*4:bw*bh*(c.targetFormat==45?8:16);
                    if(n>t.data.length-pos||want>t.target+c.targetSize-dest)throw new IOException("invalid mip size");
                    byte[] mip=convertMip(Arrays.copyOfRange(t.data,pos,pos+n),w,h,c.sourceFormat,c.targetFormat);
                    if(mip==null||mip.length!=want)throw new IOException("native texture conversion failed");
                    System.arraycopy(mip,0,template,dest,want);pos+=n;dest+=want;
                }
                if(pos!=t.size||dest!=t.target+c.targetSize||!MessageDigest.isEqual(hash(template),t.resultHash))throw new IOException("converted texture differs");
                result.put(t.name,template);
            }
            return result;
        }
    }
    private SteamConvertedTextures(){}
}
