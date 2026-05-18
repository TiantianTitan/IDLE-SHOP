# Transparency Fix Prompt

Use this when ChatGPT returns an image with a gray/white checkerboard background baked into the pixels.

```text
Please fix this image only.

Problem:
- The image shows a gray/white checkerboard background, but it is not truly transparent.
- The checkerboard pattern is baked into the image pixels.

Required fix:
- Remove the checkerboard background completely.
- Remove all leftover white/gray fill inside internal cutout spaces, including between legs, inside arms, bag straps, hair gaps, and clothing gaps.
- Export as a PNG with a real alpha-transparent background.
- Preserve the character/object exactly.
- Do not change pose, proportions, colors, clothing, outline, lighting, or canvas size.
- Do not add a white box, gray canvas, drop shadow background, or fake transparency pattern.
- No text, no watermark.

Output:
- PNG
- Real alpha transparency
- Same canvas size
- Subject unchanged
```

## Acceptance

Reject the file if any checkerboard, white rectangle, gray canvas, or background texture remains visible in the PNG.
Reject the file if internal negative spaces such as between legs, arm gaps, bag strap gaps, or hair gaps contain white/gray pixels instead of alpha transparency.
