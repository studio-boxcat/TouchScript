/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Text;
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
        private static StringBuilder sb;

        /// <summary>
        /// Determines whether the pointer is over its target GameObject.
        /// </summary>
        /// <param name="pointer"> The pointer. </param>
        /// <returns> <c>true</c> if the pointer is over the GameObject; <c>false</c> otherwise.</returns>
        public static bool IsPointerOnTarget(Pointer pointer)
        {
            if (pointer == null) return false;
            return IsPointerOnTarget(pointer, pointer.GetPressData().Target);
        }

        /// <summary>
        /// Determines whether the pointer is over a specific GameObject.
        /// </summary>
        /// <param name="pointer"> The pointer. </param>
        /// <param name="target"> The target. </param>
        /// <returns> <c>true</c> if the pointer is over the GameObject; <c>false</c> otherwise.</returns>
        public static bool IsPointerOnTarget(IPointer pointer, Transform target)
        {
            HitData hit;
            return IsPointerOnTarget(pointer, target, out hit);
        }

        /// <summary>
        /// Determines whether the pointer is over a specific GameObject.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <param name="target">The target.</param>
        /// <param name="hit">The hit.</param>
        /// <returns> <c>true</c> if the pointer is over the GameObject; <c>false</c> otherwise. </returns>
        public static bool IsPointerOnTarget(IPointer pointer, Transform target, out HitData hit)
        {
            hit = default(HitData);
            if (pointer == null || target == null) return false;
            hit = pointer.GetOverData();
            if (hit.Target == null) return false;
            return hit.Target.IsChildOf(target);
        }

        /// <summary>
        /// Formats currently pressed buttons as a string.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <returns>Formatted string of currently pressed buttons.</returns>
        public static string PressedButtonsToString(Pointer.PointerButtonState buttons)
        {
            initStringBuilder();

            PressedButtonsToString(buttons, sb);
            return sb.ToString();
        }

        /// <summary>
        /// Formats currently pressed buttons as a string.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <param name="builder">The string builder to use.</param>
        public static void PressedButtonsToString(Pointer.PointerButtonState buttons, StringBuilder builder)
        {
            if ((buttons & Pointer.PointerButtonState.FirstButtonPressed) != 0) builder.Append("1");
            else builder.Append("_");
        }

        /// <summary>
        /// Formats the state of buttons as a string.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <returns>Formatted string of the buttons state.</returns>
        public static string ButtonsToString(Pointer.PointerButtonState buttons)
        {
            initStringBuilder();

            ButtonsToString(buttons, sb);
            return sb.ToString();
        }

        /// <summary>
        /// Formats the state of buttons as a string.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <param name="builder">The string builder to use.</param>
        public static void ButtonsToString(Pointer.PointerButtonState buttons, StringBuilder builder)
        {
            if ((buttons & Pointer.PointerButtonState.FirstButtonDown) != 0) builder.Append("v");
            else if ((buttons & Pointer.PointerButtonState.FirstButtonUp) != 0) builder.Append("^");
            else if ((buttons & Pointer.PointerButtonState.FirstButtonPressed) != 0) builder.Append("1");
            else builder.Append("_");
        }

        /// <summary>
        /// Adds pressed state to downed buttons.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <returns>Changed buttons state.</returns>
        public static Pointer.PointerButtonState DownPressedButtons(Pointer.PointerButtonState buttons)
        {
            if ((buttons & Pointer.PointerButtonState.FirstButtonPressed) != 0)
                buttons |= Pointer.PointerButtonState.FirstButtonDown;
            return buttons;
        }

        /// <summary>
        /// Adds downed state to pressed buttons.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <returns>Changed buttons state.</returns>
        public static Pointer.PointerButtonState PressDownButtons(Pointer.PointerButtonState buttons)
        {
            if ((buttons & Pointer.PointerButtonState.FirstButtonDown) != 0)
                buttons |= Pointer.PointerButtonState.FirstButtonPressed;
            return buttons;
        }

        /// <summary>
        /// Converts pressed buttons to up state.
        /// </summary>
        /// <param name="buttons">The buttons state.</param>
        /// <returns>Changed buttons state.</returns>
        public static Pointer.PointerButtonState UpPressedButtons(Pointer.PointerButtonState buttons)
        {
            var btns = Pointer.PointerButtonState.Nothing;
            if ((buttons & Pointer.PointerButtonState.FirstButtonPressed) != 0)
                btns |= Pointer.PointerButtonState.FirstButtonUp;
            return btns;
        }

        private static void initStringBuilder()
        {
            if (sb == null) sb = new StringBuilder();
            sb.Length = 0;
        }
    }
}