package com.openbonk.dataprep;

import java.io.*;
import java.nio.file.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

/** Local-only experimental OBB stage; plans and templates contain game data. */
public final class SteamLocalizationObb {
    private static final int MAX_BUNDLE=32*1024*1024, MAX_PLAN=4*1024*1024;
    private static final long MAX_OBB=1024L*1024*1024;
    private static final String NAME="localization-(?:assets-shared_assets_all|string-tables-[a-z0-9()_-]+_assets_all)\\.bundle";
    private SteamLocalizationObb() {}
    private static MessageDigest sha(){try{return MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException e){throw new AssertionError(e);}}
    private static String hex(byte[] bytes){StringBuilder s=new StringBuilder();for(byte b:bytes)s.append(String.format("%02x",b&255));return s.toString();}
    private static byte[] read(InputStream in,long size,int limit) throws IOException {
        if(size<0 || size>limit)throw new IOException("input size invalid");byte[] data=new byte[(int)size];
        try(DataInputStream d=new DataInputStream(in)){d.readFully(data);if(d.read()!=-1)throw new IOException("input exceeds expected size");}return data;
    }
    private static String fileHash(File f) throws IOException {
        if(!f.isFile()||f.length()<=0||f.length()>MAX_OBB)throw new IOException("invalid file size");
        MessageDigest d=sha();try(InputStream in=new FileInputStream(f)){byte[] b=new byte[65536];for(int n;(n=in.read(b))!=-1;)if(n>0)d.update(b,0,n);}return hex(d.digest());
    }
    private static void safeName(String s)throws IOException{
        if(s.isEmpty()||s.startsWith("/")||s.indexOf('\\')>=0||s.indexOf(':')>=0)throw new IOException("unsafe entry");
        for(String p:s.split("/",-1))if(p.isEmpty()||p.equals(".")||p.equals(".."))throw new IOException("unsafe entry");
    }
    private static String rootFor(String entry,String suffix){
        if(entry.equals(suffix))return "";
        if(entry.equals("Megabonk_Data/"+suffix)||entry.endsWith("/Megabonk_Data/"+suffix))return entry.substring(0,entry.length()-suffix.length());return null;
    }
    private static final class Row {String source;byte[] plan,planHash;}
    public static void restore(File template,File steam,File planFile,String trustedHash,File output)throws IOException {
        Path target=output.toPath().toAbsolutePath(),parent=target.getParent();
        if(parent==null||!Files.isDirectory(parent)||Files.exists(target,LinkOption.NOFOLLOW_LINKS)||!steam.isFile()||steam.length()<=0||steam.length()>MAX_OBB||!planFile.isFile())throw new IOException("invalid files");
        byte[] data=read(new FileInputStream(planFile),planFile.length(),MAX_PLAN);
        if(trustedHash==null||!trustedHash.matches("[0-9a-f]{64}")||!hex(sha().digest(data)).equals(trustedHash))throw new IOException("plan checksum differs");
        Map<String,Row> rows=new HashMap<>(); String templateHash;
        try{
            JSONObject plan=new JSONObject(new String(data,"UTF-8"));
            if(plan.getInt("schema")!=1||!plan.getString("kind").equals("experimental-localization-cab-obb"))throw new IOException("unsupported plan");
            String platform=plan.getString("platform"),folder=platform.equals("linux")?"StandaloneLinux64":platform.equals("windows")?"StandaloneWindows64":null;
            if(folder==null)throw new IOException("invalid platform");templateHash=plan.getString("template_sha256");
            if(!templateHash.matches("[0-9a-f]{64}"))throw new IOException("invalid template checksum");
            JSONArray records=plan.getJSONArray("records");if(records.length()!=18)throw new IOException("invalid bundle count");
            for(int i=0;i<records.length();i++){
                JSONObject r=records.getJSONObject(i);String entry=r.getString("entry"),source=r.getString("source");
                if(!entry.matches("assets/aa/Android/"+NAME)||!source.equals("StreamingAssets/aa/"+folder+"/"+entry.substring("assets/aa/Android/".length())))throw new IOException("invalid bundle mapping");
                Row row=new Row();row.source=source;row.plan=Base64.getDecoder().decode(r.getString("plan"));row.planHash=sha().digest(row.plan);
                if(row.plan.length>MAX_PLAN||!hex(row.planHash).equals(r.getString("plan_sha256"))||rows.put(entry,row)!=null)throw new IOException("invalid bundle plan");
            }
        }catch(JSONException|IllegalArgumentException e){throw new IOException("invalid localization plan",e);}
        if(!fileHash(template).equals(templateHash))throw new IOException("template checksum differs");
        Path temp=Files.createTempFile(parent,".openbonk-locale-",".obb");boolean committed=false;
        try(ZipFile input=new ZipFile(template);ZipFile sources=new ZipFile(steam)){
            Map<String,ZipEntry> found=new HashMap<>();Set<String> names=new HashSet<>();String root=null;
            Enumeration<? extends ZipEntry> it=sources.entries();
            while(it.hasMoreElements()){
                ZipEntry e=it.nextElement();if(e.isDirectory())continue;String name=e.getName();safeName(name);
                if(names.size()>=20000||!names.add(name))throw new IOException("duplicate Steam ZIP entry");
                for(Row row:rows.values()){
                    String candidate=rootFor(name,row.source);if(candidate==null)continue;
                    if(root!=null&&!root.equals(candidate))throw new IOException("conflicting Steam roots");root=candidate;
                    if(found.put(row.source,e)!=null)throw new IOException("duplicate Steam source");
                }
            }
            if(found.size()!=rows.size())throw new IOException("missing Steam localization bundle");
            names.clear();Set<String> restored=new HashSet<>();long total=0;
            try(ZipOutputStream out=new ZipOutputStream(new BufferedOutputStream(Files.newOutputStream(temp)))){
                it=input.entries();while(it.hasMoreElements()){
                    ZipEntry e=it.nextElement();if(e.isDirectory())continue;String name=e.getName();safeName(name);
                    if(names.size()>=20000||!names.add(name)||e.getSize()<0||e.getSize()>MAX_OBB-total)throw new IOException("invalid template entry");total+=e.getSize();
                    Row row=rows.get(name);byte[] patched=null;long crc=e.getCrc(),size=e.getSize();
                    if(row!=null){
                        byte[] old=read(input.getInputStream(e),size,MAX_BUNDLE);ZipEntry src=found.get(row.source);
                        byte[] original=read(sources.getInputStream(src),src.getSize(),MAX_BUNDLE);
                        patched=SteamLocalizationCab.restore(old,original,row.plan,row.planHash);CRC32 c=new CRC32();c.update(patched);crc=c.getValue();size=patched.length;restored.add(name);
                    }
                    if(crc<0)throw new IOException("missing CRC");ZipEntry dest=new ZipEntry(name);dest.setTime(0);dest.setMethod(ZipEntry.STORED);dest.setSize(size);dest.setCompressedSize(size);dest.setCrc(crc);out.putNextEntry(dest);
                    if(patched!=null)out.write(patched);else try(InputStream in=input.getInputStream(e)){byte[] b=new byte[65536];long left=size;while(left>0){int n=in.read(b,0,(int)Math.min(left,b.length));if(n<0)throw new EOFException();if(n>0){out.write(b,0,n);left-=n;}}if(in.read()!=-1)throw new IOException("oversized entry");}
                    out.closeEntry();
                }
            }
            if(!restored.equals(rows.keySet()))throw new IOException("missing template bundle");
            Files.move(temp,target);committed=true;
        }finally{if(!committed)Files.deleteIfExists(temp);}
    }
}
