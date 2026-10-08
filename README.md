# Cozy Nodes

Cozy Nodes is a Unity package for building gameplay systems with node graphs.
Use it for dialogue, cutscenes, quests, UI flow, game-state flow, and other
game-specific systems created on top of the package.

![Cozy Nodes banner](https://quietfalls.net/wp-content/uploads/2026/01/CozyNodes_Banner2.jpg)

> [!important] Unity requirement
> Cozy Nodes requires **Unity 6.6.1f1 (6000.6.1f1) or later**. Unity 6.4+
> already includes Graph Toolkit in the Editor, so do not install the old
> experimental Graph Toolkit package separately.

## Installation

1. In Unity, open **Window > Package Management > Package Manager**.
2. Select **+ > Install package from git URL...**.
3. Paste `https://github.com/ShaderFactory/Cozy-Nodes.git`.
4. Select **Install**.

Package identifier: `com.shaderfactory.cozygraphtoolkit`

## Your first graph

1. Create a Cozy Graph from **Assets > Create > Shader Factory > Cozy Graph Toolkit > Graph**.
2. Open the graph and create `Start Node`, `Print Node`, and `End Node`.
3. Connect `Start Node → Print Node → End Node`.
4. Add `CozyManager` to a GameObject in the scene and assign the graph to its
   **Runtime Graph** field.
5. Enter Play Mode. The text in the Print Node appears in the Unity Console.

## Node reference

### Flow nodes

| Node | What it does |
| --- | --- |
| **Start Node** | Begins graph execution. A graph normally has one Start Node. |
| **End Node** | Ends the current execution path. |
| **Print Node** | Writes its **Message** value to the Unity Console, then continues through **out**. The Message field supports multiple lines. |
| **Branch Node** | Reads a boolean **Condition** and continues through either **True** or **False**. |
| **Wait For Trigger** | Pauses execution until `CozyManager.Trigger` receives the matching **Trigger Name**. Useful for UI buttons, dialogue choices, and gameplay events. |
| **Invoke Event** | Notifies game code through `CozyManager.EventInvoked`. The graph sends an **Event Name** and optional **Payload**; the game decides how to react. |
| **Set Variable** | Changes a Blackboard variable for this running graph only. Connect the Blackboard variable node to **Variable** and its new value to **Value**. |

### Value nodes

| Node | What it does |
| --- | --- |
| **Combine String** | Joins **First** and **Second** into a string **Result**. |
| **Add Integer** | Adds integer inputs **A** and **B**, then provides the integer **Result**. Useful for gold, counters, inventory quantities, levels, and quests. |
| **Blackboard Variable** | A built-in Graph Toolkit node created from a Blackboard variable. It provides the variable's current runtime value to connected Cozy Nodes. |

## Automatic value conversions

Cozy Nodes keeps ports typed, but permits a small set of safe conversions when a
value connection needs them. For example, an `Add Integer` result can connect
directly to the string **Message** input on a Print Node.

| From | To | Result |
| --- | --- | --- |
| `int` | `string` | Text such as `42` |
| `float` | `string` | Text such as `3.5` |
| `bool` | `string` | Text such as `True` or `False` |
| `int` | `float` | A non-lossy numeric conversion |

Other type pairs stay disconnected in the Editor. A generic node such as
`Set Variable` accepts different source types, but displays an error on the node
when the connected value cannot be stored in its chosen Blackboard variable.

> [!tip] Adding node images
> Each node has a clear entry in this reference. When screenshots are ready, expand
> the relevant row into a dedicated subsection and add its image there. No code or
> package change is needed.

## Blackboard variables

Create variables in the Graph Toolkit Blackboard. Their defaults are stored in the
graph asset, while each `CozyManager` creates its own temporary runtime copy when
the graph starts.

This means a `Set Variable` node can change a value for one running graph without
editing the graph asset or affecting another Manager.

## Development status

> [!warning]
> Cozy Nodes is under active development. Flow execution, triggers, events,
> Blackboard reads and runtime variable writes are available. Save-data nodes,
> numeric comparisons, floating-point math, and the public API for custom game
> nodes are still to come.
