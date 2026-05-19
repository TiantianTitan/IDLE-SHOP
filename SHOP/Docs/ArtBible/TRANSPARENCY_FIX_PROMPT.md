# Transparency Fix Prompt

Use this when ChatGPT returns an image with a gray/white checkerboard background baked into the pixels.

```text
Please fix this image only.

Problem:
- The image shows a gray/white checkerboard background, but it is not truly transparent.
- The checkerboard pattern is baked into the image pixels.
- Sometimes the background may appear as pure black, pure white, gray, or another solid color; that is also invalid.

Required fix:
- Remove the checkerboard background completely.
- Remove pure black, pure white, gray, colored, or any other background pixels completely.
- Remove all leftover white/gray fill inside internal cutout spaces, including between legs, inside arms, bag straps, hair gaps, and clothing gaps.
- Export as a PNG with a real alpha-transparent background for Unity Hub / Unity Editor sprite import.
- Preserve the character/object exactly.
- Do not change pose, proportions, colors, clothing, outline, lighting, or canvas size.
- Do not add a white box, gray canvas, drop shadow background, or fake transparency pattern.
- Do not add a black background, white background, gray background, colored background, or any display-only backdrop.
- No text, no watermark.

Output:
- PNG
- Real alpha transparency
- No background pixels at all
- Same canvas size
- Subject unchanged
```

## Acceptance

Reject the file if any checkerboard, black background, white rectangle, gray canvas, colored background, or background texture remains visible in the PNG.
Reject the file if internal negative spaces such as between legs, arm gaps, bag strap gaps, or hair gaps contain white/gray pixels instead of alpha transparency.
