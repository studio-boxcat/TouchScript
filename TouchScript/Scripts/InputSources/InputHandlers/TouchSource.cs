/*
 * @author Michael Holub
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// Unity touch handling implementation which can be embedded and controlled from other (input) classes.
    /// </summary>
    public class TouchSource : IInputSource, IDisposable
    {
        #region Private variables

        readonly PointerPool _pointerPool;
        readonly IPointerEventListener _pointerEventListener;
        // Unity fingerId -> TouchScript touch info
        readonly Dictionary<int, TouchState> _systemToInternalId = new(10);

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="TouchSource" /> class.
        /// </summary>
        public TouchSource(IPointerEventListener pointerEventListener)
        {
            _pointerEventListener = pointerEventListener;
            _pointerPool = new PointerPool(this);
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
                        if (_systemToInternalId.TryGetValue(t.fingerId, out touchState) && touchState.Phase != TouchPhase.Canceled)
                        {
                            // Ending previous touch (missed a frame)
                            internalRemovePointer(touchState.Pointer);
                            _systemToInternalId[t.fingerId] = new TouchState(internalAddPointer(t.position));
                        }
                        else
                        {
                            _systemToInternalId.Add(t.fingerId, new TouchState(internalAddPointer(t.position)));
                        }
                        break;
                    case TouchPhase.Moved:
                        if (_systemToInternalId.TryGetValue(t.fingerId, out touchState))
                        {
                            if (touchState.Phase != TouchPhase.Canceled)
                            {
                                touchState.Pointer.NewPosition = t.position;
                                _pointerEventListener.UpdatePointer(touchState.Pointer);
                            }
                        }
                        else
                        {
                            // Missed began phase
                            _systemToInternalId.Add(t.fingerId, new TouchState(internalAddPointer(t.position)));
                        }
                        break;
                    // NOTE: Unity touch on Windows reports Cancelled as Ended
                    // when a touch goes out of display boundary
                    case TouchPhase.Ended:
                        if (_systemToInternalId.TryGetValue(t.fingerId, out touchState))
                        {
                            _systemToInternalId.Remove(t.fingerId);
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
                        if (_systemToInternalId.TryGetValue(t.fingerId, out touchState))
                        {
                            _systemToInternalId.Remove(t.fingerId);
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
                        if (_systemToInternalId.TryGetValue(t.fingerId, out touchState))
                        {
                        }
                        else
                        {
                            // Missed begin phase
                            _systemToInternalId.Add(t.fingerId, new TouchState(internalAddPointer(t.position)));
                        }
                        break;
                }
            }

            return Input.touchCount > 0;
        }

        /// <inheritdoc />
        public bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            if (pointer == null) return false;

            int fingerId = -1;
            foreach (var touchState in _systemToInternalId)
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
                if (shouldReturn) _systemToInternalId[fingerId] = new TouchState(internalReturnPointer(pointer));
                else _systemToInternalId[fingerId] = new TouchState(pointer, TouchPhase.Canceled);
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        void IInputSource.INTERNAL_DiscardPointer([NotNull] Pointer pointer)
        {
            _pointerPool.Release(pointer);
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        public void Dispose()
        {
            foreach (var touchState in _systemToInternalId)
            {
                if (touchState.Value.Phase != TouchPhase.Canceled) internalCancelPointer(touchState.Value.Pointer);
            }
            _systemToInternalId.Clear();
        }

        #endregion

        #region Private functions

        [NotNull]
        private Pointer internalAddPointer(Vector2 position)
        {
            var pointer = _pointerPool.Get(position);
            pointer.Pressing = true;
            _pointerEventListener.AddPointer(pointer);
            _pointerEventListener.PressPointer(pointer);
            return pointer;
        }

        private Pointer internalReturnPointer(Pointer pointer)
        {
            var newPointer = _pointerPool.Get(pointer.Position);
            newPointer.CopyFrom(pointer);
            pointer.Pressing = true;
            newPointer.IsReturned = true;
            _pointerEventListener.AddPointer(newPointer);
            _pointerEventListener.PressPointer(newPointer);
            return newPointer;
        }

        private void internalRemovePointer(Pointer pointer)
        {
            pointer.Pressing = false;
            _pointerEventListener.ReleasePointer(pointer);
            _pointerEventListener.RemovePointer(pointer);
        }

        private void internalCancelPointer(Pointer pointer)
        {
            _pointerEventListener.CancelPointer(pointer);
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