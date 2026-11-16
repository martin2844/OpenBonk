package android.content;
public interface SharedPreferences {
    boolean contains(String key);
    String getString(String key, String fallback);
    Editor edit();
    interface Editor { Editor putString(String key, String value); Editor remove(String key); boolean commit(); }
}
