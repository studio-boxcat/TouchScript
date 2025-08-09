using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TouchScript
{
    internal static class L
    {
        [Conditional("DEBUG"), HideInCallstack]
        public static void I(string message, Object context = null) => Debug.Log($"[TouchScript] {message}", context);
        [Conditional("DEBUG"), HideInCallstack]
        public static void W(string message, Object context = null) => Debug.LogWarning($"[TouchScript] {message}", context);
        [HideInCallstack]
        public static void E(string message) => Debug.LogError($"[TouchScript] {message}");
    }
}