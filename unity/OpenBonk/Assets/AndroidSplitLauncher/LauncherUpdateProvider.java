package com.megabonk.launcher;

import android.content.ContentProvider;
import android.content.ContentValues;
import android.database.Cursor;
import android.net.Uri;
import android.os.ParcelFileDescriptor;
import java.io.File;
import java.io.FileNotFoundException;

/** Exposes only the verified update to the Android package installer. */
public final class LauncherUpdateProvider extends ContentProvider {
    @Override public boolean onCreate() { return true; }
    @Override public String getType(Uri uri) { return "application/vnd.android.package-archive"; }
    @Override public ParcelFileDescriptor openFile(Uri uri, String mode) throws FileNotFoundException {
        if (!"/update.apk".equals(uri.getPath()) || !"r".equals(mode)) throw new FileNotFoundException();
        return ParcelFileDescriptor.open(new File(getContext().getCacheDir(), "openbonk-update.apk"), ParcelFileDescriptor.MODE_READ_ONLY);
    }
    @Override public Cursor query(Uri u, String[] projection, String selection, String[] args, String sort) {
        if (!"/update.apk".equals(u.getPath())) return null;
        String[] columns = projection == null ? new String[] { android.provider.OpenableColumns.DISPLAY_NAME, android.provider.OpenableColumns.SIZE } : projection;
        android.database.MatrixCursor cursor = new android.database.MatrixCursor(columns);
        Object[] values = new Object[columns.length];
        for (int i = 0; i < columns.length; i++) {
            if (android.provider.OpenableColumns.DISPLAY_NAME.equals(columns[i])) values[i] = "OpenBonk-update.apk";
            else if (android.provider.OpenableColumns.SIZE.equals(columns[i])) values[i] = new File(getContext().getCacheDir(), "openbonk-update.apk").length();
        }
        cursor.addRow(values); return cursor;
    }
    @Override public Uri insert(Uri u, ContentValues v) { throw new UnsupportedOperationException(); }
    @Override public int update(Uri u, ContentValues v, String s, String[] a) { throw new UnsupportedOperationException(); }
    @Override public int delete(Uri u, String s, String[] a) { throw new UnsupportedOperationException(); }
}
