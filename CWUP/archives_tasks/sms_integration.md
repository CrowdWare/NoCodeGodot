# Port SMS Runtime from Kotlin to C# and Add Safe ProjectFS API

## Goal

Replace the existing Kotlin-based SMS runtime with a native C# implementation inside the NoCodeRunner, and expose a safe, capability-based API for project file operations (read/write/create/delete).
After this is working, remove the old “load and execute .cs plugins” path completely.

This enables the current use case: show project data in TreeView and edit files in CodeEdit via SMS-driven actions.

⸻

## Scope

In scope
	1.	SMS runtime port (Kotlin → C#)
	•	Implement the currently tested SMS language features in C# (same behavior as the live-tested Kotlin version).
	•	Keep it minimal: only what you already use/tested.
	2.	ProjectFS Capability API (safe)
Provide SMS-accessible functions for project file operations:
	•	ProjectFS.List(dir)
	•	ProjectFS.Exists(path)
	•	ProjectFS.ReadText(path)
	•	ProjectFS.WriteText(path, content)
	•	ProjectFS.CreateDir(path)
	•	ProjectFS.Delete(path) (file or empty dir; define behavior clearly)
	•	Optional if needed for editor: ProjectFS.ReadBytes, ProjectFS.WriteBytes
	3.	TreeView + CodeEdit integration
	•	Populate TreeView from the project directory using ProjectFS.List.
	•	When a file is selected, load content into CodeEdit using ProjectFS.ReadText.
	•	When CodeEdit is saved, write back using ProjectFS.WriteText.
	•	Deletions/creates from UI must use ProjectFS APIs only.
	4.	Remove old C# plugin loading
	•	Delete/disable any mechanism that loads arbitrary .cs from disk and executes it.
	•	Replace the initialization hook with SMS-driven entry points.

⸻

## Security Requirements (non-negotiable)

ProjectFS must be a root-jail:
	•	All paths are relative to projectRoot (no absolute paths).
	•	Block traversal: reject paths containing .. after normalization.
	•	Block symlink/junction escape:
	•	Resolve real path and ensure it stays under projectRoot.
	•	Define a clear rule for allowed operations:
	•	Read/write/create/delete only inside project root.
	•	Add safe limits:
	•	Max file size for ReadText (e.g. 5–20 MB, configurable)
	•	Max request size for WriteText
	•	Return structured errors (e.g. NotFound, AccessDenied, InvalidPath, TooLarge).

⸻

## Implementation Notes (KISS)
	•	SMS runtime should not expose .NET standard libraries to scripts.
	•	SMS gets only explicit host APIs:
	•	projectFS.*
	•	ui.* (if you already have it)
	•	log.*

⸻

## Deliverables
	1.	C# SMS runtime module
	•	Parser + executor matching the Kotlin behavior (at least for features used in the live test).
	2.	ProjectFS capability class + tests
	•	Unit tests for path normalization + traversal + symlink escape.
	3.	UI wiring
	•	TreeView lists project directories/files.
	•	CodeEdit opens/saves selected file.
	4.	Old .cs loading removed
	•	No code path remains that loads arbitrary .cs from disk.

⸻

## Acceptance Criteria
	•	✅ A sample project opens and TreeView shows the project directory structure.
	•	✅ Clicking a file loads its text into CodeEdit.
	•	✅ Saving from CodeEdit writes the file back to disk via ProjectFS.
	•	✅ Creating/deleting files/folders from UI works via ProjectFS.
	•	✅ Attempts like ../../somewhere are blocked.
	•	✅ Symlink escape attempts are blocked (test case included).
	•	✅ There is no remaining feature that loads .cs plugins from disk.
	•	✅ The behavior matches the already-tested Kotlin SMS version for the used features.

⸻

## Suggested Milestones
	1.	Port SMS core (parsing + execution for currently used constructs)
	2.	Add ProjectFS capability + tests
	3.	Wire TreeView + CodeEdit
	4.	Remove .cs loading path
	5.	Regression test with the existing live-tested SMS scripts

## Code to Port
The Code for the Kotlin Implementation you can find in the folder sms-kotlin.
We have on addition that should also be implemented.
This is elseif.

```kotlin
if (conditionA) {
    doSomething()
} else if (consitionB) {
    doSomethingElse()
} else {
    doNothing()
}
```

## Sample Code to port to SMS
We have a sample in /docs/NoCodeDesigner/app.cs which reads a directory to display files in a treeview and acts on TreeViewItem-Events.
This we should port to app.sms.
In the code we then have code like this.
```kotlin
var tree
var codeEdit

fun ready() {
    tree = GetObjectById("treeview")
    codeEdit = GetObjectById("codeEdit")
    codeEdit.onSave("CodeEditOnSave") // register handler by name
    PopulateTree(tree, "res:/")
}

fun TreeItemSelected(tree, item)
{
    Info("NoCodeDesigner.App", "treeItemSelected -> treeView=${tree.Value}, text='${item.Text}'")
    codeEdit.loadFile(tree.Value)
}

fun TreeItemToggled(tree, item, isOn)
{
    Info("NoCodeDesigner.App", "treeItemToggled -> treeView=${tree.Value}, text='${item.Text}', isOn=${isOn}")
}

fun PopulateTree(tree, path) {
    var id = 0
    var root = tree.CreateItem()
    root.SetText(id++, "docs")
    root.Collapsed = false

    for (file in ProjektFS.getFiles()) {
        // fill tree here
        var item = root.CreateItem()
        item.SetText(id, file.name)
    }
}

fun CodeEditOnSave(codeEdit) {
    var path = codeEdit.Path  // current file path
    var text = codeEdit.Text  // current editor text
    ProjectFS.WriteText(path, text)
    Info("NoCodeDesigner.App", "Saved via SMS: ${path}")
}
```

Addionally we should use the CodeEdit from SML load/save content selected by treeview.
```qml
TreeView {
    id: treeview
    showGuides: false
}

CodeEdit {
    id: codeEdit
    text: "Window { titel: \"Test\"}"     
    syntax: "sml"       
}
```