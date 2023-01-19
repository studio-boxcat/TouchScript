/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using System.Linq;
using TouchScript.Hit;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;
using TouchScript.Core;
using UnityEngine.Assertions;

namespace TouchScript.Gestures
{
    /// <summary>
    /// Base class for all gestures.
    /// </summary>
    public abstract class Gesture : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Possible states of a gesture.
        /// </summary>
        public enum GestureState
        {
            /// <summary>
            /// Gesture is idle.
            /// </summary>
            Idle,

            /// <summary>
            /// Gesture started looking for the patern.
            /// </summary>
            Possible,

            /// <summary>
            /// Continuous gesture has just begun.
            /// </summary>
            Began,

            /// <summary>
            /// Started continuous gesture is updated.
            /// </summary>
            Changed,

            /// <summary>
            /// Continuous gesture is ended.
            /// </summary>
            Ended,

            /// <summary>
            /// Gesture is cancelled.
            /// </summary>
            Cancelled,

            /// <summary>
            /// Gesture is failed by itself or by another recognized gesture.
            /// </summary>
            Failed,

            /// <summary>
            /// Gesture is recognized.
            /// </summary>
            Recognized = Ended
        }

        /// <summary>
        /// Current state of the number of pointers.
        /// </summary>
        protected enum PointersNumState
        {
            /// <summary>
            /// The number of pointers is between min and max thresholds.
            /// </summary>
            InRange,

            /// <summary>
            /// The number of pointers is less than min threshold.
            /// </summary>
            TooFew,

            /// <summary>
            /// The number of pointers is greater than max threshold.
            /// </summary>
            TooMany,

            /// <summary>
            /// The number of pointers passed min threshold this frame and is now in range.
            /// </summary>
            PassedMinThreshold,

            /// <summary>
            /// The number of pointers passed max threshold this frame and is now in range.
            /// </summary>
            PassedMaxThreshold,

            /// <summary>
            /// The number of pointers passed both min and max thresholds.
            /// </summary>
            PassedMinMaxThreshold
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when gesture changes state.
        /// </summary>
        public event Action<GestureStateChangeEventArgs> StateChanged;

        /// <summary>
        /// Occurs when gesture is cancelled.
        /// </summary>
        public event Action Cancelled;

        #endregion

        #region Public properties

        /// <summary>
        /// Gets current gesture state.
        /// </summary>
        /// <value> Current state of the gesture. </value>
        public GestureState State
        {
            get { return state; }
            private set
            {
                PreviousState = state;
                state = value;

                switch (value)
                {
                    case GestureState.Idle:
                        onIdle();
                        break;
                    case GestureState.Possible:
                        onPossible();
                        break;
                    case GestureState.Began:
                        retainPointers();
                        onBegan();
                        break;
                    case GestureState.Changed:
                        onChanged();
                        break;
                    case GestureState.Recognized:
                        // Only retain/release pointers for continuos gestures
                        if (PreviousState is GestureState.Changed or GestureState.Began)
                            releasePointers(true);
                        onRecognized();
                        break;
                    case GestureState.Failed:
                        onFailed();
                        break;
                    case GestureState.Cancelled:
                        if (PreviousState is GestureState.Changed or GestureState.Began)
                            releasePointers(false);
                        onCancelled();
                        break;
                }

                StateChanged?.InvokeHandleExceptions(GestureStateChangeEventArgs.GetCachedEventArgs(state, PreviousState));
            }
        }

        /// <summary>
        /// Gets previous gesture state.
        /// </summary>
        /// <value> Previous state of the gesture. </value>
        public GestureState PreviousState { get; private set; }

        /// <summary>
        /// Gets current screen position.
        /// </summary>
        /// <value> Gesture's position in screen coordinates. </value>
        public virtual Vector2 ScreenPosition
        {
            get
            {
                if (NumPointers == 0)
                {
                    return cachedScreenPosition.IsValid()
                        ? cachedScreenPosition : InvalidPosition.Value;
                }
                return activePointers[0].Position;
            }
        }

        /// <summary>
        /// Gets previous screen position.
        /// </summary>
        /// <value> Gesture's previous position in screen coordinates. </value>
        public virtual Vector2 PreviousScreenPosition
        {
            get
            {
                if (NumPointers == 0)
                {
                    return cachedPreviousScreenPosition.IsValid()
                        ? cachedPreviousScreenPosition : InvalidPosition.Value;
                }
                return activePointers[0].PreviousPosition;
            }
        }

