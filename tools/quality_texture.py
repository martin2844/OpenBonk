"""Desktop C interface to the experimental deterministic ETC2 quality encoder."""
import ctypes
from pathlib import Path


class QualityEncoder:
    def __init__(self, library, rgba=False, extra=0):
        if type(extra) is not int or extra not in (0, 1, 2, 3) or (rgba and extra): raise ValueError('invalid quality metric')
        path = Path(library).expanduser()
        if path.is_symlink() or not path.is_file(): raise ValueError('quality library must be a regular file')
        self.path = path.resolve(strict=True)
        self.library = ctypes.CDLL(str(self.path))
        self.extra = extra
        try: self.convert = getattr(self.library, 'openbonk_quality_extra_mip' if extra else 'openbonk_quality_rgba_mip' if rgba else 'openbonk_quality_mip')
        except AttributeError as error: raise ValueError('library has no quality encoder') from error
        self.convert.argtypes = [ctypes.c_void_p, ctypes.c_size_t, ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_void_p, ctypes.c_size_t]
        if extra: self.convert.argtypes.append(ctypes.c_int)
        self.convert.restype = ctypes.c_int

    def rgba_variant(self):
        return QualityEncoder(self.path, rgba=True)

    def extra_variant(self, metric):
        return QualityEncoder(self.path, extra=metric)

    def encode(self, data, width, height, source_format):
        if (not isinstance(data, bytes) or not data or len(data) > 64 * 1024 * 1024 or
                type(width) is not int or type(height) is not int or not 1 <= width <= 2048 or not 1 <= height <= 2048 or
                type(source_format) is not int or source_format not in (1, 3, 4, 10, 12, 25)):
            raise ValueError('invalid quality texture input')
        size = ((width + 3) // 4) * ((height + 3) // 4) * 16
        source = ctypes.create_string_buffer(data); target = ctypes.create_string_buffer(size)
        args = [source, len(data), width, height, source_format, target, size]
        if self.extra: args.append(self.extra)
        if self.convert(*args) != size:
            raise ValueError('quality texture conversion failed')
        return target.raw
