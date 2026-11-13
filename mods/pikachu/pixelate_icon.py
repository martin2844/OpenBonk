"""Turn the Blender face render into a transparent, pixel-style game portrait."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter

root = Path(__file__).resolve().parent
source = Image.open(root / 'study/portrait-face-render.png').convert('RGBA')
small = source.resize((32, 32), Image.Resampling.LANCZOS)
original_alpha = small.getchannel('A')
rgb = ImageEnhance.Color(small.convert('RGB')).enhance(1.45)
rgb = ImageEnhance.Contrast(rgb).enhance(1.18)
small = rgb.convert('RGBA'); small.putalpha(original_alpha)
# The miniature face loses the source model's fine mouth at icon scale;
# redraw that detail on the pixel grid and remove the thin tail silhouette
# that otherwise sticks out between the ears in this fixed portrait view.
draw = ImageDraw.Draw(small)
draw.rectangle((17, 0, 18, 3), fill=(0, 0, 0, 0))
draw.point((15, 13), fill=(56, 35, 25, 255))
draw.line([(11, 15), (13, 16), (15, 15), (17, 16), (19, 15)], fill=(91, 48, 24, 255), width=1)
alpha = small.getchannel('A').point(lambda a: 255 if a >= 96 else 0)
outline = alpha.filter(ImageFilter.MaxFilter(3))
background = Image.new('RGBA', small.size, (20, 15, 23, 0))
background.putalpha(outline)
small.putalpha(alpha)
background.alpha_composite(small)
background.resize((384, 384), Image.Resampling.NEAREST).save(root / 'content/portrait.png')
background.resize((384, 384), Image.Resampling.NEAREST).save(root / 'study/portrait-pixel-preview.png')