        /// <summary>
        /// Gets list of gesture's active pointers.
        /// </summary>
        /// <value> The list of pointers owned by this gesture. </value>
        public List<Pointer> ActivePointers => activePointers;

        /// <summary>
        /// Gets the number of active pointerss.
        /// </summary>
        /// <value> The number of pointers owned by this gesture. </value>
        public int NumPointers => numPointers;

        #endregion

        #region Private variables

        /// <summary>
        /// Reference to global TouchManager.
        /// </summary>
        protected TouchManager touchManager { get; private set; }

        /// <summary>
        /// The state of min/max number of pointers.
        /// </summary>
        protected PointersNumState pointersNumState { get; private set; }

        /// <summary>
        /// Pointers the gesture currently owns and works with.
        /// </summary>
        protected List<Pointer> activePointers = new List<Pointer>(10);

        /// <summary>
        /// Cached transform of the parent object.
        /// </summary>
        protected Transform cachedTransform;

        [SerializeField]
        private int minPointers = 0;

        [SerializeField]
        private int maxPointers = 0;

        [SerializeField]
        // Serialized list of gestures for Unity IDE.
        private List<Gesture> friendlyGestures = new List<Gesture>();

        private int numPointers;
        private GestureManager gestureManagerInstance;
        private GestureState state = GestureState.Idle;

        /// <summary>
        /// Cached screen position. 
        /// Used to keep tap's position which can't be calculated from pointers when the gesture is recognized since all pointers are gone.
        /// </summary>
        protected Vector2 cachedScreenPosition;

        /// <summary>
        /// Cached previous screen position.
        /// Used to keep tap's position which can't be calculated from pointers when the gesture is recognized since all pointers are gone.
        /// </summary>
        protected Vector2 cachedPreviousScreenPosition;

        #endregion

        #region Public methods

        /// <summary>
        /// Adds a friendly gesture.
        /// </summary>
        /// <param name="gesture"> The gesture. </param>
        public void AddFriendlyGesture(Gesture gesture)
        {
            if (gesture == null || gesture == this) return;

            registerFriendlyGesture(gesture);
            gesture.registerFriendlyGesture(this);
        }

        /// <summary>
        /// Checks if a gesture is friendly with this gesture.
        /// </summary>
        /// <param name="gesture"> A gesture to check. </param>
        /// <returns> <c>true</c> if gestures are friendly; <c>false</c> otherwise. </returns>
        public bool IsFriendly(Gesture gesture)
        {
            return friendlyGestures.Contains(gesture);
        }

        /// <summary>
        /// Determines whether gesture controls a pointer.
        /// </summary>
        /// <param name="pointer"> The pointer. </param>
        /// <returns> <c>true</c> if gesture controls the pointer point; <c>false</c> otherwise. </returns>
        public bool HasPointer(Pointer pointer)
        {
            return activePointers.Contains(pointer);
        }

        /// <summary>
        /// Determines whether this instance can prevent the specified gesture.
        /// </summary>
        /// <param name="gesture"> The gesture. </param>
        /// <returns> <c>true</c> if this instance can prevent the specified gesture; <c>false</c> otherwise. </returns>
        public virtual bool CanPreventGesture(Gesture gesture)
        {
            if (gesture.CanBePreventedByGesture(this)) return !IsFriendly(gesture);
            return false;
        }

        /// <summary>
        /// Determines whether this instance can be prevented by specified gesture.
        /// </summary>
        /// <param name="gesture"> The gesture. </param>
        /// <returns> <c>true</c> if this instance can be prevented by specified gesture; <c>false</c> otherwise. </returns>
        public virtual bool CanBePreventedByGesture(Gesture gesture) => !IsFriendly(gesture);

        /// <summary>
        /// Specifies if gesture can receive this specific pointer point.
        /// </summary>
        /// <param name="pointer"> The pointer. </param>
        /// <returns> <c>true</c> if this pointer should be received by the gesture; <c>false</c> otherwise. </returns>
        public virtual bool ShouldReceivePointer(Pointer pointer) => true;

        /// <summary>
        /// Specifies if gesture can begin or recognize.
        /// </summary>
        /// <returns> <c>true</c> if gesture should begin; <c>false</c> otherwise. </returns>
        public virtual bool ShouldBegin() => true;

