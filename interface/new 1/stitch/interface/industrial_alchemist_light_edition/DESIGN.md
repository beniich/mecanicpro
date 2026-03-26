# Design System: Industrial Alchemist — Light Edition

## 1. Overview & Creative North Star
The Creative North Star for this design system is **"The Technical Atelier."** 

This is not a generic utility dashboard; it is a high-precision digital instrument designed for the bright, demanding environment of a modern workshop. It balances the "Industrial"—sharp lines, technical typography, and high contrast—with the "Alchemist"—ethereal glassmorphism, layered depth, and airy compositions. 

To break the "template" look, we move away from boxed-in layouts. Instead, we utilize **intentional asymmetry** and **tonal layering**. Elements should feel like precision-cut sheets of glass and metal resting on a blueprint. By using a "Submerged Grid" (subtle patterns on `#F8FAFC`), we provide a technical anchor that allows the UI elements to float with purpose.

---

## 2. Colors & Surface Architecture

The palette is anchored by a high-clarity neutral base, punctuated by high-energy industrial accents.

### The Palette
*   **Primary (Industrial Energy):** `primary` (#A04100) and `primary_container` (#FF6B00 / MecaPro Orange). Used for critical actions and brand presence.
*   **Secondary (Technical Precision):** `secondary` (#006970) and `secondary_container` (#12F1FF / Cyan). Used for data visualization and secondary status indicators.
*   **Background:** `background` (#F7F9FB). A clinical, off-white workspace.

### The "No-Line" Rule
Traditional 1px solid borders are strictly prohibited for sectioning. Boundaries must be defined through:
1.  **Tonal Shifts:** Placing a `surface_container_low` element against the `background`.
2.  **Glassmorphism:** Using semi-transparent surfaces with a 12px–20px `backdrop-blur`.

### Surface Hierarchy & Nesting
Treat the UI as a physical stack of materials. 
*   **Level 0 (The Floor):** `background` (#F7F9FB) with a subtle dot-grid pattern.
*   **Level 1 (The Bench):** `surface_container_low` for large content areas.
*   **Level 2 (The Tool):** `surface_container_lowest` (#FFFFFF) for primary cards, creating a "lifted" look.
*   **Level 3 (The Lens):** Floating overlays using white glassmorphism (Surface color at 70% opacity + blur).

### Signature Textures
Apply a subtle linear gradient to `primary_container` CTAs (e.g., `#FF6B00` to `#FF8533`) at a 135-degree angle. This prevents the orange from feeling "flat" and adds a machined, metallic sheen.

---

## 3. Typography: Technical Authority

We use **Space Grotesk** for its idiosyncratic, technical character and a **Monospace** face for raw data.

*   **Display & Headlines:** Bold and unapologetic. Use `display-lg` (3.5rem) with tight tracking (-0.02em) for hero metrics. The sharp terminals of Space Grotesk mirror industrial cutting tools.
*   **Title & Body:** `title-md` (1.125rem) should be used for section headers to maintain high legibility in bright environments.
*   **Data Layers:** All numerical values, coordinates, and timestamps must use a Monospace font. This ensures "tabular lining," where numbers align perfectly in columns for quick scanning.
*   **Labeling:** `label-sm` (0.6875rem) in all-caps with +0.05em tracking for metadata, mimicking technical drawings.

---

## 4. Elevation & Depth

### The Layering Principle
Depth is achieved through **Tonal Layering**. To highlight a module, do not reach for a shadow first; instead, shift its container to `surface_container_lowest` (#FFFFFF) against a `surface_container` background. This creates a "soft-edge" separation that feels premium and intentional.

### Ambient Shadows
When an element must float (e.g., a modal or a floating action button), use an **Ambient Shadow**:
*   **Y-Offset:** 8px | **Blur:** 24px
*   **Color:** `on_surface` (#191C1E) at **4% opacity**.
This mimics natural light diffusion in a cleanroom rather than a harsh digital drop shadow.

### Glassmorphism & Depth
For overlays, use the "Alchemist" effect:
*   **Fill:** `surface_container_lowest` at 60-80% opacity.
*   **Backdrop Blur:** 16px.
*   **Ghost Border:** A 1px stroke using `outline_variant` (#E2BFB0) at 15% opacity to catch the light on the "edge" of the glass.

---

## 5. Components

### Buttons
*   **Primary:** `primary_container` (#FF6B00) background, `on_primary_container` (#572000) text. Sharp 0px corners. High-energy, high-contrast.
*   **Secondary:** `secondary_container` (#12F1FF) background. Use for "System Ready" or "Active" states.
*   **Tertiary:** No background. Bold Space Grotesk text with a `primary` underline that appears only on hover.

### Cards & Lists
*   **No Dividers:** Forbid the use of horizontal lines. Use **8 (2rem)** or **10 (2.5rem)** spacing from the scale to separate list items.
*   **Nesting:** Place `surface_container_highest` headers inside `surface_container_lowest` cards to create internal hierarchy.

### Input Fields
*   **Style:** Underline only. Use `outline` (#8E7164) for the bottom border (2px).
*   **Focus State:** The border transitions to `primary` (#FF6B00) with a 4px vertical "pulse" at the start of the input.

### Technical Chips
*   **Visuals:** Rectangular, 0px radius. Background: `surface_container_high`. 
*   **Interaction:** On selection, background flips to `primary` and text to `on_primary`.

---

## 6. Do's and Don'ts

### Do
*   **DO** use whitespace as a structural element. If a section feels cluttered, increase the padding rather than adding a border.
*   **DO** ensure all interactive elements have a "Sharp" profile (0px border-radius).
*   **DO** use Cyan (`secondary_container`) sparingly for "Read-Only" data or "System Healthy" states to contrast against the Action-oriented Orange.

### Don't
*   **DON'T** use rounded corners. This system is "Industrial"—it values the precision of a 90-degree angle.
*   **DON'T** use pure black for shadows. Always use a low-opacity tint of the surface color to maintain the "Airy" personality.
*   **DON'T** use standard icons. Opt for thin-stroke (1px or 1.5px) technical icons that match the weight of the Monospace data.

### Accessibility Note
In high-glare workshop environments, contrast is king. Always validate that `on_surface` text against `glass` overlays meets a minimum 4.5:1 ratio. If the background pattern is too busy, increase the opacity of the glass layer.