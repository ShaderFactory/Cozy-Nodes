# Changelog

All notable changes to Cozy Nodes are documented here. The package follows
[Semantic Versioning](https://semver.org): a major version for breaking changes,
a minor version for new features, a patch version for fixes.

## [2.0.0] - 2026-11-12

### Breaking
- **Int Comparison** and **Float Comparison** were removed. Use **Compare Numbers**,
  which accepts integers and floats. Graphs that used the old nodes must be updated.
- Legacy nodes (Add Integer, Greater Than Integer, Invoke Event, End, Branch,
  Combine String) and the old samples were removed.
- Assemblies were renamed to `shaderfactory.cozynodes.runtime` and
  `shaderfactory.cozynodes.editor`.
- `CozyManager` moved to `Runtime/MonoBehaviours`, and graph execution now runs
  through sessions, scopes and execution flows (`CozyGraphSession`,
  `CozyGraphScope`, `CozyExecutionFlow`).

### Added
- Sub Graphs, including data outputs.
- Arrays of string, integer, float and boolean: Array Make, Array Empty,
  Array Get Item, Array Length.
- Context and Block nodes (`CozyContext`, `CozyBlock`).
- Scene Management nodes: Load Scene, Reload Active Scene, Set Active Scene,
  Get Active Scene Info, Is Scene Loaded.
- Events: On Update, On Late Update, Emit Event, plus the
  `CozyGraphEventListener` and `CozyGraphGlobalEventEmitter` components.
- Control flow: Wait For Seconds, Stop Graph; improved For Loop and Wait For Trigger.
- Comparison: Compare Numbers, Compare Strings, Compare Boolean.
- `[ProvidedInputSet]`: one input port per entry returned by an
  `ICozyInputSetProvider`, keyed by a stable key.
- Hover descriptions: `Tooltip` on `[CozyNodeMenu]`, `[CozyContextMenu]` and
  `[CozyBlockMenu]` is shown on the node header. Every built-in node has one.
- Node colors, widths and custom icons through attributes.

### Fixed
- Implicit conversion between port types.
- Value outputs exposed by Sub Graphs.

## [1.0.0]
- First usable release.
