package android.content;
import android.content.res.AssetManager;
import java.io.File;
import java.nio.file.Path;
public final class Context {
    private final AssetManager assets;
    private final File cache;
    private final File files;
    public Context(Path assets, File cache) {
        this.assets = new AssetManager(assets); this.cache = cache;
        this.files = new File(cache.getParentFile(), cache.getName() + "-files");
        if (!files.mkdir() && !files.isDirectory()) throw new IllegalStateException("cannot create files directory");
    }
    public Context getApplicationContext() { return this; }
    public AssetManager getAssets() { return assets; }
    public File getCacheDir() { return cache; }
    public File getFilesDir() { return files; }
}
