package com.openbonk.dataprep;

import java.io.OutputStream;
import java.util.zip.Deflater;
import java.util.zip.ZipOutputStream;

/** ZIP writer using the same raw-DEFLATE implementation on every Android ABI. */
public final class SteamStableZipOutputStream extends ZipOutputStream {
 public SteamStableZipOutputStream(OutputStream out){super(out);def.end();def=new SteamStableDeflater(Deflater.DEFAULT_COMPRESSION);}
}
