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
    /// Recognizes a tap.
    /// </summary>
    [AddComponentMenu("TouchScript/Gestures/Tap Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_TapGesture.htm")]
    public class TapGesture : Gesture
    {
        #region Events

        /// <summary>
        /// Occurs when gesture is recognized.
        /// </summary>
        public event Action Tapped;

        #endregion

        #region Private variables

        [SerializeField]
        private int numberOfTapsRequired = 1;

        // isActive works in a tap cycle (i.e. when double/tripple tap is being recognized)
        // State -> Possible happens when the first pointer is detected
        private bool isActive = false;
        private int tapsDone;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override bool ShouldReceivePointer(Pointer pointer)
        {
            if (!base.ShouldReceivePointer(pointer)) return false;
            // Ignore redispatched pointers — they come from 2+ pointer gestures when one is left with 1 pointer.
            // In this state it means that the user doesn't have an intention to tap the object.
            return pointer.IsReturned is false;
        }

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (activePointers.Count == pointers.Count)
            {
                if (tapsDone >= numberOfTapsRequired) // Might be delayed and retapped while waiting
                    reset();
            }
            if (pointersNumState == PointersNumState.None)
            {
                // Starting the gesture when it is already active? => we released one finger and pressed again
                if (isActive) setState(GestureState.Failed);
                else
                {
                    if (State == GestureState.Idle) setState(GestureState.Possible);
                    isActive = true;
                }
            }
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
            base.pointersReleased(pointers);

            {
                if (activePointers.Count == 0)
                {
                    if (!isActive)
                    {
                        setState(GestureState.Failed);
                        return;
                    }

                    // pointers outside of gesture target are ignored in shouldCachePointerPosition()
                    // if all pointers are outside ScreenPosition will be invalid
                    if (ScreenPosition.IsInvalid())
                    {
                        setState(GestureState.Failed);
                    }
                    else
                    {
                        tapsDone++;
                        isActive = false;
                        if (tapsDone >= numberOfTapsRequired) setState(GestureState.Ended);
                    }
                }
            }
        }

        /// <inheritdoc />
        protected override void onRecognized()
        {
            base.onRecognized();

            if (State.IsIdleOrPossible())
                setState(GestureState.Failed);
            Tapped?.InvokeHandleExceptions();
        }

        /// <inheritdoc />
        protected override void reset()
        {
            base.reset();

            isActive = false;
            if (State.IsIdleOrPossible())
                setState(GestureState.Failed);
            tapsDone = 0;
        }

        /// <inheritdoc />
        protected override bool shouldCachePointerPosition(Pointer value)
        {
            // Points must be over target when released
            return PointerUtils.IsPointerOnTarget(value, cachedTransform);
        }

        #endregion
    }
}