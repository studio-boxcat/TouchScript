using System.Collections.Generic;
using System.Linq;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = TouchScript.Utils.Logger;

namespace TouchScript.InputSources
{
    public readonly struct PointerContainer
    {
        static PointerId _nextPointerId = (PointerId) 1;
        static PointerId IssuePointerId() => _nextPointerId++;

        static readonly Logger _logger = new(nameof(PointerContainer));

        public readonly Dictionary<PointerId, Pointer> Pointers;

        readonly Stack<Pointer> _pool;

        public PointerContainer(int capacity)
        {
            Pointers = new Dictionary<PointerId, Pointer>(capacity);
            _pool = new Stack<Pointer>();
        }

        public bool Empty() => Pointers.Count == 0;

        public Pointer Create(Vector2 pos, IInputSource inputSource)
        {
            var pointerId = IssuePointerId();
            if (_pool.TryPop(out var pointer))
            {
                _logger.Info($"{nameof(Create)}: {pointerId} (Pool)");
            }
            else
            {
                _logger.Info($"{nameof(Create)}: {pointerId}");
                pointer = new Pointer();
            }

            Assert.AreEqual(PointerId.Invalid, pointer.Id);
            pointer.INTERNAL_Init(pointerId, inputSource, pos);

            Assert.IsTrue(Pointers.Values.All(p => !ReferenceEquals(p, pointer)));
            Pointers.Add(pointerId, pointer);
            Assert.IsTrue(Pointers.Count < 20);

            return pointer;
        }

        public void Destroy(Pointer pointer)
        {
            var pointerId = pointer.Id;
            _logger.Info($"{nameof(Destroy)}: {pointerId}");

            Assert.AreNotEqual(PointerId.Invalid, pointerId);
            Assert.IsTrue(_pool.All(p => !ReferenceEquals(p, pointer)));

            var removed = Pointers.Remove(pointerId);
            Assert.IsTrue(removed);

            pointer.INTERNAL_Reset();
            _pool.Push(pointer);
        }
    }
}