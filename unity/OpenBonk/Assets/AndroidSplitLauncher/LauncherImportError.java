package com.megabonk.launcher;

import android.system.ErrnoException;
import android.system.OsConstants;
import java.util.IdentityHashMap;
import java.util.Locale;

/** Makes storage exhaustion actionable without hiding other import failures. */
final class LauncherImportError {
    static String describe(Throwable error, boolean spanish) {
        IdentityHashMap<Throwable, Boolean> seen = new IdentityHashMap<>();
        for (Throwable cause = error; cause != null && seen.put(cause, Boolean.TRUE) == null; cause = cause.getCause()) {
            String message = cause.getMessage();
            if ((cause instanceof ErrnoException && ((ErrnoException) cause).errno == OsConstants.ENOSPC)
                    || (message != null && (message.contains("ENOSPC")
                    || message.toLowerCase(Locale.ROOT).contains("no space left on device")))) {
                return spanish ? "No hay espacio suficiente. Liberá espacio y volvé a intentarlo."
                               : "Not enough storage. Free some space and try again.";
            }
        }
        String message = error.getMessage();
        if ("ZIP too large".equals(message)) return spanish
                ? "El ZIP supera el tamaño permitido."
                : "The ZIP exceeds the allowed size.";
        if ("package too large".equals(message)) return spanish
                ? "El archivo supera el límite de 128 MB para mods."
                : "The file exceeds the 128 MB mod limit.";
        if ("not a zip".equals(message)) return spanish
                ? "El archivo seleccionado no es un ZIP. Los mods de PC (.dll o paquetes BepInEx/Thunderstore) no son compatibles; los mods de OpenBonk son ZIP con manifest.json y main.lua."
                : "The selected file is not a ZIP. PC mods (.dll or BepInEx/Thunderstore packages) are not supported; OpenBonk mods are ZIP files with manifest.json and main.lua.";
        if ("pc mod package".equals(message)) return spanish
                ? "Este es un paquete de mod de PC/Steam (BepInEx/Thunderstore) y OpenBonk no puede cargarlo. Usá un mod hecho para OpenBonk."
                : "This is a PC/Steam mod package (BepInEx/Thunderstore), which OpenBonk cannot load. Use a mod built for OpenBonk.";
        if ("unsupported mod package".equals(message)) return spanish
                ? "Este ZIP no tiene el formato de un mod de OpenBonk. Los mods de OpenBonk necesitan manifest.json y main.lua, o un manifiesto de contenido compatible."
                : "This ZIP does not use the OpenBonk mod format. OpenBonk mods need manifest.json and main.lua, or a supported content manifest.";
        if ("manifest.json and main.lua required".equals(message) || "unexpected files".equals(message)
                || "unexpected or duplicate file".equals(message) || "invalid path".equals(message)
                || "invalid directory".equals(message) || "files must share a directory".equals(message)
                || "multiple content manifests".equals(message)) return spanish
                ? "El ZIP no contiene un mod de OpenBonk compatible. Los mods de OpenBonk llevan manifest.json y main.lua (o un manifiesto de contenido) en la raíz; los mods de PC/Steam no son compatibles."
                : "The ZIP doesn't contain a supported OpenBonk mod. OpenBonk mods have manifest.json and main.lua (or a content manifest) at the ZIP root; PC/Steam mods are not compatible.";
        if ("unsupported manifest or package ID".equals(message)) return spanish
                ? "El manifiesto del mod no es compatible con esta versión de OpenBonk. Actualizá OpenBonk o usá un mod hecho para esta versión."
                : "The mod manifest isn't supported by this OpenBonk version. Update OpenBonk or use a mod built for this release.";
        if ("invalid script".equals(message)) return spanish
                ? "El script del mod está dañado o no es texto UTF-8 válido. Volvé a descargarlo o creá el ZIP de nuevo."
                : "The mod script is damaged or is not valid UTF-8 text. Download it again or recreate the ZIP.";
        if ("storage unavailable".equals(message) || "cannot stage mod".equals(message)) return spanish
                ? "No se pudo guardar el mod en este dispositivo. Liberá espacio y volvé a intentarlo."
                : "Could not save the mod on this device. Free some space and try again.";
        if ("too many files".equals(message) || "content too large".equals(message)) return spanish
                ? "El mod de contenido supera el tamaño o la cantidad de archivos admitidos."
                : "The content mod exceeds the supported size or file count.";
        if ("empty ZIP".equals(message)) return spanish
                ? "El archivo seleccionado está vacío. Elegí el ZIP de datos de Steam."
                : "The selected file is empty. Choose the Steam data ZIP.";
        if ("incompatible ZIP".equals(message)) return spanish
                ? "El ZIP está incompleto, dañado o pertenece a una versión no compatible."
                : "The ZIP is incomplete, damaged or from an unsupported version.";
        if ("cannot open selected file".equals(message)) return spanish
                ? "No se pudo abrir el archivo seleccionado. Intentá seleccionarlo de nuevo."
                : "Could not open the selected file. Try selecting it again.";
        if ("native converter unavailable".equals(message)) return spanish
                ? "No se pudo iniciar el conversor de datos en este dispositivo. Los datos anteriores se conservaron."
                : "Could not start the data converter on this device. The previous data was kept.";
        if ("missing file or size mismatch".equals(message) || "missing content files".equals(message)) return spanish
                ? "Al mod le faltan archivos o no coinciden con su manifiesto. Descargalo o creá el ZIP de nuevo."
                : "The mod is missing files or they do not match its manifest. Download it or create the ZIP again.";
        if ("missing manifest".equals(message)) return spanish
                ? "El ZIP no contiene un manifiesto de mod. Elegí un ZIP de mod compatible."
                : "The ZIP has no mod manifest. Choose a compatible mod ZIP.";
        if ("ZIP entry damaged".equals(message) || "content hash mismatch".equals(message) || "previous mod is damaged".equals(message)) return spanish
                ? "El archivo está dañado o incompleto. Volvé a descargarlo o creá el ZIP de nuevo."
                : "The file is damaged or incomplete. Download it again or recreate the ZIP.";
        if (message != null) {
            String lower = message.toLowerCase(Locale.ROOT);
            if (lower.contains("zip end header") || lower.contains("not a zip file") || lower.contains("zip file is empty")
                    || lower.contains("invalid cen header") || lower.contains("invalid loc header"))
                return spanish ? "El ZIP está dañado o incompleto. Volvé a descargarlo o creá el ZIP de nuevo."
                               : "The ZIP is damaged or incomplete. Download it again or recreate it.";
        }
        return message == null || message.isEmpty() ? error.getClass().getSimpleName() : message;
    }

    private LauncherImportError() {}
}
