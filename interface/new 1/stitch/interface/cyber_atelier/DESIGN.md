# Design System Strategy: The Cyber-Atelier Framework

## 1. Overview & Creative North Star
**Creative North Star: "The Industrial Alchemist"**

The design system is not a standard dashboard; it is a high-precision digital workshop. We are moving away from the "flat web" and toward a multi-layered, tactile environment that feels like a laser-etched glass interface resting on a heavy industrial workbench. 

The aesthetic prioritizes **Intentional Asymmetry**. Rather than a rigid, centered grid, we utilize weighted layouts where primary data (AI Gauges) anchors one side, while secondary status elements "float" in the peripheral. By overlapping frosted glass surfaces with sharp, neon accents, we create a sense of three-dimensional depth that feels both sophisticated and high-tech.

---

## 2. Colors & Atmospheric Depth

This system utilizes a high-contrast palette to simulate a dark-mode, heads-up display (HUD).

### The "No-Line" Rule
**Explicit Instruction:** Do not use 1px solid borders for sectioning. Structural definition must be achieved through background shifts. Use `surface_container_low` for the main canvas and `surface_container_high` for interactive zones. Boundaries are felt through tonal contrast, not drawn with lines.

### Surface Hierarchy & Nesting
Treat the UI as a physical stack of semi-transparent materials:
- **Base Layer:** `surface_dim` (#0e0e0e) for the global background.
- **Mid Layer:** `surface_container` (#1a1919) for large content areas.
- **Top Layer:** `surface_container_highest` (#262626) for active cards and modals.

### The Glass & Gradient Rule
For floating elements (like `OsShell` or `Quick Tiles`), apply a backdrop-blur (12px–20px) to the `surface_variant` token at 60% opacity. 
- **CTAs:** Use a "Laser Gradient" transitioning from `primary` (#ff9159) to `primary_container` (#ff7a2f) at a 45-degree angle to provide a machined, metallic sheen.

---

## 3. Typography: Technical Authority

The typography system creates a "Blueprint" aesthetic by pairing technical, geometric headers with highly readable body text.

*   **Display & Headlines (Rajdhani/SpaceGrotesk):** Use these for data visualization and "Cyber-Atelier" titles. The condensed, squared-off nature of these fonts conveys engineering precision.
    *   *Display-LG:* 3.5rem. Use for high-impact AI confidence scores.
*   **Body & Titles (Inter):** Use for all functional UI text. Inter’s neutral, clean apertures balance the aggression of the Rajdhani headers.
    *   *Body-MD:* 0.875rem. Use for technical descriptions and status logs.
*   **Labels (Inter):** 0.75rem. Use for micro-data and "Quick Tile" descriptors.

---

## 4. Elevation & Depth: Tonal Layering

We avoid traditional "material" shadows in favor of **Luminescent Depth**.

*   **The Layering Principle:** Depth is achieved by "stacking" surface tiers. An `OsShell` component should utilize `surface_bright` (#2c2c2c) at 40% opacity with a blur, creating a "lift" from the dark industrial background without a single shadow.
*   **Ambient Glows:** Instead of drop shadows, use `secondary` (#00eefc) at 5%-10% opacity as an outer glow for active "Quick Tiles." This simulates neon light reflecting off a dark metal surface.
*   **The Ghost Border:** If high-contrast accessibility is required, use `outline_variant` at 15% opacity. It should feel like a faint laser etching, not a solid stroke.
*   **Glassmorphism:** All modal surfaces must use a semi-transparent `surface_container` with a `backdrop-filter: blur(15px)`. This integrates the UI into the environment, making it feel like a projection rather than an overlay.

---

## 5. Components

### OsShell (System Bar)
The top bar must be a full-width "Glass" element. 
- **Style:** Background `surface_container_low` at 70% opacity + backdrop blur. 
- **Detail:** Use a `primary` (#ff9159) 2px "Laser-Sweep" underline that animates when the system is processing data.

### Quick Tiles
- **Structure:** `md` roundedness (0.375rem). 
- **State:** Inactive tiles use `surface_container_highest`. Active tiles use a `secondary` (#00eefc) glow-border (Ghost Border style).

### AI Confidence Gauges
- **Visual:** Don't use standard progress bars. Use concentric rings using `secondary` for "Confidence" and `primary` for "Critical Alerts." 
- **Typography:** Central numeric value in `display-sm` (Rajdhani).

### Input Fields
- **Style:** Underline-only (Ghost Border style) using `outline`. 
- **Active State:** The underline transforms into a `primary` color glow. No background fill—let the surface-container show through.

### Buttons
- **Primary:** Gradient fill (`primary` to `primary_dim`). White text (`on_primary_fixed`).
- **Secondary:** Transparent background with a `secondary` 10% opacity glow and `secondary` text.
- **Tertiary:** No border, no background. `on_surface_variant` text.

---

## 6. Do's and Don'ts

### Do:
- **Use Vertical White Space:** Use the `16` (3.5rem) or `20` (4.5rem) spacing tokens to separate major modules.
- **Micro-interactions:** Add a 200ms "Laser-Sweep" (a moving linear gradient) to components when they transition from idle to active.
- **Color Intent:** Use `secondary` (#00F0FF) exclusively for "System Health" and "Data." Use `primary` (#FF6B00) for "Action" and "Power."

### Don't:
- **No Solid Outlines:** Never wrap a card in a #FFFFFF or #000000 1px border. Use background contrast.
- **No Standard Grids:** Avoid perfectly symmetrical 4-column layouts. Try a 2/3 - 1/3 split to create an editorial, high-end dashboard feel.
- **No Pure Greys:** Ensure all dark surfaces use the `surface` palette (#0e0e0e), which has a microscopic hint of industrial warmth to prevent the screen from looking "dead."