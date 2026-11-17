package com.megabonk.launcher;
import org.json.JSONObject;
final class SplitDataLauncher {
    static boolean validModHooks(JSONObject manifest) { return LauncherModHookValidation.validModHooks(manifest); }
}
