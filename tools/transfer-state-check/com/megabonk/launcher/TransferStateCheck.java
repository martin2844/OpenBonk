package com.megabonk.launcher;

import java.util.concurrent.CountDownLatch;
import java.util.concurrent.atomic.AtomicInteger;

public final class TransferStateCheck {
    private static void check(boolean condition, String message) {
        if (!condition) throw new AssertionError(message);
    }
    public static void main(String[] args) throws Exception {
        LauncherTransferState state = new LauncherTransferState();
        check(!state.snapshot().busy, "idle UI must be enabled");
        long first = state.begin("nimbo", true);
        LauncherTransferState.Snapshot downloading = state.snapshot();
        check(first > 0 && downloading.busy && downloading.updating, "update starts immediately busy");
        check(state.begin("other", false) == 0, "duplicate click cannot start a second worker");
        state.phase(first, LauncherTransferState.Phase.VERIFY);
        check(state.snapshot().busy && state.snapshot().phase == LauncherTransferState.Phase.VERIFY,
            "verification keeps the spinner");
        state.phase(first, LauncherTransferState.Phase.INSTALL);
        check(state.snapshot().busy, "installation keeps the spinner");
        check(downloading.phase == LauncherTransferState.Phase.DOWNLOAD,
            "old UI snapshot must not mutate beneath a recreated view");
        state.finish(first, "checksum mismatch");
        LauncherTransferState.Snapshot failed = state.snapshot();
        check(!failed.busy && "checksum mismatch".equals(failed.failure), "failure restores retry and preserves explanation");
        long retry = state.begin("nimbo", true);
        check(retry > first && state.snapshot().failure == null, "retry clears the old error");
        state.finish(first, null);
        state.phase(first, LauncherTransferState.Phase.INSTALL);
        check(state.snapshot().generation == retry && state.snapshot().phase == LauncherTransferState.Phase.DOWNLOAD,
            "late callbacks from an earlier attempt cannot finish or alter the retry");
        state.finish(retry, null);
        check(!state.snapshot().busy && state.snapshot().phase == LauncherTransferState.Phase.DONE,
            "success hides spinner and permits installed state");
        state.phase(retry, LauncherTransferState.Phase.VERIFY);
        check(state.snapshot().phase == LauncherTransferState.Phase.DONE, "finished transfer cannot restart via stale phase");

        LauncherTransferState simultaneous = new LauncherTransferState();
        CountDownLatch start = new CountDownLatch(1);
        AtomicInteger winners = new AtomicInteger();
        Thread[] clicks = new Thread[16];
        for (int i = 0; i < clicks.length; i++) {
            clicks[i] = new Thread(() -> {
                try { start.await(); } catch (InterruptedException e) { throw new AssertionError(e); }
                if (simultaneous.begin("app", true) != 0) winners.incrementAndGet();
            });
            clicks[i].start();
        }
        start.countDown();
        for (Thread click : clicks) click.join();
        check(winners.get() == 1, "only one concurrent request can own app download");
        check(state.begin("new-mod", false) > retry && !state.snapshot().updating,
            "mod installation remains independent of the app transfer");
        System.out.println("Transfer state PASS: phases, duplicate/concurrent clicks, failure/retry, immutable recreation snapshots, stale callbacks, independent app/mod transfers");
    }
}
