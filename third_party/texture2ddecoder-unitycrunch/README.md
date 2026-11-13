# Unity Crunch decoder headers

These unmodified headers come from the `unitycrunch` directory in
[texture2ddecoder](https://github.com/K0lb3/texture2ddecoder), commit
`3ebc3b758bd6b1a3108b50148f7998ee34f058c6`. The surrounding project is MIT
licensed; see `LICENSE.texture2ddecoder`. The Crunch headers include their
original zlib copyright and license notices, which must be retained.

The OpenBonk adapter in `analysis/data-conversion/unpack-unity-crunch-cubemap.cpp`
uses these headers for a narrowly validated six-face cubemap conversion. It is
not a general-purpose parser for arbitrary untrusted files.