        /// <summary>
        /// Cancels this gesture.
        /// </summary>
        /// <param name="cancelPointers"> if set to <c>true</c> also implicitly cancels all pointers owned by the gesture. </param>
        /// <param name="returnPointers"> if set to <c>true</c> redispatched all canceled pointers. </param>
        public void Cancel(bool cancelPointers, bool returnPointers)
        {
            switch (state)
            {
                case GestureState.Cancelled:
                case GestureState.Failed:
                    return;
            }

            setState(GestureState.Cancelled);

            if (!cancelPointers) return;
            for (var i = 0; i < numPointers; i++) touchManager.CancelPointer(activePointers[i].Id, returnPointers);
        }

        /// <summary>
        /// Returns <see cref="HitData"/> for gesture's <see cref="ScreenPosition"/>, i.e. what is right beneath it.
        /// </summary>
        public HitData GetScreenPositionHitData()
        {
            LayerManager.GetHitTarget(ScreenPosition, out var hit);
            return hit;
        }

        #endregion

        #region Unity methods

        /// <inheritdoc />
        protected virtual void Awake()
        {
            cachedTransform = transform;

            var count = friendlyGestures.Count;
            for (var i = 0; i < count; i++)
            {
                AddFriendlyGesture(friendlyGestures[i]);
            }
        }

        /// <summary>
        /// Unity Start handler.
        /// </summary>
        protected virtual void OnEnable()
        {
            // TouchManager might be different in another scene
            touchManager = TouchManager.Instance;
            gestureManagerInstance = GestureManager.Instance;

            INTERNAL_Reset();
        }

        /// <summary>
        /// Unity OnDisable handler.
        /// </summary>
        protected virtual void OnDisable()
        {
            setState(GestureState.Cancelled);
        }

        /// <summary>
        /// Unity OnDestroy handler.
        /// </summary>
        protected virtual void OnDestroy()
        {
            var copy = new List<Gesture>(friendlyGestures);
            var count = copy.Count;
            for (var i = 0; i < count; i++)
            {
                INTERNAL_RemoveFriendlyGesture(copy[i]);
            }
        }

        #endregion

        #region Internal functions

        internal void INTERNAL_SetState(GestureState value)
        {
            setState(value);
        }

        internal void INTERNAL_Reset()
        {
            activePointers.Clear();
            numPointers = 0;
            pointersNumState = PointersNumState.TooFew;
            reset();
        }

        internal void INTERNAL_PointersPressed(IList<Pointer> pointers)
        {
            Assert.IsTrue(pointers.All(p => activePointers.Contains(p) == false));

            var count = pointers.Count;
            var total = numPointers + count;
            pointersNumState = PointersNumState.InRange;

            if (minPointers <= 0)
            {
                // MinPointers is not set and we got our first pointers
                if (numPointers == 0) pointersNumState = PointersNumState.PassedMinThreshold;
            }
            else
            {
                if (numPointers < minPointers)
                {
                    // had < MinPointers, got >= MinPointers
                    if (total >= minPointers) pointersNumState = PointersNumState.PassedMinThreshold;
                    else pointersNumState = PointersNumState.TooFew;
                }
            }

            if (maxPointers > 0)
            {
                if (numPointers <= maxPointers)
                {
                    if (total > maxPointers)
                    {
                        // this event we crossed both MinPointers and MaxPointers
                        if (pointersNumState == PointersNumState.PassedMinThreshold) pointersNumState = PointersNumState.PassedMinMaxThreshold;
                        // this event we crossed MaxPointers
                        else pointersNumState = PointersNumState.PassedMaxThreshold;
                    }
                }
                // last event we already were over MaxPointers
                else pointersNumState = PointersNumState.TooMany;
            }

            if (state is GestureState.Began or GestureState.Changed)
            {
                for (var i = 0; i < count; i++) pointers[i].INTERNAL_Retain();
            }

            activePointers.AddRange(pointers);
            numPointers = total;
            pointersPressed(pointers);
        }

        internal void INTERNAL_PointersUpdated(IList<Pointer> pointers)
        {
            pointersNumState = PointersNumState.InRange;
            if (minPointers > 0 && numPointers < minPointers) pointersNumState = PointersNumState.TooFew;
            if (maxPointers > 0 && pointersNumState == PointersNumState.InRange && numPointers > maxPointers) pointersNumState = PointersNumState.TooMany;
            pointersUpdated(pointers);
        }

