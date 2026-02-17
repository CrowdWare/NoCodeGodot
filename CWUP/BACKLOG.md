# Backlog

## Functional Requirements

### Platform Support
- The NoCodeDesigner must run on Windows, macOS, and Linux.
- Feature parity across platforms (no platform-exclusive features).
- Platform-specific integrations must degrade gracefully on unsupported systems.
- The NoCodeRunner must run on Android in addition to desktop platforms.

## Non-Functional Requirements

### Performance
- Stable frame pacing; avoid stutter (consistent frame times).
- Scalable quality settings (low / medium / high) and resolution scaling.
- Predictable CPU/GPU budgets per frame.
- Heavy I/O, parsing, and preparation tasks may run on worker threads; all scene graph mutations must occur on the main thread.

### Determinism & Reproducibility
- Fixed and well-defined tick rates for gameplay simulation.
- Simulation logic decoupled from render frame rate.
- Repeatable combat outcomes driven by server state and validation.
- Clients are non-authoritative and act as presentation and input layers.

### Stability & Fault Tolerance
- No hard crashes on malformed SML, scripts, or content.
- Graceful failure with clear error logs and safe fallbacks.
- Unknown or unsupported SML entries must produce warnings, not fatal errors.
- Clean shutdown on SIGTERM; always flush and close saves safely.

### Security (Self-Hosted / Guild Servers)
- Validate all client inputs on the server.
- Rate limiting for spam-prone actions (abilities, RPCs, join/leave).
- Business logic executes server-side only.
- Clear separation between admin and player privileges.
- No remote code execution through content or configuration.
- Sandbox security rules (e.g., ProjectFS root jail, no absolute paths, no traversal, no symlink escapes) must be strictly enforced and must not be bypassed.
- No backdoors or implicit bypass paths may be introduced; all new features must explicitly respect the existing security model.

### Maintainability
- KISS principle: one scripting and behavior system for bots, bosses, doors, traps, switches, and interactions.
- Minimal special cases; behavior is defined declaratively and bound consistently.
- Versioned network protocol and save formats with explicit migration paths.

### Observability
- Structured logs including timestamp, level, subsystem, and entity identifier.
- Lightweight debug overlays for demo and training modes (e.g., DPS, threat, cooldowns, CC timers).
- Simple server status output (uptime, connected players, tick duration).

### Configurability
- All runtime settings defined via configuration files.
- Optional hot reload for SML and configuration files in local and development modes.
- Explicitly documented non-goals to manage scope and expectations.

### Portability
- Guild server runs on common Linux VPS distributions.
- Reproducible builds with pinned toolchains where possible.

### Data & Persistence
- Atomic save writes (write temp → fsync → rename).
- Manual and automated backup support with clear rotation strategy.
- Deterministic serialization order for world and state data.

### Usability
- Clear in-game feedback for core rules (engage range, resets, CC breaks).
- Companion control limited to turn and XZ movement for predictability.
- Simple server lifecycle: build → deploy → run as service.

### Compatibility & Upgrades
- Clear client/server version compatibility checks.
- Backward compatibility where feasible; explicit breaking changes.
- Changelogs focus on behavior and system-level changes.

## Use Cases

## Tasks for NoCodeRunner
- [x] tasks/initial.md
- [x] tasks/sml_parser_core.md
- [x] tasks/node_factory.md
- [x] tasks/ui_elements.md
- [x] tasks/3d_integration.md
- [x] tasks/ui_scaling.md
- [x] tasks/animation_control_ui.md
- [x] tasks/camera_interaction.md
- [x] tasks/action_binding.md
- [x] tasks/caching.md
- [x] tasks/layout.md
- [x] tasks/markdown.md
- [x] tasks/documentation.md
- [x] tasks/padding.md
- [x] tasks/code_edit.md
- [x] tasks/id.md
- [x] tasks/dual_layout_properties.md
- [x] tasks/treeview.md
- [x] tasks/sms_integration.md
- [x] tasks/docking_foundation.md
- [x] tasks/docking_menu_commands.md
- [x] tasks/docking_floating_windows.md
- [x] tasks/docking_tabs_reordering.md
- [x] tasks/docking_dragdrop_ux.md
- [x] tasks/docking_layout_persistence.md
- [x] tasks/mainmenu.md
- [ ] tasks/floats_in_sml.md
- [ ] tasks/sms_events.md
- [ ] tasks/refactoring.md
- [ ] tasks/documentation.md
- [ ] tasks/issues_menu.md
- [/] tasks/issues.md                   check again after refactoring
- [/] tasks/missing_features.md         check again after refactoring

## Tasks for NoCodeDesigner
- [ ] Lifecycle-Template für neue Scripts einführen (OnInit / OnReadyAsync sichtbar als optionaler Einstieg)
- [ ] Konventionsbasierte UI-Event-Hooks im Script-Template dokumentieren (z. B. treeItemSelected, treeItemToggled, listItemSelected)
- [ ] Script-Erzeugung im Designer auf neues Template umstellen