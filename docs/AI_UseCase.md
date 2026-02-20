# AI Use Case: Prompt-to-SML Execution Loop

## Title
AI-Driven Application Generation with Executable SML

---

## 1. Overview

This use case describes a system in which an AI does not generate traditional source code (C#, C++, etc.), but instead produces declarative SML (Simple Markup Language).

SML defines the structural specification of an application.  
The NoCodeRunner renders and executes this structure deterministically inside a sandboxed runtime.

The system forms a closed, fully validatable loop:

Prompt → SML → Validation → Sandbox Execution → Feedback → AI Patch

---

## 2. Objective

- Describe applications using natural language
- Let AI generate structurally correct SML
- Make every result immediately executable
- Eliminate manual UI programming
- Ensure deterministic and validated execution

---

## 3. Core Principle

### Traditional Approach

User → AI → generates C#/C++ → compiler → runtime errors → debugging

### Structural Approach

User → AI → generates SML → validator → runner → immediate visual execution

SML is:

- Declarative
- Tree-structured
- Strictly validatable
- Free of expressions
- Free of side effects
- Free of complex imperative logic

This makes it significantly more stable and predictable for AI generation compared to traditional source code.

---

## 4. System Architecture

### Components

1. Prompt Editor  
   - Natural language input  
   - Optional structural constraints  

2. AI Engine  
   - Generates SML  
   - Optionally generates SMS (Script) for events  

3. SML Validator  
   - Verifies structural correctness  
   - Checks required properties  
   - Validates types and allowed values  

4. Auto-Fix Layer  
   - Consumes validator errors  
   - Applies targeted structural patches  

5. NoCodeRunner Sandbox  
   - Renders UI  
   - Executes events  
   - Runs in an isolated runtime  
   - Provides structured logging  

6. Preview Overlay  
   - Live rendering  
   - Device switching (Desktop / Mobile)  

---

## 5. Execution Flow (Executable Spec Loop)

1. The user describes the application  
   Example: "Create an app with a left menu and a document area on the right."

2. The AI generates SML.

3. The validator checks:
   - Missing IDs  
   - Invalid properties  
   - Incorrect values  

4. If errors occur:
   - The validator produces structured error messages  
   - The AI receives the errors as input  
   - The AI generates a precise structural patch  

5. The updated SML is reloaded.

6. The runner executes the application inside the sandbox.

7. The user refines the application through additional prompts.

Each iteration produces an executable interpreted result.

---

## 6. Benefits

### 1. AI-Optimized Target Language
SML focuses on structure rather than complex logic.

### 2. Deterministic Behavior
No hidden side effects or unpredictable execution paths.

### 3. Strict Validation
Structural rules prevent architectural degradation.

### 4. Immediate Execution
Every AI iteration results in a runnable application which interpretes the code.

### 5. Self-Healing Potential
Validator output can directly drive automatic AI correction.

### 6. Human-Readable and Transparent by Design

SML and SMS are intentionally simple and easy to read.

Because both languages are structurally minimal and declarative, users can immediately understand:

- What the AI generated  
- Where something is located  
- Which property controls which behavior  
- Whether the AI correctly interpreted the intent  

This makes AI output fully transparent and verifiable.

Example SML:

```qml
Window {
    title: "Dashboard"

    HBoxContainer {
        Panel {
            id: sidebar
            width: 280
        }

        Panel {
            id: content
            sizeFlagsHorizontal: ExpandFill
        }
    }
}
```

Even non-programmers can clearly see:

- There is a `Window`
- It contains an `HBoxContainer`
- The left `Panel` has a fixed width
- The right `Panel` fills the remaining space

If the user wants to adjust the layout, they immediately know where to change:

```qml
width: 320   // adjusted sidebar width
```

Example SMS (Simple Script):

```javascript
on saveButton.clicked() {
    dialog.open()
}
```

The structure is explicit and minimal:

- Event source
- Event name
- Direct action

Because of this simplicity, users can:

- Manually refine AI-generated output
- Validate correctness at a glance
- Learn the structure quickly
- Maintain full control over the application

SML and SMS are not black boxes.
They are readable specifications.

---

## 7. Example Scenario

User prompt:  
"I need a dashboard with a sidebar on the left and content on the right."

The AI generates a structural SML layout.

The runner loads and renders the UI immediately.

The user refines the layout through further prompts such as:
"Increase sidebar width to 320."  
"Add a toolbar at the top."

The AI adjusts only the relevant structural nodes.

---

## 8. Possible Extensions

- Structural diff view between versions
- Prompt history as a timeline
- AI-driven layout optimization
- SML-to-mobile responsive adjustments
- SML export to other target platforms
- Automated structural refactoring

---

## 9. Strategic Significance

In this model, NoCode does not mean:
"The user does not write code."

Instead, it means:
"The AI does not generate imperative code, but structured specification."

The system becomes:

- AI-optimized  
- Validation-centered  
- Platform-agnostic  
- Deterministically scalable  

---

## 10. Conclusion

This model shifts the focus from programming to structuring.

The NoCodeRunner becomes:

- An execution engine  
- A validation authority  
- A canonical AI target platform  

The result is a new form of application development:

Specification instead of programming.  
Structure instead of logic.  
Executable description instead of source code.