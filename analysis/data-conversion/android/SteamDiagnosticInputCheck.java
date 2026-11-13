package com.openbonk.dataprep;
import java.io.*;
import java.security.*;
import java.util.*;
import java.util.zip.*;
import static com.openbonk.dataprep.SteamExactTextures.*;

/** Early validation of the pinned files used by the private diagnostic pipeline. */
public final class SteamDiagnosticInputCheck {
 public static final class Failure extends IOException {
  public final String file; public final boolean missing;
  Failure(String file,boolean missing){super((missing?"Missing: ":"Unsupported or damaged: ")+file);this.file=file;this.missing=missing;}
 }
 private static final class Item {final String name;final long[] size;final String[] sha;Item(String n,long l,String lh,long w,String wh){name=n;size=new long[]{l,w};sha=new String[]{lh,wh};}}
 private static final Item[] ITEMS={
  new Item("level1",2101944L,"fe6e50ab3323b523dc53a4151fcc12dcf4d5fd0076d14b5150ba16dd64371551",2101944L,"9fa2811a0dab3be720f1c626a12b6d8062e7219a60f513e10de9519f1339c93c"),
  new Item("level1.resS",116112L,"bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6",116112L,"bb81d103c119b2c62abd68766cdb58de392922911b877a6b737d8e46090313e6"),
  new Item("level2",10417064L,"1bb0eafb262237c2e1cee771bde78f3e7727ab574fc01f5a60977d0098b406f8",10417064L,"853ee36652352d3245f335d498a2c70ff9fe3e0380e14991a83fd65c847d48d6"),
  new Item("level2.resS",74576L,"544f0a484be350056444c7776f157aab9da9f467df611ebdbf9a7f1da0181b51",74576L,"544f0a484be350056444c7776f157aab9da9f467df611ebdbf9a7f1da0181b51"),
  new Item("level4",2701232L,"df80b6b32996444082b7ddce3e0cbe3573dbe25d90184f28a10bda9413e88600",2701248L,"d840ce413aa05ceb141ba5234ced8394aca0d82e0f76063eacd08a8b59bfc4f2"),
  new Item("resources.assets",4092292L,"a71923a420e792cd46c107eb8e607443ceb9357ef988e4efb73caafa3d35a366",527348L,"142439d97de6546370e03b3c09831c4401383e056816aee19966465ef861e0db"),
  new Item("resources.assets.resS",2621536L,"58b5f2965eb4a325f77fad08df75c036334e5625e58833fd02cf42fa8ee16d50",2621536L,"58b5f2965eb4a325f77fad08df75c036334e5625e58833fd02cf42fa8ee16d50"),
  new Item("sharedassets0.assets",131481128L,"01ff22507aa0ef446543b038429039deb92f817b89668bed858bafe99235c19a",131699616L,"772b0a4fc30592c6da17667e532efd0674f76eb7eb5ad24fc5673229b8fe525d"),
  new Item("sharedassets0.assets.resS",888864L,"374d7bd078c7428c9a1815d3ef3d942a26353c7e3af63258aa55dbb9837cee7b",888864L,"374d7bd078c7428c9a1815d3ef3d942a26353c7e3af63258aa55dbb9837cee7b"),
  new Item("sharedassets1.assets",56239204L,"d9c955e43af9151f4d9d4e3ae9328ff2582bde7a1c9aa4580ddb2ba95ad66b35",50251684L,"64c5156116e20fe645acc133bfae7ab1fdd3a7bbc99f915559efc0640be0349a"),
  new Item("sharedassets1.assets.resS",113115008L,"43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea",113115008L,"43c08c5f3228f725f01567bacfabfbb6d2ff50afd7a0536c5a9e1953f6e86fea"),
  new Item("sharedassets1.resource",60845700L,"28f6806a1ca59ab2b7aa77b6d7ba1492b0cbd76d6518caa6bad70e4762c45d5c",60845700L,"29c8f2252d881f0ad039c58a30e8773a45b65ed3a3cbee9f31789350dc12d50d"),
  new Item("sharedassets2.assets",5824160L,"e4c4298e57fe5aaa76836132e6821b297199c60b2106fa12a0db4ef17a67f5da",5503192L,"cb59b62352b2ed5654fc1f1df451cf8f221c260627df6ee5feb7fcedac71cfa9"),
  new Item("sharedassets2.assets.resS",10434976L,"e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84",10434976L,"e2444f63f9950a81858a74fdeaa9c219277d30cb1a5361ca7b206ab2667a9a84"),
  new Item("sharedassets2.resource",5365152L,"79f5dc7f17f979dc841accec3ed48744cfe78f787001f308b94632e2166503fa",5365152L,"79f5dc7f17f979dc841accec3ed48744cfe78f787001f308b94632e2166503fa"),
  new Item("sharedassets4.assets",693868L,"cfbc8731afc9dfb6137b85e63d324d5376b4a7d066b7ffdce2cef32c5ca233e3",693876L,"896ae7ac28e9ff8916121322326842369bcc9616e57ddd7764813085775130d9"),
  new Item("sharedassets4.assets.resS",564048L,"862c2f10062861982d7dd0593fa9345811147c3f3018a91365c3fe8260067127",564048L,"862c2f10062861982d7dd0593fa9345811147c3f3018a91365c3fe8260067127"),
  new Item("sharedassets4.resource",1780640L,"aa7cba281dd84a509bc1c0f200fa69cb21640615d92f16a65e9959db69a3b78b",1780640L,"aa7cba281dd84a509bc1c0f200fa69cb21640615d92f16a65e9959db69a3b78b"),
 };
 public static void validate(File input)throws IOException {
  try(ZipFile zip=new ZipFile(input)){
   ZipEntry[] found=new ZipEntry[ITEMS.length];Set<String> seen=new HashSet<>();String common=null;Enumeration<? extends ZipEntry> all=zip.entries();
   while(all.hasMoreElements()){
    ZipEntry e=all.nextElement();String name=e.getName();
    if(seen.size()>=20000||name.startsWith("/")||name.indexOf('\\')>=0||name.indexOf(':')>=0||!seen.add(name))throw new IOException("unsafe or duplicate ZIP entry");
    for(String part:name.split("/"))if(part.isEmpty()||part.equals(".")||part.equals(".."))throw new IOException("unsafe ZIP path");
    for(int i=0;i<ITEMS.length;i++){
     String root=rootFor(name,ITEMS[i].name);if(root==null)continue;
     if(found[i]!=null||common!=null&&!common.equals(root))throw new IOException("conflicting Steam roots");found[i]=e;common=root;
    }
   }
   for(int i=0;i<ITEMS.length;i++)if(found[i]==null||found[i].isDirectory())throw new Failure(ITEMS[i].name,true);
   int platform=-1;
   for(int i=0;i<ITEMS.length;i++)if(ITEMS[i].name.equals("sharedassets1.assets")){
    if(found[i].getSize()==ITEMS[i].size[0])platform=0;else if(found[i].getSize()==ITEMS[i].size[1])platform=1;else throw new Failure(ITEMS[i].name,false);
   }
   if(platform<0)throw new IOException("invalid preflight inventory");
   for(int i=0;i<ITEMS.length;i++){
    Item row=ITEMS[i];ZipEntry e=found[i];if(e.getSize()!=row.size[platform]||e.getCrc()<0)throw new Failure(row.name,false);
    MessageDigest digest;try{digest=MessageDigest.getInstance("SHA-256");}catch(NoSuchAlgorithmException ex){throw new AssertionError(ex);}
    CRC32 crc=new CRC32();long size=0;byte[] buffer=new byte[65536];
    try(InputStream in=zip.getInputStream(e)){for(int n;(n=in.read(buffer))!=-1;){if(n==0)continue;if(n>row.size[platform]-size)throw new Failure(row.name,false);size+=n;digest.update(buffer,0,n);crc.update(buffer,0,n);}}
    catch(Failure ex){throw ex;}catch(IOException ex){throw new Failure(row.name,false);}
    if(size!=row.size[platform]||crc.getValue()!=e.getCrc()||!hex(digest.digest()).equals(row.sha[platform]))throw new Failure(row.name,false);
   }
  }
 }
 private SteamDiagnosticInputCheck(){}
}
