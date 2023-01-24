/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections;
using System.Collections.Generic;
using TouchScript.Core;
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
        [SerializeField] PointerCursor _cursorPrefab;
        [SerializeField] uint _cursorPixelSize = 128;

        readonly Stack<PointerCursor> _cursorPool = new();
        readonly Dictionary<PointerId, PointerCursor> _cursors = new(10);

        TouchManager _touchManager;

        void OnEnable()
        {
            _touchManager = TouchManager.Instance;

            _touchManager.PointerAdded += PointerAddedHandler;
            _touchManager.PointerRemoved += PointerRemovedHandler;
            _touchManager.PointerPressed += PointerPressedHandler;
            _touchManager.PointerReleased += PointersReleasedHandler;
            _touchManager.PointerUpdated += PointerUpdatedHandler;
            _touchManager.PointerCancelled += PointerRemovedHandler;
        }

        void OnDisable()
        {
            _touchManager.PointerAdded -= PointerAddedHandler;
            _touchManager.PointerRemoved -= PointerRemovedHandler;
            _touchManager.PointerPressed -= PointerPressedHandler;
            _touchManager.PointerReleased -= PointersReleasedHandler;
            _touchManager.PointerUpdated -= PointerUpdatedHandler;
            _touchManager.PointerCancelled -= PointerRemovedHandler;
        }

        #region Event handlers

        void PointerAddedHandler(Pointer pointer)
        {
            if (_cursorPool.TryPop(out var cursor) == false)
            {
                cursor = Instantiate(_cursorPrefab, transform, false);
                var cursorTrans = (RectTransform) cursor.transform;
                cursorTrans.anchorMin = cursorTrans.anchorMax = Vector2.zero;
                cursorTrans.sizeDelta = new Vector2(_cursorPixelSize, _cursorPixelSize);
            }

            cursor.Init(pointer.Position);
            cursor.name = ((int) pointer.Id).ToString();
            cursor.gameObject.SetActive(true);
            _cursors.Add(pointer.Id, cursor);
        }

        void PointerRemovedHandler(Pointer pointer)
        {
            var pointerId = pointer.Id;
            if (_cursors.TryGetValue(pointerId, out var cursor) == false)
                return;

            _cursors.Remove(pointerId);
            StartCoroutine(CoRemove(cursor));

            IEnumerator CoRemove(PointerCursor cursor)
            {
                yield return null;
                cursor.gameObject.SetActive(false);
                _cursorPool.Push(cursor);
            }
        }

        void PointerPressedHandler(Pointer pointer)
        {
            var pointerId = pointer.Id;
            if (_cursors.TryGetValue(pointerId, out var cursor) == false)
                return;

            cursor.SetState(PointerCursor.CursorState.Pressed);
        }

        void PointerUpdatedHandler(Pointer pointer)
        {
            var pointerId = pointer.Id;
            if (_cursors.TryGetValue(pointerId, out var cursor) == false)
                return;

            var cursorTrans = (RectTransform) cursor.transform;
            cursorTrans.anchoredPosition = pointer.Position;
        }

        void PointersReleasedHandler(Pointer pointer)
        {
            var pointerId = pointer.Id;
            if (_cursors.TryGetValue(pointerId, out var cursor) == false)
                return;

            cursor.SetState(PointerCursor.CursorState.Released);
        }

        #endregion
    }
}