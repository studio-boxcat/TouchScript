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
    /// Recognizes fast movement before releasing pointers. Doesn't care how much time pointers were on surface and how much they moved.
    /// </summary>
    [AddComponentMenu("TouchScript/Gestures/Flick Gesture")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Gestures_FlickGesture.htm")]
    public class FlickGesture : Gesture
    {
        #region Constants

        /// <summary>
        /// Direction of a flick.
        /// </summary>
        public enum GestureDirection
        {
            /// <summary>
            /// Direction doesn't matter.
            /// </summary>
            Any,

            /// <summary>
            /// Only horizontal.
            /// </summary>
            Horizontal,

            /// <summary>
            /// Only vertical.
            /// </summary>
            Vertical,
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when gesture is recognized.
        /// </summary>
        public event Action Flicked;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets or sets time interval in seconds in which pointers must move by <see cref="MinDistance"/> for gesture to succeed.
        /// </summary>
        /// <value> Interval in seconds in which pointers must move by <see cref="MinDistance"/> for gesture to succeed. </value>
        public float FlickTime
        {
            get { return flickTime; }
            set { flickTime = value; }
        }

        /// <summary>
        /// Gets or sets minimum distance in cm to move in <see cref="FlickTime"/> before ending gesture for it to be recognized.
        /// </summary>
        /// <value> Minimum distance in cm to move in <see cref="FlickTime"/> before ending gesture for it to be recognized. </value>
        public float MinDistance
        {
            get { return minDistance; }
            set { minDistance = value; }
        }

        /// <summary>
        /// Gets or sets minimum distance in cm pointers must move to start recognizing this gesture.
        /// </summary>
        /// <value> Minimum distance in cm pointers must move to start recognizing this gesture. </value>
        /// <remarks> Prevents misinterpreting taps. </remarks>
        public float MovementThreshold
        {
            get { return movementThreshold; }
            set { movementThreshold = value; }
        }

        /// <summary>
        /// Gets or sets direction to look for.
        /// </summary>
        /// <value> Direction of movement. </value>
        public GestureDirection Direction
        {
            get { return direction; }
            set { direction = value; }
        }

        /// <summary>
        /// Gets flick direction (not normalized) when gesture is recognized.
        /// </summary>
        public Vector2 ScreenFlickVector { get; private set; }

        /// <summary>
        /// Gets flick time in seconds pointers moved by <see cref="ScreenFlickVector"/>.
        /// </summary>
        public float ScreenFlickTime { get; private set; }

        #endregion

        #region Private variables

        [SerializeField]
        private float flickTime = .1f;

        [SerializeField]
        private float minDistance = 1f;

        [SerializeField]
        private float movementThreshold = .5f;

        [SerializeField]
        private GestureDirection direction = GestureDirection.Any;

        private bool moving = false;
        private Vector2 movementBuffer = Vector2.zero;
        private bool isActive = false;
        private TimedSequence<Vector2> deltaSequence = new TimedSequence<Vector2>();

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected void LateUpdate()
        {
            if (!isActive) return;

            deltaSequence.Add(ScreenPosition - PreviousScreenPosition);
        }

        #endregion

        #region Gesture callbacks

        /// <inheritdoc />
        protected override void pointersPressed(IList<Pointer> pointers)
        {
            base.pointersPressed(pointers);

            if (pointersNumState == PointersNumState.None)
            {
                // Starting the gesture when it is already active? => we released one finger and pressed again while moving
                if (isActive) setState(GestureState.Failed);
                else isActive = true;
            }
        }

        /// <inheritdoc />
        protected override void pointersUpdated(IList<Pointer> pointers)
        {
            base.pointersUpdated(pointers);

            if (isActive || !moving)
            {
                movementBuffer += ScreenPosition - PreviousScreenPosition;
                var dpiMovementThreshold = MovementThreshold * DisplayDevice.DotsPerCentimeter;
                if (movementBuffer.sqrMagnitude >= dpiMovementThreshold * dpiMovementThreshold)
                {
                    moving = true;
                }
            }
        }

        /// <inheritdoc />
        protected override void pointersReleased(IList<Pointer> pointers)
        {
            base.pointersReleased(pointers);

            if (activePointers.Count == 0)
            {
                if (!isActive || !moving)
                {
                    setState(GestureState.Failed);
                    return;
                }

                deltaSequence.Add(ScreenPosition - PreviousScreenPosition);

                float lastTime;
                var deltas = deltaSequence.FindElementsLaterThan(Time.unscaledTime - FlickTime, out lastTime);
                var totalMovement = Vector2.zero;
                var count = deltas.Count;
                for (var i = 0; i < count; i++) totalMovement += deltas[i];

                switch (Direction)
                {
                    case GestureDirection.Horizontal:
                        totalMovement.y = 0;
                        break;
                    case GestureDirection.Vertical:
                        totalMovement.x = 0;
                        break;
                }

                if (totalMovement.magnitude < MinDistance * DisplayDevice.DotsPerCentimeter)
                {
                    setState(GestureState.Failed);
                }
                else
                {
                    ScreenFlickVector = totalMovement;
                    ScreenFlickTime = Time.unscaledTime - lastTime;
                    setState(GestureState.Ended);
                }
            }
        }

        /// <inheritdoc />
        protected override void onRecognized()
        {
            base.onRecognized();
            Flicked?.InvokeHandleExceptions();
        }

        /// <inheritdoc />
        protected override void reset()
        {
            base.reset();

            isActive = false;
            moving = false;
            movementBuffer = Vector2.zero;
        }

        #endregion
    }
}