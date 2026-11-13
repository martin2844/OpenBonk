package com.openbonk.dataprep;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.*;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Builds an explicitly INCOMPLETE diagnostic OBB. It is not a playable release. */
public final class SteamPartialObbStage {
    private static final String BASE="ca36181d234fe22d0d565bd2bc199d7fab1be4e880e3276083b43c071f66716d";
    private static final String PLAN="a3d1a041b70a7eb546a6a41056356c77cc83c921dc77a272b8477a938e8589b5";
    private static final String AUDIO="28f275bd17593fbffc218edb0b66e1458b13d887456c200074a70b2927c2d9f6";
    private static final String AUDIO2="6abc02d2702824b936add3ea2a6780b5b10e2954e1df7543b8cc79b5c9e1b1bc";
    private static final String AUDIO4="2f320598479bd723d63bb920cc582b62c95f5961e59eca85d9ca20f1782824f7";
    private static final String MAP0="4a9165d29c82e714026768d5c1ada0fdf9134dc3cdea77db6b48b949747456ce",MAP1="9f3397f979f5dbe7559971941ed6815d25388e740beba9b405f89d2ad1ed08bf";
    private static final String SHARED2="a3bb71785a61a2acddb230e28b1b1f8466c076a944112e18273433a635faea37";
    private static final Set<String> FOLDERS=new HashSet<>(Arrays.asList("scenes","audio","meshes","textures","animations","converted-textures","derived-extra-textures"));
    private static final class Record {String entry,folder,name;long offset;int size;byte[] hash;}
    private static final class Stats {long size,crc;String hash;}
    private static MessageDigest digest(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static Stats copy(InputStream input,OutputStream output,long limit)throws IOException{
        MessageDigest h=digest();CRC32 crc=new CRC32();byte[] buffer=new byte[65536];long size=0;
        for(int n;(n=input.read(buffer))!=-1;){if(n==0)continue;if(n>limit-size)throw new IOException("stream exceeds limit");h.update(buffer,0,n);crc.update(buffer,0,n);if(output!=null)output.write(buffer,0,n);size+=n;}
        Stats s=new Stats();s.size=size;s.crc=crc.getValue();s.hash=hex(h.digest());return s;
    }
    private static final class CountingOutput extends FilterOutputStream {long count;CountingOutput(OutputStream o){super(o);}public void write(int b)throws IOException{out.write(b);count++;}public void write(byte[] b,int off,int len)throws IOException{out.write(b,off,len);count+=len;}}
    private static String str(DataInputStream in)throws IOException{int n=in.readUnsignedShort();if(n==0||n>256)throw new IOException("invalid plan string");return new String(bytes(in,n),StandardCharsets.US_ASCII);}
    private static byte[] readRange(Path root,Record r)throws IOException{
        Path dir=root.resolve(r.folder),path=dir.resolve(r.name);
        if(Files.isSymbolicLink(dir)||Files.isSymbolicLink(path)||!Files.isRegularFile(path,LinkOption.NOFOLLOW_LINKS))throw new IOException("invalid prepared file");
        try(RandomAccessFile in=new RandomAccessFile(path.toFile(),"r")){
            if(r.offset<0||r.offset>in.length()-r.size)throw new IOException("prepared range outside file");
            byte[] result=new byte[r.size];in.seek(r.offset);in.readFully(result);
            if(!MessageDigest.isEqual(hash(result),r.hash))throw new IOException("prepared entry checksum differs: "+r.entry);return result;
        }
    }
    public static synchronized void assembleDiagnostic(File baseFile,File preparation,File planFile,File output)throws IOException{
        assemble(baseFile,preparation,planFile,null,null,null,null,output);
    }
    public static synchronized void assembleWithSharedAudioDiagnostic(File baseFile,File preparation,File planFile,File sharedAudio,File output)throws IOException{
        if(sharedAudio==null)throw new IOException("missing reconstructed shared audio");
        assemble(baseFile,preparation,planFile,sharedAudio,null,null,null,output);
    }
    /** Local-only path: reconstructs the large shared resource from Steam before assembly. */
    public static synchronized void assembleFromSteamDiagnostic(File baseFile,File preparation,File planFile,File steamZip,File audioPlan,File output)throws IOException{
        Path result=output.toPath().toAbsolutePath(),parent=result.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(result,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        Path temporary=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp");
        try{writeReconstructedAudio(steamZip,audioPlan,temporary);assemble(baseFile,preparation,planFile,temporary.toFile(),null,null,null,output);}
        finally{Files.deleteIfExists(temporary);}
    }
    public static synchronized void assembleBothAudioFromSteamDiagnostic(File baseFile,File preparation,File planFile,File steamZip,File audioPlan,File audio2Plan,File output)throws IOException{
        Path result=output.toPath().toAbsolutePath(),parent=result.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(result,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        Path first=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp"),second=null;
        try{second=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp");writeReconstructedAudio(steamZip,audioPlan,first);writeReconstructedAudio2(steamZip,audio2Plan,second);assemble(baseFile,preparation,planFile,first.toFile(),second.toFile(),null,null,output);}
        finally{Files.deleteIfExists(first);if(second!=null)Files.deleteIfExists(second);}
    }
    public static synchronized void assembleThreeAudioFromSteamDiagnostic(File baseFile,File preparation,File planFile,File steamZip,File audioPlan,File audio2Plan,File audio4Plan,File output)throws IOException{
        Path result=output.toPath().toAbsolutePath(),parent=result.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(result,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        Path first=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp"),second=null,third=null;
        try{second=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp");third=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp");writeReconstructedAudio(steamZip,audioPlan,first);writeReconstructedAudio2(steamZip,audio2Plan,second);writeReconstructedAudio4(steamZip,audio4Plan,third);assemble(baseFile,preparation,planFile,first.toFile(),second.toFile(),third.toFile(),null,output);}
        finally{Files.deleteIfExists(first);if(second!=null)Files.deleteIfExists(second);if(third!=null)Files.deleteIfExists(third);}
    }
    public static synchronized void assembleMapAndAudioDiagnostic(File baseFile,File preparation,File planFile,File steamZip,File audioPlan,File audio2Plan,File audio4Plan,File mapPlan,File output)throws IOException{
        Path result=output.toPath().toAbsolutePath(),parent=result.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(result,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        Map<String,byte[]> map=SteamShared4MapAsset.restore(baseFile,steamZip,mapPlan);
        Path first=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp"),second=null,third=null;
        try{second=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp");third=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp");writeReconstructedAudio(steamZip,audioPlan,first);writeReconstructedAudio2(steamZip,audio2Plan,second);writeReconstructedAudio4(steamZip,audio4Plan,third);assemble(baseFile,preparation,planFile,first.toFile(),second.toFile(),third.toFile(),map,output);}
        finally{Files.deleteIfExists(first);if(second!=null)Files.deleteIfExists(second);if(third!=null)Files.deleteIfExists(third);}
    }
    public static synchronized void assembleShared2AndMapDiagnostic(File baseFile,File preparation,File planFile,File steamZip,File audioPlan,File audio2Plan,File audio4Plan,File mapPlan,File mesh2Plan,File texture2Plan,File animation2Plan,File final2Plan,File output)throws IOException{
        Path result=output.toPath().toAbsolutePath(),parent=result.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(result,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
        Map<String,byte[]> map=SteamShared4MapAsset.restore(baseFile,steamZip,mapPlan);byte[] shared2=SteamShared2Complete.restore(baseFile,steamZip,mesh2Plan,texture2Plan,animation2Plan,final2Plan);
        for(int i=0;i<22;i++){int start=i*1048576,end=Math.min(shared2.length,start+1048576);if(start>=end)throw new IOException("invalid shared2 split");map.put("sharedassets2.assets.split"+i,Arrays.copyOfRange(shared2,start,end));}shared2=null;
        Path first=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp"),second=null,third=null;
        try{second=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp");third=Files.createTempFile(parent,".openbonk-shared-audio-",".tmp");writeReconstructedAudio(steamZip,audioPlan,first);writeReconstructedAudio2(steamZip,audio2Plan,second);writeReconstructedAudio4(steamZip,audio4Plan,third);assemble(baseFile,preparation,planFile,first.toFile(),second.toFile(),third.toFile(),map,output);}
        finally{Files.deleteIfExists(first);if(second!=null)Files.deleteIfExists(second);if(third!=null)Files.deleteIfExists(third);}
    }
    private static void writeReconstructedAudio(File steamZip,File audioPlan,Path output)throws IOException{
        SteamSharedAudioResource.restoreToFile(steamZip,audioPlan,output.toFile());
    }
    private static void writeReconstructedAudio2(File steamZip,File audioPlan,Path output)throws IOException{
        byte[] audio=SteamShared2AudioResource.restore(steamZip,audioPlan);Files.write(output,audio,StandardOpenOption.TRUNCATE_EXISTING,StandardOpenOption.WRITE);
    }
    private static void writeReconstructedAudio4(File steamZip,File audioPlan,Path output)throws IOException{
        byte[] audio=SteamShared4AudioResource.restore(steamZip,audioPlan);Files.write(output,audio,StandardOpenOption.TRUNCATE_EXISTING,StandardOpenOption.WRITE);
    }
    private static Stats verifyAudio(File audio,long expectedSize,String expectedHash)throws IOException{
        if(audio==null)return null;Path path=audio.toPath();if(Files.isSymbolicLink(path)||!Files.isRegularFile(path,LinkOption.NOFOLLOW_LINKS)||Files.size(path)!=expectedSize)throw new IOException("invalid reconstructed shared audio");
        Stats stats;try(InputStream in=Files.newInputStream(path)){stats=copy(in,null,64*1024*1024);}if(!stats.hash.equals(expectedHash))throw new IOException("shared audio checksum differs");return stats;
    }
    private static void assemble(File baseFile,File preparation,File planFile,File sharedAudio,File sharedAudio2,File sharedAudio4,Map<String,byte[]> map,File output)throws IOException{
        if(!baseFile.isFile())throw new IOException("incompatible base");
        String baseHash=hex(hash(file(baseFile,16*1024*1024)));
        boolean noProbe=baseHash.equals("f0bb1f860804e25687085bca64a4554fc292bd03129eb0bb2303a277da52e003");
        if(!noProbe&&!baseHash.equals(BASE))throw new IOException("incompatible base");
        int expectedEntries=noProbe?943:946;
        byte[] planBytes=file(planFile,1024*1024);if(!hex(hash(planBytes)).equals(PLAN))throw new IOException("incompatible partial OBB plan");
        Stats audioStats=verifyAudio(sharedAudio,50848708L,AUDIO),audio2Stats=verifyAudio(sharedAudio2,5365152L,AUDIO2),audio4Stats=verifyAudio(sharedAudio4,1780640L,AUDIO4);
        if(map!=null){
            if((map.size()!=2&&map.size()!=24)||!map.containsKey("sharedassets4.assets.split0")||!map.containsKey("sharedassets4.assets.split1")||!hex(hash(map.get("sharedassets4.assets.split0"))).equals(MAP0)||!hex(hash(map.get("sharedassets4.assets.split1"))).equals(MAP1))throw new IOException("map splits differ");
            if(map.size()==24){MessageDigest h=digest();int count=0;for(int i=0;i<22;i++){byte[] piece=map.get("sharedassets2.assets.split"+i);if(piece==null||piece.length!=(i==21?258200:1048576))throw new IOException("shared2 split differs");h.update(piece);count+=piece.length;}if(count!=22278296||!hex(h.digest()).equals(SHARED2))throw new IOException("shared2 asset differs");}
        }
        Path root=preparation.toPath().toAbsolutePath(),dest=output.toPath().toAbsolutePath(),parent=dest.getParent();
        if(Files.isSymbolicLink(root)||!Files.isDirectory(root,LinkOption.NOFOLLOW_LINKS)||parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("invalid input or output");
        Map<String,Record> records=new LinkedHashMap<>();Map<Path,Long> lengths=new HashMap<>();
        try(DataInputStream in=new DataInputStream(new ByteArrayInputStream(planBytes))){
            if(!Arrays.equals(bytes(in,8),new byte[]{'O','B','S','P','B','0','0','1'})||in.readInt()!=344)throw new IOException("bad partial OBB plan");
            for(int i=0;i<344;i++){
                Record r=new Record();r.entry=str(in);r.folder=str(in);r.name=str(in);r.offset=in.readLong();r.size=in.readInt();r.hash=bytes(in,32);
                if(!FOLDERS.contains(r.folder)||!r.name.matches("[a-zA-Z0-9_.-]+")||!r.entry.matches("assets/bin/Data/[a-zA-Z0-9_.-]+")||r.offset<0||r.offset>16*1024*1024||r.size<=0||r.size>8*1024*1024||records.put(r.entry,r)!=null)throw new IOException("invalid partial OBB record");
                Path file=root.resolve(r.folder).resolve(r.name);lengths.put(file,Math.max(lengths.getOrDefault(file,0L),r.offset+r.size));
            }
            if(in.read()!=-1)throw new IOException("trailing partial plan data");
        }
        for(Map.Entry<Path,Long> e:lengths.entrySet())if(!Files.isRegularFile(e.getKey(),LinkOption.NOFOLLOW_LINKS)||Files.size(e.getKey())!=e.getValue())throw new IOException("prepared file length differs");
        Path temporary=Files.createTempFile(parent,".openbonk-partial-obb-",".tmp");
        try{
            Set<String> used=new HashSet<>(),seen=new HashSet<>();boolean usedAudio=false,usedAudio2=false,usedAudio4=false;int usedMap=0;
            try(ZipFile source=new ZipFile(baseFile);CountingOutput counted=new CountingOutput(new BufferedOutputStream(Files.newOutputStream(temporary)));ZipOutputStream out=new SteamStableZipOutputStream(counted)){
                out.setComment("OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE");
                Enumeration<? extends ZipEntry> entries=source.entries();
                while(entries.hasMoreElements()){
                    ZipEntry old=entries.nextElement();String name=old.getName();if(!seen.add(name)||seen.size()>20000||old.isDirectory()||old.getSize()<0||old.getSize()>64*1024*1024)throw new IOException("unexpected base entry");
                    Record r=records.get(name);boolean firstAudio=audioStats!=null&&name.equals("assets/bin/Data/sharedassets1.resource"),secondAudio=audio2Stats!=null&&name.equals("assets/bin/Data/sharedassets2.resource"),thirdAudio=audio4Stats!=null&&name.equals("assets/bin/Data/sharedassets4.resource"),audioEntry=firstAudio||secondAudio||thirdAudio;Stats current=firstAudio?audioStats:secondAudio?audio2Stats:audio4Stats;File currentFile=firstAudio?sharedAudio:secondAudio?sharedAudio2:sharedAudio4;String currentHash=firstAudio?AUDIO:secondAudio?AUDIO2:AUDIO4;String mapName=name.startsWith("assets/bin/Data/")?name.substring("assets/bin/Data/".length()):"";byte[] data=r!=null?readRange(root,r):map!=null?map.get(mapName):null;if(map!=null&&map.containsKey(mapName))usedMap++;
                    if(firstAudio)usedAudio=true;
                    if(secondAudio)usedAudio2=true;
                    if(thirdAudio)usedAudio4=true;
                    if(r!=null)used.add(name);
                    ZipEntry next=new ZipEntry(name);next.setTime(SteamZipTime.fixed());next.setMethod(old.getMethod());
                    if(old.getMethod()==ZipEntry.STORED){long size,crc;if(audioEntry){size=current.size;crc=current.crc;}else if(data!=null){CRC32 computed=new CRC32();computed.update(data);size=data.length;crc=computed.getValue();}else{size=old.getSize();crc=old.getCrc();}next.setSize(size);next.setCompressedSize(size);next.setCrc(crc);
                        int nameBytes=name.getBytes(StandardCharsets.UTF_8).length;int pad=(int)((4-((counted.count+30+nameBytes+4)%4))%4);byte[] extra=new byte[4+pad];extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)pad;next.setExtra(extra);
                    }else if(old.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unsupported ZIP method");
                    out.putNextEntry(next);
                    if(data!=null)out.write(data);
                    else if(audioEntry){try(InputStream in=Files.newInputStream(currentFile.toPath())){Stats actual=copy(in,out,64*1024*1024);if(actual.size!=current.size||!actual.hash.equals(currentHash))throw new IOException("shared audio changed during assembly");}}
                    else{try(InputStream in=source.getInputStream(old)){Stats actual=copy(in,out,64*1024*1024);if(actual.size!=old.getSize()||actual.crc!=old.getCrc())throw new IOException("base entry differs");}}
                    out.closeEntry();
                }
            }
            if(used.size()!=344||seen.size()!=expectedEntries||(audioStats!=null&&!usedAudio)||(audio2Stats!=null&&!usedAudio2)||(audio4Stats!=null&&!usedAudio4)||(map!=null&&usedMap!=map.size())||!hex(hash(file(baseFile,16*1024*1024))).equals(baseHash))throw new IOException("incomplete or changed base");
            try(ZipFile check=new ZipFile(temporary.toFile())){
                if(check.size()!=expectedEntries||!"OPENBONK INCOMPLETE DIAGNOSTIC - NOT PLAYABLE".equals(check.getComment()))throw new IOException("partial archive verification failed");
                for(Record r:records.values())if(!MessageDigest.isEqual(hash(entry(check,check.getEntry(r.entry),8*1024*1024)),r.hash))throw new IOException("partial archive entry differs");
                if(audioStats!=null){ZipEntry e=check.getEntry("assets/bin/Data/sharedassets1.resource");if(e==null)throw new IOException("missing audio");try(InputStream in=check.getInputStream(e)){Stats actual=copy(in,null,64*1024*1024);if(actual.size!=audioStats.size||!actual.hash.equals(AUDIO))throw new IOException("partial archive audio differs");}}
                if(audio2Stats!=null){ZipEntry e=check.getEntry("assets/bin/Data/sharedassets2.resource");if(e==null)throw new IOException("missing second audio");try(InputStream in=check.getInputStream(e)){Stats actual=copy(in,null,64*1024*1024);if(actual.size!=audio2Stats.size||!actual.hash.equals(AUDIO2))throw new IOException("partial archive second audio differs");}}
                if(audio4Stats!=null){ZipEntry e=check.getEntry("assets/bin/Data/sharedassets4.resource");if(e==null)throw new IOException("missing third audio");try(InputStream in=check.getInputStream(e)){Stats actual=copy(in,null,64*1024*1024);if(actual.size!=audio4Stats.size||!actual.hash.equals(AUDIO4))throw new IOException("partial archive third audio differs");}}
                if(map!=null)for(Map.Entry<String,byte[]> e:map.entrySet())if(!MessageDigest.isEqual(hash(entry(check,check.getEntry("assets/bin/Data/"+e.getKey()),1048576)),hash(e.getValue())))throw new IOException("partial archive map differs");
            }
            Files.move(temporary,dest);
        }finally{Files.deleteIfExists(temporary);}
    }
    private SteamPartialObbStage(){}
}
