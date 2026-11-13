# Texture converter notices — local research

These notices concern only code used in the experimental native texture
converter. They do not cover the game, Unity, shaders, plans, artwork, or any
other dependency, and do not establish that the OpenBonk APK is distributable.

- `etcpak-BSD-3-Clause.txt` is the license from wolfpld/etcpak commit
  `4b68aad232c15fa15202ae0f51b1531909e668c8`. The package source used in the
  experiment matches that revision for the audited core files. The upstream
  file is at <https://github.com/wolfpld/etcpak/blob/4b68aad232c15fa15202ae0f51b1531909e668c8/LICENSE.txt>.
- `etcpak-wrapper-MIT.txt` is the license in the local K0lb3/etcpak 0.9.15
  source package. Its upstream project is <https://github.com/K0lb3/etcpak>.
- `bcdec-dual-license.txt` is the license block from the `bcdec.h` shipped
  with that source package, matching the pinned etcpak commit. Its upstream is
  <https://github.com/iOrange/bcdec>.
- `texture2ddecoder-MIT.txt` is the project license that accompanies the
  vendored Unity Crunch headers in `third_party/texture2ddecoder-unitycrunch`.
- `crn_decomp.h-notice.txt` and `crnlib.h-notice.txt` preserve the zlib notices
  from those headers; source provenance is documented in the adjacent
  `third_party/texture2ddecoder-unitycrunch/README.md`.
- The adapter's BC1/BC3 decoding follows Pillow 13.0.0's `BcnDecode.c`, whose
  header marks that file as public domain under CC0. See
  <https://github.com/python-pillow/Pillow/blob/13.0.0/src/libImaging/BcnDecode.c>.

The exact compiler inputs and complete notices for the final artifact still
need an independent audit. These files are not a complete third-party list.
