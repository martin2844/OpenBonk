package com.openbonk.dataprep;
import java.io.*;
import java.nio.file.*;

/** Private diagnostic pipeline; it does not create a complete or distributable game. */
public final class SteamLocalizationObbPreparation {
 public static void prepare(File partial,File steam,File localePlan,File obbPlan,File output)throws IOException{
  Path dest=output.toPath().toAbsolutePath(),parent=dest.getParent();if(parent==null||!Files.isDirectory(parent)||Files.exists(dest,LinkOption.NOFOLLOW_LINKS))throw new IOException("output must be new");
  Path temp=Files.createTempDirectory(parent,".openbonk-locale-prep-"),bundles=temp.resolve("bundles");
  try{SteamLocalizationText.restore(steam,localePlan,bundles.toFile());SteamLocalizationDiagnosticObb.assemble(partial,bundles.toFile(),obbPlan,dest.toFile());}
  finally{if(Files.isDirectory(bundles,LinkOption.NOFOLLOW_LINKS)){try(DirectoryStream<Path> files=Files.newDirectoryStream(bundles)){for(Path f:files)Files.deleteIfExists(f);}Files.deleteIfExists(bundles);}Files.deleteIfExists(temp);}
 }
 private SteamLocalizationObbPreparation(){}
}
