/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using JetBrains.Annotations;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// Unity mouse handling implementation which can be embedded and controlled from other (input) classes.
    /// </summary>
    public class MouseHandler : IInputHandler, IDisposable
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

        private IInputSource input;
        private IPointerEventListener pointerEventListener;

        private State state;
        private ObjectPool<Pointer> mousePool;
        private Pointer mousePointer, fakeMousePointer;
        private Vector2 mousePointPos = Vector2.zero;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseHandler" /> class.
        /// </summary>
        /// <param name="input">An input source to init new pointers with.</param>
        /// <param name="addPointer">A function called when a new pointer is detected.</param>
        /// <param name="updatePointer">A function called when a pointer is moved or its parameter is updated.</param>
        /// <param name="pressPointer">A function called when a pointer touches the surface.</param>
        /// <param name="releasePointer">A function called when a pointer is lifted off.</param>
        /// <param name="removePointer">A function called when a pointer is removed.</param>
        /// <param name="cancelPointer">A function called when a pointer is cancelled.</param>
        public MouseHandler(IInputSource input, IPointerEventListener pointerEventListener)
        {
            this.input = input;
            this.pointerEventListener = pointerEventListener;

            mousePool = new ObjectPool<Pointer>(4, newPointer, null, resetPointer);

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
                pointerEventListener.CancelPointer(mousePointer);
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
                    mousePointer.Position = pos;
                    pointerEventListener.UpdatePointer(mousePointer);
                }
                updated = true;
            }

            if (mousePointer == null) return false;

            var buttons = state == State.MouseAndFake ? fakeMousePointer.Buttons : mousePointer.Buttons;
            var newButtons = getMouseButtons();
            var scroll = Input.mouseScrollDelta;
            if (!Mathf.Approximately(scroll.sqrMagnitude, 0.0f))
            {
                mousePointer.ScrollDelta = scroll;
                pointerEventListener.UpdatePointer(mousePointer);
            }
            else
            {
                mousePointer.ScrollDelta = Vector2.zero;
            }

            if (Application.isEditor)
            {
                switch (state)
                {
                    case State.Mouse:
                        if (Input.GetKeyDown(KeyCode.LeftAlt) && !Input.GetKeyUp(KeyCode.LeftAlt)
                            && ((newButtons & Pointer.PointerButtonState.AnyButtonPressed) == 0))
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
                            if ((newButtons & Pointer.PointerButtonState.AnyButtonDown) != 0)
                            {
                                // A button is down while holding Alt
                                fakeMousePointer = internalAddPointer(pos, newButtons, mousePointer.Flags | Pointer.FLAG_ARTIFICIAL);
                                pointerEventListener.PressPointer(fakeMousePointer);
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
                                fakeMousePointer.Position = pos;
                                pointerEventListener.UpdatePointer(fakeMousePointer);
                            }
                            if ((newButtons & Pointer.PointerButtonState.AnyButtonPressed) == 0)
                            {
                                // All buttons are released, Alt is still holding
                                stateStationaryFake();
                            }
                            else if (buttons != newButtons)
                            {
                                fakeMousePointer.Buttons = newButtons;
                                pointerEventListener.UpdatePointer(fakeMousePointer);
                            }
                        }
                        break;
                    case State.StationaryFake:
                        if (buttons != newButtons) updateButtons(buttons, newButtons);
                        if ((newButtons & Pointer.PointerButtonState.AnyButtonPressed) != 0)
                        {
                            if (mousePointPos != pos)
                            {
                                if (Input.GetKey(KeyCode.LeftControl))
                                {
                                    fakeMousePointer.Position += (pos - mousePointer.Position);
                                    pointerEventListener.UpdatePointer(fakeMousePointer);
                                }
                                else if (Input.GetKey(KeyCode.LeftShift))
                                {
                                    fakeMousePointer.Position -= (pos - mousePointer.Position);
                                    pointerEventListener.UpdatePointer(fakeMousePointer);
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
            else
            {
                if (buttons != newButtons)
                {
                    updateButtons(buttons, newButtons);
                    updated = true;
                }
            }

            mousePointPos = pos;
            return updated;
        }

        /// <inheritdoc />
        public void UpdateResolution(int width, int height) {}

        /// <inheritdoc />
        public bool CancelPointer([NotNull] Pointer pointer, bool shouldReturn)
        {
            if (pointer.Equals(mousePointer))
            {
                pointerEventListener.CancelPointer(mousePointer);
                if (shouldReturn) mousePointer = internalReturnPointer(mousePointer);
                else mousePointer = internalAddPointer(mousePointer.Position); // can't totally cancel mouse pointer
                return true;
            }
            if (pointer.Equals(fakeMousePointer))
            {
                pointerEventListener.CancelPointer(fakeMousePointer);
                if (shouldReturn) fakeMousePointer = internalReturnPointer(fakeMousePointer);
                else fakeMousePointer = null;
                return true;
            }
            return false;
        }

        /// <inheritdoc />
        public bool DiscardPointer([NotNull] Pointer pointer)
        {
            mousePool.Release(pointer);
            return true;
        }

        /// <summary>
        /// Releases resources.
        /// </summary>
        public void Dispose()
        {
            if (mousePointer != null)
            {
                pointerEventListener.CancelPointer(mousePointer);
                mousePointer = null;
            }
            if (fakeMousePointer != null)
            {
                pointerEventListener.CancelPointer(fakeMousePointer);
                fakeMousePointer = null;
            }
        }

        #endregion

        #region Private functions

        private Pointer.PointerButtonState getMouseButtons()
        {
            Pointer.PointerButtonState buttons = Pointer.PointerButtonState.Nothing;

            if (Input.GetMouseButton(0)) buttons |= Pointer.PointerButtonState.FirstButtonPressed;
            if (Input.GetMouseButtonDown(0)) buttons |= Pointer.PointerButtonState.FirstButtonDown;
            if (Input.GetMouseButtonUp(0)) buttons |= Pointer.PointerButtonState.FirstButtonUp;

            return buttons;
        }

        private void updateButtons(Pointer.PointerButtonState oldButtons, Pointer.PointerButtonState newButtons)
        {
            // pressed something
            if (oldButtons == Pointer.PointerButtonState.Nothing)
            {
                // pressed and released this frame
                if ((newButtons & Pointer.PointerButtonState.AnyButtonPressed) == 0)
                {
                    // Add pressed buttons for processing
                    mousePointer.Buttons = PointerUtils.PressDownButtons(newButtons);
                    pointerEventListener.PressPointer(mousePointer);
                    internalReleaseMousePointer(newButtons);
                }
                // pressed this frame
                else
                {
                    mousePointer.Buttons = newButtons;
                    pointerEventListener.PressPointer(mousePointer);
                }
            }
            // released or button state changed
            else
            {
                // released this frame
                if ((newButtons & Pointer.PointerButtonState.AnyButtonPressed) == 0)
                {
                    mousePointer.Buttons = newButtons;
                    internalReleaseMousePointer(newButtons);
                }
                // button state changed this frame
                else
                {
                    mousePointer.Buttons = newButtons;
                    pointerEventListener.UpdatePointer(mousePointer);
                }
            }
        }

        private bool fakeTouchReleased()
        {
            if (!Input.GetKey(KeyCode.LeftAlt))
            {
                // Alt is released, need to kill the fake touch
                fakeMousePointer.Buttons = PointerUtils.UpPressedButtons(fakeMousePointer.Buttons); // Convert current pressed buttons to UP
                pointerEventListener.ReleasePointer(fakeMousePointer);
                pointerEventListener.RemovePointer(fakeMousePointer);
                fakeMousePointer = null; // Will be returned to the pool by INTERNAL_DiscardPointer
                return true;
            }
            return false;
        }

        [JetBrains.Annotations.NotNull]
        private Pointer internalAddPointer(Vector2 position, Pointer.PointerButtonState buttons = Pointer.PointerButtonState.Nothing, uint flags = 0)
        {
            var pointer = mousePool.Get();
            pointer.Position = position;
            pointer.Buttons |= buttons;
            pointer.Flags |= flags;
            pointerEventListener.AddPointer(pointer);
            pointerEventListener.UpdatePointer(pointer);
            return pointer;
        }

        private void internalReleaseMousePointer(Pointer.PointerButtonState buttons)
        {
            mousePointer.Flags &= ~Pointer.FLAG_RETURNED;
            pointerEventListener.ReleasePointer(mousePointer);
        }

        private Pointer internalReturnPointer(Pointer pointer)
        {
            var newPointer = mousePool.Get();
            newPointer.CopyFrom(pointer);
            newPointer.Flags |= Pointer.FLAG_RETURNED;
            pointerEventListener.AddPointer(newPointer);
            if ((newPointer.Buttons & Pointer.PointerButtonState.AnyButtonPressed) != 0)
            {
                // Adding down state this frame
                newPointer.Buttons = PointerUtils.DownPressedButtons(newPointer.Buttons);
                pointerEventListener.PressPointer(newPointer);
            }
            return newPointer;
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