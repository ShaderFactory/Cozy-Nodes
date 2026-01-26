using UnityEngine;

namespace ShaderFactory.CozyGraphToolkit.Editor
{
    public static class StringHelper
    {
        public static string RemoveHierarchyFromClassName(string _text)
        {
            string[] splitText = _text.Split(".");
            return splitText[splitText.Length - 1];
        }
    }
}
