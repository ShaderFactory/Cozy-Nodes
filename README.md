# Cozy Nodes

Cozy Nodes is a Unity package for building gameplay systems with node graphs.
Use it for dialogue, cutscenes, quests, UI flow, game-state flow, and other
game-specific systems created on top of the package.

![Cozy Nodes banner](https://quietfalls.net/wp-content/uploads/2026/01/CozyNodes_Banner2.jpg)

> [!important] Unity requirement
> Cozy Nodes requires **Unity 6.6.1f1 (6000.6.1f1) or later**. Unity 6.4+
> already includes Graph Toolkit in the Editor, so do not install the old
> experimental Graph Toolkit package separately.

## 💻 Installation

1. In Unity, open **Window > Package Management > Package Manager**.
2. Select **+ > Install package from git URL...**.
3. Paste `https://github.com/ShaderFactory/Cozy-Nodes.git`.
4. Select **Install**.

Package identifier: `com.shaderfactory.cozygraphtoolkit`

## 📝 How to use
### 📈 Making your first graph.
1. Create a Cozy Graph from **Assets > Create > Shader Factory > Cozy Graph Toolkit > Graph**.
2. Open the graph and create `Start Node`, `Print Node`, and `End Node`.
3. Connect `Start Node → Print Node → End Node`.
4. Add `CozyManager` to a GameObject in the scene and assign the graph to its
   **Runtime Graph** field.
5. Enter Play Mode. The text in the Print Node appears in the Unity Console.

## Nodes

<details>
<summary>Set Variable Node</summary>

Changes a Blackboard variable for this running graph only. Connect the Blackboard variable node to **Variable** and its new value to **Value**.
<img width="900" height="200" alt="image" src="https://github.com/ShaderFactory/Cozy-Nodes/blob/main/Documentation~/Images/image-node-setvariable.png?raw=true" />

</details>

## Development status

> [!warning]
> Cozy Nodes is under active development. Flow execution, triggers, events,
> Blackboard reads and runtime variable writes are available. Save-data nodes,
> numeric comparisons, floating-point math, and the public API for custom game
> nodes are still to come.
