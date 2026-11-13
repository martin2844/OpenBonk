package com.openbonk.dataprep;
import java.io.*;
import java.util.*;
import java.util.zip.ZipFile;
public final class SteamLocalizationDiagnosticObbCheck {
 public static void main(String[] a)throws Exception{
  if(a.length!=5)throw new IllegalArgumentException("partial bundles plan output expected");File out=new File(a[3]),parent=out.getAbsoluteFile().getParentFile();String[] before=parent.list((d,n)->n.startsWith(".openbonk-locale-obb-"));if(before==null)throw new IOException("missing directory");Arrays.sort(before);
  try{SteamLocalizationDiagnosticObb.assemble(new File(a[0]),new File(a[1]),new File(a[2]),out);if(!a[4].equals("pass"))throw new AssertionError("invalid input accepted");try(ZipFile z=new ZipFile(out)){if(z.size()!=1081||!z.getComment().contains("INCOMPLETE"))throw new AssertionError("invalid diagnostic archive");}}catch(IOException e){if(a[4].equals("pass")||!a[4].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
  String[] after=parent.list((d,n)->n.startsWith(".openbonk-locale-obb-"));if(after==null)throw new IOException("missing directory");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary file leaked");System.out.println("PASS: "+a[4]);
 }
}
