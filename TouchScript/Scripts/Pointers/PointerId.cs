namespace TouchScript.Pointers
{
    public enum PointerId
    {
        Invalid = -1,
    }

    public static class PointerIdUtils
    {
        public static bool IsValid(this PointerId value) => value != PointerId.Invalid;
    }
}