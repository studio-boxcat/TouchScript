using System.Collections.Generic;
using System.Linq;
using TouchScript.Pointers;
using TouchScript.Utils;
using UnityEngine.Assertions;

namespace TouchScript.InputSources
{
    public readonly struct PointerChanges
    {
        readonly Dictionary<PointerId, PointerChange> _changes;
        static readonly Logger _logger = new(nameof(PointerChanges));

        public PointerChanges(int capacity)
        {
            _changes = new Dictionary<PointerId, PointerChange>(capacity);
        }

        public void Flush(PointerContainer pointers, List<(Pointer, PointerChange)> changes)
        {
            Assert.AreEqual(0, changes.Count);

            foreach (var (pointerId, pointer) in pointers.Pointers)
            {
                Assert.AreEqual(pointerId, pointer.Id);
                if (_changes.TryGetValue(pointerId, out var change))
                    changes.Add((pointer, change));
            }

#if DEBUG
            if (_changes.Count != changes.Count)
            {
                foreach (var (pointerId, change) in _changes)
                {
                    if (changes.Any(x => x.Item1.Id == pointerId))
                        continue;

                    _logger.Error($"삭제된 포인터의 변경사항이 발견되었습니다: {pointerId}, {change}");
                }
            }
#endif

            _changes.Clear();
        }

        public void Put(PointerId pointerId, PointerChange change)
        {
            Assert.IsTrue(pointerId.IsValid());

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
            change.Updated = true;
            _changes[pointerId] = change;
        }

        public void Put_Cancel(PointerId pointerId)
        {
            Assert.IsTrue(pointerId.IsValid());

            if (_changes.TryGetValue(pointerId, out var change) == false)
                change = default;
            Assert.IsFalse(change.Cancelled);
            change.Cancelled = true;
            _changes[pointerId] = change;
        }
    }
}