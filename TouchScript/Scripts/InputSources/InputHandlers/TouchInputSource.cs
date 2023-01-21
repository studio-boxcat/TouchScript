/*
 * @author Michael Holub
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using JetBrains.Annotations;
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
        readonly Dictionary<int, TouchState> _states = new(10);
        static readonly Logger _logger = new(nameof(TouchInputSource));

        public TouchInputSource(PointerContainer pointerContainer)
        {
            _pointerContainer = pointerContainer;
        }

        public bool UpdateInput(PointerChanges changes)
        {
            for (var i = 0; i < Input.touchCount; ++i)
            {
                var t = Input.GetTouch(i);
                var fingerId = t.fingerId;
                var phase = t.phase;
                var pos = t.position;

                // 없다가 생긴 경우.
                if (_states.TryGetValue(t.fingerId, out var touchState) == false)
                {
                    // 없다가 생긴게 취소된 상태라면 무시함.
                    if (phase == TouchPhase.Canceled)
                        continue;

                    if (phase != TouchPhase.Ended)
                    {
                        var newPointer = CreatePointer(fingerId, pos, false);
                        changes.Put_AddAndPress(newPointer.Id);
                    }
                    // 이미 Ended 라면, SingleFrameTap 으로 취급.
                    else
                    {
                        var newPointer = CreatePointer(fingerId, pos, true);
                        changes.Put_SingleFrameTap(newPointer.Id);
                    }

                    continue;
                }

                // 이전에 있던 포인터를 업데이트하는 경우.
                var pointer = touchState.Pointer;
                var pointerId = pointer.Id;
                var ended = touchState.Ended;
                switch (phase)
                {
                    case TouchPhase.Began:
                    {
                        if (!ended)
                            changes.Put_ReleaseAndRemove(pointerId);

                        var newPointer = CreatePointer(fingerId, pos, false);
                        changes.Put_AddAndPress(newPointer.Id);
                        break;
                    }
                    case TouchPhase.Moved:
                    {
                        if (!ended)
                        {
                            pointer.NewPosition = pos;
                            changes.Put_Update(pointerId);
                        }
                        break;
                    }
                    // NOTE: Unity touch on Windows reports Cancelled as Ended
                    // when a touch goes out of display boundary
                    case TouchPhase.Ended:
                        if (!ended)
                        {
                            changes.Put_ReleaseAndRemove(pointerId);
                            _states[fingerId] = new TouchState(pointer, true);
                        }
                        break;
                    case TouchPhase.Canceled:
                        if (!ended)
                        {
                            changes.Put_Cancel(pointerId);
                            _states.Remove(fingerId);
                        }
                        break;
                    case TouchPhase.Stationary:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return Input.touchCount > 0;
        }

        Pointer CreatePointer(int fingerId, Vector2 pos, bool ended)
        {
            _logger.Info($"CreatePointer: {fingerId}, {ended}");
            var newPointer = _pointerContainer.Create(pos, this);
            var touchState = new TouchState(newPointer, ended);
            _states[fingerId] = touchState;
            Assert.IsTrue(_states.Count < 20);
            return newPointer;
        }

        public void CancelPointer(Pointer pointer, bool shouldReturn, PointerChanges changes)
        {
            var pointerId = pointer.Id;
            Assert.IsTrue(pointerId.IsValid());

            var fingerId = int.MaxValue;
            var ended = false;
            foreach (var touchState in _states)
            {
                if (touchState.Value.Pointer != pointer)
                    continue;
                fingerId = touchState.Key;
                ended = touchState.Value.Ended;
                break;
            }

            if (fingerId == int.MaxValue)
            {
                _logger.Warning("포인터에 해당하는 상태를 찾을 수 없습니다. 이미 취소된 포인터일 수 있습니다.");
                return;
            }

            changes.Put_Cancel(pointerId);
            _states.Remove(fingerId);

            // 이미 끝난 터치의 경우, Return 을 해도 의미가 없다.
            if (ended) return;

            // 끝난 터치가 아니고 리턴해야하는 경우.
            if (shouldReturn)
            {
                var newPointer = CreatePointer(fingerId, default, false);
                newPointer.CopyFrom(pointer);
                var change = new PointerChange {Added = true};
                if (pointer.Pressing) change.Pressed = true;
                newPointer.IsReturned = true;
                changes.Put(newPointer.Id, change);
            }
        }

        public void CancelAllPointers(PointerChanges changes)
        {
            foreach (var (pointer, _) in _states.Values)
                changes.Put_Cancel(pointer.Id);
            _states.Clear();
        }

        public void INTERNAL_DiscardPointer([NotNull] Pointer pointer)
        {
            var pointerId = pointer.Id;
            _logger.Info("Discard: " + pointerId);

            foreach (var (fingerId, touchState) in _states)
            {
                if (touchState.Pointer.Id == pointerId)
                {
                    _states.Remove(fingerId);
                    break;
                }
            }

            _pointerContainer.Destroy(pointer);
        }

        readonly struct TouchState
        {
            public readonly Pointer Pointer;
            public readonly bool Ended;

            public TouchState(Pointer pointer, bool ended)
            {
                Pointer = pointer;
                Ended = ended;
            }

            public void Deconstruct(out Pointer pointer, out bool ended)
            {
                pointer = Pointer;
                ended = Ended;
            }
        }
    }
}