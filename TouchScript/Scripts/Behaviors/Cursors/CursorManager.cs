/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Core;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.Behaviors.Cursors
{
    /// <summary>
    /// <para>Pointer visualizer which shows pointer circles with debug text using Unity UI.</para>
    /// <para>The script should be placed on an element with RectTransform or a Canvas. A reference prefab is provided in TouchScript package.</para>
    /// </summary>
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Behaviors_Cursors_CursorManager.htm")]
    public class CursorManager : MonoBehaviour
    {
        #region Private variables

        [SerializeField]
        private PointerCursor mouseCursor;

        [SerializeField]
        private uint cursorPixelSize = 64;

        private RectTransform rect;
        private ObjectPool<PointerCursor> mousePool;
        private Dictionary<PointerId, PointerCursor> cursors = new(10);

        #endregion

        #region Unity methods

        private void Awake()
        {
            mousePool = new ObjectPool<PointerCursor>(2, instantiateMouseProxy, null, clearProxy);

            rect = transform as RectTransform;
            if (rect == null)
            {
                Debug.LogError("CursorManager must be on an UI element!");
                enabled = false;
            }
        }

        private void OnEnable()
        {
            var touchManager = TouchManager.Instance;
            if (touchManager == null) return;

            touchManager.PointersAdded += PointersAddedHandler;
            touchManager.PointersRemoved += PointersRemovedHandler;
            touchManager.PointersPressed += PointersPressedHandler;
            touchManager.PointersReleased += PointersReleasedHandler;
            touchManager.PointersUpdated += PointersUpdatedHandler;
            touchManager.PointersCancelled += PointersRemovedHandler;
        }

        private void OnDisable()
        {
            var touchManager = TouchManager.Instance;
            if (touchManager == null) return;

            touchManager.PointersAdded -= PointersAddedHandler;
            touchManager.PointersRemoved -= PointersRemovedHandler;
            touchManager.PointersPressed -= PointersPressedHandler;
            touchManager.PointersReleased -= PointersReleasedHandler;
            touchManager.PointersUpdated -= PointersUpdatedHandler;
            touchManager.PointersCancelled -= PointersRemovedHandler;
        }

        #endregion

        #region Private functions

        private PointerCursor instantiateMouseProxy()
        {
            return Instantiate(mouseCursor);
        }

        private void clearProxy(PointerCursor cursor)
        {
            cursor.Hide();
        }

        #endregion

        #region Event handlers

        private void PointersAddedHandler(List<Pointer> pointers)
        {
            foreach (var pointer in pointers)
            {
                var cursor = mousePool.Get();
                cursor.Size = cursorPixelSize;
                cursor.Init(rect, pointer);
                cursors.Add(pointer.Id, cursor);
            }
        }

        private void PointersRemovedHandler(List<Pointer> pointers)
        {
            foreach (var pointer in pointers)
            {
                if (!cursors.TryGetValue(pointer.Id, out var cursor)) continue;
                cursors.Remove(pointer.Id);

                mousePool.Release(cursor);
            }
        }

        private void PointersPressedHandler(List<Pointer> pointers)
        {
            foreach (var pointer in pointers)
            {
                if (!cursors.TryGetValue(pointer.Id, out var cursor)) continue;
                cursor.SetState(pointer, PointerCursor.CursorState.Pressed);
            }
        }

        private void PointersUpdatedHandler(List<Pointer> pointers)
        {
            foreach (var pointer in pointers)
            {
                if (!cursors.TryGetValue(pointer.Id, out var cursor)) continue;
                cursor.UpdatePointer(pointer);
            }
        }

        private void PointersReleasedHandler(List<Pointer> pointers)
        {
            foreach (var pointer in pointers)
            {
                if (!cursors.TryGetValue(pointer.Id, out var cursor)) continue;
                cursor.SetState(pointer, PointerCursor.CursorState.Released);
            }
        }

        #endregion
    }
}