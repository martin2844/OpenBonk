package android.content.res;
import java.io.*;
import java.nio.file.*;
public final class AssetManager {
    private final Path root;
    public AssetManager(Path root) { this.root = root; }
    public InputStream open(String name) throws IOException { return Files.newInputStream(root.resolve(name)); }
    public String[] list(String name) throws IOException {
        File[] files = root.resolve(name).toFile().listFiles();
        if (files == null) return new String[0];
        String[] names = new String[files.length]; for (int i = 0; i < files.length; i++) names[i] = files[i].getName();
        return names;
    }
}
