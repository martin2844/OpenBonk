package com.megabonk.launcher;

/** Process-local transfer state; no Activity or View survives recreation here. */
final class LauncherTransferState {
    enum Phase { IDLE, DOWNLOAD, VERIFY, INSTALL, DONE, FAILED }
    static final class Snapshot {
        final long generation;
        final String id, failure;
        final boolean updating, busy;
        final Phase phase;
        Snapshot(long generation, String id, boolean updating, Phase phase, String failure) {
            this.generation = generation; this.id = id; this.updating = updating;
            this.phase = phase; this.failure = failure;
            busy = phase == Phase.DOWNLOAD || phase == Phase.VERIFY || phase == Phase.INSTALL;
        }
    }
    private Snapshot current = new Snapshot(0, "", false, Phase.IDLE, null);
    synchronized long begin(String id, boolean updating) {
        if (current.busy) return 0;
        current = new Snapshot(current.generation + 1, id, updating, Phase.DOWNLOAD, null);
        return current.generation;
    }
    synchronized void phase(long generation, Phase phase) {
        if (!current.busy || generation != current.generation) return;
        if (phase != Phase.VERIFY && phase != Phase.INSTALL) throw new IllegalArgumentException("not a transfer phase");
        current = new Snapshot(generation, current.id, current.updating, phase, null);
    }
    synchronized void finish(long generation, String failure) {
        if (!current.busy || generation != current.generation) return;
        current = new Snapshot(generation, current.id, current.updating,
            failure == null ? Phase.DONE : Phase.FAILED, failure);
    }
    synchronized Snapshot snapshot() { return current; }
}
