/*
 * @author Valentin Simonov / http://va.lent.in/
 */

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
    public class MouseInputSource : IInputSource
    {
        readonly PointerContainer _pointerContainer;

        Pointer _mousePointer;

        /// <summary>
        /// Initializes a new instance of the <see cref="MouseInputSource" /> class.
        /// </summary>
        public MouseInputSource(PointerContainer pointerContainer)
        {
            _pointerContainer = pointerContainer;
        }

        public bool UpdateInput(PointerChanges changes)
        {
            var pos = (Vector2) Input.mousePosition;
            PointerChange change = default;

            if (_mousePointer == null)
            {
                _mousePointer = _pointerContainer.Create(pos, this);
                change.Added = true;
            }

            Assert.IsTrue(_mousePointer.Id.IsValid());
            Assert.IsFalse(_mousePointer.IsReturned);

            if (pos != _mousePointer.Position)
            {
                _mousePointer.NewPosition = pos;
                change.Updated = true;
            }

            var (mousePressing, mouseUp) = GetMouseButtons();
            if (mousePressing && _mousePointer.Pressing == false)
            {
                _mousePointer.Pressing = true;
                change.Pressed = true;
            }

            if (mouseUp)
            {
                _mousePointer.Pressing = false;
                _mousePointer.IsReturned = false;
                change.Released = true;
            }

            changes.Put(_mousePointer, change);
            return true;
        }

        public void CancelPointer([NotNull] Pointer pointer, bool shouldReturn, PointerChanges changes)
        {
            Assert.IsTrue(pointer.Id.IsValid());

            if (_mousePointer != pointer)
            {
                Logger.Warning("알 수 없는 포인터입니다. 이전에 취소한 포인터일 수 있습니다: " + pointer.Id);
                changes.Put_Cancel(pointer);
                return;
            }

            // 우선 Pointer 를 Cancel 함.
            changes.Put_Cancel(_mousePointer);
            _mousePointer = null;

            if (shouldReturn == false)
                return;

            // 새로운 포인터를 생성.
            var newPointer = _pointerContainer.Create(pointer.Position, this);
            newPointer.CopyFrom(pointer);
            var change = new PointerChange {Added = true};
            if (newPointer.Pressing) change.Pressed = true;
            changes.Put(newPointer, change);

            // 새로운 포인터로 스왑.
            _mousePointer = newPointer;
            // 누르고 있었을 경우에만 IsReturned 로 처리함.
            if (_mousePointer.Pressing)
                _mousePointer.IsReturned = true;
        }

        /// <summary>
        /// Cancels the mouse pointer.
        /// </summary>
        public void CancelMousePointer(PointerChanges changes)
        {
            if (_mousePointer == null) return;
            changes.Put_Cancel(_mousePointer);
            _mousePointer = null;
        }

        public void CancelAllPointers(PointerChanges changes) => CancelMousePointer(changes);

        void IInputSource.INTERNAL_DiscardPointer([NotNull] Pointer pointer)
        {
            Assert.IsTrue(pointer.Id.IsValid());

            if (_mousePointer == pointer)
                _mousePointer = null;
            _pointerContainer.Destroy(pointer);
        }

        static (bool MousePressing, bool MouseUp) GetMouseButtons()
        {
            if (Input.GetMouseButtonUp(0))
                return (true, true);

            var mousePressing = Input.GetMouseButton(0);
            return (mousePressing || Input.GetMouseButtonDown(0), !mousePressing);
        }
    }
}