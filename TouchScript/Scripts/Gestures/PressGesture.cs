/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.Gestures
{
    /// <summary>
    /// Recognizes when an object is touched. Works with any gesture unless a Delegate is set.
    /// </summary>
    /// <remarks>
    /// <para>PressGesture fires immediately and would ultimately kill every other non-friendly gesture. So one would have to manually make it friendly with everything in a general use-case. That's why it's made friendly with everyone by default.</para>
    /// <para>But there are cases when one would like to determine if parent container was pressed or its child. In current implementation both PressGestures will fire.</para>
    /// <para>One approach would be to somehow make parent's PressGesture not friendly with child's one. But looking at how gesture recognition works we can see that this won't work. Since we would like child's gesture to fail parent's gesture. When child's PressGesture is recognized the system asks it if it can prevent parent's gesture, and it obviously can't because it's friendly with everything. And it doesn't matter that parent's gesture can be prevented by child's one... because child's one can't prevent parent's gesture and this is asked first.</para>
    /// <para>This is basically what <see cref="IgnoreChildren"/> is for. It makes parent's PressGesture only listen for TouchPoints which lend directly on it.</para>
    /// </remarks>
    [AddComponentMenu("TouchScript/Gestures/Press Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_PressGesture.htm")]
    public class PressGesture : Gesture
    {
        #region Events

        /// <summary>
        /// Occurs when gesture is recognized.
        /// </summary>
        public event Action Pressed;

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        public override bool CanPreventGesture() => false;

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (pointersNumState == PointersNumState.None)
                setState(GestureState.Ended);
        }

        /// <inheritdoc />
        protected override void onRecognized()
        {
            base.onRecognized();
            Pressed?.InvokeHandleExceptions();
        }

        #endregion
    }
}