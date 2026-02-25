# Task 4: Extend Syntax Highlighting for Attached Properties and Resource References

## Goal

Enhance the SML syntax highlighter to properly recognize and colorize:

-   Attached Properties (e.g., `DockContainer.title`)
-   ID-based Attached Properties (e.g., `container.title`)
-   Resource References using `@Namespace.path`
-   Full qualified reference strings (e.g., `@Colors.primary`)

------------------------------------------------------------------------

## Motivation

With the introduction of:

-   Attached Properties
-   Namespaced Resource References
-   Localization bindings

The current syntax highlighter no longer reflects the semantic structure
of SML.

Proper highlighting is required to:

-   Improve readability
-   Differentiate structure from metadata
-   Strengthen the language identity of SML
-   Support future autocomplete and refactoring features

------------------------------------------------------------------------

## Requirements

### 1. Token Categories to Introduce

#### A. AttachedPropertyKey

Pattern:

    Identifier "." Identifier ":"

Examples:

    DockContainer.title:
    container.title:

Highlighting: - Entire qualified part (`DockContainer.title` or
`container.title`) in a distinct color. - Suggested: Blue.

------------------------------------------------------------------------

#### B. ResourceReference

Pattern:

    @Namespace.path

Examples:

    @Colors.primary
    @Layouts.default.padding
    @Strings.window.caption

Highlighting: - The `@` symbol highlighted distinctly (suggested:
blue). - The full qualified reference (`@Colors.primary`) highlighted
consistently. - Suggested: entire reference in green or blue (consistent
choice required).

------------------------------------------------------------------------

#### C. ResourceReference with Fallback

Pattern:

    @Resource.path, "Fallback"

Highlighting rules: - `@Resource.path` highlighted as
ResourceReference. - Fallback literal highlighted as normal string
literal. - Comma treated as neutral delimiter.

------------------------------------------------------------------------

### 2. Lexer Rules

Before colon detection: - If token matches
`Identifier "." Identifier ":"` → classify as AttachedPropertyKey

Before value parsing: - If token starts with `@` followed by
`Identifier("."Identifier)*` → classify as ResourceReference

Must not conflict with: - Decimal numbers - File paths - Standard string
literals

------------------------------------------------------------------------

### 3. Color Design Guidelines

Recommended scheme:

-   AttachedPropertyKey → Blue
-   ResourceReference → Green (or Blue for consistency)
-   `@` symbol → Same color as reference
-   Normal properties → Default property color
-   String literals → Existing string color

Important: The entire reference string should be colored uniformly
(e.g., `@Colors.primary` as one semantic unit).

------------------------------------------------------------------------

### 4. Editor Behavior

-   Highlight must work per-line (compatible with current line-based
    editor).
-   No multi-line state required.
-   Must not rely on global parsing context.

------------------------------------------------------------------------

### 5. Future Compatibility

Highlighting logic should be extendable for:

-   Autocomplete support
-   Refactor-safe renaming
-   Static validation feedback
-   Hover inspection of resource definitions

------------------------------------------------------------------------

## Acceptance Criteria

-   Attached properties are visually distinct from normal properties.
-   Resource references are consistently colored as semantic units.
-   Fallback syntax does not break highlighting.
-   No regression in existing highlighting behavior.
