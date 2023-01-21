/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using TouchScript.InputSources;
using TouchScript.Layers.UI;
using TouchScript.Utils;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript.Core
{
    public sealed class TouchManager : MonoBehaviour
    {
        public static TouchManager Instance;

        public readonly StandardInput Input = new();

        [SerializeField, Required, ChildGameObjectsOnly]
        TouchScriptInputModule _uiInputModule;

        readonly PointerChanges _changes = new(10);

        public event Action FrameStarted;
        public event Action FrameFinished;
        public event Action<Pointer> PointerAdded;
        public event Action<Pointer> PointerUpdated;
        public event Action<Pointer> PointerPressed;
        public event Action<Pointer> PointerReleased;
        public event Action<Pointer> PointerRemoved;
        public event Action<Pointer> PointerCancelled;

        void Update()
        {
            foreach (var pointer in Input.GetPointers())
                pointer.INTERNAL_FrameStarted();
            Input.UpdateInput(_changes);
            UpdatePointers();
        }

        readonly List<KeyValuePair<Pointer, PointerChange>> _tmpChanges = new();

        void UpdatePointers()
        {
            _tmpChanges.Clear();

            FrameStarted?.InvokeHandleExceptions();

            foreach (var pointer in Input.GetPointers())
                pointer.INTERNAL_UpdatePosition();

            _changes.Flush(_tmpChanges);

            _uiInputModule.ProcessTouchEvents(_tmpChanges);

            foreach (var (pointer, change) in _tmpChanges)
            {
                Assert.IsTrue(pointer.Id.IsValid());

                // 포인터가 취소된 경우, 취소만을 업데이트하고 나머지는 무시.
                if (change.Cancelled)
                {
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
            Input.CancelAllPointers(_changes);
        }
    }
}