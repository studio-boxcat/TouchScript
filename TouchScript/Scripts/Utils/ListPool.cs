using System.Collections.Generic;

namespace TouchScript.Utils
{
    class ListPool<T>
    {
        readonly List<List<T>> _stack;

        public ListPool(int capacity)
        {
            _stack = new List<List<T>>(capacity);
        }

        public List<T> Get()
        {
            var count = _stack.Count;
            if (count is 0)
                return new List<T>();
            var list = _stack[count - 1];
            _stack.RemoveAt(count - 1);
            return list;
        }

        public void Release(List<T> element)
        {
            element.Clear();
            _stack.Add(element);
        }
    }
}