package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;
public final class SteamUnityFsRepackCheck {
 public static void main(String[] a)throws Exception{
  if(a.length!=3)throw new IllegalArgumentException("headers raws output");Path headers=Paths.get(a[0]),raws=Paths.get(a[1]),out=Paths.get(a[2]);Files.createDirectory(out);int count=0;
  try(DirectoryStream<Path> files=Files.newDirectoryStream(raws,"*.bundle.raw")){for(Path raw:files){String n=raw.getFileName().toString();String bundle=n.substring(0,n.length()-4);byte[] result=SteamUnityFsRepack.repack(Files.readAllBytes(headers.resolve(bundle+".header")),Files.readAllBytes(raw));Files.write(out.resolve(bundle),result,StandardOpenOption.CREATE_NEW);count++;}}
  if(count!=17)throw new IOException("missing locales");System.out.println("PASS: "+count+" diagnostic bundles repacked");
 }
}
