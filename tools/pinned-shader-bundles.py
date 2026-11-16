#!/usr/bin/env python3
"""Validate and stage one pinned local Android shader cache.

Research inputs only: the cache includes third-party shaders whose provenance
and distribution rights remain unresolved.
"""
import hashlib
from pathlib import Path
import shutil

PINS = {
    'builtin-probe': 'aa5a321cdf6e36ebca124efa2552b4f9cf6dea7851012f417ecd14bf30b7398d',
    'feature-probe': '5426ce1284c68776ca88ae26f12ddddb213f681db6a5c611a4fd34e67ca506c5',
    'autoexposure': '60810d9ac95b11b592da8724ed60e58aa6b7c0857fca4c937f82bdc267e494be',
    'exposurehistogram': '824c9bca118bc4a432b6915f05e7f3be6222d278bca088edfcec36222336009a',
    'gaussiandownsample': '264a23eb724a9b7edc7505901d2d24e0ce453c81a89a532bbc577d7d41d5bb1f',
    'histogram': '14e3b74e4f90829af60c8555e5ae625c509bf1bc2d0803a436f95614b08c11da',
    'localshader-instance': 'a9591bc169626d90a25b6d1fef938983b52bde19307f915d94638e6720ed00a2',
    'localshader-study': '4e34205e40c813ffe1e7e830946c542731a035e023f8a2b014c10e8417e3fc65',
    'lut3dbaker': '0b649b1139c75b2b23973d035e86752305409489ef959b60275b81c6ddfc6c29',
    'multiscalevodownsample1': '35c6718fbdc76e626194c87728493b206da340fbd672c7669f037162131a85cf',
    'multiscalevodownsample2': '8f9e7744153d7b5c8e1874280b5502637f73a5d0cf50f88bbbc65f6da7eea95d',
    'multiscalevorender': 'edee95157d4013787e8d06623253d3526c2df390aa9e470d84cbbdf21f9a7321',
    'multiscalevoupsample': 'bd0a5e7ed8db34980ba5cc6cf603e69b7577d0cc4a94f3b17275f291caaf2f4c',
    'postprocess-study': '7cbb023315a2ad6722bf11e23cc0139b0208dea1d81f3202ad7ebcbc0851eb80',
    'texture3dlerp': 'da4cb323bca77f85b64445d1811060d2ef6cb96c07709caa073c8d8179b46a32',
    'vectorscope': '753a83dd8bd4589810419d3273f5c9f77c3e59d14ff177b92f943d3b3ff59998',
    'waveform': 'b521e97e19d36157323b9c0dd2f9393c225ef4fd483fc0f227e21807410af8e7',
}
PACKAGE_NAMES = frozenset(('postprocess-study', 'autoexposure', 'multiscalevorender', 'multiscalevoupsample',
                           'multiscalevodownsample1', 'multiscalevodownsample2', 'lut3dbaker', 'histogram',
                           'texture3dlerp', 'gaussiandownsample', 'exposurehistogram', 'waveform', 'vectorscope'))
PROBE_SHADER_PINS = {
    'feature-probe': {
        'TextMeshPro/Distance Field Overlay': '948cd1df79af85ab1d8ee98d6f8eac2fb3e56bc853f1f576f5e4734bb01b8fe3',
        'Megabonk/Native Cubemap Skybox': '563fb2120ba4133310b28b1e3f78c357fa90abf5590493a43b6e9eaf9d0f501b',
    },
    'builtin-probe': {
        'Standard': '0aefc185255fe48e294785d13ef2c356f985f5d3c429cee3645a2a24b1942dfd',
        'Unlit/Texture': '721cbe7559753e1640ef7cfb30c64b86611b478f05393f5f80897e0df8dfa588',
        'Legacy Shaders/Particles/Alpha Blended': '07fba39c06ec68de954bae7f9676fb5ebb9d412a91ec6645faf999e1a2ea9104',
    },
}


def verify_probe(file, name):
    """Temporary material GUIDs can vary; pin every shader object instead."""
    import UnityPy
    expected = PROBE_SHADER_PINS[name]
    actual = {}
    for obj in UnityPy.load(str(file)).objects:
        if obj.type.name == 'ComputeShader': raise ValueError('unexpected compute shader in '+name)
        if obj.type.name != 'Shader': continue
        shader = obj.read_typetree()['m_ParsedForm']['m_Name']
        if shader in actual or shader not in expected: raise ValueError('unexpected shader in '+name)
        actual[shader] = hashlib.sha256(obj.get_raw_data()).hexdigest()
    if actual != expected: raise ValueError('incompatible shader objects in '+name)


def verify(folder, package=None):
    folder = Path(folder)
    package = Path(package) if package is not None else None
    for name, expected in PINS.items():
        file = (package if package is not None and name in PACKAGE_NAMES else folder)/name
        if file.is_symlink() or not file.is_file() or file.stat().st_size > 8*1024*1024:
            raise ValueError('missing or oversized cached shader: '+str(file))
        with file.open('rb') as stream:
            actual = hashlib.file_digest(stream, 'sha256').hexdigest()
        if actual != expected:
            if name not in PROBE_SHADER_PINS: raise ValueError('incompatible cached shader: '+str(file))
            verify_probe(file, name)


def stage(folder, destination, package=None):
    folder, destination = Path(folder).resolve(), Path(destination).absolute()
    package = Path(package).resolve() if package is not None else None
    if destination.exists() or destination.is_symlink() or destination.resolve().is_relative_to(folder) or (package is not None and destination.resolve().is_relative_to(package)):
        raise ValueError('shader output must be new and outside the cache')
    verify(folder, package)
    destination.mkdir(mode=0o700)
    try:
        for name in PINS:
            source = package if package is not None and name in PACKAGE_NAMES else folder
            shutil.copyfile(source/name, destination/name)
        verify(destination)
    except BaseException:
        shutil.rmtree(destination)
        raise
