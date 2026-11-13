package com.megabonk.launcher;
import android.content.SharedPreferences;
import java.nio.file.*;
import java.io.*;
import java.util.*;
import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicReference;
public class RecoveryCheck {
 static class Prefs implements SharedPreferences {
  Map<String,Boolean> state=new HashMap<>(); boolean fail; CountDownLatch entered, release;
  public boolean contains(String k){return state.containsKey(k);} public boolean getBoolean(String k,boolean d){return state.getOrDefault(k,d);}
  public Editor edit(){return new Editor(){Map<String,Boolean> changes=new HashMap<>(); Set<String> removed=new HashSet<>(); public Editor putBoolean(String k,boolean v){changes.put(k,v);return this;} public Editor remove(String k){removed.add(k);return this;} public boolean commit(){if(entered!=null){entered.countDown();try{if(!release.await(5,TimeUnit.SECONDS))throw new AssertionError("commit wait timed out");}catch(InterruptedException e){throw new AssertionError(e);}entered=null;}if(fail)return false; for(String k:removed)state.remove(k);state.putAll(changes);return true;}};}
 }
 static void check(boolean v,String m){if(!v)throw new AssertionError(m);}
 static File dir(Path root,String name,String data)throws Exception{Path p=Files.createDirectories(root.resolve(name));Files.writeString(p.resolve("manifest.json"),data);Files.writeString(p.resolve("main.lua"),data);return p.toFile();}
 static String read(Path root,String id)throws Exception{return Files.readString(root.resolve(id).resolve("manifest.json"));}
 public static void main(String[] args)throws Exception{
  Path tmp=Files.createTempDirectory("mod-recovery-"); Path root=Files.createDirectory(tmp.resolve("Mods")); Prefs p=new Prefs(); p.state.put("pikachu",true);
  dir(root,"pikachu","old"); File stage=dir(root,".stage","new"); LauncherModRecovery.install(root.toFile(),"pikachu",stage,p);
  check(read(root,"pikachu").equals("new")&&!p.getBoolean("pikachu",true)&&!Files.exists(root.resolve(".previous-pikachu")),"update");
  check(read(tmp.resolve("ModsHistory"),"pikachu").equals("old") && LauncherModRecovery.hasPrevious(root.toFile(),"pikachu"),"previous retained");
  p.state.put("pikachu",true); LauncherModRecovery.restorePrevious(root.toFile(),"pikachu",p);
  check(read(root,"pikachu").equals("old") && read(tmp.resolve("ModsHistory"),"pikachu").equals("new") && !p.getBoolean("pikachu",true),"restore swaps and disables");
  LauncherModRecovery.restorePrevious(root.toFile(),"pikachu",p);
  check(read(root,"pikachu").equals("new") && read(tmp.resolve("ModsHistory"),"pikachu").equals("old"),"restore swap is reversible");
  Files.move(root.resolve("pikachu"),root.resolve(".previous-pikachu")); LauncherModRecovery.recover(root.toFile()); check(read(root,"pikachu").equals("new"),"crash before promotion");
  dir(root,".previous-pikachu","previous"); LauncherModRecovery.recover(root.toFile()); check(read(root,"pikachu").equals("new")&&!Files.exists(root.resolve(".previous-pikachu")) && read(tmp.resolve("ModsHistory"),"pikachu").equals("previous"),"crash after promotion");
  p.state.put("pikachu",true); try { LauncherModRecovery.install(root.toFile(),"pikachu",root.resolve("missing-stage").toFile(),p); throw new AssertionError("missing stage accepted"); } catch(IOException expected){}
  check(read(root,"pikachu").equals("new")&&p.getBoolean("pikachu",false),"failed promotion restores old data and enabled state");
  p.state.put("pikachu",true); p.fail=true; try { LauncherModRecovery.restorePrevious(root.toFile(),"pikachu",p); throw new AssertionError("restore state failure accepted"); } catch(IOException expected){}
  check(read(root,"pikachu").equals("new") && read(tmp.resolve("ModsHistory"),"pikachu").equals("previous"),"failed restore preserves both versions");
  File bad=dir(root,".stage2","bad"); try { LauncherModRecovery.install(root.toFile(),"pikachu",bad,p); throw new AssertionError("failed prefs accepted"); } catch(IOException expected){}
  check(read(root,"pikachu").equals("new"),"failed state commit preserves mod");
  p.fail=false; p.entered=new CountDownLatch(1); p.release=new CountDownLatch(1); File concurrentStage=dir(root,".stage3","latest"); AtomicReference<Throwable> failure=new AtomicReference<>();
  Thread update=new Thread(()->{try{LauncherModRecovery.install(root.toFile(),"pikachu",concurrentStage,p);}catch(Throwable e){failure.set(e);}});
  Thread recovery=new Thread(()->{try{LauncherModRecovery.recover(root.toFile());}catch(Throwable e){failure.set(e);}});
  update.start();check(p.entered.await(5,TimeUnit.SECONDS),"update entered transaction");recovery.start();
  long until=System.nanoTime()+TimeUnit.SECONDS.toNanos(5);while(recovery.getState()!=Thread.State.BLOCKED&&recovery.isAlive()&&System.nanoTime()<until)Thread.yield();
  boolean blocked=recovery.getState()==Thread.State.BLOCKED;p.release.countDown();update.join(5000);recovery.join(5000);
  check(blocked&&!update.isAlive()&&!recovery.isAlive()&&failure.get()==null,"recovery waits for active update");
  check(read(root,"pikachu").equals("latest")&&read(tmp.resolve("ModsHistory"),"pikachu").equals("new"),"concurrent update and recovery preserve both versions");
  System.out.println("PASS previous retention, reversible restore, interruption recovery, failures and serialized concurrent recovery");
 }
}
