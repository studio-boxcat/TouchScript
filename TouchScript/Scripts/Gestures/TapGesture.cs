﻿/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections;
using System.Collections.Generic;
using TouchScript.Utils;
using TouchScript.Utils.Attributes;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Profiling;

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
        public event EventHandler<EventArgs> Tapped
        {
            add { tappedInvoker += value; }
            remove { tappedInvoker -= value; }
        }

        // Needed to overcome iOS AOT limitations
        private EventHandler<EventArgs> tappedInvoker;

        /// <summary>
        /// Unity event, occurs when gesture is recognized.
        /// </summary>
        public GestureEvent OnTap = new GestureEvent();

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets the number of taps required for the gesture to recognize.
        /// </summary>
        /// <value> The number of taps required for this gesture to recognize. <c>1</c> — dingle tap, <c>2</c> — double tap. </value>
        public int NumberOfTapsRequired
        {
            get { return numberOfTapsRequired; }
            set
            {
                if (value <= 0) numberOfTapsRequired = 1;
                else numberOfTapsRequired = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum hold time before gesture fails.
        /// </summary>
        /// <value> Number of seconds a user should hold their fingers before gesture fails. </value>
        public float TimeLimit
        {
            get { return timeLimit; }
            set { timeLimit = value; }
        }

        /// <summary>
        /// Gets or sets maximum distance for point cluster must move for the gesture to fail.
        /// </summary>
        /// <value> Distance in cm pointers must move before gesture fails. </value>
        public float DistanceLimit
        {
            get { return distanceLimit; }
            set
            {
                distanceLimit = value;
                distanceLimitInPixelsSquared = Mathf.Pow(distanceLimit * touchManager.DotsPerCentimeter, 2);
            }
        }

        #endregion

        #region Private variables

        [SerializeField]
        private int numberOfTapsRequired = 1;

        [SerializeField]
        [NullToggle(NullFloatValue = float.PositiveInfinity)]
        private float timeLimit = float.PositiveInfinity;

        [SerializeField]
        [NullToggle(NullFloatValue = float.PositiveInfinity)]
        private float distanceLimit = float.PositiveInfinity;

        private float distanceLimitInPixelsSquared;

        // isActive works in a tap cycle (i.e. when double/tripple tap is being recognized)
        // State -> Possible happens when the first pointer is detected
        private bool isActive = false;
        private int tapsDone;
        private Vector2 startPosition;
        private Vector2 totalMovement;

#if UNITY_5_6_OR_NEWER
        private CustomSampler gestureSampler;
#endif

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override bool ShouldReceivePointer(Pointer pointer)
        {
            if (!base.ShouldReceivePointer(pointer)) return false;
            // Ignore redispatched pointers — they come from 2+ pointer gestures when one is left with 1 pointer.
            // In this state it means that the user doesn't have an intention to tap the object.
            return (pointer.Flags & Pointer.FLAG_RETURNED) == 0;
        }

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

#if UNITY_5_6_OR_NEWER
            gestureSampler = CustomSampler.Create("[TouchScript] Tap Gesture");
#endif
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            distanceLimitInPixelsSquared = Mathf.Pow(distanceLimit * touchManager.DotsPerCentimeter, 2);
        }

        [ContextMenu("Basic Editor")]
        private void switchToBasicEditor()
        {
            basicEditor = true;
        }

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
#if UNITY_5_6_OR_NEWER
            gestureSampler.Begin();
#endif

            base.pointersPressed(pointers);

            if (pointersNumState == PointersNumState.PassedMaxThreshold ||
                pointersNumState == PointersNumState.PassedMinMaxThreshold)
            {
                setState(GestureState.Failed);
#if UNITY_5_6_OR_NEWER
                gestureSampler.End();
#endif
                return;
            }

            if (NumPointers == pointers.Count)
            {
                // the first ever pointer
                if (tapsDone == 0)
                {
                    startPosition = pointers[0].Position;
                    if (timeLimit < float.PositiveInfinity) StartCoroutine("wait");
                }
                else if (tapsDone >= numberOfTapsRequired) // Might be delayed and retapped while waiting
                {
                    reset();
                    startPosition = pointers[0].Position;
                    if (timeLimit < float.PositiveInfinity) StartCoroutine("wait");
                }
                else
                {
                    if (distanceLimit < float.PositiveInfinity)
                    {
                        if ((pointers[0].Position - startPosition).sqrMagnitude > distanceLimitInPixelsSquared)
                        {
                            setState(GestureState.Failed);
#if UNITY_5_6_OR_NEWER
                            gestureSampler.End();
#endif
                            return;
                        }
                    }
                }
            }
            if (pointersNumState == PointersNumState.PassedMinThreshold)
            {
                // Starting the gesture when it is already active? => we released one finger and pressed again
                if (isActive) setState(GestureState.Failed);
                else
                {
                    if (State == GestureState.Idle) setState(GestureState.Possible);
                    isActive = true;
                }
            }

#if UNITY_5_6_OR_NEWER
            gestureSampler.End();
#endif
        }

        /// <inheritdoc />
        protected override void pointersUpdated(IList<Pointer> pointers)
        {
#if UNITY_5_6_OR_NEWER
            gestureSampler.Begin();
#endif

            base.pointersUpdated(pointers);

            if (distanceLimit < float.PositiveInfinity)
            {
                totalMovement += pointers[0].Position - pointers[0].PreviousPosition;
                if (totalMovement.sqrMagnitude > distanceLimitInPixelsSquared) setState(GestureState.Failed);
            }

#if UNITY_5_6_OR_NEWER
            gestureSampler.End();
#endif
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
#if UNITY_5_6_OR_NEWER
            gestureSampler.Begin();
#endif

            base.pointersReleased(pointers);

            {
                if (NumPointers == 0)
                {
                    if (!isActive)
                    {
                        setState(GestureState.Failed);
#if UNITY_5_6_OR_NEWER
                        gestureSampler.End();
#endif
                        return;
                    }

                    // pointers outside of gesture target are ignored in shouldCachePointerPosition()
                    // if all pointers are outside ScreenPosition will be invalid
                    if (TouchManager.IsInvalidPosition(ScreenPosition))
                    {
                        setState(GestureState.Failed);
                    }
                    else
                    {
                        tapsDone++;
                        isActive = false;
                        if (tapsDone >= numberOfTapsRequired) setState(GestureState.Recognized);
                    }
                }
            }

#if UNITY_5_6_OR_NEWER
            gestureSampler.End();
#endif
        }

        /// <inheritdoc />
        protected override void onRecognized()
        {
            base.onRecognized();

            StopCoroutine("wait");
            if (tappedInvoker != null) tappedInvoker.InvokeHandleExceptions(this, EventArgs.Empty);
            if (UseUnityEvents) OnTap.Invoke(this);
        }

        /// <inheritdoc />
        protected override void reset()
        {
            base.reset();

            isActive = false;
            totalMovement = Vector2.zero;
            StopCoroutine("wait");
            tapsDone = 0;
        }

        /// <inheritdoc />
        protected override bool shouldCachePointerPosition(Pointer value)
        {
            // Points must be over target when released
            return PointerUtils.IsPointerOnTarget(value, cachedTransform);
        }

        #endregion

        #region private functions

        private IEnumerator wait()
        {
            // WaitForSeconds is affected by time scale!
            var targetTime = Time.unscaledTime + TimeLimit;
            while (targetTime > Time.unscaledTime) yield return null;

            if (State == GestureState.Idle || State == GestureState.Possible) setState(GestureState.Failed);
        }

        #endregion
    }
}