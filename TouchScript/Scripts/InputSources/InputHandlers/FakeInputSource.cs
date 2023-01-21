using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.InputSources.InputHandlers
{
    public class FakeInputSource : IInputSource
    {
        readonly PointerContainer _pointers;
        Pointer _pointer;

        Vector2? _pointPos;
        bool _release;

        public FakeInputSource(PointerContainer pointers)
        {
            _pointers = pointers;
        }

        public void UpdateInput(PointerChanges changes)
        {
            Assert.IsFalse(_pointPos.HasValue && _release);

            if (_release)
            {
                if (_pointer != null)
                    changes.Put_ReleaseAndRemove(_pointer);

                _release = false;
            }
            else if (_pointPos.HasValue)
            {
                if (_pointer == null)
                {
                    _pointer = _pointers.Create(_pointPos.Value, this);
                    changes.Put_AddAndPress(_pointer);
                }
                else
                {
                    _pointer.NewPosition = _pointPos.Value;
                    changes.Put_Update(_pointer);
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
            Assert.AreNotEqual(PointerId.Invalid, pointer.Id);
            Assert.AreEqual(_pointer, pointer);

            if (_pointer != pointer)
            {
                Utils.Logger.Warning("알 수 없는 포인터입니다. 이전에 취소한 포인터일 수 있습니다: " + pointer.Id);
                return;
            }

            // 우선 Pointer 를 Cancel 함.
            changes.Put_Cancel(_pointer);
            _pointer = null;

            if (shouldReturn == false)
                return;

            // 새로운 포인터를 생성.
            var newPointer = _pointers.Create(pointer.Position, this);
            newPointer.CopyFrom(pointer);
            var change = new PointerChange {Added = true};
            if (pointer.Pressing) change.Pressed = true;
            changes.Put(newPointer, change);

            // 새로운 포인터로 스왑.
            _pointer = newPointer;
            // 누르고 있었을 경우에만 IsReturned 로 처리함.
            if (_pointer.Pressing)
                _pointer.IsReturned = true;
        }

        public void INTERNAL_DiscardPointer(Pointer pointer)
        {
            Assert.IsTrue(pointer.Id.IsValid());

            if (_pointer == pointer)
                _pointer = null;
            _pointers.Destroy(pointer);
        }

        public void CancelAllPointers(PointerChanges changes)
        {
            if (_pointer == null) return;
            changes.Put_Cancel(_pointer);
            _pointer = null;
        }
    }
}