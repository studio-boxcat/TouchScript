using System.Collections.Generic;
using TouchScript.Pointers;
using UnityEngine.Assertions;

namespace TouchScript.InputSources
{
    public readonly struct PointerChanges
    {
        private readonly Dictionary<PointerId, PointerChange> _changes;

        public PointerChanges(int capacity)
        {
            _changes = new Dictionary<PointerId, PointerChange>(capacity);
        }

        public override string ToString()
        {
            return "[" + string.Join(",", _changes) + "]";
        }

        public bool Empty() => _changes.Count == 0;

        public void Flush(PointerContainer pointers, List<(Pointer, PointerChange)> changes)
        {
            Assert.AreEqual(0, changes.Count);

            foreach (var (pointerId, pointer) in pointers.Pointers)
            {
                Assert.AreEqual(pointerId, pointer.Id);
                if (_changes.TryGetValue(pointerId, out var change))
                    changes.Add((pointer, change));
            }

            _changes.Clear();
        }

        public void Put(PointerId pointerId, PointerChange change)
        {
            Assert.IsTrue(pointerId.IsValid());
            Assert.IsFalse(change.Cancelled);

            if (_changes.TryGetValue(pointerId, out var oldChange))
            {
                _changes[pointerId] = PointerChange.MergeWithCheck(change, oldChange);
            }
            else
            {
                _changes[pointerId] = change;
            }
        }

        public void Put_AddAndPress(PointerId pointerId)
        {
            Assert.IsTrue(pointerId.IsValid());

            if (_changes.TryGetValue(pointerId, out var change) == false)
                change = default;
            Assert.IsFalse(change.Added);
            Assert.IsFalse(change.Pressed);
            Assert.IsFalse(change.Cancelled);
            change.Added = true;
            change.Pressed = true;
            _changes[pointerId] = change;
        }

        public void Put_ReleaseAndRemove(PointerId pointerId)
        {
            Assert.IsTrue(pointerId.IsValid());

            if (_changes.TryGetValue(pointerId, out var change) == false)
                change = default;
            Assert.IsFalse(change.Released);
            Assert.IsFalse(change.Removed);
            Assert.IsFalse(change.Cancelled);
            change.Released = true;
            change.Removed = true;
            _changes[pointerId] = change;
        }

        public void Put_SingleFrameTap(PointerId pointerId)
        {
            Assert.IsTrue(pointerId.IsValid());

            if (_changes.TryGetValue(pointerId, out var change) == false)
                change = default;
            Assert.IsFalse(change.Added);
            Assert.IsFalse(change.Pressed);
            Assert.IsFalse(change.Released);
            Assert.IsFalse(change.Removed);
            Assert.IsFalse(change.Cancelled);
            change.Added = true;
            change.Pressed = true;
            change.Released = true;
            change.Removed = true;
            _changes[pointerId] = change;
        }

        public void Put_Update(PointerId pointerId)
        {
            Assert.IsTrue(pointerId.IsValid());

            if (_changes.TryGetValue(pointerId, out var change) == false)
                change = default;
            Assert.IsFalse(change.Updated);
            Assert.IsFalse(change.Cancelled);
            change.Updated = true;
            _changes[pointerId] = change;
        }

        public void Put_Cancel(PointerId pointerId)
        {
            Assert.IsTrue(pointerId.IsValid());

            if (_changes.TryGetValue(pointerId, out var change) == false)
                change = default;
            Assert.IsFalse(change.Cancelled);
            Assert.IsFalse(change.Removed);
            change.Cancelled = true;
            _changes[pointerId] = change;
        }
    }
}