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
    /// Recognizes when last pointer is released from target. Works with any gesture unless a Delegate is set. 
    /// </summary>
    /// <seealso cref="PressGesture"/>
    [AddComponentMenu("TouchScript/Gestures/Release Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_ReleaseGesture.htm")]
    public class ReleaseGesture : Gesture
    {
        #region Events

        /// <summary>
        /// Occurs when gesture is recognized.
        /// </summary>
        public event Action Released;

        #endregion

		#region Gesture callbacks

        /// <inheritdoc />
        public override bool CanPreventGesture() => false;

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (pointersNumState == PointersNumState.None && State == GestureState.Idle)
            {
                setState(GestureState.Possible);
            }
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
            base.pointersReleased(pointers);

            if (pointersNumState == PointersNumState.None) setState(GestureState.Ended);
        }

        /// <inheritdoc />
        protected override void onRecognized()
        {
            base.onRecognized();
            Released?.InvokeHandleExceptions();
        }

        #endregion
    }
}