using System.Diagnostics;
using UnityEngine.Assertions;

namespace TouchScript.InputSources
{
    public struct PointerChange
    {
        public bool Added;
        public bool Updated;
        public bool Pressed;
        public bool Released;
        public bool Removed;
        public bool Cancelled;

        public override string ToString()
        {
            var str = "(";
            if (Added) str += "Added|";
            if (Updated) str += "Updated|";
            if (Pressed) str += "Pressed|";
            if (Released) str += "Released|";
            if (Removed) str += "Removed|";
            if (Cancelled) str += "Cancelled|";
            str += ")";
            return str;
        }

        public bool CancelledOnly()
        {
            return !Added && !Updated && !Pressed && !Released && !Removed && Cancelled;
        }

        public static PointerChange MergeWithCheck(PointerChange a, PointerChange b)
        {
            AssertCollision(a, b);

            return new PointerChange
            {
                Added = a.Added || b.Added,
                Updated = a.Updated || b.Updated,
                Pressed = a.Pressed || b.Pressed,
                Released = a.Released || b.Released,
                Removed = a.Removed || b.Removed,
                Cancelled = a.Cancelled || b.Cancelled,
            };
        }

        [Conditional("DEBUG")]
        private static void AssertCollision(PointerChange a, PointerChange b)
        {
            Assert.IsFalse(a.Added && b.Added);
            Assert.IsFalse(a.Updated && b.Updated);
            Assert.IsFalse(a.Pressed && b.Pressed);
            Assert.IsFalse(a.Released && b.Released);
            Assert.IsFalse(a.Removed && b.Removed);
            Assert.IsFalse(a.Cancelled && b.Cancelled);
        }
    }
}