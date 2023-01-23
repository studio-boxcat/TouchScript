/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TouchScript.Devices.Display;
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

        #region Public properties

        /// <summary>
        /// Gets or sets total time in seconds required to hold pointers still.
        /// </summary>
        /// <value> Time in seconds. </value>
        public float TimeToPress
        {
            get { return timeToPress; }
            set { timeToPress = value; }
        }

        #endregion

        #region Private variables

        [SerializeField]
        private float timeToPress = 1;

        [SerializeField]
        private float distanceLimit = float.PositiveInfinity;

        private float distanceLimitInPixelsSquared;

        private Vector2 totalMovement;

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            distanceLimitInPixelsSquared = Mathf.Pow(distanceLimit * DisplayDevice.DotsPerCentimeter, 2);
        }

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (pointersNumState == PointersNumState.PassedMaxThreshold ||
                pointersNumState == PointersNumState.PassedMinMaxThreshold)
            {
                setState(GestureState.Failed);
            }
            else if (pointersNumState == PointersNumState.PassedMinThreshold)
            {
                setState(GestureState.Possible);
                StartCoroutine(wait());
            }
        }

        /// <inheritdoc />
        protected override void pointersUpdated(IList<Pointer> pointers)
        {
            base.pointersUpdated(pointers);

            if (distanceLimit < float.PositiveInfinity)
            {
                totalMovement += ScreenPosition - PreviousScreenPosition;
                if (totalMovement.sqrMagnitude > distanceLimitInPixelsSquared) setState(GestureState.Failed);
            }
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
            base.pointersReleased(pointers);

            if (pointersNumState == PointersNumState.PassedMinThreshold)
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
            var targetTime = Time.unscaledTime + TimeToPress;
            while (targetTime > Time.unscaledTime) yield return null;

            if (State == GestureState.Possible)
            {
                var data = GetScreenPositionHitData();
                if (data.Target == null || !data.Target.IsChildOf(cachedTransform))
                    setState(GestureState.Failed);
                else
                    setState(GestureState.Ended);
            }
        }

        #endregion
    }
}