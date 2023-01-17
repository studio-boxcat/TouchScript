/*
 * @author Michael Holub
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// Unity touch handling implementation which can be embedded and controlled from other (input) classes.
    /// </summary>
    public class TouchHandler : IInputHandler, IDisposable
    {
        #region Private variables

        private IInputSource input;
        private IPointerEventListener pointerEventListener;

        private ObjectPool<Pointer> touchPool;
        // Unity fingerId -> TouchScript touch info
        private Dictionary<int, TouchState> systemToInternalId = new Dictionary<int, TouchState>(10);

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TouchHandler" /> class.
        /// </summary>
        /// <param name="input">An input source to init new pointers with.</param>
        /// <param name="addPointer">A function called when a new pointer is detected.</param>
        /// <param name="updatePointer">A function called when a pointer is moved or its parameter is updated.</param>
        /// <param name="pressPointer">A function called when a pointer touches the surface.</param>
        /// <param name="releasePointer">A function called when a pointer is lifted off.</param>
        /// <param name="removePointer">A function called when a pointer is removed.</param>
        /// <param name="cancelPointer">A function called when a pointer is cancelled.</param>
        public TouchHandler(IInputSource input, IPointerEventListener pointerEventListener)
        {
            this.input = input;
            this.pointerEventListener = pointerEventListener;

            touchPool = new ObjectPool<Pointer>(10, newPointer, null, resetPointer, "TouchHandler/Touch");
            touchPool.Name = "Touch";
        }

        #region Public methods

        /// <inheritdoc />
        public bool UpdateInput()
        {
            for (var i = 0; i < Input.touchCount; ++i)
            {
                var t = Input.GetTouch(i);

                TouchState touchState;
                switch (t.phase)
                {
                    case TouchPhase.Began:
                        if (systemToInternalId.TryGetValue(t.fingerId, out touchState) && touchState.Phase != TouchPhase.Canceled)
                        {
                            // Ending previous touch (missed a frame)
                            internalRemovePointer(touchState.Pointer);
                            systemToInternalId[t.fingerId] = new TouchState(internalAddPointer(t.position));
                        }
                        else
                        {
                            systemToInternalId.Add(t.fingerId, new TouchState(internalAddPointer(t.position)));
                        }
                        break;
                    case TouchPhase.Moved:
                        if (systemToInternalId.TryGetValue(t.fingerId, out touchState))
                        {
                            if (touchState.Phase != TouchPhase.Canceled)
                            {
                                touchState.Pointer.Position = t.position;
                                pointerEventListener.UpdatePointer(touchState.Pointer);
                            }
                        }
                        else
                        {
                            // Missed began phase
                            systemToInternalId.Add(t.fingerId, new TouchState(internalAddPointer(t.position)));
                        }
                        break;
                    // NOTE: Unity touch on Windows reports Cancelled as Ended
                    // when a touch goes out of display boundary
                    case TouchPhase.Ended:
                        if (systemToInternalId.TryGetValue(t.fingerId, out touchState))
                        {
                            systemToInternalId.Remove(t.fingerId);
                            if (touchState.Phase != TouchPhase.Canceled) internalRemovePointer(touchState.Pointer);
                        }
                        else
                        {
                            // Missed one finger begin-end transition
                            var pointer = internalAddPointer(t.position);
                            internalRemovePointer(pointer);
                        }
                        break;
                    case TouchPhase.Canceled:
                        if (systemToInternalId.TryGetValue(t.fingerId, out touchState))
                        {
                            systemToInternalId.Remove(t.fingerId);
                            if (touchState.Phase != TouchPhase.Canceled) internalCancelPointer(touchState.Pointer);
                        }
                        else
                        {
                            // Missed one finger begin-end transition
                            var pointer = internalAddPointer(t.position);
                            internalCancelPointer(pointer);
                        }
                        break;
                    case TouchPhase.Stationary:
                        if (systemToInternalId.TryGetValue(t.fingerId, out touchState)) {}
                        else
                        {
                            // Missed begin phase
                            systemToInternalId.Add(t.fingerId, new TouchState(internalAddPointer(t.position)));
                        }
                        break;
                }
            }

            return Input.touchCount > 0;
        }

        /// <inheritdoc />
        public void UpdateResolution(int width, int height) {}

        /// <inheritdoc />
        public bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            if (pointer == null) return false;

            int fingerId = -1;
            foreach (var touchState in systemToInternalId)
            {
                if (touchState.Value.Pointer == pointer && touchState.Value.Phase != TouchPhase.Canceled)
                {
                    fingerId = touchState.Key;
                    break;
                }
            }
            if (fingerId > -1)
            {
                internalCancelPointer(pointer);
                if (shouldReturn) systemToInternalId[fingerId] = new TouchState(internalReturnPointer(pointer));
                else systemToInternalId[fingerId] = new TouchState(pointer, TouchPhase.Canceled);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public bool DiscardPointer([NotNull] Pointer pointer)
        {
            touchPool.Release(pointer);
            return true;
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var touchState in systemToInternalId)
            {
                if (touchState.Value.Phase != TouchPhase.Canceled) internalCancelPointer(touchState.Value.Pointer);
            }
            systemToInternalId.Clear();
        }

        #endregion

        #region Private functions

        [NotNull]
        private Pointer internalAddPointer(Vector2 position)
        {
            var pointer = touchPool.Get();
            pointer.Position = position;
            pointer.Buttons |= Pointer.PointerButtonState.FirstButtonDown | Pointer.PointerButtonState.FirstButtonPressed;
            pointerEventListener.AddPointer(pointer);
            pointerEventListener.PressPointer(pointer);
            return pointer;
        }

        private Pointer internalReturnPointer(Pointer pointer)
        {
            var newPointer = touchPool.Get();
            newPointer.CopyFrom(pointer);
            pointer.Buttons |= Pointer.PointerButtonState.FirstButtonDown | Pointer.PointerButtonState.FirstButtonPressed;
            newPointer.Flags |= Pointer.FLAG_RETURNED;
            pointerEventListener.AddPointer(newPointer);
            pointerEventListener.PressPointer(newPointer);
            return newPointer;
        }

        private void internalRemovePointer(Pointer pointer)
        {
            pointer.Buttons &= ~Pointer.PointerButtonState.FirstButtonPressed;
            pointer.Buttons |= Pointer.PointerButtonState.FirstButtonUp;
            pointerEventListener.ReleasePointer(pointer);
            pointerEventListener.RemovePointer(pointer);
        }

        private void internalCancelPointer(Pointer pointer)
        {
            pointerEventListener.CancelPointer(pointer);
        }

        private void resetPointer(Pointer p)
        {
            p.INTERNAL_Reset();
        }

        private Pointer newPointer()
        {
            return new Pointer(input);
        }

        #endregion

        private readonly struct TouchState
        {
            public readonly Pointer Pointer;
            public readonly TouchPhase Phase;

            public TouchState(Pointer pointer, TouchPhase phase = TouchPhase.Began)
            {
                Pointer = pointer;
                Phase = phase;
            }
        }
    }
}