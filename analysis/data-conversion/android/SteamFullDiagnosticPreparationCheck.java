package com.openbonk.dataprep;
import java.io.*;
import java.util.*;
public final class SteamFullDiagnosticPreparationCheck {
 public static void main(String[] a)throws Exception{
  if(a.length!=7)throw new IllegalArgumentException("library base-apk base-obb steam plan-dir output expected");System.load(a[0]);File out=new File(a[5]),parent=out.getAbsoluteFile().getParentFile();String[] before=parent.list((d,n)->n.startsWith(".openbonk-full-diagnostic-"));if(before==null)throw new IOException("missing output parent");Arrays.sort(before);
  try{SteamFullDiagnosticPreparation.prepare(new File(a[1]),new File(a[2]),new File(a[3]),new File(a[4]),out,label->System.out.println("STAGE: "+label));if(!a[6].equals("pass"))throw new AssertionError("invalid input accepted");}catch(IOException e){if(a[6].equals("pass")||!a[6].equals(e.getMessage()))throw e;if(out.exists())throw new AssertionError("partial output published");}
  String[] after=parent.list((d,n)->n.startsWith(".openbonk-full-diagnostic-"));if(after==null)throw new IOException("missing output parent");Arrays.sort(after);if(!Arrays.equals(before,after))throw new AssertionError("temporary files leaked");System.out.println("PASS: "+a[6]);
 }
}