        internal void INTERNAL_PointersReleased(IList<Pointer> pointers)
        {
            Assert.IsTrue(pointers.All(p => activePointers.Contains(p)));

            var count = pointers.Count;
            var total = numPointers - count;
            pointersNumState = PointersNumState.InRange;

            if (minPointers <= 0)
            {
                // have no pointers
                if (total == 0) pointersNumState = PointersNumState.PassedMinThreshold;
            }
            else
            {
                if (numPointers >= minPointers)
                {
                    // had >= MinPointers, got < MinPointers
                    if (total < minPointers) pointersNumState = PointersNumState.PassedMinThreshold;
                }
                // last event we already were under MinPointers
                else pointersNumState = PointersNumState.TooFew;
            }

            if (maxPointers > 0)
            {
                if (numPointers > maxPointers)
                {
                    if (total <= maxPointers)
                    {
                        // this event we crossed both MinPointers and MaxPointers
                        if (pointersNumState == PointersNumState.PassedMinThreshold) pointersNumState = PointersNumState.PassedMinMaxThreshold;
                        // this event we crossed MaxPointers
                        else pointersNumState = PointersNumState.PassedMaxThreshold;
                    }
                    // last event we already were over MaxPointers
                    else pointersNumState = PointersNumState.TooMany;
                }
            }

            for (var i = 0; i < count; i++) activePointers.Remove(pointers[i]);
            numPointers = total;

            if (NumPointers == 0)
            {
                var lastPoint = pointers[count - 1];
                if (shouldCachePointerPosition(lastPoint))
                {
                    cachedScreenPosition = lastPoint.Position;
                    cachedPreviousScreenPosition = lastPoint.PreviousPosition;
                }
                else
                {
                    cachedScreenPosition = InvalidPosition.Value;
                    cachedPreviousScreenPosition = InvalidPosition.Value;
                }
            }

            pointersReleased(pointers);
        }

        internal void INTERNAL_PointersCancelled(IList<Pointer> pointers)
        {
            Assert.IsTrue(pointers.All(p => activePointers.Contains(p)));

            var count = pointers.Count;
            var total = numPointers - count;
            pointersNumState = PointersNumState.InRange;

            if (minPointers <= 0)
            {
                // have no pointers
                if (total == 0) pointersNumState = PointersNumState.PassedMinThreshold;
            }
            else
            {
                if (numPointers >= minPointers)
                {
                    // had >= MinPointers, got < MinPointers
                    if (total < minPointers) pointersNumState = PointersNumState.PassedMinThreshold;
                }
                // last event we already were under MinPointers
                else pointersNumState = PointersNumState.TooFew;
            }

            if (maxPointers > 0)
            {
                if (numPointers > maxPointers)
                {
                    if (total <= maxPointers)
                    {
                        // this event we crossed both MinPointers and MaxPointers
                        if (pointersNumState == PointersNumState.PassedMinThreshold) pointersNumState = PointersNumState.PassedMinMaxThreshold;
                        // this event we crossed MaxPointers
                        else pointersNumState = PointersNumState.PassedMaxThreshold;
                    }
                    // last event we already were over MaxPointers
                    else pointersNumState = PointersNumState.TooMany;
                }
            }

            for (var i = 0; i < count; i++) activePointers.Remove(pointers[i]);
            numPointers = total;
            pointersCancelled(pointers);
        }

