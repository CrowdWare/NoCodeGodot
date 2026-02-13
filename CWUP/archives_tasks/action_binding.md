# C# Action Binding
	•	Action resolver system
	•	Bind SML IDs to C# handlers
	•	Central event dispatcher
	•	Error logging for missing actions
	•	Extend Action parser to support web:<url> without breaking http://... (split on first : only)
	•	Implement HandleWeb(url) using OS.ShellOpen(url)
	•	Add URL allowlist: only http, https, file (log + ignore otherwise)
	•	Add docs/examples for the three action types in the SML reference