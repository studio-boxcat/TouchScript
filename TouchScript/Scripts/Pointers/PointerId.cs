namespace TouchScript.Pointers
{
    public enum PointerId : int
    {
        Invalid = -1,
    }

    public static class PointerIdUtils
    {
        public static bool IsInvalid(this PointerId value) => value == PointerId.Invalid;
        public static bool IsValid(this PointerId value) => value != PointerId.Invalid;
    }
}