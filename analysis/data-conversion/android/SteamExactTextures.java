package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.LinkOption;
import java.nio.file.Path;
import java.nio.file.StandardOpenOption;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.*;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;

/** Experimental exact recovery of pinned Steam texture or animation payloads. */
public final class SteamExactTextures {
    private static final String BASE_SHA="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static boolean compatibleBaseHash(String hash){return hash.equals(BASE_SHA)||hash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");}
    private static final String PLAN_SHA="052c62fc74526a35408e24ea1e32bcf9af7bd9b7f40bf47423bcdedee288b992";
    private static final String ANIMATION_PLAN_SHA="c44f6ec24a439fe9d2ab86cdeaf888f64024c95977764fef15a1d1353fc2d241";
    private static final String PRIVATE_ANIMATION_PLAN_SHA="c6344331782f79bcc2c26dfb10ecfc942ef435c6e31bd237a5bca49c9b8aeaec";
    private static final String BANDIT_DIRECT_ANIMATION_PLAN_SHA="4c7f8b9ac3561896589ceae08e074a6e6a0f0216e9347fcfc8e05ef52d62da0a";
    private static final String KNIGHT_DIRECT_ANIMATION_PLAN_SHA="5234be766f35adcef52b6b8114d0273f0be1f5a60d3e89d78e9e90a14e12ba89";
    private static final String BATCH_DIRECT_ANIMATION_PLAN_SHA="4c31e0fa6c7c9b95bfbbccac7cc2786e6ed5e5d4d6faa59ddaf4b0a4f6528cfb";
    private static final String BATCH2_DIRECT_ANIMATION_PLAN_SHA="c744c59fa5d60e6f73285e8277c7615db8fd38201298d6466ca7d4bde47a04a7";
    private static final String BATCH3_DIRECT_ANIMATION_PLAN_SHA="857857af2a38b4452112d58c8e157f2448a5e8b58838159ef93b762676d64a33";
    private static final String SHARED_DIRECT_ANIMATION_PLAN_SHA="72b2f5bbff0e255c2fa14af35d73b22a2c81f65bb5725cc92c00e09cffd3a975";
    static final String[] SOURCE_NAMES={"sharedassets1.assets","sharedassets1.assets.resS","sharedassets2.assets","sharedassets2.assets.resS"};
    static final long[] LINUX_SIZES={56239204L,113115008L,5824160L,10434976L},WINDOWS_SIZES={50251684L,113115008L,5503192L,10434976L};
    static final class Texture {String name;byte[] templateHash,resultHash,payloadHash,data,patch;int target,size,linuxFile,windowsFile,filled;long linuxOffset,windowsOffset;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    static byte[] hash(byte[] b){return digest().digest(b);}
    static String hex(byte[] b){char[] d="0123456789abcdef".toCharArray(),r=new char[b.length*2];for(int i=0;i<b.length;i++){r[2*i]=d[(b[i]&255)>>>4];r[2*i+1]=d[b[i]&15];}return new String(r);}
    private static byte[] read(InputStream in,int limit)throws IOException{ByteArrayOutputStream out=new ByteArrayOutputStream();byte[] b=new byte[65536];int total=0;for(int n;(n=in.read(b))!=-1;){if(n==0)continue;if(n>limit-total)throw new IOException("input too large");out.write(b,0,n);total+=n;}return out.toByteArray();}
    static byte[] file(File f,int limit)throws IOException{try(InputStream in=new FileInputStream(f)){return read(in,limit);}}
    static byte[] entry(ZipFile zip,ZipEntry e,int limit)throws IOException{if(e==null||e.isDirectory()||e.getSize()<0||e.getSize()>limit)throw new IOException("invalid ZIP entry");try(InputStream in=zip.getInputStream(e)){byte[] b=read(in,limit);if(b.length!=e.getSize())throw new IOException("truncated ZIP entry");return b;}}
    static byte[] bytes(DataInputStream in,int n)throws IOException{byte[] b=new byte[n];in.readFully(b);return b;}
    static String rootFor(String name,String suffix){if(name.equals(suffix))return "";if(name.equals("Megabonk_Data/"+suffix)||name.endsWith("/Megabonk_Data/"+suffix))return name.substring(0,name.length()-suffix.length());return null;}
    static void collect(ZipFile zip,ZipEntry entry,long expected,byte[] wanted,int fileId,boolean windows,List<Texture> textures)throws IOException{
        if(entry==null||entry.isDirectory()||entry.getSize()!=expected)throw new IOException("invalid texture source");
        MessageDigest hash=digest();byte[] buf=new byte[65536];long offset=0;
        try(InputStream in=zip.getInputStream(entry)){
            for(int n;(n=in.read(buf))!=-1;){
                if(n==0)continue;if(n>expected-offset)throw new IOException("texture source too large");hash.update(buf,0,n);
                for(Texture t:textures){
                    if((windows?t.windowsFile:t.linuxFile)!=fileId)continue;
                    long start=windows?t.windowsOffset:t.linuxOffset,from=Math.max(start,offset),to=Math.min(start+t.size,offset+n);
                    if(from<to){int len=(int)(to-from);System.arraycopy(buf,(int)(from-offset),t.data,(int)(from-start),len);t.filled+=len;}
                }
                offset+=n;
            }
        }
        if(offset!=expected||!MessageDigest.isEqual(hash.digest(),wanted))throw new IOException("texture source checksum differs");
    }
    public static Map<String,byte[]> restore(File baseFile,File steamFile,File planFile)throws IOException{
        return restoreInternal(baseFile,steamFile,planFile,0);
    }
    static Map<String,byte[]> restoreAnimations(File baseFile,File steamFile,File planFile,boolean privatePlan)throws IOException{
        return restoreInternal(baseFile,steamFile,planFile,privatePlan?2:1);
    }
    private static Map<String,byte[]> restoreInternal(File baseFile,File steamFile,File planFile,int mode)throws IOException{
        if(!baseFile.isFile()||baseFile.length()>16*1024*1024||!compatibleBaseHash(hex(hash(file(baseFile,16*1024*1024)))))throw new IOException("incompatible base OBB");
        byte[] planBytes=file(planFile,1024*1024);String planHash=hex(hash(planBytes));boolean privateAnimation=planHash.equals(PRIVATE_ANIMATION_PLAN_SHA)||planHash.equals(BANDIT_DIRECT_ANIMATION_PLAN_SHA)||planHash.equals(KNIGHT_DIRECT_ANIMATION_PLAN_SHA)||planHash.equals(BATCH_DIRECT_ANIMATION_PLAN_SHA)||planHash.equals(BATCH2_DIRECT_ANIMATION_PLAN_SHA)||planHash.equals(BATCH3_DIRECT_ANIMATION_PLAN_SHA)||planHash.equals(SHARED_DIRECT_ANIMATION_PLAN_SHA),animation=planHash.equals(ANIMATION_PLAN_SHA);
        if((mode==0&&!planHash.equals(PLAN_SHA))||(mode==1&&!animation)||(mode==2&&!privateAnimation))throw new IOException("incompatible exact-data plan");
        if(!steamFile.isFile()||steamFile.length()<=0||steamFile.length()>1024L*1024*1024)throw new IOException("invalid Steam ZIP size");
        try(ZipFile base=new ZipFile(baseFile);ZipFile steam=new ZipFile(steamFile);DataInputStream plan=new DataInputStream(new ByteArrayInputStream(planBytes))){
            int expectedCount=privateAnimation?108:animation?25:56;
            if(!Arrays.equals(bytes(plan,8),new byte[]{'O','B','S','T','X','0','0',(byte)(privateAnimation?'3':animation?'2':'1')})||plan.readInt()!=expectedCount)throw new IOException("invalid texture plan");
            byte[][] linuxHashes=new byte[4][],windowsHashes=new byte[4][];for(int i=0;i<4;i++)linuxHashes[i]=bytes(plan,32);for(int i=0;i<4;i++)windowsHashes[i]=bytes(plan,32);
            List<Texture> textures=new ArrayList<>();Set<String> outputs=new HashSet<>();long total=0;
            for(int i=0;i<expectedCount;i++){
                Texture t=new Texture();t.name=new String(bytes(plan,32),"US-ASCII");t.templateHash=bytes(plan,32);t.resultHash=bytes(plan,32);t.payloadHash=bytes(plan,32);
                t.target=plan.readInt();t.size=plan.readInt();t.linuxFile=plan.readInt();t.linuxOffset=plan.readLong();t.windowsFile=plan.readInt();t.windowsOffset=plan.readLong();
                if(!t.name.matches("[0-9a-f]{32}")||!outputs.add(t.name)||t.target<0||t.size<=0||t.size>8*1024*1024||t.linuxFile<0||t.linuxFile>=4||t.windowsFile<0||t.windowsFile>=4||t.linuxOffset<0||t.windowsOffset<0||t.linuxOffset>LINUX_SIZES[t.linuxFile]-t.size||t.windowsOffset>WINDOWS_SIZES[t.windowsFile]-t.size)throw new IOException("invalid texture range");
                if(privateAnimation){int count=plan.readInt();if(count<0||count>t.size||count>plan.available()/5)throw new IOException("invalid animation patch");t.patch=bytes(plan,count*5);}
                total+=t.size;if(total>16*1024*1024)throw new IOException("texture limit exceeded");t.data=new byte[t.size];textures.add(t);
            }
            if(plan.read()!=-1)throw new IOException("trailing texture plan data");
            ZipEntry[] source=new ZipEntry[4];String commonRoot=null;Set<String> names=new HashSet<>();Enumeration<? extends ZipEntry> entries=steam.entries();
            while(entries.hasMoreElements()){
                ZipEntry e=entries.nextElement();String name=e.getName();
                if(names.size()>=20000||name.startsWith("/")||name.indexOf('\\')>=0||name.indexOf(':')>=0||!names.add(name))throw new IOException("unsafe or duplicate ZIP entry");
                for(String part:name.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
                for(int i=0;i<4;i++){String root=rootFor(name,SOURCE_NAMES[i]);if(root!=null){if(source[i]!=null||commonRoot!=null&&!commonRoot.equals(root))throw new IOException("conflicting Steam roots");source[i]=e;commonRoot=root;}}
            }
            if(source[0]==null)throw new IOException("missing Steam asset");
            boolean windows;if(source[0].getSize()==WINDOWS_SIZES[0])windows=true;else if(source[0].getSize()==LINUX_SIZES[0])windows=false;else throw new IOException("unsupported Steam asset");
            for(int i=0;i<4;i++)collect(steam,source[i],(windows?WINDOWS_SIZES:LINUX_SIZES)[i],(windows?windowsHashes:linuxHashes)[i],i,windows,textures);
            Map<String,byte[]> result=new LinkedHashMap<>();
            for(Texture t:textures){
                byte[] template=entry(base,base.getEntry("assets/bin/Data/"+t.name),8*1024*1024);
                if(!MessageDigest.isEqual(hash(template),t.templateHash)||t.filled!=t.size||!MessageDigest.isEqual(hash(t.data),t.payloadHash)||t.target>template.length-t.size)throw new IOException("texture template or payload differs");
                if(t.patch!=null){long last=-1;for(int p=0;p<t.patch.length;p+=5){long where=(t.patch[p]&255L)|((t.patch[p+1]&255L)<<8)|((t.patch[p+2]&255L)<<16)|((t.patch[p+3]&255L)<<24);byte value=t.patch[p+4];if(where<=last||where>=t.size||t.data[(int)where]==value)throw new IOException("invalid animation patch entry");t.data[(int)where]=value;last=where;}}
                for(int i=t.target;i<t.target+t.size;i++)if(template[i]!=0)throw new IOException("texture target not blank");
                System.arraycopy(t.data,0,template,t.target,t.size);
                if(!MessageDigest.isEqual(hash(template),t.resultHash))throw new IOException("texture result differs");result.put(t.name,template);
            }
            return result;
        }
    }
    public static void restoreToDirectory(File base,File steam,File plan,File output)throws IOException{
        Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be a new directory");
        Map<String,byte[]> files=restore(base,steam,plan);Path staged=Files.createTempDirectory(parent,".openbonk-texture-");
        try{for(Map.Entry<String,byte[]> item:files.entrySet()){
            Path path=staged.resolve(item.getKey());Files.write(path,item.getValue(),StandardOpenOption.CREATE_NEW,StandardOpenOption.WRITE);
            if(!MessageDigest.isEqual(hash(file(path.toFile(),8*1024*1024)),hash(item.getValue())))throw new IOException("staged texture differs");
        }Files.move(staged,dest);}
        finally{if(Files.exists(staged,LinkOption.NOFOLLOW_LINKS)){for(String name:files.keySet())Files.deleteIfExists(staged.resolve(name));Files.deleteIfExists(staged);}}
    }
    private SteamExactTextures(){}
}
