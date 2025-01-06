/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using System.Linq;
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
        /// Current state of the number of pointers.
        /// </summary>
        protected enum PointersNumState
        {
            Reset, // Not valuated yet
            None,
            Exists,
        }

        #endregion

        #region Events

        /// <summary>
        /// Occurs when gesture changes state.
        /// </summary>
        public event Action<GestureState, GestureState> StateChanged;

        #endregion

        #region Public properties

        private GestureState state = GestureState.Idle;

        /// <summary>
        /// Gets current gesture state.
        /// </summary>
        /// <value> Current state of the gesture. </value>
        public GestureState State => state;

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
                if (activePointers.Count == 0)
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
                if (activePointers.Count == 0)
                {
                    return cachedPreviousScreenPosition.IsValid()
                        ? cachedPreviousScreenPosition : InvalidPosition.Value;
                }
                return activePointers[0].PreviousPosition;
            }
        }

        #endregion

        #region Private variables

        [NonSerialized] private TouchManager touchManager;
        [NonSerialized] private GestureManager gestureManager;

        /// <summary>
        /// Pointers the gesture currently owns and works with.
        /// </summary>
        public readonly List<Pointer> activePointers = new(10);

        private int numPointers;

        protected PointersNumState pointersNumState = PointersNumState.Reset;

        /// <summary>
        /// Cached transform of the parent object.
        /// </summary>
        protected Transform cachedTransform;

        /// <summary>
        /// Cached screen position. 
        /// Used to keep tap's position which can't be calculated from pointers when the gesture is recognized since all pointers are gone.
        /// </summary>
        private Vector2 cachedScreenPosition;

        /// <summary>
        /// Cached previous screen position.
        /// Used to keep tap's position which can't be calculated from pointers when the gesture is recognized since all pointers are gone.
        /// </summary>
        private Vector2 cachedPreviousScreenPosition;

        #endregion

        #region Public methods

        /// <summary>
        /// Determines whether this instance can prevent the specified gesture.
        /// </summary>
        /// <returns> <c>true</c> if this instance can prevent the specified gesture; <c>false</c> otherwise. </returns>
        public virtual bool CanPreventGesture() => true;

        /// <summary>
        /// Specifies if gesture can receive this specific pointer point.
        /// </summary>
        /// <param name="pointer"> The pointer. </param>
        /// <returns> <c>true</c> if this pointer should be received by the gesture; <c>false</c> otherwise. </returns>
        public virtual bool ShouldReceivePointer(Pointer pointer) => true;

        #endregion

        #region Unity methods

        private void Awake() => cachedTransform = transform;

        /// <summary>
        /// Unity Start handler.
        /// </summary>
        private void OnEnable()
        {
            // TouchManager might be different in another scene
            touchManager ??= TouchManager.Instance;
            gestureManager ??= GestureManager.Instance;

            INTERNAL_Reset();
        }

        /// <summary>
        /// Unity OnDisable handler.
        /// </summary>
        protected void OnDisable() => setState(GestureState.Cancelled);

        #endregion

        #region Internal functions

        internal void INTERNAL_SetState(GestureState value) => setState(value);

        internal void INTERNAL_Reset()
        {
            activePointers.Clear();
            numPointers = 0;
            pointersNumState = PointersNumState.Reset;
            reset();
        }

        internal void INTERNAL_PointersPressed(List<Pointer> pointers)
        {
            Assert.IsTrue(pointers.All(p => activePointers.Contains(p) == false));

            var count = pointers.Count;
            var total = numPointers + count;

            // MinPointers is not set and we got our first pointers
            // XXX: On other methods, we use condition `total is 0`, but here we use numPointers instead.
            // This is because gestures determine to whether they can transition to Possible state with PointerNumState.None.
            pointersNumState = numPointers is 0 ? PointersNumState.None : PointersNumState.Exists;

            if (state.IsBeganOrChanged())
            {
                for (var i = 0; i < count; i++) pointers[i].INTERNAL_Retain();
            }

            activePointers.AddRange(pointers);
            numPointers = total;
            pointersPressed(pointers);
        }

        internal void INTERNAL_PointersUpdated(List<Pointer> pointers)
        {
            pointersNumState = PointersNumState.Exists;
            pointersUpdated(pointers);
        }

        internal void INTERNAL_PointersReleased(List<Pointer> pointers)
        {
            Assert.IsTrue(pointers.All(p => activePointers.Contains(p)));

            var count = pointers.Count;
            var total = numPointers - count;

            // have no pointers
            pointersNumState = total is 0 ? PointersNumState.None : PointersNumState.Exists;

            for (var i = 0; i < count; i++) activePointers.Remove(pointers[i]);
            numPointers = total;

            if (activePointers.Count == 0)
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

        internal void INTERNAL_PointersCancelled(List<Pointer> pointers)
        {
            Assert.IsTrue(pointers.All(p => activePointers.Contains(p)));

            var count = pointers.Count;
            var total = numPointers - count;

            // have no pointers
            pointersNumState = total is 0 ? PointersNumState.None : PointersNumState.Exists;

            for (var i = 0; i < count; i++) activePointers.Remove(pointers[i]);
            numPointers = total;

            // moved below the threshold
            if (total is 0)
            {
                // cancel started gestures
                if (state.IsBeganOrChanged())
                    setState(GestureState.Cancelled);
            }
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Should the gesture cache this pointers to use it later in calculation of <see cref="ScreenPosition"/>.
        /// </summary>
        /// <param name="value"> Pointer to cache. </param>
        /// <returns> <c>true</c> if pointers should be cached; <c>false</c> otherwise. </returns>
        protected virtual bool shouldCachePointerPosition(Pointer value) => true;

        /// <summary>
        /// Tries to change gesture state.
        /// </summary>
        /// <param name="newState"> New state. </param>
        /// <returns> <c>true</c> if state was changed; otherwise, <c>false</c>. </returns>
        protected bool setState(GestureState newState)
        {
            // Check if the gesture is destroyed.
            if (gestureManager == null)
            {
                L.W("[Gesture] GestureManager is not set or destroyed.");
                return false;
            }


            // Resolve newState.
            var canChangeOrPrevented = gestureManager.INTERNAL_GesturePrepareStateChange(this, newState);
            var resolvedState = canChangeOrPrevented ? newState : GestureState.Failed;
#if UNITY_EDITOR
            if (State != resolvedState) // Log only if the state is changed.
                L.I($"[Gesture] {GetType().Name} state: {State} → {newState}, {canChangeOrPrevented}", this);
#endif


            // Set state value.
            PreviousState = state;
            state = resolvedState;


            // Enter & exit state.
            switch (resolvedState)
            {
                case GestureState.Began:
                    retainPointers();
                    onBegan();
                    break;
                case GestureState.Changed:
                    onChanged();
                    break;
                case GestureState.Ended:
                    // Only retain/release pointers for continuos gestures
                    if (PreviousState.IsBeganOrChanged())
                        releasePointers(true);
                    onRecognized();
                    break;
                case GestureState.Cancelled:
                    if (PreviousState.IsBeganOrChanged())
                        releasePointers(false);
                    break;
            }


            // Notify state change.
            // Even if the state is not changed, we should notify the state change.
            StateChanged?.InvokeHandleExceptions(PreviousState, state);


            // Return if the state is changed.
            return newState == resolvedState;
        }

        #endregion

        #region Callbacks

        /// <summary>
        /// Called when new pointers appear.
        /// </summary>
        /// <param name="pointers"> The pointers. </param>
        protected virtual void pointersPressed(IList<Pointer> pointers) { }

        /// <summary>
        /// Called for moved pointers.
        /// </summary>
        /// <param name="pointers"> The pointers. </param>
        protected virtual void pointersUpdated(IList<Pointer> pointers) { }

        /// <summary>
        /// Called if pointers are removed.
        /// </summary>
        /// <param name="pointers"> The pointers. </param>
        protected virtual void pointersReleased(IList<Pointer> pointers) { }

        /// <summary>
        /// Called to reset gesture state after it fails or recognizes.
        /// </summary>
        protected virtual void reset()
        {
            cachedScreenPosition = InvalidPosition.Value;
            cachedPreviousScreenPosition = InvalidPosition.Value;
        }

        /// <summary>
        /// Called when state is changed to Began.
        /// </summary>
        protected virtual void onBegan() { }

        /// <summary>
        /// Called when state is changed to Changed.
        /// </summary>
        protected virtual void onChanged() { }

        /// <summary>
        /// Called when state is changed to Recognized.
        /// </summary>
        protected virtual void onRecognized() { }

        #endregion

        #region Private functions

        private void retainPointers()
        {
            var total = activePointers.Count;
            for (var i = 0; i < total; i++)
                activePointers[i].INTERNAL_Retain();
        }

        private void releasePointers(bool cancel)
        {
            var total = activePointers.Count;
            for (var i = 0; i < total; i++)
            {
                var pointer = activePointers[i];
                if (pointer.INTERNAL_Release() == 0 && cancel)
                    touchManager.CancelPointer(pointer);
            }
        }

        #endregion
    }
}