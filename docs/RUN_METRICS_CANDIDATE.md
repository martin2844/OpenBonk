# Run Metrics integration candidate

Run Metrics requires a host with the `run_metrics` hook. Do not publish its catalog entry for older APKs that lack that hook. Build the distributable mod ZIP with `python3 mods/tools/package_mod.py mods/run-metrics OUTPUT.zip`; it imports disabled.

The Android data target for this candidate is SHA-256 `9e1bb997a09945e448d1580222ce487a46d6840a39795c9734710cbc7e8bbf2e` (374365424 bytes). Linux and Windows reconstruction must both produce that target. Embedded deltas cover the compatible source hashes for versions 0.1.1 through 0.1.7. Compatible 0.1.0 data first uses the existing verified remote 0.1.0-to-0.1.4 patch, then the embedded 0.1.4-to-candidate delta. Migration is transactional and retains the original OBB.

Use version code 9 or later and pass `--obb-migration-library` to the dual-input packager; it validates the target and all five patch digests. The APK assembled for local testing uses an isolated application ID and debug signing; it is not the release APK. Sign and independently test the production candidate before publication. The Linux and Windows output, local game inputs and intermediate OBBs are private.
