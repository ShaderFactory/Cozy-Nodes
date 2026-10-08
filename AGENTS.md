# Cozy Nodes — Development Style

## Goal

Cozy Nodes is a reusable node-graph framework for Unity games. It must stay
independent from any one game while making custom game nodes straightforward to add.

## Code style

- Prefer explicit, readable C# over condensed syntax.
- Use descriptive variable names and keep each method focused on one job.
- Comment the intent and the flow of non-obvious code, especially graph import,
  runtime evaluation, serialization, and execution decisions.
- Follow the existing project's direct, explanatory commenting style.
- Keep abstractions small. Do not add complexity before a concrete graph feature needs it.

## Architecture

- Keep editor-only Graph Toolkit code separate from runtime code.
- Keep core behavior generic. Do not add importer or executor special cases for a
  particular game node when a shared node contract can express the behavior.
- Separate flow nodes from value nodes:
  - Flow nodes use execution ports to perform actions and choose the next path.
  - Value nodes are evaluated only when another node asks for their output.
- Runtime value evaluation must follow input connections recursively until it reaches
  a direct value or a source node that can calculate the requested output.

## Verification

- When adding a core node, build the smallest graph that proves its behavior.
- Check Unity Console output after the graph is imported and after entering Play mode.
