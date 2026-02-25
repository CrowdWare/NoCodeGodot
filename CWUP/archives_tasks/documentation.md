# Generate an SMS Reference Doc (Controls, Properties, Events)

## Goal

Create a single SMS/SML reference document that lists all supported SML controls and for each control:
	•	all properties (including inherited ones)
	•	all events (including inherited ones)
	•	event parameter lists (if any)
	•	notes on whether an event belongs to the container or to an item

This document is used for IntelliSense and for users.

## Rules / Clarifications
	•	Events belong to the Godot control that emits the signal.
	•	Example: tabs.tabChanged(index) belongs to TabContainer (tabs), not to individual tab pages.
	•	For “item-like” structures that are not real nodes (e.g. PopupMenu items), list item-events under the emitting control, with a note about the optional id-based sugar:
	•	PopupMenu.idPressed(id) (container event)
	•	<itemId>.pressed() (only if the SML item has an explicit id)

## Required Output

Produce one file, e.g.:
	•	docs/sms-reference.sml (preferred) or docs/sms-reference.sms

Structure example (format is free, but must be consistent and readable):
	•	Control name
	•	Base classes (inheritance chain)
	•	Properties table/list (name + type + default if known)
	•	Events list (name + params)

## Data Source

Use the set of controls/properties/events that the runtime actually supports after this refactoring.
Do not document “future” controls.

## Definition of Done
	•	The reference doc covers all supported controls.
	•	Each control lists inherited properties/events (not only direct ones).
	•	Clear separation of:
	•	container events vs item-sugar events
	•	Contains the TabContainer example correctly:
	•	tabChanged(index) under TabContainer, not TabPage