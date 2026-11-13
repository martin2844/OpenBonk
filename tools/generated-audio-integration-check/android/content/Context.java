package android.content;
import android.content.res.AssetManager;
import java.io.File;
import java.nio.file.Path;
public final class Context {
    private final AssetManager assets;
    private final File cache;
    public Context(Path assets, File cache) { this.assets = new AssetManager(assets); this.cache = cache; }
    public Context getApplicationContext() { return this; }
    public AssetManager getAssets() { return assets; }
    public File getCacheDir() { return cache; }
}
