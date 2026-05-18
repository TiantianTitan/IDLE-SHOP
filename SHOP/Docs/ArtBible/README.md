# Idle Shop Art Bible

This folder defines the art production rules for MVP 3.0.

The goal is asset continuity. New art must be generated from approved master assets, not as unrelated one-off images.

## Roles

Codex owns:

- Asset folder structure and naming.
- Unity loading tables and fallback behavior.
- Gameplay state mapping to sprites and effects.
- Validation checklists before an asset batch is accepted.

ChatGPT owns:

- AI image prompts based on this bible.
- Iterating generated images against master assets.
- Producing action frames, UI states, and effects that match the approved style.

The user owns:

- World direction and taste decisions.
- Approving master assets.
- Deciding when a batch is accepted for integration.

## Production Flow

1. Approve a master asset.
2. Store it under `Assets/Art/MVP30/.../Base/`.
3. Generate state variants only from that master.
4. Place generated variants in the matching state folder.
5. Codex wires the variants to gameplay states.
6. Test in Unity for readability, scale, transparency, and timing.

## MVP 3.0 Priorities

1. Customer animation set: enter, idle, pay, happy, leave.
2. Cashier animation set: idle, work, success.
3. Shelf state set: full, low, empty, selected, restocked.
4. Order UI set: current order, selected item, complete stamp, progress states.
5. FX set: cash float, restock success, order complete glow, upgrade success.

Do not add more product or employee systems until the above visual loop reads clearly.
