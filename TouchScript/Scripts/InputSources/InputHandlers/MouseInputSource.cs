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
        static readonly Logger _logger = new(nameof(MouseInputSource));

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
            if (Input.mousePresent == false)
            {
                CancelMousePointer(changes);
                return false;
            }

            var pos = (Vector2) Input.mousePosition;
            PointerChange change = default;

            if (_mousePointer == null)
            {
                _mousePointer = _pointerContainer.Create(pos, this);
                change.Added = true;
            }

            var pointerId = _mousePointer.Id;
            Assert.IsTrue(pointerId.IsValid());
            Assert.IsFalse(_mousePointer.IsReturned);

            if (pos != _mousePointer.Position)
            {
                _mousePointer.NewPosition = pos;
                change.Updated = true;
            }

            var (mousePressing, mouseUp) = GetMouseButtons();
            if (mousePressing && _mousePointer.Pressing == false)
                change.Pressed = true;

            if (mouseUp && (_mousePointer.Pressing || change.Pressed))
            {
                _mousePointer.IsReturned = false;
                change.Released = true;
            }

            changes.Put(pointerId, change);
            return true;
        }

        public void CancelPointer([NotNull] Pointer pointer, bool shouldReturn, PointerChanges changes)
        {
            var pointerId = pointer.Id;
            Assert.IsTrue(pointerId.IsValid());

            if (ReferenceEquals(_mousePointer, pointer) == false)
            {
                _logger.Warning("알 수 없는 포인터입니다. 이전에 취소한 포인터일 수 있습니다: " + pointerId);
                changes.Put_Cancel(pointerId);
                return;
            }

            // 우선 Pointer 를 Cancel 함.
            changes.Put_Cancel(pointerId);
            _mousePointer = null;

            if (shouldReturn == false)
                return;

            // 새로운 포인터를 생성.
            var newPointer = _pointerContainer.Create(pointer.Position, this);
            newPointer.CopyFrom(pointer);
            var change = new PointerChange {Added = true};
            if (pointer.Pressing) change.Pressed = true;
            changes.Put(newPointer.Id, change);

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
            changes.Put_Cancel(_mousePointer.Id);
            _mousePointer = null;
        }

        public void CancelAllPointers(PointerChanges changes) => CancelMousePointer(changes);

        void IInputSource.INTERNAL_DiscardPointer([NotNull] Pointer pointer)
        {
            _logger.Info("Discard: " + pointer.Id);
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