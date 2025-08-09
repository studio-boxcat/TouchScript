using System.Collections.Generic;
using TouchScript.Core;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.InputSources.InputHandlers
{
    public class FakeInputSource : IInputSource
    {
        private readonly PointerContainer _pointerContainer;

        private readonly Dictionary<PointerId, Pointer> _pointers = new();
        private readonly Dictionary<PointerId, PointerChange> _upComingChanges = new();

        public FakeInputSource(PointerContainer pointerContainer)
        {
            _pointerContainer = pointerContainer;
        }

        public void Deactivate(PointerChanges changes)
        {
            // _logger.Info(nameof(Deactivate));

            // XXX: _pointers 로 관리되는 포인터는 모두 Removed 도 Cancelled 도 아닌 살아있는 포인터들.
            // 모두 Cancel 처리한다.
            foreach (var (pointerId, pointer) in _pointers)
            {
                Assert.AreEqual(pointerId, pointer.Id);
                changes.Put_Cancel(pointerId);
            }

            // XXX: 가지고 있는 변경사항들을 모두 changes 에 넣어줌.
            // Click 과 같이 Added 와 동시에 Removed 를 설정하는 경우, _pointers 에 등록되지 않아 누락될 수 있다.
            foreach (var (pointerId, change) in _upComingChanges)
                changes.Put(pointerId, change);

            _pointers.Clear();
            _upComingChanges.Clear();
        }

        public bool UpdateInput(PointerChanges changes)
        {
            foreach (var (pointerId, change) in _upComingChanges)
                changes.Put(pointerId, change);
            _upComingChanges.Clear();
            return _pointers.Count > 0;
        }

        public void CancelPointer(Pointer pointer, PointerChanges changes)
        {
            var pointerId = pointer.Id;
            Assert.AreNotEqual(PointerId.Invalid, pointerId);
            Assert.AreEqual(_pointers[pointerId], pointer);

            _pointers.Remove(pointerId);
            _upComingChanges.Remove(pointerId);
            changes.Put_Cancel(pointerId);

            // Return pointer
            {
                var newPointer = _pointerContainer.Create(pointer.Position, this);
                newPointer.CopyPositions(pointer);
                // XXX: 자동으로 Cancelled 까지 연결되지 않으면 leak 이 발생.
                var change = new PointerChange {Added = true, Cancelled = true};
                if (pointer.Pressing) change.Pressed = true;
                newPointer.IsReturned = true;

                _pointers.Add(newPointer.Id, newPointer);
                changes.Put(newPointer.Id, change);
            }
        }

        public void INTERNAL_DiscardPointer(Pointer pointer, bool cancelled)
        {
            // _logger.Info("Discard: " + pointer.Id);
            Assert.IsTrue(pointer.Id.IsValid());
            Assert.IsFalse(_pointers.ContainsKey(pointer.Id));
            Assert.IsFalse(_upComingChanges.ContainsKey(pointer.Id));
            _pointerContainer.Destroy(pointer);
        }

        public bool IsPointerValid(PointerId pointerId)
        {
            return _pointers.ContainsKey(pointerId);
        }

        public void Click(Vector2 pos)
        {
            if (TouchManager.Instance.enabled == false)
                return;

            var pointer = _pointerContainer.Create(pos, this);
            // _logger.Info(nameof(Click) + ": " + pointer.Id);
            var change = new PointerChange {Added = true, Pressed = true, Released = true, Removed = true};
            _upComingChanges.Add(pointer.Id, change);
        }

        public PointerId Press(Vector2 pos)
        {
            if (TouchManager.Instance.enabled == false)
                return PointerId.Invalid;

            var pointer = _pointerContainer.Create(pos, this);
            var pointerId = pointer.Id;
            // _logger.Info(nameof(Press) + ": " + pointerId);
            _pointers.Add(pointerId, pointer);
            _upComingChanges.Add(pointerId, new PointerChange {Added = true, Pressed = true});
            return pointer.Id;
        }

        public void Point(PointerId pointerId, Vector2 pos)
        {
            // _logger.Info(nameof(Point) + ": " + pointerId);
            Assert.IsTrue(TouchManager.Instance.enabled);

            var pointer = _pointers[pointerId];
            pointer.NewPosition = pos;
            var change = _upComingChanges.GetValueOrDefault(pointerId);
            change.Updated = true;
            _upComingChanges[pointer.Id] = change;
        }

        public void Release(PointerId pointerId)
        {
            // _logger.Info(nameof(Release) + ": " + pointerId);
            Assert.IsTrue(TouchManager.Instance.enabled);
            Assert.IsTrue(_pointers.ContainsKey(pointerId));

            var change = _upComingChanges.GetValueOrDefault(pointerId);
            Assert.IsFalse(change.Released);
            Assert.IsFalse(change.Removed);
            Assert.IsTrue(_pointers[pointerId].Pressing || change.Pressed);

            change.Released = true;
            change.Removed = true;
            _pointers.Remove(pointerId);
            _upComingChanges[pointerId] = change;
        }
    }
}