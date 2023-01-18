namespace TouchScript.Pointers
{
    public enum PointerId : int
    {
        Invalid = -1,
    }

    public static class PointerIdIssuer
    {
        static PointerId _nextPointerId = (PointerId) 1;

        public static PointerId Issue() => _nextPointerId++;
    }
}