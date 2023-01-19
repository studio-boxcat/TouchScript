/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using JetBrains.Annotations;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// Unity mouse handling implementation which can be embedded and controlled from other (input) classes.
    /// </summary>
    public class MouseSource : IInputSource, IDisposable
    {
        #region Consts

        private enum State
        {
            /// <summary>
            /// Only mouse pointer is active
            /// </summary>
            Mouse,

            /// <summary>
            /// ALT is pressed but mouse isn't
            /// </summary>
            WaitingForFake,

            /// <summary>
            /// Mouse and fake pointers are moving together after ALT+PRESS
            /// </summary>
            MouseAndFake,

            /// <summary>
            /// After ALT+RELEASE fake pointer is stationary while mouse can move freely
            /// </summary>
            StationaryFake
        }

        #endregion

        #region Private variables

        readonly PointerPool _pointerPool;
        readonly IPointerEventListener _pointerEventListener;

        private State state;
        private Pointer mousePointer, fakeMousePointer;
        private Vector2 mousePointPos = Vector2.zero;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseSource" /> class.
        /// </summary>
        /// <param name="input">An input source to init new pointers with.</param>
        /// <param name="addPointer">A function called when a new pointer is detected.</param>
        /// <param name="updatePointer">A function called when a pointer is moved or its parameter is updated.</param>
        /// <param name="pressPointer">A function called when a pointer touches the surface.</param>
        /// <param name="releasePointer">A function called when a pointer is lifted off.</param>
        /// <param name="removePointer">A function called when a pointer is removed.</param>
        /// <param name="cancelPointer">A function called when a pointer is cancelled.</param>
        public MouseSource(IPointerEventListener pointerEventListener)
        {
            _pointerPool = new PointerPool(this);
            _pointerEventListener = pointerEventListener;

            mousePointPos = Input.mousePosition;
            mousePointer = internalAddPointer(mousePointPos);

            stateMouse();
        }

        #region Public methods

        /// <summary>
        /// Cancels the mouse pointer.
        /// </summary>
        public void CancelMousePointer()
        {
            if (mousePointer != null)
            {
                _pointerEventListener.CancelPointer(mousePointer);
                mousePointer = null;
            }
        }

        /// <inheritdoc />
        public bool UpdateInput()
        {
            var pos = (Vector2) Input.mousePosition;
            bool updated = false;

            if (mousePointPos != pos)
            {
                if (mousePointer == null)
                {
                    mousePointer = internalAddPointer(pos);
                }
                else
                {
                    mousePointer.NewPosition = pos;
                    _pointerEventListener.UpdatePointer(mousePointer);
                }
                updated = true;
            }

            if (mousePointer == null) return false;

            var buttons = state == State.MouseAndFake ? fakeMousePointer.Button : mousePointer.Button;
            var newButtons = getMouseButtons();
            var scroll = Input.mouseScrollDelta;
            if (!Mathf.Approximately(scroll.sqrMagnitude, 0.0f))
            {
                mousePointer.ScrollDelta = scroll;
                _pointerEventListener.UpdatePointer(mousePointer);
            }
            else
            {
                mousePointer.ScrollDelta = Vector2.zero;
            }

#if UNITY_EDITOR
            {
                switch (state)
                {
                    case State.Mouse:
                        if (Input.GetKeyDown(KeyCode.LeftAlt)
                            && !Input.GetKeyUp(KeyCode.LeftAlt)
                            && newButtons.Released)
                        {
                            stateWaitingForFake();
                        }
                        else
                        {
                            if (buttons != newButtons) updateButtons(buttons, newButtons);
                        }
                        break;
                    case State.WaitingForFake:
                        if (Input.GetKey(KeyCode.LeftAlt))
                        {
                            if (newButtons.Down)
                            {
                                // A button is down while holding Alt
                                fakeMousePointer = internalAddPointer(pos, newButtons);
                                _pointerEventListener.PressPointer(fakeMousePointer);
                                stateMouseAndFake();
                            }
                        }
                        else
                        {
                            stateMouse();
                        }
                        break;
                    case State.MouseAndFake:
                        if (fakeTouchReleased())
                        {
                            stateMouse();
                        }
                        else
                        {
                            if (mousePointPos != pos)
                            {
                                fakeMousePointer.NewPosition = pos;
                                _pointerEventListener.UpdatePointer(fakeMousePointer);
                            }
                            if (newButtons.Released)
                            {
                                // All buttons are released, Alt is still holding
                                stateStationaryFake();
                            }
                            else if (buttons != newButtons)
                            {
                                fakeMousePointer.Button = newButtons;
                                _pointerEventListener.UpdatePointer(fakeMousePointer);
                            }
                        }
                        break;
                    case State.StationaryFake:
                        if (buttons != newButtons) updateButtons(buttons, newButtons);
                        if (newButtons.Pressed)
                        {
                            if (mousePointPos != pos)
                            {
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    fakeMousePointer.NewPosition = fakeMousePointer.Position + (pos - mousePointer.Position);
                                    _pointerEventListener.UpdatePointer(fakeMousePointer);
                                }
                                else if (Input.GetKey(KeyCode.LeftShift))
                                {
                                    fakeMousePointer.NewPosition = fakeMousePointer.Position - (pos - mousePointer.Position);
                                    _pointerEventListener.UpdatePointer(fakeMousePointer);
                                }
                            }
                        }
                        if (fakeTouchReleased())
                        {
                            stateMouse();
                        }
                        break;
                }
            }
#else
            {
                if (buttons != newButtons)
                {
                    updateButtons(buttons, newButtons);
                    updated = true;
                }
            }
#endif

            mousePointPos = pos;
            return updated;
        }

        /// <inheritdoc />
        public bool CancelPointer([NotNull] Pointer pointer, bool shouldReturn)
        {
            if (pointer.Equals(mousePointer))
            {
                _pointerEventListener.CancelPointer(mousePointer);
                mousePointer = shouldReturn ? internalReturnPointer(mousePointer) : null;
                return true;
            }

            if (pointer.Equals(fakeMousePointer))
            {
                _pointerEventListener.CancelPointer(fakeMousePointer);
                fakeMousePointer = shouldReturn ? internalReturnPointer(fakeMousePointer) : null;
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
            if (mousePointer != null)
            {
                _pointerEventListener.CancelPointer(mousePointer);
                mousePointer = null;
            }
            if (fakeMousePointer != null)
            {
                _pointerEventListener.CancelPointer(fakeMousePointer);
                fakeMousePointer = null;
            }
        }

        #endregion

        #region Private functions

        private PointerButtonState getMouseButtons()
        {
            PointerButtonState buttons = default;

            if (Input.GetMouseButton(0)) buttons.Pressed = true;
            if (Input.GetMouseButtonDown(0)) buttons.Down = true;
            if (Input.GetMouseButtonUp(0)) buttons.Up = true;

            return buttons;
        }

        private void updateButtons(PointerButtonState oldButtons, PointerButtonState newButtons)
        {
            Assert.AreNotEqual(oldButtons, newButtons);

            // pressed something
            if (oldButtons == default)
            {
                // pressed and released this frame
                if (newButtons.Released)
                {
                    // Add pressed buttons for processing
                    mousePointer.Button = PointerUtils.PressDownButtons(newButtons);
                    _pointerEventListener.PressPointer(mousePointer);
                    internalReleaseMousePointer();
                }
                // pressed this frame
                else
                {
                    mousePointer.Button = newButtons;
                    _pointerEventListener.PressPointer(mousePointer);
                }
            }
            // released or button state changed
            else
            {
                // released this frame
                if (newButtons.Released)
                {
                    mousePointer.Button = newButtons;
                    internalReleaseMousePointer();
                }
                // button state changed this frame
                else
                {
                    mousePointer.Button = newButtons;
                    _pointerEventListener.UpdatePointer(mousePointer);
                }
            }
        }

        private bool fakeTouchReleased()
        {
            if (!Input.GetKey(KeyCode.LeftAlt))
            {
                // Alt is released, need to kill the fake touch
                fakeMousePointer.Button = PointerUtils.UpPressedButtons(fakeMousePointer.Button); // Convert current pressed buttons to UP
                _pointerEventListener.ReleasePointer(fakeMousePointer);
                _pointerEventListener.RemovePointer(fakeMousePointer);
                fakeMousePointer = null; // Will be returned to the pool by INTERNAL_DiscardPointer
                return true;
            }
            return false;
        }

        [NotNull]
        private Pointer internalAddPointer(Vector2 position, PointerButtonState buttons = default)
        {
            var pointer = _pointerPool.Get(position);
            pointer.Button = buttons;
            _pointerEventListener.AddPointer(pointer);
            _pointerEventListener.UpdatePointer(pointer);
            return pointer;
        }

        private void internalReleaseMousePointer()
        {
            mousePointer.IsReturned = false;
            _pointerEventListener.ReleasePointer(mousePointer);
        }

        private Pointer internalReturnPointer(Pointer pointer)
        {
            var newPointer = _pointerPool.Get(pointer.Position);
            newPointer.CopyFrom(pointer);
            mousePointer.IsReturned = true;
            _pointerEventListener.AddPointer(newPointer);
            if (newPointer.Button.Pressed)
            {
                // Adding down state this frame
                newPointer.Button = PointerUtils.DownPressedButtons(newPointer.Button);
                _pointerEventListener.PressPointer(newPointer);
            }
            return newPointer;
        }

        #endregion

        #region State logic

        private void stateMouse()
        {
            setState(State.Mouse);
        }

        private void stateWaitingForFake()
        {
            setState(State.WaitingForFake);
        }

        private void stateMouseAndFake()
        {
            setState(State.MouseAndFake);
        }

        private void stateStationaryFake()
        {
            setState(State.StationaryFake);
        }

        private void setState(State newState)
        {
            state = newState;
        }

        #endregion
    }
}