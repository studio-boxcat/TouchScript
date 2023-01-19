/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Hit;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.Utils
{
    /// <summary>
    /// Utility methods to work with Pointers.
    /// </summary>
    public static class PointerUtils
    {
        /// <summary>
        /// Determines whether the pointer is over a specific GameObject.
        /// </summary>
        /// <param name="pointer"> The pointer. </param>
        /// <param name="target"> The target. </param>
        /// <returns> <c>true</c> if the pointer is over the GameObject; <c>false</c> otherwise.</returns>
        public static bool IsPointerOnTarget(Pointer pointer, Transform target)
        {
            return IsPointerOnTarget(pointer, target, out _);
        }

        /// <summary>
        /// Determines whether the pointer is over a specific GameObject.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <param name="target">The target.</param>
        /// <param name="hit">The hit.</param>
        /// <returns> <c>true</c> if the pointer is over the GameObject; <c>false</c> otherwise. </returns>
        public static bool IsPointerOnTarget(Pointer pointer, Transform target, out HitData hit)
        {
            hit = default(HitData);
            if (pointer == null || target == null) return false;
            hit = pointer.GetOverData();
            if (hit.Target == null) return false;
            return hit.Target.IsChildOf(target);
        }

        /// <summary>
        /// Adds pressed state to downed buttons.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <returns>Changed buttons state.</returns>
        public static PointerButtonState DownPressedButtons(PointerButtonState buttons)
        {
            if (buttons.Pressed)
                buttons.Down = true;
            return buttons;
        }

        /// <summary>
        /// Adds downed state to pressed buttons.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <returns>Changed buttons state.</returns>
        public static PointerButtonState PressDownButtons(PointerButtonState buttons)
        {
            if (buttons.Down)
                buttons.Pressed = true;
            return buttons;
        }

        /// <summary>
        /// Converts pressed buttons to up state.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <returns>Changed buttons state.</returns>
        public static PointerButtonState UpPressedButtons(PointerButtonState buttons)
        {
            return new PointerButtonState {Up = buttons.Pressed};
        }
    }
}