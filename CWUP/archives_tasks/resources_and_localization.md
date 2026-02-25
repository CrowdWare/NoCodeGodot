# Task 2: Implement Resource System and Localization in SML

## Goal

Introduce a unified Resource System with namespaced references using the
`@` operator and optional fallback values.

------------------------------------------------------------------------

## Motivation

Replace string keys and redundant theme duplication with structured
resource references.

Example:

    text: @Strings.window.caption, "Default Value"
    color: @Colors.primary
    icon: @Icons.save
    padding: @Layouts.default.padding

------------------------------------------------------------------------

## Requirements

### 1. Resource Namespaces

Support top-level resource definitions:

    Strings { ... }
    Colors { ... }
    Icons { ... }
    Layouts { ... }

### 2. Reference Syntax

-   `@Namespace.path`
-   Optional fallback: `@Namespace.path, "Fallback"`
-   Must not introduce general expressions.

### 3. Parser Changes

Value rule:

    Value := ResourceRef ("," Literal)? | Literal | Number

### 4. Validation

-   Validate existence of resource at compile-time.
-   Provide error for unknown namespace or key.
-   Type validation (Color, String, Number).

### 5. Runtime Behavior

-   Resolve references at layout-time or language-change event.
-   Store both reference and fallback internally.
-   Support hot language switching.

### 6. Editor Support

-   Syntax highlight `@Namespace.path` as ResourceReference.
-   Autocomplete namespace and keys.

------------------------------------------------------------------------

## Acceptance Criteria

-   Resources resolve correctly with fallback.
-   Language switching re-resolves Strings.
-   Unknown resources generate compile errors.
