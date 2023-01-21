using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = TouchScript.Utils.Logger;

namespace TouchScript.InputSources.InputHandlers
{
    public class FakeInputSource : IInputSource
    {
        readonly PointerContainer _pointerContainer;
        static readonly Logger _logger = new(nameof(FakeInputSource));

        Pointer _pointer;
        Vector2? _pointPos;
        bool _release;

        public FakeInputSource(PointerContainer pointerContainer)
        {
            _pointerContainer = pointerContainer;
        }

        public void UpdateInput(PointerChanges changes)
        {
            Assert.IsTrue(_pointer == null || _pointer.Id.IsValid());
            Assert.IsFalse(_pointPos.HasValue && _release);

            if (_release)
            {
                if (_pointer != null)
                    changes.Put_ReleaseAndRemove(_pointer.Id);

                _release = false;
            }
            else if (_pointPos.HasValue)
            {
                if (_pointer == null)
                {
                    _pointer = _pointerContainer.Create(_pointPos.Value, this);
                    changes.Put_AddAndPress(_pointer.Id);
                }
                else
                {
                    _pointer.NewPosition = _pointPos.Value;
                    changes.Put_Update(_pointer.Id);
                }

                _pointPos = null;
            }
        }

        public void Point(Vector2 pos)
        {
            Assert.IsFalse(_pointPos.HasValue);
            Assert.IsFalse(_release);
            _pointPos = pos;
        }

        public void Release()
        {
            Assert.IsFalse(_pointPos.HasValue);
            Assert.IsFalse(_release);
            _release = true;
        }

        public void CancelPointer(Pointer pointer, bool shouldReturn, PointerChanges changes)
        {
            var pointerId = pointer.Id;
            Assert.AreNotEqual(PointerId.Invalid, pointerId);
            Assert.AreEqual(_pointer, pointer);

            if (ReferenceEquals(_pointer, pointer) == false)
            {
                _logger.Warning("알 수 없는 포인터입니다. 이전에 취소한 포인터일 수 있습니다: " + pointerId);
                return;
            }

            // 우선 Pointer 를 Cancel 함.
            changes.Put_Cancel(pointerId);
            _pointer = null;

            if (shouldReturn == false)
                return;

            // 새로운 포인터를 생성.
            var newPointer = _pointerContainer.Create(pointer.Position, this);
            newPointer.CopyFrom(pointer);
            var change = new PointerChange {Added = true};
            if (pointer.Pressing) change.Pressed = true;
            changes.Put(newPointer.Id, change);

            // 새로운 포인터로 스왑.
            _pointer = newPointer;
            // 누르고 있었을 경우에만 IsReturned 로 처리함.
            if (_pointer.Pressing)
                _pointer.IsReturned = true;
        }

        public void INTERNAL_DiscardPointer(Pointer pointer)
        {
            _logger.Info("Discard: " + pointer.Id);
            Assert.IsTrue(pointer.Id.IsValid());

            if (_pointer == pointer)
                _pointer = null;
            _pointerContainer.Destroy(pointer);
        }

        public void CancelAllPointers(PointerChanges changes)
        {
            _logger.Info("CancelAllPointers");

            if (_pointer == null) return;
            changes.Put_Cancel(_pointer.Id);
            _pointer = null;
        }
    }
}