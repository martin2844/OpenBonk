# Native dependencies

`etcpak-0.9.15.tar.gz` is the unmodified source distribution of the Python
`etcpak` package, SHA-256
`fe09e32d8aa75b8163832428c3d6df65a51b2b7e0237292c10c1224ca5728a43`.
Its wrapper is MIT licensed; its bundled etcpak sources are BSD-3-Clause and
its bundled bcdec header is dual-licensed. See the distribution and the
corresponding files in `openbonk-texture-notices/` for copyright and license
notices. Only the relevant C++ sources are used by the native adapter.

`etc2comp-39422c1.tar.gz` contains `EtcLib`, `AUTHORS` and `LICENSE` from
[Google Etc2Comp](https://github.com/google/etc2comp) commit
`39422c1aa2f4889d636db5790af1d0be6ff3a226`. The included source is licensed
under Apache-2.0. The archive SHA-256 is
`ba661f81c69fa4eecb2654afcacb0639617a619d964d6baf2310997c75f4381b`.
It is used only by the standalone experimental quality probe, not the APK.

The adjacent directories document the exact zlib and Unity Crunch sources and
their licenses. Game assets, user data and Unity binaries are not included.
