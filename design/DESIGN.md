# Dokkana Design System — "Souk Modern"

This is the **single source of truth** for how Dokkana looks. Every Angular component
must use these tokens. The visual reference is [`index.html`](index.html) — open it in a
browser to see the system applied.

> One direction, locked. Do not introduce new colors, fonts, or radii. If something is
> missing here, add it to this file first, then use it.

---

## 1. Brand essence

Warm, local, and modern. Dokkana is the neighborhood shop (دكانة) brought online: it
should feel inviting and Egyptian, not like a generic global SaaS or a cold luxury
boutique. Editorial serif headlines + clean UI + a warm clay palette. Teal is reserved
**only** for AI moments so the intelligence feels special.

---

## 2. Color

Values are **OKLCH** (authoritative). Hex equivalents are approximate, for tools that
need them. Tailwind v4 and modern browsers accept OKLCH directly.

### Core neutrals
| Token | OKLCH | Hex (approx) | Use |
|---|---|---|---|
| `sand` | `oklch(0.968 0.013 78)` | `#F7F4EE` | Page background |
| `cream` | `oklch(0.993 0.008 82)` | `#FEFCF8` | Raised surfaces, cards |
| `ink` | `oklch(0.255 0.022 48)` | `#2B2620` | Primary text, dark sections |
| `ink-soft` | `oklch(0.46 0.020 52)` | `#6E665B` | Secondary text |
| `border` | `oklch(0.89 0.016 72)` | `#E4DED3` | Hairlines, card borders |
| `drench` | `oklch(0.265 0.028 46)` | `#2E2720` | "For Store Owners" dark band |

### Brand + accents
| Token | OKLCH | Hex (approx) | Use |
|---|---|---|---|
| `clay` ⭐ | `oklch(0.62 0.15 42)` | `#C26B43` | **Primary brand.** CTAs, links, highlights |
| `clay-deep` | `oklch(0.50 0.135 40)` | `#9E5232` | Hover/pressed, prices |
| `nile` | `oklch(0.61 0.085 195)` | `#4F9D99` | **AI only:** search focus, assistant, AI badges |
| `nile-deep` | `oklch(0.45 0.075 198)` | `#2F6F6E` | AI gradient end |
| `ochre` | `oklch(0.80 0.115 82)` | `#D9A441` | Badges, ratings, chart highlights |
| `hibiscus` | `oklch(0.56 0.17 22)` | `#BF4A3D` | Sale, "favorite" heart, low-stock alerts |

### Color rules
- **Strategy = committed.** Clay carries the brand; everything else is neutral. One
  saturated accent dominates. Do not rainbow.
- **Teal (`nile`) is sacred to AI.** Only AI search, the assistant, and AI badges use it.
  A normal button is never teal.
- **Never pure `#000` / `#fff`.** Every neutral is tinted warm (see values above).
- Text on `clay`/`drench`/`ink` = `cream`. Text on `sand`/`cream` = `ink`.
- Dark mode: invert to a charcoal base (`oklch(0.19 0.018 238)`), keep clay + nile accents.

---

## 3. Typography

Load from Google Fonts:
```html
<link href="https://fonts.googleapis.com/css2?family=Fraunces:ital,opsz,wght@0,9..144,400;0,9..144,500;0,9..144,600;1,9..144,500&family=Plus+Jakarta+Sans:wght@400;500;600;700&family=IBM+Plex+Sans+Arabic:wght@400;500;600;700&display=swap" rel="stylesheet">
```

| Role | Font | Use |
|---|---|---|
| Display | **Fraunces** (serif) | Headlines, prices, big numbers. Weight 500, optical sizing on |
| UI / body | **Plus Jakarta Sans** | Everything else: buttons, nav, body, labels |
| Arabic | **IBM Plex Sans Arabic** | All Arabic text (set on `[dir="rtl"]`) |

### Scale (≥1.25 ratio between steps)
| Step | Size | Weight | Font |
|---|---|---|---|
| Hero | `clamp(40px, 5.6vw, 68px)` | 500 | Fraunces |
| Section title | `clamp(30px, 4vw, 46px)` | 500 | Fraunces |
| H3 | 23px | 600 | Jakarta |
| Body large | 18px | 400 | Jakarta |
| Body | 15–16px | 400 | Jakarta |
| Label / caption | 12.5–13px | 500–600 | Jakarta |

- Body line length capped at **65–75ch**.
- Hero serif may use *italic* for the emphasized word, colored `clay`.

---

## 4. Spacing, radius, elevation, motion

