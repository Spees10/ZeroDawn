# Design System Specification: The Deep Ocean Editorial

## 1. Overview & Creative North Star
**Creative North Star: The Silent Authority**

This design system moves away from the frantic, high-contrast layouts of typical SaaS products toward a "Blue Ocean" editorial experience. It is designed to feel like a premium digital ledger—calm, expansive, and deeply professional. We achieve this through **Organic Asymmetry** and **Tonal Depth**. By utilizing a "matte ivory" background with lavender undertones, we create a canvas that feels more like expensive stationary than a digital screen. 

The system rejects the "boxed-in" look of standard grids. Instead, we use breathing room (negative space) as a structural element, allowing content to sit on layered surfaces that mimic the natural stratification of deep water. It is optimized for **RTL-first environments**, ensuring that the visual weight and typographic flow remain authoritative and balanced when read from right to left.

---

## 2. Colors: Tonal Stratification

The palette is anchored in deep ocean blues and sophisticated teals, set against an airy, lavender-tinted white.

### The Palette (Key Tokens)
*   **Primary (`#4456a6`):** The "Deep Ocean" blue. Used for primary actions and key brand moments.
*   **Secondary (`#3b665d`):** A muted, professional teal. Used for supporting information and secondary actions.
*   **Tertiary (`#006955`):** A high-intent sea green, used sparingly for success states or specialized highlights.
*   **Background (`#fbf8ff`):** The "Matte Ivory." A lavender-tinted white that provides a soft, non-fatiguing backdrop.

### The "No-Line" Rule
Traditional 1px borders are strictly prohibited for sectioning. Contrast and containment must be achieved through **background color shifts**. 
*   Place a `surface_container_low` section against a `surface` background to define a zone.
*   Use `surface_container_highest` to draw the eye to the most critical interactive areas.

### Signature Textures & Glassmorphism
To create a "High-End" feel, use **Backdrop Blurs**. Floating elements (like navigation bars or modals) should use a semi-transparent `surface` color with a `blur-md` effect. This allows the subtle lavender-blue of the background to bleed through, creating a sense of environmental integration.

---

## 3. Typography: The Ink-on-Paper Feel
We utilize **Manrope** across all scales. The goal is "Ink-on-Paper" clarity—high legibility with a sophisticated, editorial weight.

*   **Display Scales (Lg: 3.5rem / Md: 2.75rem):** Use sparingly for hero moments. These should feel like magazine headlines—bold, authoritative, and given ample white space.
*   **Headline & Title (Lg: 2rem / Md: 1.75rem):** The backbone of the hierarchy. Use `on_surface` to maintain a sharp, high-contrast "ink" look against the light backgrounds.
*   **Body (Md: 0.875rem):** Optimized for long-form reading. Use a slightly relaxed line height to enhance the "airy" feel of the system.
*   **Labels (Md: 0.75rem):** Used for utility. In RTL contexts, ensure labels are aligned to the right, maintaining a strong vertical "spine" for the layout.

---

## 4. Elevation & Depth: Tonal Layering
We do not use structural lines to separate content. Depth is achieved via **Tonal Stacking**.

*   **The Layering Principle:** Imagine the UI as stacked sheets of fine paper. 
    *   **Base:** `surface` (The canvas).
    *   **Sectioning:** `surface_container_low` (Subtle grouping).
    *   **Cards:** `surface_container_lowest` (Floating/High-priority content).
*   **Ambient Shadows:** If a card must "float," use a shadow that is virtually invisible. 
    *   *Specification:* Blur: 24px - 40px | Opacity: 4% - 6% | Color: A tint of `on_surface`. 
    *   Avoid grey shadows; shadows should feel like light being absorbed by the lavender background.
*   **The "Ghost Border" Fallback:** If accessibility requires a border, use `outline_variant` at **15% opacity**. It should be felt rather than seen.

---

## 5. Components

### Buttons
*   **Primary:** Solid `primary` background with `on_primary` (white) text. Use `ROUND_FOUR` (0.25rem) for a precise, "product-grade" corner.
*   **Secondary:** `secondary_container` background with `on_secondary_container` text. This provides a soft, low-contrast alternative for less critical actions.
*   **Tertiary:** Ghost style. No background, `primary` text. Transitions to a subtle `surface_variant` on hover.

### Input Fields
*   **Style:** Avoid heavy borders. Use a `surface_container_high` background with a 1px "Ghost Border" at the bottom or a very subtle `outline_variant`.
*   **RTL Focus:** Icons must be mirrored. The "Search" magnifying glass sits on the left for RTL, with the text baseline starting from the right.

### Cards & Lists
*   **The Divider Rule:** Divider lines are forbidden. Use **Vertical Space (Spacing 6: 2rem)** or a transition from `surface_container` to `surface` to denote list items.
*   **Cards:** Should utilize the `surface_container_lowest` token to create a "lifted" appearance without needing heavy shadows.

### Navigation (Signature Component)
*   **The Floating Rail:** Use a floating sidebar or bottom bar with a `Glassmorphism` effect. The background should be `surface` at 80% opacity with a 20px blur. This creates a high-end, modern depth.

---

## 6. Do's and Don'ts

### Do:
*   **Do** use asymmetrical margins to create an editorial, "non-template" look.
*   **Do** rely on the Spacing Scale (especially 8, 10, and 12) to give the content room to breathe.
*   **Do** use `primary_container` for large background areas that need to feel authoritative.
*   **Do** ensure all iconography is optical-weight-matched to the Manrope body text.

### Don't:
*   **Don't** use 100% black (`#000000`). Always use `on_surface` (`#161a2e`) for a deep, midnight-ink feel.
*   **Don't** use 1px solid borders for layout containers.
*   **Don't** use "Drop Shadows" from standard UI libraries. Use the Ambient Shadow specification provided.
*   **Don't** crowd the interface. If it feels busy, increase the spacing by two increments on the scale.