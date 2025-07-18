using System.Collections.Generic;
using System.Linq;
using TouchScript.Core;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.InputSources
{
    public readonly struct PointerContainer
    {
        private static PointerId _nextPointerId = (PointerId) 1;
        private static PointerId IssuePointerId() => _nextPointerId++;

        // static readonly Logger _logger = new(nameof(PointerContainer));

        public readonly Dictionary<PointerId, Pointer> Pointers;

        private readonly Stack<Pointer> _pool;

        public PointerContainer(int capacity)
        {
            Pointers = new Dictionary<PointerId, Pointer>(capacity);
            _pool = new Stack<Pointer>();
        }

        public override string ToString()
        {
            return $"PointerContainer→{string.Join(",", Pointers.Keys)}";
        }

        public bool Empty() => Pointers.Count == 0;

        public Pointer Create(Vector2 pos, IInputSource inputSource)
        {
            Assert.IsTrue(TouchManager.Instance.enabled);

            var pointerId = IssuePointerId();
            if (_pool.TryPop(out var pointer))
            {
                // _logger.Info($"{nameof(Create)}: {pointerId} ({inputSource.GetType().Name}) (From Pool)");
            }
            else
            {
                // _logger.Info($"{nameof(Create)}: {pointerId} ({inputSource.GetType().Name})");
                pointer = new Pointer();
            }

            Assert.AreEqual(PointerId.Invalid, pointer.Id);
            pointer.INTERNAL_Init(pointerId, inputSource, pos);

            Assert.IsTrue(Pointers.Values.All(p => !ReferenceEquals(p, pointer)));
            Pointers.Add(pointerId, pointer);

#if DEBUG
            if (Pointers.Count > 20)
                Debug.LogError("Too many pointers: " + Pointers.Count);
#endif

            return pointer;
        }

        public void Destroy(Pointer pointer)
        {
            var pointerId = pointer.Id;
            // _logger.Info($"{nameof(Destroy)}: {pointerId}");

            Assert.AreNotEqual(PointerId.Invalid, pointerId);
            Assert.IsTrue(_pool.All(p => !ReferenceEquals(p, pointer)));

            var removed = Pointers.Remove(pointerId);
            Assert.IsTrue(removed);

            pointer.INTERNAL_Reset();
            _pool.Push(pointer);

            // XXX: 너무 pool 에 원소가 많아지면 버그로 간주.
            Assert.IsTrue(_pool.Count < 64);
        }
    }
}