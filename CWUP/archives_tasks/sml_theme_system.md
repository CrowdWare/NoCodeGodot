# Task 3: SML-Based Theme Definitions with Centralized Resources

## Goal

Keep the current theme system, but define Colors, Icons, and Layout
constants in SML and generate platform-specific theme files (e.g.,
theme.tres).

------------------------------------------------------------------------

## Motivation

Avoid redundant theme definitions (e.g., AccentColor defined multiple
times). Centralize design tokens in SML.

Example:

    Colors {
        primary: "#28A9E0"
        accent: "#28A9E0"
    }

    Layouts {
        default {
            padding: 12
            radius: 10
        }
    }

Usage:

    Button {
        color: @Colors.accent
        padding: @Layouts.default.padding
    }

------------------------------------------------------------------------

## Requirements

### 1. Central Design Tokens

-   Define Colors, Icons, Layout constants in SML.
-   All theme-relevant values must reference these tokens.

### 2. Theme Generator

-   Build-step converts SML Theme definitions into platform theme files.
-   Generator expands references into required duplicated values.

### 3. No Expressions

-   Only reference resolution.
-   No arithmetic or dynamic logic.

### 4. Deterministic Output

-   Same input SML must produce identical theme file output.
-   Stable ordering of generated properties.

### 5. Editor Support

-   Autocomplete for design tokens.
-   Highlight resource references consistently.

------------------------------------------------------------------------

## Acceptance Criteria

-   AccentColor defined once.
-   Generated theme file correctly reflects SML definitions.
-   No manual duplication required in theme source.
