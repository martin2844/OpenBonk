package com.megabonk.launcher;
import android.content.Context;
import java.io.*;
import java.lang.reflect.*;
import java.nio.charset.StandardCharsets;
import java.util.function.Consumer;

/** Optional local-only preparer; absent from ordinary launcher builds. */
final class LauncherDiagnosticIntegration {
 private static final String CLASS="com.megabonk.launcher.LauncherDiagnosticGenerator";
 private static final String MARKER="OPENBONK_PRIVATE_DIAGNOSTIC_V1";
 static boolean available(Context context){
  try(InputStream in=context.getAssets().open("openbonk-diagnostic/marker")){
   byte[] bytes=new byte[64];int total=0,n;while(total<bytes.length&&(n=in.read(bytes,total,bytes.length-total))!=-1){if(n>0)total+=n;}
   if(!MARKER.equals(new String(bytes,0,total,StandardCharsets.US_ASCII))||in.read()!=-1)return false;
   return LauncherPreparedSteamImport.Generator.class.isAssignableFrom(Class.forName(CLASS));
  }catch(IOException|ClassNotFoundException|LinkageError|SecurityException e){return false;}
 }
 static LauncherPreparedSteamImport.Generator create(Context context,Consumer<String> progress)throws IOException{
  try{Constructor<?> c=Class.forName(CLASS).getDeclaredConstructor(Context.class,Consumer.class);c.setAccessible(true);return (LauncherPreparedSteamImport.Generator)c.newInstance(context,progress);}
  catch(ReflectiveOperationException|LinkageError|SecurityException e){throw new IOException("cannot start diagnostic preparer",e);}
 }
 private LauncherDiagnosticIntegration(){}
}
