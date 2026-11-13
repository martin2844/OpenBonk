package android.content;
public interface SharedPreferences {
 boolean contains(String key); boolean getBoolean(String key, boolean defValue); Editor edit();
 interface Editor { Editor putBoolean(String key, boolean value); Editor remove(String key); boolean commit(); }
}
