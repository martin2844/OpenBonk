package com.megabonk.launcher;
import java.io.*;
import java.nio.file.*;
import java.util.*;
import java.util.zip.*;
import org.json.*;

public class PassiveCheck {
 static byte[] bytes(InputStream in) throws IOException { ByteArrayOutputStream b=new ByteArrayOutputStream(); byte[] buf=new byte[8192]; int n; while ((n=in.read(buf))!=-1) b.write(buf,0,n); return b.toByteArray(); }
 static void rewrite(Path source, Path dest, Object passive, boolean remove) throws Exception {
  try (ZipFile in=new ZipFile(source.toFile()); ZipOutputStream out=new ZipOutputStream(Files.newOutputStream(dest))) {
   Enumeration<? extends ZipEntry> entries=in.entries();
   while(entries.hasMoreElements()) { ZipEntry e=entries.nextElement(); if(e.isDirectory()) continue; byte[] data; try(InputStream s=in.getInputStream(e)){data=bytes(s);}
    if(e.getName().equals("manifest.json")) { JSONObject m=new JSONObject(new String(data,java.nio.charset.StandardCharsets.UTF_8)); if(remove)m.remove("passive"); else m.put("passive",passive); data=m.toString().getBytes(java.nio.charset.StandardCharsets.UTF_8); }
    out.putNextEntry(new ZipEntry(e.getName())); out.write(data); out.closeEntry();
   }
  }
 }
 static JSONObject passive(String stat, double amount) {return new JSONObject().put("name","Passive").put("description","Per-level bonus").put("perLevel",new JSONArray().put(new JSONObject().put("stat",stat).put("amount",amount)));}
 static void install(Path zip, Path root, RecoveryCheck.Prefs prefs) throws Exception { try(ZipFile z=new ZipFile(zip.toFile())) { LauncherContentImporter.install(z,root.toFile(),prefs); } }
 static Map<String,String> hashes(Path root) throws Exception {
  Map<String,String> result=new TreeMap<>();
  try(java.util.stream.Stream<Path> paths=Files.walk(root)) {
   for(Path file:(Iterable<Path>)paths.filter(Files::isRegularFile)::iterator) {
    byte[] hash=java.security.MessageDigest.getInstance("SHA-256").digest(Files.readAllBytes(file));
    result.put(root.relativize(file).toString(),Base64.getEncoder().encodeToString(hash));
   }
  }
  return result;
 }
 public static void main(String[] args) throws Exception {
  Path source=Paths.get(args[0]), temp=Files.createTempDirectory("passive-validation-"); Path root=Files.createDirectory(temp.resolve("Mods")), zip=temp.resolve("mod.zip"); RecoveryCheck.Prefs prefs=new RecoveryCheck.Prefs();
  install(source,root,prefs); prefs.state.put("pikachu",true); Map<String,String> before=hashes(root.resolve("pikachu"));
  List<Object> invalid=new ArrayList<>(); invalid.add("bad"); invalid.add(new JSONObject()); invalid.add(passive("Typo",0.1)); invalid.add(passive("MaxHealth",1.01)); invalid.add(passive("MaxHealth",-1.01)); invalid.add(passive("0",0.1));
  JSONObject duplicate=passive("MaxHealth",0.1); duplicate.getJSONArray("perLevel").put(new JSONObject().put("stat","MaxHealth").put("amount",0.2)); invalid.add(duplicate);
  JSONObject empty=passive("MaxHealth",0.1); empty.put("perLevel",new JSONArray()); invalid.add(empty);
  JSONObject tooMany=passive("MaxHealth",0.1); JSONArray entries=new JSONArray(); for(int i=0;i<17;i++) entries.put(new JSONObject().put("stat","MaxHealth").put("amount",0.1)); tooMany.put("perLevel",entries); invalid.add(tooMany);
  for(Object value:invalid) { rewrite(source,zip,value,false); boolean rejected=false; try { install(zip,root,prefs); } catch(IOException expected){rejected=true;} if(!rejected || !before.equals(hashes(root.resolve("pikachu"))) || !prefs.getBoolean("pikachu",false)) throw new AssertionError("invalid update accepted or existing state changed: "+value); }
  rewrite(source,zip,passive("MaxHealth",1),false); install(zip,root,prefs);
  rewrite(source,zip,JSONObject.NULL,false); install(zip,root,prefs);
  rewrite(source,zip,null,true); install(zip,root,prefs);
  System.out.println("PASS: "+invalid.size()+" invalid updates preserve all existing files and enabled state; valid, null and absent passives import");
 }
}
