using System.Diagnostics;

namespace TouchScript.Utils
{
    static class L
    {
        [Conditional("DEBUG")]
        public static void W(string message, UnityEngine.Object context = null)
        {
            UnityEngine.Debug.LogWarning(message, context);
        }
    }
}