# Design tokens

Unity-style admin UI (dark). Keep in sync with `src/index.css` `@theme` and `.dark` blocks.

## Colors (from Unity editor theme)

| Token | Hex |
| --- | --- |
| bg.app | `#1e1e1e` |
| bg.panel | `#252526` |
| bg.panel-alt | `#2d2d30` |
| border | `#3f3f46` |
| text.primary | `#ddd` |
| text.secondary | `#9b9b9b` |
| **accent** | **`#00AD09`** |
| danger | `#d54545` |

**Accent hover** (UI only, not in RTF source): `#14c417`

## Typography

- UI font: Inter (18px light baseline)
- Mono: JetBrains Mono
- Base size: 11px
- Headers: 14px / 12px / 11px

## Components

- Inputs: 24–28px tall, 1px border, 2px radius
- Buttons: same height, accent fill
- Tables: 28px row height, 1px row dividers, hover `#2a2d2e`
- Trees: indent 16px per level, twisty arrow on hover
- Offsets: 4px between buttons, tables, panels

## Transactional email

HTML emails mirror these tokens via `EduCollab.Application/Services/Notifications/EmailDesignTokens.cs` (inline styles; keep in sync when tokens change).
