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

Package identifier: `com.shaderfactory.cozynodes`

## 📝 How to use
### 📈 Making your first graph.
1. Create a Cozy Graph from **Assets > Create > Cozy Tools > Cozy Graph**.
2. Open the graph. It starts with `On Start` connected to **Debug > Print**.
3. Change the Print message or its **Log Type** option.
4. Add **Cozy Nodes > Graph Manager** from Unity's **Add Component** menu, then assign the graph to its
   **Runtime Graph** field.
5. Enter Play Mode. The message appears in the Unity Console.

### Authoring custom nodes

Game code creates a node by inheriting from `CozyNode`. The package generates
its editor representation, so custom nodes never reference UnityEditor or Graph
Toolkit APIs directly.

Use `[Input]` and `[Output]` for graph values. Cozy values currently support
`string`, `int`, `float`, `bool`, and one-dimensional arrays of those types.
Their serialization, defaults, and runtime copy behavior are defined in one
central value-type registry, so adding a future built-in type follows one
deliberate path rather than scattering per-type array handling through nodes.
Use `[Option]` for local node configuration that should appear in the node's
**Options** section. Options support `string`, `int`, `float`, `bool`, and
enums; read their graph-specific values at runtime with
`context.GetOptionValue<T>(nameof(Field))`.

Arrays are ordinary typed graph values. A game-authored node can declare
`[Input] public int[] Values;` and read it with
`context.GetInputValue<int[]>(nameof(Values))`. Cozy Nodes gives array values
copy semantics: a port or Blackboard read receives its own array instance.
`[InputList]` is different: it is an editor convenience for a variable number of
individual input ports, not an array travelling through a wire.

The built-in **Utilities/Array** nodes provide the first authoring path:
**Array Make** creates a typed array from configurable element inputs,
**Array Length** returns its size, and **Array Get Item** reads an index without
throwing. Get Item returns both **Item** and **Is Valid**, so a graph can handle
an out-of-range index explicitly.

Graph Toolkit has no public wildcard-value port API. Cozy Nodes therefore uses
its own explicit type-parameter metadata instead of pretending that a normal
`object` port is type-safe. Once resolved, an Int array wire can only connect
to an Int array input.

For a type parameter that represents a complete value, declare
`[CozyTypeParameter("T", AllowArrayTypes = true)]`. Its picker offers the
supported scalar types and their one-dimensional array forms. Do not enable
that setting on a parameter used as `T[]`: Cozy Nodes deliberately does not
support nested arrays.

### Type-parameter nodes

When a node must work for more than one scalar type, declare a shared type
parameter rather than using `object` as a normal port type. The graph author
chooses the parameter's concrete type in the node UI, and Cozy rebuilds every
matching port from that single persisted selection.

```csharp
[CozyNodeMenu("Utilities/Array", "Array Make")]
public class ArrayMakeNode : CozyArrayNode
{
    [Option("Element Count")]
    public int ElementCount = 2;

    [InputList(nameof(ElementCount), "Element")]
    [CozyTypeParameterPort("T")]
    public object[] Elements;

    [Output("Array")]
    [CozyTypeParameterPort("T", IsArray = true)]
    public object Result;
}
```

`object` here is only an implementation backing field. It never becomes the
graph's actual port type: `T` resolves to `string`, `int`, `float`, or `bool`,
and `T[]` resolves to the matching one-dimensional array type. A node can
explicitly opt in to using `T` itself as an array when that is meaningful, such
as **Set Variable**. A node with no selected type cannot be imported into a
runtime graph.

`[InputList]` creates a repeated input group whose count is controlled by an
integer option. Declare it on an array field, then read its values in graph order
with `context.GetInputValues<T>(nameof(Field))`.

Flow nodes override `Execute` instead of `GetValue`. Use `[FlowInput]` and one
or more `[FlowOutput("Name")]` attributes to declare execution ports, then
return `CozyNodeExecutionResult.ContinueWith("Name")` for the path to follow.

**Control Flow > Wait For Seconds** pauses only its current graph flow for the
given number of scaled game-time seconds, then follows `out`. The pause belongs
to `CozyGraphRunner`, so it works inside a `For` Body: the next iteration begins
only after the waiting branch finishes.

```csharp
[CozyNodeMenu("Utilities/String", "Combine String")]
public class CombineStringNode : CozyNode
{
    [Option("Input Count")]
    public int InputCount = 2;

    [InputList(nameof(InputCount), "Text", MinimumCount = 2, MaximumCount = 16)]
    public string[] Text;

    [Output]
    public string Result;

    public override object GetValue(CozyNodeContext context, string outputPortName)
    {
        if (outputPortName != nameof(Result))
            return null;

        return string.Concat(context.GetInputValues<string>(nameof(Text)));
    }
}
```

Increasing the option keeps existing input values and connections. Reducing it
removes inputs above the new count; Cozy Nodes writes a Console warning when
that would remove connected inputs.

### Sizing a text node

