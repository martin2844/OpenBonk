import contextlib
import importlib.util
import io
from pathlib import Path
import shutil
import tempfile
import unittest


SCRIPT = Path(__file__).resolve().parents[1] / "refresh-preserved-runtime.py"
SPEC = importlib.util.spec_from_file_location("refresh_preserved_runtime", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(MODULE)


class PreservedRuntimeRefreshTest(unittest.TestCase):
    def setUp(self):
        temporary = tempfile.TemporaryDirectory(prefix="openbonk-refresh-test-")
        self.addCleanup(temporary.cleanup)
        self.root = Path(temporary.name)
        self.export = self.root / "export"
        self.preserved = self.root / "preserved"
        self.assets = self.root / "checkout/Assets"
        self.source = self.export / MODULE.MAIN
        self.target = self.preserved / MODULE.MAIN
        self.generated = self.target / MODULE.JAVA / "SplitExpectedData.java"
        self.put(self.generated, 'final class SplitExpectedData { static final String SHA256 = "' + "ab" * 32 + '"; }')
        self.put(self.source / "Il2CppOutputProject/Source/il2cppOutput/Il2CppCodeRegistration.cpp", "new code")
        self.put(self.target / "Il2CppOutputProject/stale.cpp", "old code")
        for name in MODULE.METADATA:
            self.put(self.source / "assets/bin/Data" / name, "new " + name)
            self.put(self.target / "assets/bin/Data" / name, "old " + name)
        for name in MODULE.LAUNCHER_SOURCES:
            self.put(self.assets / "AndroidSplitLauncher" / name, "new " + name)
            self.put(self.target / MODULE.JAVA / name, "old " + name)
        # A broad source overlay would overwrite both of these sentinels.
        self.put(self.assets / "AndroidSplitLauncher/SplitExpectedData.java", 'SHA256 = "EMBEDDED";')
        self.put(self.assets / "AndroidSplitLauncher/Unrelated.java", "checkout unrelated")
        self.put(self.target / MODULE.JAVA / "Unrelated.java", "generated unrelated")
        self.put(self.target / "assets/bin/Data/globalgamemanagers", "preserved registry")
        self.put(self.target / "assets/bin/Data/globalgamemanagers.assets.split0", "preserved game data")
        self.put(self.target / "assets/unity_obb_guid", "preserved OBB identity")
        self.put(self.preserved / "launcher/build.gradle", "preserved version and signing")
        self.put(self.source / "assets/bin/Data/globalgamemanagers", "fresh incompatible registry")

    @staticmethod
    def put(path, text):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text, encoding="utf-8")

    def snapshot(self):
        return {str(p.relative_to(self.root)): p.read_bytes() for p in self.root.rglob("*") if p.is_file()}

    def refresh(self, **kwargs):
        with contextlib.redirect_stdout(io.StringIO()):
            MODULE.refresh(self.export, self.preserved, self.assets, **kwargs)

    @unittest.skipUnless(shutil.which("rsync"), "rsync is required for the real copy fixture")
    def test_refresh_preserves_generated_config_and_other_assets(self):
        before = self.snapshot()
        self.refresh()
        after = self.snapshot()
        allowed = {str((self.target / "assets/bin/Data" / name).relative_to(self.root)) for name in MODULE.METADATA}
        allowed.update(str((self.target / MODULE.JAVA / name).relative_to(self.root)) for name in MODULE.LAUNCHER_SOURCES)
        il2cpp_prefix = str((self.target / "Il2CppOutputProject").relative_to(self.root)) + "/"
        for name in before.keys() | after.keys():
            if name not in allowed and not name.startswith(il2cpp_prefix):
                self.assertEqual(before.get(name), after.get(name), name)
        for name in MODULE.METADATA:
            self.assertEqual((self.target / "assets/bin/Data" / name).read_bytes(), ("new " + name).encode())
        for name in MODULE.LAUNCHER_SOURCES:
            self.assertEqual((self.target / MODULE.JAVA / name).read_bytes(), ("new " + name).encode())
        self.assertFalse((self.target / "Il2CppOutputProject/stale.cpp").exists())
        self.assertEqual((self.target / "Il2CppOutputProject/Source/il2cppOutput/Il2CppCodeRegistration.cpp").read_text(), "new code")

    def test_placeholder_fails_before_any_mutation(self):
        for value in ("EMBEDDED", "UNRESOLVED", "", "0" * 64):
            with self.subTest(value=value):
                self.put(self.generated, f'final class SplitExpectedData {{ static final String SHA256 = "{value}"; }}')
                before = self.snapshot()
                with self.assertRaisesRegex(ValueError, "generated SHA256"):
                    self.refresh()
                self.assertEqual(before, self.snapshot())

    def test_missing_source_fails_before_any_mutation(self):
        (self.assets / "AndroidSplitLauncher/SplitDataLauncher.java").unlink()
        before = self.snapshot()
        with self.assertRaisesRegex(ValueError, "Missing regular file"):
            self.refresh()
        self.assertEqual(before, self.snapshot())

    @unittest.skipUnless(shutil.which("rsync"), "rsync is required by the helper")
    def test_dry_run_changes_nothing(self):
        before = self.snapshot()
        self.refresh(dry_run=True)
        self.assertEqual(before, self.snapshot())


if __name__ == "__main__":
    unittest.main()
