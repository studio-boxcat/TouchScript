/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using JetBrains.Annotations;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = TouchScript.Utils.Logger;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// Unity mouse handling implementation which can be embedded and controlled from other (input) classes.
    /// </summary>
    public class MouseSource : IInputSource, IDisposable
    {
        #region Private variables

        readonly PointerPool _pointerPool;
        readonly IPointerEventListener _pointerEventListener;

        Pointer _mousePointer;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseSource" /> class.
        /// </summary>
        public MouseSource(IPointerEventListener pointerEventListener)
        {
            _pointerPool = new PointerPool(this);
            _pointerEventListener = pointerEventListener;
        }

        #region Public methods

        /// <summary>
        /// Cancels the mouse pointer.
        /// </summary>
        public void CancelMousePointer()
        {
            if (_mousePointer != null)
            {
                _pointerEventListener.CancelPointer(_mousePointer);
                _mousePointer = null;
            }
        }

        public bool UpdateInput()
        {
            // 마우스가 있었다가 사라진 상황.
            if (Input.mousePresent == false)
            {
                CancelMousePointer();
                return false;
            }

            var pos = (Vector2) Input.mousePosition;

            if (_mousePointer == null)
            {
                _mousePointer = _pointerPool.Get(pos);
                _mousePointer.Pressing = false;
                _pointerEventListener.AddPointer(_mousePointer);
            }

            Assert.IsTrue(_mousePointer.Id.IsValid());
            Assert.IsFalse(_mousePointer.IsReturned);

            if (pos != _mousePointer.Position)
            {
                _mousePointer.NewPosition = pos;
                _pointerEventListener.UpdatePointer(_mousePointer);
            }

            var (mousePressing, mouseUp) = GetMouseButtons();
            if (mousePressing && _mousePointer.Pressing == false)
            {
                _mousePointer.Pressing = true;
                _pointerEventListener.PressPointer(_mousePointer);
            }
            if (mouseUp)
            {
                _mousePointer.Pressing = false;
                _pointerEventListener.ReleasePointer(_mousePointer);
            }

            return true;
        }

        /// <inheritdoc />
        public bool CancelPointer([NotNull] Pointer pointer, bool shouldReturn)
        {
            if (_mousePointer != pointer)
            {
                Logger.Warning("알 수 없는 포인터입니다. 이전에 취소한 포인터일 수 있습니다: " + pointer.Id);
                return false;
            }

            // 우선 Pointer 를 Cancel 함.
            _pointerEventListener.CancelPointer(pointer);

            // shouldReturn 이 false 일 경우, 단순히 포인터를 삭제하면 끝남.
            if (shouldReturn == false)
            {
                _mousePointer = null;
                return true;
            }

            // 새로운 포인터를 생성.
            var newPointer = _pointerPool.Get(pointer.Position);
            newPointer.CopyFrom(pointer);
            _pointerEventListener.AddPointer(newPointer);
            if (newPointer.Pressing)
                _pointerEventListener.PressPointer(newPointer);

            // 새로운 포인터로 스왑.
            _mousePointer = newPointer;
            // 이전 포인터는 IsReturned 처리를 함.
            pointer.IsReturned = true;
            return true;
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
            if (_mousePointer != null)
            {
                _pointerEventListener.CancelPointer(_mousePointer);
                _mousePointer = null;
            }
        }

        #endregion

        #region Private functions

        static (bool MousePressing, bool MouseUp) GetMouseButtons()
        {
            if (Input.GetMouseButtonUp(0))
                return (true, true);

            var mousePressing = Input.GetMouseButton(0);
            return (mousePressing || Input.GetMouseButtonDown(0), !mousePressing);
        }

        #endregion
    }
}