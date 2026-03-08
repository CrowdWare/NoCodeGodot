# Risks

Policy: By alpha freeze, all open `High` and `Medium` risks must be resolved or explicitly downgraded with documented rationale.

| Risk                                      | File                        | Primary Solution   | Status | Priority |
|-------------------------------------------|-----------------------------|--------------------|--------|----------|
| "Unlimited Grok Exports" Flatrate-Abo     | grok_unlimited.md     | Strict BYOK        | Open   | High     |
| JSON bridge fallback in release hot path  | tasks/native_release_no_json_bridge.md | Typed bridge + release gate | Open | High |
| Native 3D tools currently scaffold-only (no visual/runtime parity) | tasks/native_3d_tools_port.md | Incremental parity milestones (viewport -> selection -> gizmos -> timeline) | Open | High |
| SMS event payload drift between C# and native controls | tasks/native_3d_tools_port.md | Contract tests for event names/arg shapes across runtimes | Open | Medium |
