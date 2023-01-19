namespace TouchScript.Pointers
{
    public enum PointerId : int
    {
        Invalid = -1,
    }

    public static class PointerIdUtils
    {
        static PointerId _nextPointerId = (PointerId) 1;

        public static PointerId IssueId() => _nextPointerId++;

        public static bool IsInvalid(this PointerId value) => value == PointerId.Invalid;
        public static bool IsValid(this PointerId value) => value != PointerId.Invalid;
    }
}