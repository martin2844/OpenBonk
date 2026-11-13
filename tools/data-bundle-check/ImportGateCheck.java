package com.megabonk.launcher;

import java.util.concurrent.CountDownLatch;
import java.util.concurrent.TimeUnit;

public final class ImportGateCheck {
    public static void main(String[] args) throws Exception {
        LauncherImportGate gate = new LauncherImportGate();
        long initial = gate.generation();
        if (!gate.begin() || gate.begin() || !gate.isBusy() || gate.generation() == initial)
            throw new AssertionError("reservation or generation failed");
        CountDownLatch waiting = new CountDownLatch(1), finished = new CountDownLatch(1);
        Thread screen = new Thread(() -> {
            try { waiting.countDown(); gate.awaitIdle(); finished.countDown(); }
            catch (InterruptedException e) { throw new AssertionError(e); }
        });
        screen.start();
        if (!waiting.await(2, TimeUnit.SECONDS) || finished.await(100, TimeUnit.MILLISECONDS))
            throw new AssertionError("screen did not wait for import");
        long during = gate.generation();
        gate.finish();
        if (!finished.await(2, TimeUnit.SECONDS) || gate.isBusy() || gate.generation() != during)
            throw new AssertionError("screen did not resume after import");
        if (!gate.begin() || gate.generation() == during) throw new AssertionError("new import not detected");
        gate.finish();
        screen.join();
        System.out.println("Import gate: PASS");
    }
}
