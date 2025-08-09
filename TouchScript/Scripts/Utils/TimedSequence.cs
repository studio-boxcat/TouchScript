/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using UnityEngine;

namespace TouchScript
{
    internal sealed class TimedSequence<T>
    {
        private List<T> points = new List<T>();
        private List<float> timestamps = new List<float>();

        public void Add(T element)
        {
            Add(element, Time.unscaledTime);
        }

        public void Add(T element, float time)
        {
            points.Add(element);
            timestamps.Add(time);
        }

        public IList<T> FindElementsLaterThan(float time, out float lastTime)
        {
            var list = new List<T>();
            var i = points.Count - 1;
            for (; i >= 0; i--)
            {
                if (timestamps[i] > time) list.Add(points[i]);
                else break;
            }
            list.Reverse();
            if (i < points.Count - 1) lastTime = timestamps[i + 1];
            else lastTime = time;

            return list;
        }
    }
}