        internal virtual void INTERNAL_RemoveFriendlyGesture(Gesture gesture)
        {
            if (gesture == null || gesture == this) return;

            unregisterFriendlyGesture(gesture);
            gesture.unregisterFriendlyGesture(this);
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Should the gesture cache this pointers to use it later in calculation of <see cref="ScreenPosition"/>.
        /// </summary>
        /// <param name="value"> Pointer to cache. </param>
        /// <returns> <c>true</c> if pointers should be cached; <c>false</c> otherwise. </returns>
        protected virtual bool shouldCachePointerPosition(Pointer value)
        {
            return true;
        }

        /// <summary>
        /// Tries to change gesture state.
        /// </summary>
        /// <param name="value"> New state. </param>
        /// <returns> <c>true</c> if state was changed; otherwise, <c>false</c>. </returns>
        protected bool setState(GestureState value)
        {
            if (gestureManagerInstance == null) return false;

            var newState = gestureManagerInstance.INTERNAL_GestureChangeState(this, value);
            State = newState;

            return value == newState;
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Called when new pointers appear.
        /// </summary>
        /// <param name="pointers"> The pointers. </param>
        protected virtual void pointersPressed(IList<Pointer> pointers) {}

        /// <summary>
        /// Called for moved pointers.
        /// </summary>
        /// <param name="pointers"> The pointers. </param>
        protected virtual void pointersUpdated(IList<Pointer> pointers) {}

        /// <summary>
        /// Called if pointers are removed.
        /// </summary>
        /// <param name="pointers"> The pointers. </param>
        protected virtual void pointersReleased(IList<Pointer> pointers) {}

        /// <summary>
        /// Called when pointers are cancelled.
        /// </summary>
        /// <param name="pointers"> The pointers. </param>
        protected virtual void pointersCancelled(IList<Pointer> pointers)
        {
            if (pointersNumState == PointersNumState.PassedMinThreshold)
            {
                // moved below the threshold
                switch (state)
                {
                    case GestureState.Began:
                    case GestureState.Changed:
                        // cancel started gestures
                        setState(GestureState.Cancelled);
                        break;
                }
            }
        }

        /// <summary>
        /// Called to reset gesture state after it fails or recognizes.
        /// </summary>
        protected virtual void reset()
        {
            cachedScreenPosition = InvalidPosition.Value;
            cachedPreviousScreenPosition = InvalidPosition.Value;
        }

        /// <summary>
        /// Called when state is changed to Idle.
        /// </summary>
        protected virtual void onIdle() {}

        /// <summary>
        /// Called when state is changed to Possible.
        /// </summary>
        protected virtual void onPossible() {}

        /// <summary>
        /// Called when state is changed to Began.
        /// </summary>
        protected virtual void onBegan() {}

        /// <summary>
        /// Called when state is changed to Changed.
        /// </summary>
        protected virtual void onChanged() {}

        /// <summary>
        /// Called when state is changed to Recognized.
        /// </summary>
        protected virtual void onRecognized() {}

        /// <summary>
        /// Called when state is changed to Failed.
        /// </summary>
        protected virtual void onFailed() {}

        /// <summary>
        /// Called when state is changed to Cancelled.
        /// </summary>
        protected virtual void onCancelled()
        {
            Cancelled?.InvokeHandleExceptions();
        }

        #endregion

        #region Private functions

        private void retainPointers()
        {
            var total = NumPointers;
            for (var i = 0; i < total; i++) activePointers[i].INTERNAL_Retain();
        }

        private void releasePointers(bool cancel)
        {
            var total = NumPointers;
            for (var i = 0; i < total; i++)
            {
                var pointer = activePointers[i];
                if (pointer.INTERNAL_Release() == 0 && cancel) touchManager.CancelPointer(pointer.Id, true);
            }
        }

        private void registerFriendlyGesture(Gesture gesture)
        {
            if (gesture == null || gesture == this) return;

            if (!friendlyGestures.Contains(gesture)) friendlyGestures.Add(gesture);
        }

        private void unregisterFriendlyGesture(Gesture gesture)
        {
            if (gesture == null || gesture == this) return;

            friendlyGestures.Remove(gesture);
        }

        #endregion
    }

    /// <summary>
    /// Event arguments for Gesture state change events.
    /// </summary>
    public class GestureStateChangeEventArgs
    {
        /// <summary>
        /// Previous gesture state.
        /// </summary>
        public Gesture.GestureState PreviousState { get; private set; }

        /// <summary>
        /// Current gesture state.
        /// </summary>
        public Gesture.GestureState State { get; private set; }

        private static GestureStateChangeEventArgs instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="GestureStateChangeEventArgs"/> class.
        /// </summary>
        public GestureStateChangeEventArgs() {}

        /// <summary>
        /// Returns cached instance of EventArgs.
        /// This cached EventArgs is reused throughout the library not to alocate new ones on every call.
        /// </summary>
        /// <param name="state"> Current gesture state. </param>
        /// <param name="previousState"> Previous gesture state. </param>
        /// <returns>Cached EventArgs object.</returns>
        public static GestureStateChangeEventArgs GetCachedEventArgs(Gesture.GestureState state, Gesture.GestureState previousState)
        {
            if (instance == null) instance = new GestureStateChangeEventArgs();
            instance.State = state;
            instance.PreviousState = previousState;
            return instance;
        }
    }
}