using UnityEngine;

namespace TouchScript.Utils
{
    public static class Logger
    {
        public static void Error(string message)
        {
            Debug.LogError(message);
        }
    }
}