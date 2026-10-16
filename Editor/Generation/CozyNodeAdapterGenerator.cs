using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ShaderFactory.CozyNodes.Runtime;
using UnityEditor;
using UnityEngine;

namespace ShaderFactory.CozyNodes.Editor
{
    /// <summary>
    /// Generates small editor-only adapters for game-authored CozyNode classes.
    ///
    /// Graph Toolkit's native item library discovers concrete editor Node types
    /// at compile time. A CozyNode deliberately stays in a runtime assembly, so
    /// this generator bridges the two assemblies without asking game developers
    /// to write a second class or to reference UnityEditor themselves.
    /// </summary>
    [InitializeOnLoad]
    internal static class CozyNodeAdapterGenerator
    {
        private const string GeneratedFolderAssetPath = "Assets/CozyNodes.Generated/Editor";
        private const string GeneratedFileName = "CozyNodeAdapters.g.cs";
        private const string GeneratedStyleFolderName = "Styles";
        private const string GeneratedStyleFilePrefix = "CozyNodeStyle_";
        private const string GeneratedNamespace = "ShaderFactory.CozyNodes.Generated";

        private static bool generationQueued;

        static CozyNodeAdapterGenerator()
        {
            ScheduleGeneration();
        }

        /// <summary>
        /// Queues one generation pass after Unity is ready to inspect loaded
        /// assemblies. Repeated asset imports share the same queued pass.
        /// </summary>
        internal static void ScheduleGeneration()
        {
            if (generationQueued)
                return;

            generationQueued = true;
            EditorApplication.delayCall += GenerateWhenUnityIsReady;
        }

        /// <summary>
        /// Keeps generated code from scheduling a redundant generation pass when
        /// Unity imports the file that this generator just wrote.
        /// </summary>
        internal static bool IsGeneratedAssetPath(string assetPath)
        {
            return assetPath.StartsWith(GeneratedFolderAssetPath, StringComparison.Ordinal);
        }

        private static void GenerateWhenUnityIsReady()
        {
            generationQueued = false;

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleGeneration();
                return;
            }

            GenerateAdapters();
        }

