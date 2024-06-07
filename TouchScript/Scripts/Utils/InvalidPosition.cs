using UnityEngine;

namespace TouchScript.Utils
{
    static class InvalidPosition
    {
        public static readonly Vector2 Value = new(float.NaN, float.NaN);


        public static bool IsInvalid(this Vector2 position)
        {
            return float.IsNaN(position.x) && float.IsNaN(position.y);
        }

        public static bool IsValid(this Vector2 position) => !position.IsInvalid();
    }
}