`[CozyNodeWidth]` gives a public custom node's input section a minimum editor
width in pixels. The output section stays compact. For a multi-line string
field, set `MinimumWidth` on `[CozyTextArea]` as well. These affect only the
Graph Toolkit presentation, not graph data or runtime behavior.

```csharp
[CozyNodeWidth(360)]
public class DialogueLineNode : CozyNode
{
    [Input("Text")]
    [CozyTextArea(5, 8, MinimumWidth = 220)]
    public string Text;
}
```

Cozy Nodes generates and owns the required editor stylesheet beside its
generated adapter. It removes that stylesheet when the node no longer needs
one, so game projects do not accumulate stale generated styles. If a node has
several text areas, the widest requested value is used for that node's text
fields.

### Coloring important nodes

`[CozyNodeColor]` sets a public custom node's Graph Toolkit header color. The
body keeps Graph Toolkit's darker surface so ports and values stay readable.
Its red, green, and blue values normally use the 0-to-1 range.
This is editor presentation only; a graph author can still set a per-node color
in the Graph Toolkit UI when they need an exception.

For a light header, use `[CozyNodeHeaderTextColor]` to set a readable title
color without affecting the node body. `[CozyNodeAccentColor]` separately
controls Graph Toolkit's native border and accent cues.

```csharp
[CozyNodeColor(0.25f, 0.72f, 0.40f)]
[CozyNodeHeaderTextColor(0f, 0f, 0f)]
[CozyNodeAccentColor(1f, 1f, 1f)]
public class GameStartedNode : CozyNode
{
    // ...
}
```

### Adding a node icon

`[CozyNodeIcon]` places a small image beside the node title in Graph Toolkit's
header. Use a Unity asset path and a transparent monochrome image: Graph
Toolkit displays this artwork at header-icon size.

```csharp
[CozyNodeIcon("Assets/MyGame/GraphIcons/OpenDoor.png")]
public class OpenDoorNode : CozyNode
{
    // ...
}
```

### Graph entries

`On Start` is an automatic entry: a `CozyManager` runs it when it begins.
`Custom Event` is a named entry that game code can start later:

```csharp
cozyManager.RunCustomEvent("Open Door");
```

This is different from **Wait for Trigger**. A Custom Event starts a new graph
path; Wait for Trigger resumes the flow that has already reached that node. A
`CozyManager` currently runs one flow at a time, so a Custom Event is not
started while another flow is still running.

**Control Flow > Wait For Trigger** waits for a matching signal from game code:

```csharp
cozyManager.Trigger("Advance Dialogue");
```

Only the current waiting flow can accept the signal. A different name, or a
signal sent while the graph is not waiting at this node, is ignored.

## Nodes

<details>
<summary>Set Variable Node</summary>

Changes a Blackboard variable for this running graph only. Connect the Blackboard variable node to **Variable** and its new value to **Value**.
<img width="900" height="200" alt="image" src="https://github.com/ShaderFactory/Cozy-Nodes/blob/main/Documentation~/Images/image-node-setvariable.png?raw=true" />

</details>

<details>
<summary>Integer Math Node</summary>

Performs the selected operation—**Add**, **Subtract**, **Multiply**, or
**Divide**—on integer inputs **A** and **B**. Division by zero returns `0`.

</details>

<details>
<summary>Int Comparison Node</summary>

Compares integer inputs **A** and **B** using **Greater Than**, **Greater Than
or Equal**, **Less Than**, **Less Than or Equal**, **Equal**, or **Not Equal**.

</details>

<details>
<summary>Float Comparison Node</summary>

Compares float inputs **A** and **B** using the same operations as Int
Comparison. Its boolean **Result** can connect directly to an If/Else node.

</details>

<details>
<summary>Float Math Node</summary>

Performs the selected operation—**Add**, **Subtract**, **Multiply**, or
**Divide**—on float inputs **A** and **B**. Choose the operation in the node's
**Options** section; the node returns the calculated **Result**.

</details>

<details>
<summary>If/Else Node</summary>

Routes execution through **True** when **Condition** is true; otherwise, it
routes execution through **False**. This is a public `CozyNode` flow node, so
game code defines it without a paired Editor node.

</details>

<details>
<summary>Print Node</summary>

Writes **Message** to the Unity Console. Choose **Log**, **Warning**, **Error**,
**Assert**, or **Exception** from its **Log Type** option.

</details>

<details>
<summary>For Node</summary>

Runs **Body** once for every integer from **Start** (inclusive) to **End**
(exclusive), moving by **Step**. **Index** provides the current iteration value;
**Exit** runs after the loop ends. Connect a Body path to **Break** to leave the
active loop immediately through Exit. A negative Step counts down, while a Step
of zero reports a graph error.

</details>

## Development status

> [!warning]
> Cozy Nodes is under active development. Execution, triggers, events,
> Blackboard reads and runtime variable writes, the public API for custom game
> nodes, public flow nodes, loops, basic integer math, and basic floating-point utilities are available.
> Save-data nodes and additional floating-point utilities are planned next.
