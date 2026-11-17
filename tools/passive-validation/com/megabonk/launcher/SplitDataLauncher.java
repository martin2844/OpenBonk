package com.megabonk.launcher;

import org.json.JSONObject;

// The content-import tests call LauncherContentImporter directly; the general
// ZIP importer's hook validator is outside the scope of this JVM harness.
final class SplitDataLauncher {
    static boolean validModHooks(JSONObject manifest) { return true; }
}