```
Spacing scale (px):   4 · 8 · 12 · 16 · 22 · 28 · 40 · 56 · 96
Container max width:  1180px, side padding 28px

Radius:   pill 999px (buttons/chips) · 24px (cards/search) · 18px (product cards) · 14px (inputs/results)

Shadow:   --shadow:    0 1px 2px oklch(0.30 0.03 50/.05), 0 8px 24px oklch(0.30 0.03 50/.07)
          --shadow-lg: 0 2px 6px oklch(0.30 0.03 50/.07), 0 24px 60px oklch(0.30 0.03 50/.12)

Motion:   --ease: cubic-bezier(0.16, 1, 0.3, 1)   (ease-out-expo, no bounce)
          Reveal: opacity 0→1 + translateY 26px→0, ~0.9s
          Hover lift: translateY(-2px) buttons, translateY(-6px) cards
          Only animate transform + opacity. Respect prefers-reduced-motion.
```

---

## 5. Components (specs)

- **Button / primary** — bg `clay`, text `cream`, pill, padding 11×22, hover lift + glow.
- **Button / dark** — bg `ink`, text `sand`. Used for the main nav CTA.
- **Button / ghost** — text link with arrow that slides on hover.
- **Search (AI)** — `cream` field, 1.5px border; on focus border `nile` + 4px teal glow ring.
- **Chip** — `sand` bg, `border`, pill; hover border + text turn `clay`.
- **Product card** — `cream`, radius 18, border. Image panel = duotone of the item's
  color, garment SVG centered. Body: name (600), store (with shop icon), price in Fraunces
  `clay-deep` + "EGP", color swatches. Hover: lift + image icon scales. Vary card sizes,
  never an identical grid.
- **Badge** — small pill, `cream`/85% bg, for category tags and the AI "✦" mark.
- **Chat bubble** — user = `drench-soft`; AI = lighter charcoal. Used for the copilot.
- **Nav** — transparent, gains frosted `sand`/85% bg + bottom border on scroll.

### Hard bans (these make it look AI-generated)
- No colored `border-left`/`border-right` accent stripes. Use full borders or bg tints.
- No gradient text (`background-clip:text`). Emphasis via weight/size/`clay`.
- No glassmorphism by default. No identical icon-card grids. No modals as first choice.

---

## 6. Bilingual / RTL

- Every translatable element carries `data-en` and `data-ar`; a toggle swaps `textContent`
  and sets `<html dir>` + `lang`.
- `[dir="rtl"]` switches the font stack to IBM Plex Sans Arabic.
- Mirror directional things: arrows, card badge corners, chat bubble tails, margins.
- The wordmark always shows both: **Dokkana** · دكانة.

---

## 7. Tailwind config (drop into the Angular app)

```js
// tailwind.config.js  →  theme.extend
colors: {
  sand:'oklch(0.968 0.013 78)', cream:'oklch(0.993 0.008 82)',
  ink:'oklch(0.255 0.022 48)', 'ink-soft':'oklch(0.46 0.020 52)',
  line:'oklch(0.89 0.016 72)', drench:'oklch(0.265 0.028 46)',
  clay:{ DEFAULT:'oklch(0.62 0.15 42)', deep:'oklch(0.50 0.135 40)' },
  nile:{ DEFAULT:'oklch(0.61 0.085 195)', deep:'oklch(0.45 0.075 198)' },
  ochre:'oklch(0.80 0.115 82)', hibiscus:'oklch(0.56 0.17 22)',
},
fontFamily: {
  display:['Fraunces','Georgia','serif'],
  sans:['"Plus Jakarta Sans"','system-ui','sans-serif'],
  ar:['"IBM Plex Sans Arabic"','sans-serif'],
},
borderRadius:{ card:'24px', product:'18px', field:'14px' },
boxShadow:{
  soft:'0 1px 2px oklch(0.30 0.03 50/.05),0 8px 24px oklch(0.30 0.03 50/.07)',
  lg:'0 2px 6px oklch(0.30 0.03 50/.07),0 24px 60px oklch(0.30 0.03 50/.12)',
},
transitionTimingFunction:{ out:'cubic-bezier(0.16,1,0.3,1)' },
```

---

## 8. How the team uses this

1. Open [`index.html`](index.html) to see the system in action (it's the visual spec).
2. Paste the Tailwind config above into the Angular app.
3. Build each Angular component from the specs in section 5, using only these tokens.
4. The other prototypes (`dokkana-2026`, `dokkana-demo-v2`, Panto) are **archived
   references only** — borrow flow/layout ideas, but the colors, fonts, and components
   come from THIS file.
