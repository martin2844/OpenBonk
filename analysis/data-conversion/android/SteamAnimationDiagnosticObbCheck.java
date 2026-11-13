package com.openbonk.dataprep;
import java.io.*;
import java.util.*;
import java.util.zip.ZipFile;
public final class SteamAnimationDiagnosticObbCheck {
 public static void main(String[] a)throws Exception{
  if(a.length!=6)throw new IllegalArgumentException("partial base steam plan output expected");File out=new File(a[4]),dir=out.getAbsoluteFile().getParentFile();String[] before=dir.list((d,n)->n.startsWith(".openbonk-animation-obb-"));if(before==null)throw new IOException("missing directory");Arrays.sort(before);
  try{SteamAnimationDiagnosticObb.assemble(new File(a[0]),new File(a[1]),new File(a[2]),new File(a[3]),out);if(!a[5].equals("pass"))throw new AssertionError("invalid input accepted");try(ZipFile z=new ZipFile(out)){if(z.size()!=1081||!z.getComment().contains("INCOMPLETE"))throw new AssertionError("incorrect diagnostic OBB");}}catch(IOException e){if(a[5].equals("pass")||!a[5].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
  String[] after=dir.list((d,n)->n.startsWith(".openbonk-animation-obb-"));if(after==null)throw new IOException("missing directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary files leaked");System.out.println("PASS: "+a[5]);
 }
}
