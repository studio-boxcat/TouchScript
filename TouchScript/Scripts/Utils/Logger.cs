using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace TouchScript.Utils
{
    public readonly struct Logger
    {
        readonly string _tag;

        public Logger(string tag)
        {
            _tag = tag;
        }

        [Conditional("DEBUG")]
        public void Info(string message)
        {
            Debug.Log($"[TS:{_tag}] {message}");
        }

        [Conditional("DEBUG")]
        public void Warning(string message)
        {
            Debug.LogWarning($"[TS:{_tag}] {message}");
        }

        public void Error(string message)
        {
            Debug.LogError($"[TS:{_tag}] {message}");
        }
    }
}