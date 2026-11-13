package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.util.Enumeration;
import java.util.HashSet;
import java.util.Set;
import java.util.zip.CRC32;
import java.util.zip.ZipEntry;
import java.util.zip.ZipFile;
import java.util.zip.ZipOutputStream;

/** Experimental video stage only; its output is not accepted by the current launcher. */
public final class SteamVideoObbStage {
    private static final String SOURCE_HASH = "a72a758d8e47b7103ffda6870b5ded543d645d0ef2b9708939af90114b031991";
    private static final String OUTPUT_HASH = "e845f267cfa96d70299fbb3c9516c5906db36ba94f85515b4bc731929361efe3";
    private static final String RESOURCE = "assets/bin/Data/sharedassets1.resource";
    private static final String SPLIT = "assets/bin/Data/sharedassets1.assets.split113";
    private static final String SOURCE_SPLIT = "90d72c1dad43ac6c32dce829f470b9354b644f670c20f8e4d83082435c427aea";
    private static final String TARGET_SPLIT = "a5993f3a9c84321393f197e64496109855b29dfac02d4dcd6d5acf08b171d62f";
    private static final String TARGET_RESOURCE = "28f275bd17593fbffc218edb0b66e1458b13d887456c200074a70b2927c2d9f6";
    private static final int PATCH_OFFSET = 685796;
    private static final byte[] OLD_OFFSET = {0x60, 0x26, (byte)0xb0, 0x01, 0, 0, 0, 0};
    private static final byte[] NEW_OFFSET = {(byte)0xa0, (byte)0xc1, 0x07, 0x03, 0, 0, 0, 0};

