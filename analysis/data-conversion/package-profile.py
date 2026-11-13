#!/usr/bin/env python3
"""Package one Megabonk account's existing encrypted saves for Android import."""
import argparse
import os
from pathlib import Path
import zipfile

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("account_directory", type=Path, help="directory containing progression.json and stats.json")
parser.add_argument("output_zip", type=Path, help="new ZIP to create (must not already exist)")
args = parser.parse_args()
source = args.account_directory
files = [source / "progression.json", source / "stats.json"]
for file in files:
    if not file.is_file() or not 0 < file.stat().st_size <= 8 * 1024 * 1024:
        parser.error(f"missing or invalid file: {file.name}")

# Exclusive creation ensures an existing archive or save is never overwritten.
fd = os.open(args.output_zip, os.O_CREAT | os.O_EXCL | os.O_WRONLY, 0o600)
try:
    with os.fdopen(fd, "wb") as output, zipfile.ZipFile(output, "w", zipfile.ZIP_DEFLATED) as archive:
        for file in files:
            archive.write(file, "profile/" + file.name)
except BaseException:
    try:
        args.output_zip.unlink()
    except OSError:
        pass
    raise
print("Profile ZIP created; original files were not modified.")
