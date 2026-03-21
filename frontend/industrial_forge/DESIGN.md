# Design System Specification: Industrial Precision

## 1. Overview & Creative North Star
The Creative North Star for this design system is **"The Digital Foreman."** 

This is not a generic dashboard; it is a high-performance diagnostic instrument. The aesthetic shifts away from "flat software" toward "industrial equipment." We achieve this through a high-contrast, dark-mode environment that prioritizes legibility under shop-floor conditions while maintaining a premium, editorial feel. 

The system breaks the "template" look by utilizing **intentional asymmetry**—pairing rigid, monospaced data with bold, oversized cinematic typography. We use layered surfaces to create depth, mimicking the stacked interfaces of modern diagnostic hardware.

---

## 2. Colors & Tonal Depth
The palette is rooted in a "Carbon & Chrome" philosophy. We use dark, desaturated neutrals to allow the high-octane oranges to signal action and status.

### The "No-Line" Rule
**Borders are prohibited for sectioning.** To define high-level layout areas, use color-blocking. A `surface-container-low` section sitting against a `surface` background provides all the separation needed. Boundaries are felt through tonal shifts, not drawn with lines.

### Surface Hierarchy & Nesting
Treat the UI as a physical engine block where components are "milled" into the surface.
*   **Base Layer:** `surface` (#111318) - The main application background.
*   **Recessed Areas:** `surface-container-lowest` (#0c0e12) - For sidebars or utility panels.
*   **Elevated Components:** `surface-container-high` (#282a2e) - For cards and floating modals.

### The "Glass & Gradient" Rule
To inject "visual soul," primary CTAs and active progress states should use a **Signature Gradient** transitioning from `primary_container` (#f5a623) to `secondary_container` (#f26411). For floating elements, use `surface_variant` at 60% opacity with a `20px` backdrop-blur to create a "Smoked Glass" effect.

---

## 3. Typography
The typographic system creates a tension between "The Headline" (Brand Authority) and "The Data" (Technical Precision).

*   **Display & Headlines (Bebas Neue):** Used for KPIs, page titles, and urgent alerts. It should be tracked out slightly (+2% to +5%) to maintain an industrial, architectural feel.
*   **Body & Titles (DM Sans):** The workhorse. High x-height ensures readability in low-light environments. 
*   **Technical Data (JetBrains Mono):** Mandatory for VINs, Part Numbers, License Plates, and Sensor Readings. This font signals "Accuracy" to the user.

**Editorial Scale:** Use `display-lg` for primary shop metrics (e.g., "Daily Revenue") to create a focal point that dwarfs standard UI text, establishing a clear information hierarchy.

---

## 4. Elevation & Depth
We eschew traditional drop shadows in favor of **Tonal Layering.**

*   **The Layering Principle:** Instead of adding a shadow to a card, place a `surface-container-highest` card on top of a `surface-container-low` background. This creates a "machined" look where components appear perfectly fitted.
*   **Ambient Glows:** For "Urgent" or "Active" states, use an ambient glow. This is a shadow with a large spread (24px+) using the `primary` (#ffc880) color at 5-8% opacity. It should look like an LED status light reflecting off a dark metal surface.
*   **The "Ghost Border" Fallback:** If a container sits on a background of the same color, use a 1px border using `outline_variant` (#524534) at **15% opacity**. It should be felt, not seen.

---

## 5. Components

### KPI Cards
*   **Structure:** No borders. Background: `surface-container-high`.
*   **Data:** Large `display-md` value in `primary`. 
*   **Accent:** A 2px vertical "power-rail" on the left edge using the `secondary` color to denote the card's active state.

### Technical Tables
*   **Forbid Dividers:** Use `surface-container-low` for even rows and `surface` for odd rows to create separation.
*   **Header:** `label-sm` in `on_surface_variant`, all caps, high tracking.
*   **Data:** All alphanumeric IDs must use `JetBrains Mono`.

### Technical Status Badges
*   **Active:** `primary` text on `primary_container` (20% opacity).
*   **Diagnostic:** `tertiary` text on `surface_bright` (20% opacity).
*   **Urgent:** `error` text on `error_container` (30% opacity) with a subtle 4px blur glow.

### Input Fields & QR Containers
*   **Inputs:** Use `surface_container_highest` for the field body. 12px radius. The label should be "floating" above in `DM Sans` Medium.
*   **QR Containers:** Invert the logic. Use a `primary_fixed` (#ffddb4) background to ensure high contrast for scanners, even in dimly lit garages.

### Chat Bubbles & Progress Bars
*   **Chat:** Operator bubbles use `surface-container-highest`. Mechanic bubbles use `primary_container`. No tails; use the 12px radius throughout.
*   **Progress:** The "track" is `surface-container-lowest`. The "indicator" is a gradient from `primary` to `secondary`.

---

## 6. Do's and Don'ts

### Do:
*   **Do** use `JetBrains Mono` for any string that contains both letters and numbers (VINs).
*   **Do** leverage "Negative Space" as a separator. If two sections feel cluttered, increase the spacing scale to `12` (2.75rem) rather than adding a line.
*   **Do** use 12px (`DEFAULT`) corner radius for almost everything to maintain a "ruggedized tablet" feel.

### Don't:
*   **Don't** use pure white (#FFFFFF) for text. Use `on_surface` (#e2e2e8) to reduce eye strain in dark environments.
*   **Don't** use standard "Drop Shadows" (Black at 25%). They look muddy on dark backgrounds. Use Tonal Layering or Glows.
*   **Don't** use Bebas Neue for body copy. It is a "shouting" font meant for headlines and labels only.