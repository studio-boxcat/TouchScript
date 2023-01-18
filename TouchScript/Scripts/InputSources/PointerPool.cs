using System.Collections.Generic;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.InputSources
{
    public readonly struct PointerPool
    {
        readonly IInputSource _input;
        readonly Stack<Pointer> _pool;

        public PointerPool(IInputSource input)
        {
            _input = input;
            _pool = new Stack<Pointer>();
        }

        public Pointer Get(Vector2 position)
        {
            if (_pool.TryPop(out var pointer) == false)
                pointer = new Pointer(_input);
            Assert.AreEqual(PointerId.Invalid, pointer.Id);
            pointer.INTERNAL_Init(PointerIdIssuer.Issue(), position);
            return pointer;
        }

        public void Release(Pointer pointer)
        {
            Assert.AreNotEqual(PointerId.Invalid, pointer.Id);
            pointer.INTERNAL_Reset();
            _pool.Push(pointer);
        }
    }
}