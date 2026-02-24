# Task 1: Implement Attached Properties in SML

## Goal

Introduce Attached Properties to SML to allow context-specific metadata
assignment (e.g., DockContainer.title) without polluting the child
component API.

------------------------------------------------------------------------

## Motivation

Attached Properties allow parent components to expose contextual
configuration to their children without introducing cross-domain
coupling.

Example:

    DockContainer {
        id: container
        Panel {
            container.title: "TabName"
        }
    }

------------------------------------------------------------------------

## Requirements

### 1. Syntax

-   Support qualified property assignments:
    -   `TypeName.property: value`
    -   `instanceId.property: value`
-   Must be distinguishable from normal properties.
-   Must be recognized before the colon token.

### 2. Parser Changes

-   Extend grammar to support:
    -   `QualifiedIdentifier "." Identifier ":" Value`
-   Store as AttachedPropertyAssignment in AST.

### 3. Resolution Rules

-   Attached properties are valid only inside children of the provider.
-   Provider must explicitly declare supported attached properties.
-   Validation must fail for unknown attached properties.

### 4. Runtime Behavior

-   During instantiation:
    -   Child collects attachments per provider.
    -   Provider reads attachment metadata when constructing
        layout/behavior.

### 5. Editor Support

-   Syntax highlighting for qualified property tokens.
-   Autocomplete for:
    -   Provider types
    -   Declared attached properties

------------------------------------------------------------------------

## Acceptance Criteria

-   Attached properties work for DockContainer.title.
-   Validation prevents undefined attached properties.
-   Syntax highlighting differentiates normal vs attached properties.
