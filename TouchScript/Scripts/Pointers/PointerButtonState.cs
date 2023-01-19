using System;

namespace TouchScript.Pointers
{
    public struct PointerButtonState
    {
        public bool Pressed;
        public bool Down;
        public bool Up;

        public bool Released => !Pressed;

        public void PressDown()
        {
            Down = true;
            Pressed = true;
        }

        public void PressUp()
        {
            Pressed = false;
            Up = true;
        }

        public void UnsetAction()
        {
            Down = false;
            Up = false;
        }

        public bool Equals(PointerButtonState other) => Pressed == other.Pressed && Down == other.Down && Up == other.Up;
        public override bool Equals(object obj) => obj is PointerButtonState other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Pressed, Down, Up);

        public static bool operator ==(PointerButtonState a, PointerButtonState b) => a.Equals(b);
        public static bool operator !=(PointerButtonState a, PointerButtonState b) => !(a == b);
    }
}