using System.Diagnostics;
using UnityEngine;
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
        public void Info(string message, Object context = null)
        {
            Debug.Log($"[TS:{_tag}] {message}", context);
        }

        [Conditional("DEBUG")]
        public void Warning(string message, Object context = null)
        {
            Debug.LogWarning($"[TS:{_tag}] {message}", context);
        }

        public void Error(string message)
        {
            Debug.LogError($"[TS:{_tag}] {message}");
        }
    }
}