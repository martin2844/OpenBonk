"""Desktop C interface to the experimental deterministic ETC2 quality encoder."""
import ctypes
from pathlib import Path


class QualityEncoder:
    def __init__(self, library):
        path = Path(library).expanduser()
        if path.is_symlink() or not path.is_file(): raise ValueError('quality library must be a regular file')
        self.library = ctypes.CDLL(str(path.resolve(strict=True)))
        try: self.convert = self.library.openbonk_quality_mip
        except AttributeError as error: raise ValueError('library has no quality encoder') from error
        self.convert.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_void_p, ctypes.c_size_t]
        self.convert.restype = ctypes.c_int

    def encode(self, data, width, height, source_format):
        if (not isinstance(data, bytes) or not data or len(data) > 64 * 1024 * 1024 or
                type(width) is not int or type(height) is not int or not 1 <= width <= 2048 or not 1 <= height <= 2048 or
                type(source_format) is not int or source_format not in (1, 3, 4, 10, 12, 25)):
            raise ValueError('invalid quality texture input')
        size = ((width + 3) // 4) * ((height + 3) // 4) * 16
        source = ctypes.create_string_buffer(data); target = ctypes.create_string_buffer(size)
        if self.convert(source, len(data), width, height, source_format, target, size) != size:
            raise ValueError('quality texture conversion failed')
        return target.raw
