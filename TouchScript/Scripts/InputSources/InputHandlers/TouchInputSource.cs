/*
 * @author Michael Holub
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = TouchScript.Utils.Logger;

namespace TouchScript.InputSources.InputHandlers
{
    /// <summary>
    /// Unity touch handling implementation which can be embedded and controlled from other (input) classes.
    /// </summary>
    public class TouchInputSource : IInputSource
    {
        readonly PointerContainer _pointerContainer;
        readonly Dictionary<int, Pointer> _pointers = new(10);
        static readonly Logger _logger = new(nameof(TouchInputSource));

        public TouchInputSource(PointerContainer pointerContainer)
        {
            _pointerContainer = pointerContainer;
        }

        static readonly List<int> _fingerIdBuf = new();

        public void Deactivate(PointerChanges changes)
        {
            Assert.AreEqual(0, _fingerIdBuf.Count);
            _fingerIdBuf.AddRange(_pointers.Keys);

            foreach (var fingerId in _fingerIdBuf)
            {
                var pointer = _pointers[fingerId];
                if (pointer == null) continue;
                changes.Put_Cancel(pointer.Id);
                _pointers[fingerId] = null;
            }

            _fingerIdBuf.Clear();
        }

        public bool UpdateInput(PointerChanges changes)
        {
            for (var i = 0; i < Input.touchCount; ++i)
            {
                var t = Input.GetTouch(i);
                var phase = t.phase;
                if (phase == TouchPhase.Stationary)
                    continue;

                var fingerId = t.fingerId;
                var pos = t.position;

                // 터치를 새롭게 시작하는 경우.
                if (phase == TouchPhase.Began)
                {
                    // 이미 포인터가 존재했다면 포인터 제거.
                    if (_pointers.TryGetValue(fingerId, out var oldPointer) && oldPointer != null)
                        changes.Put_ReleaseAndRemove(oldPointer.Id);

                    // 새롭게 터치를 생성함.
                    var newPointer = CreatePointer(fingerId, pos);
                    changes.Put_AddAndPress(newPointer.Id);
                    _pointers[fingerId] = newPointer;
                    continue;
                }

                // 이하 있던 터치를 업데이트하는 경우.

                // fingerId 에 대응하는 Pointer 가 없는 경우.
                if (_pointers.TryGetValue(fingerId, out var pointer) == false)
                {
                    // 없다가 생긴게 취소된 상태라면 무시함.
                    if (phase == TouchPhase.Canceled)
                        continue;

                    if (phase != TouchPhase.Ended)
                    {
                        var newPointer = CreatePointer(fingerId, pos);
                        changes.Put_AddAndPress(newPointer.Id);
                        _pointers[fingerId] = newPointer;
                    }
                    // 이미 Ended 라면, SingleFrameTap 으로 취급.
                    else
                    {
                        var newPointer = CreatePointer(fingerId, pos);
                        changes.Put_SingleFrameTap(newPointer.Id);
                        _pointers[fingerId] = null;
                    }

                    continue;
                }

                // fingerId 에 대응하는 Pointer 가 있는 경우.
                // 이미 끝난 터치는 무시.
                if (pointer == null) continue;

                var pointerId = pointer.Id;
                switch (phase)
                {
                    case TouchPhase.Moved:
                        pointer.NewPosition = pos;
                        changes.Put_Update(pointerId);
                        break;
                    // NOTE: Unity touch on Windows reports Cancelled as Ended
                    // when a touch goes out of display boundary
                    case TouchPhase.Ended:
                        changes.Put_ReleaseAndRemove(pointerId);
                        _pointers[fingerId] = null;
                        break;
                    case TouchPhase.Canceled:
                        changes.Put_Cancel(pointerId);
                        _pointers[fingerId] = null;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return Input.touchCount > 0;
        }

        Pointer CreatePointer(int fingerId, Vector2 pos)
        {
            _logger.Info($"CreatePointer: {fingerId}");
            return _pointerContainer.Create(pos, this);
        }

        public void CancelPointer(Pointer pointer, bool shouldReturn, PointerChanges changes)
        {
            var pointerId = pointer.Id;
            Assert.IsTrue(pointerId.IsValid());

            // pointer 에 대응되는 fingerId 를 탐색.
            // 탐색에 성공하면, Removed 도 Cancelled 도 요청되지 않았다는 것.
            var fingerId = int.MaxValue;
            foreach (var (curFingerId, curPointer) in _pointers)
            {
                if (curPointer == null || curPointer != pointer)
                    continue;
                fingerId = curFingerId;
                break;
            }

            if (fingerId == int.MaxValue)
            {
                _logger.Warning("포인터에 해당하는 상태를 찾을 수 없습니다. 이미 취소된 포인터일 수 있습니다.");
                return;
            }

            changes.Put_Cancel(pointerId);

            if (shouldReturn)
            {
                var newPointer = CreatePointer(fingerId, default);
                newPointer.CopyPositions(pointer);
                newPointer.IsReturned = true;
                changes.Put_AddAndPress(newPointer.Id);
                _pointers[fingerId] = newPointer;
            }
            else
            {
                _pointers[fingerId] = null;
            }
        }

        void IInputSource.INTERNAL_DiscardPointer(Pointer pointer, bool cancelled)
        {
            var pointerId = pointer.Id;
            _logger.Info("Discard: " + pointerId);
            Assert.IsFalse(_pointers.ContainsValue(pointer));
            _pointerContainer.Destroy(pointer);
        }
    }
}