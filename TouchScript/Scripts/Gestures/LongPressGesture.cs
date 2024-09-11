/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TouchScript.Core;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.Gestures
{
    /// <summary>
    /// Gesture which recognizes a point cluster which didn't move for specified time since it appeared.
    /// </summary>
    [AddComponentMenu("TouchScript/Gestures/Long Press Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_LongPressGesture.htm")]
    public class LongPressGesture : Gesture
    {
        #region Events

        /// <summary>
        /// Occurs when gesture is recognized.
        /// </summary>
        public event Action LongPressed;

        #endregion

        #region Private variables

        [SerializeField]
        private float timeToPress = 1;

        [SerializeField]
        private float distanceLimit;

        private float distanceLimitInPixelsSquared;

        private Vector2 totalMovement;

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (pointersNumState == PointersNumState.None)
            {
                setState(GestureState.Possible);
                StartCoroutine(wait());
            }
        }

        /// <inheritdoc />
        protected override void pointersUpdated(IList<Pointer> pointers)
        {
            base.pointersUpdated(pointers);

            if (distanceLimit is not 0)
            {
                totalMovement += ScreenPosition - PreviousScreenPosition;

                // Initialize distanceLimitInPixelsSquared
                if (distanceLimitInPixelsSquared is 0)
                {
                    var limitInPixels = distanceLimit * DisplayDevice.DotsPerCentimeter;
                    distanceLimitInPixelsSquared = limitInPixels * limitInPixels;
                }

                if (totalMovement.sqrMagnitude > distanceLimitInPixelsSquared)
                    setState(GestureState.Failed);
            }
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
            base.pointersReleased(pointers);

            if (pointersNumState == PointersNumState.None)
            {
                setState(GestureState.Failed);
            }
        }

        /// <inheritdoc />
        protected override void onRecognized()
        {
            LongPressed?.InvokeHandleExceptions();
        }

        /// <inheritdoc />
        protected override void reset()
        {
            base.reset();

            totalMovement = Vector2.zero;
            StopCoroutine(wait());
        }

        #endregion

        #region Private functions

        private IEnumerator wait()
        {
            // WaitForSeconds is affected by time scale!
            var targetTime = Time.unscaledTime + timeToPress;
            while (targetTime > Time.unscaledTime) yield return null;

            if (State is not GestureState.Possible)
                yield break;

            var isHit = LayerManager.GetHitTarget(ScreenPosition, out var hit)
                        && hit.Target.IsChildOf(cachedTransform);
            setState(isHit ? GestureState.Ended : GestureState.Failed);
        }

        #endregion
    }
}