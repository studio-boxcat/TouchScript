using System.Collections.Generic;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.InputSources
{
    public readonly struct PointerContainer
    {
        static PointerId _nextPointerId = (PointerId) 1;
        static PointerId IssuePointerId() => _nextPointerId++;

        public readonly List<Pointer> Pointers;
        readonly Stack<Pointer> _pool;

        public PointerContainer(int capacity)
        {
            Pointers = new List<Pointer>(capacity);
            _pool = new Stack<Pointer>();
        }

        public Pointer Create(Vector2 pos, IInputSource inputSource)
        {
            if (_pool.TryPop(out var pointer) == false)
                pointer = new Pointer();
            Assert.AreEqual(PointerId.Invalid, pointer.Id);
            pointer.INTERNAL_Init(IssuePointerId(), inputSource, pos);

            Assert.IsFalse(Pointers.Contains(pointer));
            Pointers.Add(pointer);

            return pointer;
        }

        public void Destroy(Pointer pointer)
        {
            Assert.AreNotEqual(PointerId.Invalid, pointer.Id);
            Assert.IsFalse(_pool.Contains(pointer));

            var removed = Pointers.Remove(pointer);
            Assert.IsTrue(removed);

            pointer.INTERNAL_Reset();
            _pool.Push(pointer);
        }
    }
}