    private static MessageDigest digest() {
        try { return MessageDigest.getInstance("SHA-256"); }
        catch (NoSuchAlgorithmException e) { throw new AssertionError(e); }
    }
    private static String hex(byte[] bytes) {
        char[] d="0123456789abcdef".toCharArray(), out=new char[bytes.length*2];
        for (int i=0;i<bytes.length;i++) { out[2*i]=d[(bytes[i]&255)>>>4];out[2*i+1]=d[bytes[i]&15]; }
        return new String(out);
    }
    private static String hashFile(File file) throws IOException {
        MessageDigest hash=digest(); byte[] buf=new byte[65536];
        try (InputStream in=new FileInputStream(file)) { for(int n;(n=in.read(buf))!=-1;) if(n>0)hash.update(buf,0,n); }
        return hex(hash.digest());
    }
    private static byte[] readSmall(ZipFile zip, ZipEntry entry, int limit) throws IOException {
        if(entry==null || entry.getSize()<0 || entry.getSize()>limit)throw new IOException("invalid split size");
        ByteArrayOutputStream out=new ByteArrayOutputStream((int)entry.getSize());
        copy(zip,entry,out); return out.toByteArray();
    }
    private static void copy(ZipFile zip, ZipEntry entry, OutputStream out) throws IOException {
        CRC32 crc=new CRC32(); long total=0; byte[] buf=new byte[65536];
        try(InputStream in=zip.getInputStream(entry)) {
            for(int n;(n=in.read(buf))!=-1;) { if(n==0)continue; if(n>entry.getSize()-total)throw new IOException("entry exceeds size"); out.write(buf,0,n);crc.update(buf,0,n);total+=n; }
        }
        if(total!=entry.getSize() || crc.getValue()!=entry.getCrc())throw new IOException("entry checksum differs: "+entry.getName());
    }
    private static String entryHash(ZipFile zip, ZipEntry entry) throws IOException {
        MessageDigest hash=digest();
        copy(zip,entry,new OutputStream() { public void write(int b){hash.update((byte)b);} public void write(byte[] b,int o,int n){hash.update(b,o,n);} });
        return hex(hash.digest());
    }
    private static ZipEntry newEntry(ZipEntry source) throws IOException {
        ZipEntry result=new ZipEntry(source.getName());result.setMethod(source.getMethod());
        if(source.getTime()>=0)result.setTime(source.getTime());
        // The source's NTFS timestamp extra is not needed on Android and can
        // cause the ZIP writer to inject another variable-length timestamp.
        if(source.getComment()!=null)result.setComment(source.getComment());
        if(source.getMethod()==ZipEntry.STORED) { result.setSize(source.getSize());result.setCompressedSize(source.getSize());result.setCrc(source.getCrc()); }
        else if(source.getMethod()!=ZipEntry.DEFLATED)throw new IOException("unsupported compression");
        return result;
    }
    private static final class CountingOutput extends FilterOutputStream {
        long count;
        CountingOutput(OutputStream out){super(out);}
        public void write(int b)throws IOException{out.write(b);count++;}
        public void write(byte[] b,int off,int len)throws IOException{out.write(b,off,len);count+=len;}
    }
    public static void restore(File source, File steamZip, File output) throws IOException {
        if(!source.isFile() || !hashFile(source).equals(SOURCE_HASH))throw new IOException("incompatible input OBB");
        byte[] clip=SteamBufferingClip.extract(steamZip);
        Path result=output.toPath().toAbsolutePath(); Path parent=result.getParent();
        if(parent==null || !Files.isDirectory(parent) || Files.exists(result))throw new IOException("output must be new");
        Path temporary=Files.createTempFile(parent,".openbonk-video-",".partial");
        try {
            try(ZipFile zip=new ZipFile(source); CountingOutput counted=new CountingOutput(new BufferedOutputStream(Files.newOutputStream(temporary))); ZipOutputStream out=new ZipOutputStream(counted)) {
                // This pinned comment also allows the experimental APK's DEX
                // hash string to be changed without altering its sort order.
                out.setComment("0000007a");
                ZipEntry asset=zip.getEntry(SPLIT), resource=zip.getEntry(RESOURCE);
                if(asset==null || resource==null || resource.getMethod()!=ZipEntry.STORED || resource.getSize()!=50839968L)throw new IOException("unexpected OBB layout");
                byte[] split=readSmall(zip,asset,1048576); MessageDigest old=digest();old.update(split);
                if(!hex(old.digest()).equals(SOURCE_SPLIT))throw new IOException("unexpected split");
                for(int i=0;i<OLD_OFFSET.length;i++)if(split[PATCH_OFFSET+i]!=OLD_OFFSET[i])throw new IOException("unexpected video offset");
                System.arraycopy(NEW_OFFSET,0,split,PATCH_OFFSET,NEW_OFFSET.length);
                MessageDigest updated=digest();updated.update(split);
                if(!hex(updated.digest()).equals(TARGET_SPLIT))throw new IOException("bad modified split");
                CRC32 crc=new CRC32();
                copy(zip,resource,new OutputStream() { public void write(int b){crc.update(b);} public void write(byte[] b,int o,int n){crc.update(b,o,n);} });
                crc.update(clip);
                Set<String> seen=new HashSet<>();Enumeration<? extends ZipEntry> entries=zip.entries();
                while(entries.hasMoreElements()) {
                    ZipEntry e=entries.nextElement();if(!seen.add(e.getName()) || seen.size()>20000)throw new IOException("duplicate OBB entry");
                    ZipEntry entry=newEntry(e);
                    if(e.getName().equals(RESOURCE)) { entry.setSize(e.getSize()+clip.length);entry.setCompressedSize(e.getSize()+clip.length);entry.setCrc(crc.getValue()); }
                    if(e.getMethod()==ZipEntry.STORED) {
                        int nameBytes=e.getName().getBytes(StandardCharsets.UTF_8).length;
                        int padding=(int)((4-((counted.count+30+nameBytes+4)%4))%4);
                        byte[] extra=new byte[4+padding]; extra[0]=0x35;extra[1]=(byte)0xd9;extra[2]=(byte)padding;
                        entry.setExtra(extra);
                    }
                    out.putNextEntry(entry);
                    if(e.getName().equals(SPLIT))out.write(split);
                    else { copy(zip,e,out); if(e.getName().equals(RESOURCE))out.write(clip); }
                    out.closeEntry();
                }
            }
            if(!hashFile(source).equals(SOURCE_HASH))throw new IOException("input OBB changed during processing");
            try(ZipFile check=new ZipFile(temporary.toFile())) {
                if(!entryHash(check,check.getEntry(SPLIT)).equals(TARGET_SPLIT) || !entryHash(check,check.getEntry(RESOURCE)).equals(TARGET_RESOURCE))
                    throw new IOException("restored entries differ");
            }
            if(!hashFile(temporary.toFile()).equals(OUTPUT_HASH))throw new IOException("ZIP writer produced an incompatible OBB");
            // The destination lives in app-private storage and callers serialize
            // preparation; without REPLACE_EXISTING, an existing output fails.
            Files.move(temporary,result);
        } finally { Files.deleteIfExists(temporary); }
    }
    private SteamVideoObbStage() {}
}
