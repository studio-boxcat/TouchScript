/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TouchScript.InputSources;
using TouchScript.InputSources.InputHandlers;
using TouchScript.Layers.UI;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;
using Logger = TouchScript.Utils.Logger;

namespace TouchScript.Core
{
    public sealed class TouchManager : MonoBehaviour
    {
        public static TouchManager Instance;

        [SerializeField, Required, ChildGameObjectsOnly]
        TouchScriptInputModule _uiInputModule;

        readonly PointerContainer _pointerContainer = new(4);
        readonly PointerChanges _changes = new(10);
        StandardInput _input;

        readonly Logger _logger = new(nameof(TouchManager));

        public event Action FrameStarted;
        public event Action FrameFinished;
        public event Action<Pointer> PointerAdded;
        public event Action<Pointer> PointerUpdated;
        public event Action<Pointer> PointerPressed;
        public event Action<Pointer> PointerReleased;
        public event Action<Pointer> PointerRemoved;
        public event Action<Pointer> PointerCancelled;

        void Awake()
        {
            _input = new StandardInput(_pointerContainer);
        }

        void Update()
        {
            foreach (var pointer in _pointerContainer.Pointers.Values)
                pointer.INTERNAL_FrameStarted();
            _input.UpdateInput(_changes);
            UpdatePointers();
        }

        readonly List<(Pointer, PointerChange)> _tmpChanges = new();

        void UpdatePointers()
        {
            FrameStarted?.InvokeHandleExceptions();

            var pointers = _pointerContainer.Pointers;
            foreach (var pointer in pointers.Values)
                pointer.INTERNAL_UpdatePosition();

            _tmpChanges.Clear();
            _changes.Flush(_pointerContainer, _tmpChanges);

            foreach (var (pointer, change) in _tmpChanges)
                _uiInputModule.ProcessTouchEvents(pointer, change);

            foreach (var (pointer, change) in _tmpChanges)
            {
                Assert.IsTrue(pointer.Id.IsValid());

                // 포인터가 취소된 경우, 취소만을 업데이트하고 나머지는 무시.
                if (change.Cancelled)
                {
                    _logger.Info("Cancelled: " + pointer.Id);
                    PointerCancelled?.InvokeHandleExceptions(pointer);
                    pointer.InputSource.INTERNAL_DiscardPointer(pointer);
                    continue;
                }

                if (change.Added)
                    PointerAdded?.InvokeHandleExceptions(pointer);

                if (change.Updated)
                    PointerUpdated?.InvokeHandleExceptions(pointer);

                if (change.Pressed)
                {
                    Assert.IsFalse(pointer.Pressing);
                    pointer.Pressing = true;
                    var hit = pointer.GetOverData();
                    pointer.INTERNAL_SetPressData(hit);
                    PointerPressed?.InvokeHandleExceptions(pointer);
                }

                if (change.Released)
                {
                    Assert.IsTrue(pointer.Pressing);
                    pointer.Pressing = false;
                    PointerReleased?.InvokeHandleExceptions(pointer);
                    pointer.INTERNAL_ClearPressData();
                }

                if (change.Removed)
                {
                    _logger.Info("Removed: " + pointer.Id);
                    PointerRemoved?.InvokeHandleExceptions(pointer);
                    pointer.InputSource.INTERNAL_DiscardPointer(pointer);
                }
            }

            FrameFinished?.InvokeHandleExceptions();
        }

        public void CancelPointer(Pointer pointer, bool shouldReturn)
        {
            pointer.InputSource.CancelPointer(pointer, shouldReturn, _changes);
        }

        public void CancelAllPointers()
        {
            _input.CancelAllPointers(_changes);
        }

        public FakeInputSource GetFakeInputSource() => _input.FakeInputSource;
    }
}