/*
 * @author Valentin Simonov / http://va.lent.in/
 * Source code copied from UnityEngine.UI.ObjectPool:
 * https://bitbucket.org/Unity-Technologies/ui/src/ccb946ecc23815d1a7099aee0ed77b0cde7ff278/UnityEngine.UI/UI/Core/Utility/ObjectPool.cs?at=5.1
 */

using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace TouchScript.Utils
{
    /// <exclude />
    public class ObjectPool<T> where T : class
    {
        public delegate T0 UnityFunc<T0>();

        private readonly Stack<T> stack;
        private readonly UnityAction<T> onGet;
        private readonly UnityAction<T> onRelease;
        private readonly UnityFunc<T> onNew;

        public string Name { get; set; }

        public ObjectPool(int capacity, UnityFunc<T> actionNew, UnityAction<T> actionOnGet = null,
                          UnityAction<T> actionOnRelease = null, string name = null)
        {
            if (actionNew == null) throw new ArgumentException("New action can't be null!");
            stack = new Stack<T>(capacity);
            onNew = actionNew;
            onGet = actionOnGet;
            onRelease = actionOnRelease;
            Name = name;
        }

        public void WarmUp(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var element = onNew();
                stack.Push(element);
            }
        }

        public T Get()
        {
            T element;
            if (stack.Count == 0)
            {
                element = onNew();
            }
            else
            {
                element = stack.Pop();
            }
            if (onGet != null) onGet(element);
            return element;
        }

        public void Release(T element)
        {
            if (onRelease != null) onRelease(element);
            stack.Push(element);
        }
    }
}