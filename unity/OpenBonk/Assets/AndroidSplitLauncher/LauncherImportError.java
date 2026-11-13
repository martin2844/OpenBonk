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
        return message == null || message.isEmpty() ? error.getClass().getSimpleName() : message;
    }

    private LauncherImportError() {}
}
