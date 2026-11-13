package com.megabonk.launcher;

/** Process-local reservation held from picker result until import worker exits. */
final class LauncherImportGate {
    private boolean busy;
    private long generation;
    private String stage;
    private String steamFailure;
    private String profileFailure;
    private String modImported;
    private String modFailure;
    private boolean modRestoration;
    private boolean modRecoveryNotice;
    private boolean profileRecoveryNotice;

    synchronized boolean begin() {
        if (busy) return false;
        busy = true;
        stage = null;
        steamFailure = null;
        profileFailure = null;
        modImported = null;
        modFailure = null;
        modRestoration = false;
        modRecoveryNotice = false;
        profileRecoveryNotice = false;
        generation++;
        return true;
    }

    synchronized boolean isBusy() { return busy; }
    synchronized long generation() { return generation; }
    synchronized void stage(String value) { if (busy) stage = value; }
    synchronized String stage() { return busy ? stage : null; }
    synchronized void steamFailure(String value) { if (busy) steamFailure = value; }
    synchronized String steamFailure() { return steamFailure; }
    synchronized void profileFailure(String value) { if (busy) profileFailure = value; }
    synchronized String profileFailure() { return profileFailure; }
    synchronized void modResult(String id, String failure, boolean restoration) { if (busy) { modImported = id; modFailure = failure; modRestoration = restoration; } }
    synchronized String modImported() { return modImported; }
    synchronized String modFailure() { return modFailure; }
    synchronized boolean modRestoration() { return modRestoration; }
    synchronized void clearModResult() { modImported = null; modFailure = null; modRestoration = false; }
    synchronized void noteModRecovery() { modRecoveryNotice = true; }
    synchronized boolean hasModRecoveryNotice() { return modRecoveryNotice; }
    synchronized void noteProfileRecovery() { profileRecoveryNotice = true; }
    synchronized boolean hasProfileRecoveryNotice() { return profileRecoveryNotice; }

    synchronized void awaitIdle() throws InterruptedException {
        while (busy) wait();
    }

    synchronized void finish() {
        if (!busy) throw new IllegalStateException("no import in progress");
        busy = false;
        stage = null;
        notifyAll();
    }
}
