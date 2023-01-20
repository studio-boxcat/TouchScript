using System.Collections.Generic;
using TouchScript.Pointers;
using UnityEngine.Assertions;

namespace TouchScript.InputSources
{
    public readonly struct PointerChanges
    {
        readonly Dictionary<Pointer, PointerChange> _changes;

        public PointerChanges(int capacity)
        {
            _changes = new Dictionary<Pointer, PointerChange>(capacity);
        }

        public void Flush(List<KeyValuePair<Pointer, PointerChange>> changes)
        {
            changes.AddRange(_changes);
            _changes.Clear();
        }

        public void Put(Pointer pointer, PointerChange change)
        {
            if (_changes.TryGetValue(pointer, out var oldChange))
            {
                _changes[pointer] = PointerChange.MergeWithCheck(change, oldChange);
            }
            else
            {
                _changes[pointer] = change;
            }
        }

        public void Put_AddAndPress(Pointer pointer)
        {
            if (_changes.TryGetValue(pointer, out var change) == false)
                change = default;
            Assert.IsFalse(change.Added);
            Assert.IsFalse(change.Pressed);
            change.Added = true;
            change.Pressed = true;
            _changes[pointer] = change;
        }

        public void Put_ReleaseAndRemove(Pointer pointer)
        {
            if (_changes.TryGetValue(pointer, out var change) == false)
                change = default;
            Assert.IsFalse(change.Released);
            Assert.IsFalse(change.Removed);
            change.Released = true;
            change.Removed = true;
            _changes[pointer] = change;
        }

        public void Put_SingleFrameTap(Pointer pointer)
        {
            if (_changes.TryGetValue(pointer, out var change) == false)
                change = default;
            Assert.IsFalse(change.Added);
            Assert.IsFalse(change.Pressed);
            Assert.IsFalse(change.Released);
            Assert.IsFalse(change.Removed);
            change.Added = true;
            change.Pressed = true;
            change.Released = true;
            change.Removed = true;
            _changes[pointer] = change;
        }

        public void Put_Update(Pointer pointer)
        {
            if (_changes.TryGetValue(pointer, out var change) == false)
                change = default;
            Assert.IsFalse(change.Updated);
            change.Updated = true;
            _changes[pointer] = change;
        }

        public void Put_Cancel(Pointer pointer)
        {
            if (_changes.TryGetValue(pointer, out var change) == false)
                change = default;
            Assert.IsFalse(change.Cancelled);
            change.Cancelled = true;
            _changes[pointer] = change;
        }
    }
}