        private static void GenerateAdapters()
        {
            List<Type> nodeTypes = FindSupportedNodeTypes();
            string generatedSource = BuildSource(nodeTypes);
            string generatedFilePath = GetGeneratedFilePath();

            Directory.CreateDirectory(Path.GetDirectoryName(generatedFilePath));
            GenerateStyles(nodeTypes);

            if (File.Exists(generatedFilePath) &&
                File.ReadAllText(generatedFilePath) == generatedSource)
            {
                return;
            }

            File.WriteAllText(generatedFilePath, generatedSource, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(GetGeneratedFileAssetPath());
        }

        /// <summary>
        /// Graph Toolkit reads a node's stylesheet from its Node attribute. For
        /// public CozyNode classes, adapters are generated after reflection has
        /// read the presentation attributes, so each styled node gets a small,
        /// owned stylesheet next to the generated adapter source.
        /// </summary>
        private static void GenerateStyles(List<Type> nodeTypes)
        {
            string styleDirectoryPath = GetGeneratedStyleDirectoryPath();
            Directory.CreateDirectory(styleDirectoryPath);

            HashSet<string> requiredStyleAssetPaths =
                new HashSet<string>(StringComparer.Ordinal);

            foreach (Type nodeType in nodeTypes)
            {
                CozyNodeDefinition definition = CozyNodeReflection.GetDefinition(nodeType);
                if (!HasPresentationStyle(definition))
                    continue;

                string styleAssetPath = GetGeneratedStyleAssetPath(nodeType);
                string styleFilePath = GetGeneratedStyleFilePath(nodeType);
                string styleSource = BuildStyleSource(definition);
                requiredStyleAssetPaths.Add(styleAssetPath);

                if (File.Exists(styleFilePath) && File.ReadAllText(styleFilePath) == styleSource)
                    continue;

                File.WriteAllText(styleFilePath, styleSource, new UTF8Encoding(false));
                AssetDatabase.ImportAsset(styleAssetPath);
            }

            // This folder is owned solely by Cozy Nodes. Removing only files with
            // the generated prefix keeps styles in step with renamed or deleted
            // node types without touching any game-authored assets.
            foreach (string styleFilePath in Directory.GetFiles(
                styleDirectoryPath,
                GeneratedStyleFilePrefix + "*.uss"))
            {
                string styleAssetPath = GetGeneratedStyleAssetPath(styleFilePath);
                if (!requiredStyleAssetPaths.Contains(styleAssetPath))
                    AssetDatabase.DeleteAsset(styleAssetPath);
            }
        }

        private static List<Type> FindSupportedNodeTypes()
        {
            List<Type> nodeTypes = new List<Type>();

            foreach (Type nodeType in TypeCache.GetTypesDerivedFrom<CozyNode>())
            {
                if (!IsSupportedNodeType(nodeType))
                    continue;

                nodeTypes.Add(nodeType);
            }

            nodeTypes.Sort((firstType, secondType) => string.Compare(
                firstType.AssemblyQualifiedName,
                secondType.AssemblyQualifiedName,
                StringComparison.Ordinal));

            return nodeTypes;
        }

        private static bool IsSupportedNodeType(Type nodeType)
        {
            if (nodeType == null || nodeType.IsAbstract || nodeType.IsGenericTypeDefinition ||
                nodeType.ContainsGenericParameters)
            {
                return false;
            }

            // The generated source lives in the project's Editor assembly, so it
            // must be able to reference the game type directly.
            if (!nodeType.IsVisible)
            {
                Debug.LogWarning(
                    $"Cozy Nodes cannot add '{nodeType.FullName}' to the native " +
                    "node menu because the class is not public.");
                return false;
            }

            if (nodeType.GetConstructor(Type.EmptyTypes) == null)
            {
                Debug.LogWarning(
                    $"Cozy Nodes cannot add '{nodeType.FullName}' to the native " +
                    "node menu because it needs a public parameterless constructor.");
                return false;
            }

            return true;
        }

        private static string BuildSource(List<Type> nodeTypes)
        {
            StringBuilder source = new StringBuilder();
            source.AppendLine("// <auto-generated>");
            source.AppendLine("// This file is generated by Cozy Nodes. Do not edit it manually.");
            source.AppendLine("// </auto-generated>");
            source.AppendLine();
            source.AppendLine("using System;");
            source.AppendLine("using ShaderFactory.CozyNodes.Editor;");
            source.AppendLine("using Unity.GraphToolkit.Editor;");
            source.AppendLine("using UnityEngine.Scripting.APIUpdating;");
            source.AppendLine();
            source.AppendLine("namespace " + GeneratedNamespace);
            source.AppendLine("{");

            foreach (Type nodeType in nodeTypes)
                AppendAdapter(source, nodeType);

            source.AppendLine("}");
            return source.ToString();
        }

        private static void AppendAdapter(StringBuilder source, Type nodeType)
        {
            CozyNodeDefinition definition = CozyNodeReflection.GetDefinition(nodeType);
            string category = "Cozy Nodes/" +
                              (string.IsNullOrWhiteSpace(definition.Category)
                                  ? "Game Nodes"
                                  : definition.Category);
            string className = "CozyNodeAdapter_" + GetStableIdentifier(nodeType);

            source.AppendLine("    [Serializable]");
            // Generated adapters compile into the game's Editor assembly, while
            // CozyGraph lives in the package's Editor assembly. Graph Toolkit
            // requires this public attribute to include cross-assembly nodes in
            // CozyGraph's native right-click item library.
            source.AppendLine("    [UseWithGraph(typeof(CozyGraph))]");
            foreach (CozyNodeMovedFromAttribute movedFrom in nodeType.GetCustomAttributes(
                         typeof(CozyNodeMovedFromAttribute),
                         true))
            {
                source.AppendLine(
                    "    [MovedFrom(true, \"" + EscapeString(movedFrom.SourceNamespace) +
                    "\", \"" + EscapeString(movedFrom.SourceAssembly) +
                    "\", \"" + EscapeString(movedFrom.SourceClassName) + "\")]" );
            }
            string iconPath = string.IsNullOrWhiteSpace(definition.IconAssetPath)
                ? "null"
                : "\"" + EscapeString(definition.IconAssetPath) + "\"";
            string nodeAttribute =
                "    [Node(\"" + EscapeString(category) + "\", " + iconPath + ", \"" +
                EscapeString(definition.Title) + "\"";

            if (HasPresentationStyle(definition))
                nodeAttribute += ", \"" + EscapeString(GetGeneratedStyleAssetPath(nodeType)) + "\"";

            source.AppendLine(nodeAttribute + ")]");
            source.AppendLine("    public sealed class " + className + " : CustomCozyNode");
            source.AppendLine("    {");
            source.AppendLine("        protected override Type GetCustomNodeType()");
            source.AppendLine("        {");
            source.AppendLine("            return typeof(" + GetSourceTypeName(nodeType) + ");");
            source.AppendLine("        }");
            source.AppendLine("    }");
            source.AppendLine();
        }

        private static string GetStableIdentifier(Type nodeType)
        {
            string typeName = nodeType.AssemblyQualifiedName;
            uint hash = 2166136261;

            for (int characterIndex = 0; characterIndex < typeName.Length; characterIndex++)
            {
                hash ^= typeName[characterIndex];
                hash *= 16777619;
            }

            return hash.ToString("X8");
        }

        private static string GetSourceTypeName(Type nodeType)
        {
            return "global::" + nodeType.FullName.Replace("+", ".");
        }

        private static string EscapeString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string GetGeneratedFilePath()
        {
            return Path.Combine(Application.dataPath, "CozyNodes.Generated", "Editor", GeneratedFileName);
        }

        private static string GetGeneratedFileAssetPath()
        {
            return GeneratedFolderAssetPath + "/" + GeneratedFileName;
        }

        private static bool HasPresentationStyle(CozyNodeDefinition definition)
        {
            if (definition == null)
                return false;

            if (definition.HasColor || definition.HasHeaderTextColor)
                return true;

            if (definition.MinimumWidth > 0)
                return true;

            foreach (CozyPortDefinition input in definition.Inputs)
            {
                if (input.MinTextAreaWidth > 0)
                    return true;
            }

            return false;
        }

        private static string BuildStyleSource(CozyNodeDefinition definition)
        {
            int textAreaWidth = 0;
            foreach (CozyPortDefinition input in definition.Inputs)
                textAreaWidth = Math.Max(textAreaWidth, input.MinTextAreaWidth);

            StringBuilder style = new StringBuilder();
            style.AppendLine("/* This file is generated by Cozy Nodes. Do not edit it manually. */");

            if (definition.HasColor)
            {
                // These are Graph Toolkit's public node-part USS class names.
                // The color is the node's header. Keeping the port area at
                // Graph Toolkit's normal dark surface preserves contrast for
                // values, ports, and user-authored options.
                string color = "#" + ColorUtility.ToHtmlStringRGBA(definition.Color);
                style.AppendLine("#title-icon-container,");
                style.AppendLine(".ge-node-title-part");
                style.AppendLine("{");
                style.AppendLine("    background-color: " + color + ";");
                style.AppendLine("}");
                style.AppendLine();
                style.AppendLine("#title-icon-container");
                style.AppendLine("{");
                style.AppendLine("    width: 100%;");
                style.AppendLine("}");
            }

            if (definition.HasHeaderTextColor)
            {
                if (definition.HasColor)
                    style.AppendLine();

                string headerTextColor = "#" +
                    ColorUtility.ToHtmlStringRGBA(definition.HeaderTextColor);
                style.AppendLine("#title-icon-container,");
                style.AppendLine(".ge-node-title-part,");
                style.AppendLine("#title-icon-container .unity-label,");
                style.AppendLine(".ge-node-title-part .unity-label");
                style.AppendLine("{");
                style.AppendLine("    color: " + headerTextColor + ";");
                style.AppendLine("}");
            }

            if (definition.MinimumWidth > 0)
            {
                if (definition.HasColor || definition.HasHeaderTextColor)
                    style.AppendLine();

                // Keep outputs at their natural width. A node's inputs and
                // outputs are separate Graph Toolkit containers, so sizing the
                // input container gives text room without stretching outputs.
                style.AppendLine(".ge-in-out-port-container-part #inputs");
                style.AppendLine("{");
                style.AppendLine("    min-width: " + definition.MinimumWidth + "px;");
                style.AppendLine("}");
            }

            if (textAreaWidth > 0)
            {
                if (definition.HasColor || definition.HasHeaderTextColor ||
                    definition.MinimumWidth > 0)
                    style.AppendLine();

                // Apply the field width only inside the input section. Outputs
                // keep their compact layout even when a node has a large text
                // editor on the other side.
                style.AppendLine(
                    ".ge-in-out-port-container-part #inputs .unity-base-text-field");
                style.AppendLine("{");
                style.AppendLine("    min-width: " + textAreaWidth + "px;");
                style.AppendLine("}");
            }

            return style.ToString();
        }

        private static string GetGeneratedStyleDirectoryPath()
        {
            return Path.Combine(
                Application.dataPath,
                "CozyNodes.Generated",
                "Editor",
                GeneratedStyleFolderName);
        }

        private static string GetGeneratedStyleAssetPath(Type nodeType)
        {
            return GeneratedFolderAssetPath + "/" + GeneratedStyleFolderName + "/" +
                   GeneratedStyleFilePrefix + GetStableIdentifier(nodeType) + ".uss";
        }

        private static string GetGeneratedStyleAssetPath(string styleFilePath)
        {
            return GeneratedFolderAssetPath + "/" + GeneratedStyleFolderName + "/" +
                   Path.GetFileName(styleFilePath);
        }

        private static string GetGeneratedStyleFilePath(Type nodeType)
        {
            return Path.Combine(
                GetGeneratedStyleDirectoryPath(),
                GeneratedStyleFilePrefix + GetStableIdentifier(nodeType) + ".uss");
        }
    }

    /// <summary>
    /// Regenerates adapters when game scripts change. The generated file is
    /// ignored as a trigger because generation writes only when its contents
    /// actually changed, preventing a recompilation loop.
    /// </summary>
    internal class CozyNodeAdapterGeneratorPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (ContainsScriptChangeOutsideGeneratedFolder(importedAssets) ||
                ContainsScriptChangeOutsideGeneratedFolder(deletedAssets) ||
                ContainsScriptChangeOutsideGeneratedFolder(movedAssets) ||
                ContainsScriptChangeOutsideGeneratedFolder(movedFromAssetPaths))
            {
                CozyNodeAdapterGenerator.ScheduleGeneration();
            }
        }

        private static bool ContainsScriptChangeOutsideGeneratedFolder(string[] assetPaths)
        {
            foreach (string assetPath in assetPaths)
            {
                if (!assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (CozyNodeAdapterGenerator.IsGeneratedAssetPath(assetPath))
                    continue;

                return true;
            }

            return false;
        }
    }
}
