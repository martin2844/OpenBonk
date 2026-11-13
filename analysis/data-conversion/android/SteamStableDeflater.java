package com.openbonk.dataprep;

import java.util.zip.Deflater;

/** Raw DEFLATE backed by the bundled zlib, used by the private data preparer. */
public final class SteamStableDeflater extends Deflater {
 static { System.loadLibrary("openbonk_zlib"); }
 private long handle;
 private boolean finishing;
 private final int configuredLevel;
 public SteamStableDeflater(int level) {
  super(level,true);
  super.end();
  configuredLevel=level;
  handle=init(level);
  if(handle==0)throw new IllegalStateException("cannot create stable deflater");
 }
 private static native long init(int level);
 private static native void input(long handle,byte[] data,int offset,int length);
 private static native int compress(long handle,byte[] data,int offset,int length,boolean finish);
 private static native long metric(long handle,int which);
 private static native void resetNative(long handle);
 private static native void closeNative(long handle);
 private void open(){if(handle==0)throw new IllegalStateException("closed stable deflater");}
 @Override public synchronized void setInput(byte[] b,int offset,int length){open();if(b==null)throw new NullPointerException();if(finishing)throw new IllegalStateException("deflater finished");input(handle,b,offset,length);}
 @Override public synchronized void setInput(byte[] b){setInput(b,0,b.length);}
 @Override public synchronized boolean needsInput(){open();return metric(handle,0)!=0;}
 @Override public synchronized void finish(){open();finishing=true;}
 @Override public synchronized boolean finished(){open();return metric(handle,1)!=0;}
 @Override public synchronized int deflate(byte[] b,int off,int len){open();if(b==null)throw new NullPointerException();return compress(handle,b,off,len,finishing);}
 @Override public synchronized int deflate(byte[] b){return deflate(b,0,b.length);}
 @Override public synchronized int deflate(byte[] b,int off,int len,int flush){if(flush!=NO_FLUSH)throw new UnsupportedOperationException("flush mode");return deflate(b,off,len);}
 @Override public synchronized long getBytesRead(){open();return metric(handle,2);}
 @Override public synchronized long getBytesWritten(){open();return metric(handle,3);}
 @Override public synchronized int getTotalIn(){return (int)getBytesRead();}
 @Override public synchronized int getTotalOut(){return (int)getBytesWritten();}
 @Override public synchronized void reset(){open();resetNative(handle);finishing=false;}
 @Override public synchronized void end(){if(handle!=0){closeNative(handle);handle=0;}}
 @Override public synchronized void setLevel(int level){if(level!=configuredLevel)throw new UnsupportedOperationException("compression level");}
}
