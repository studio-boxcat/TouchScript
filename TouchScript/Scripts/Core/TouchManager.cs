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

        bool _isUpdating = false;

        void Update()
        {
            Assert.IsFalse(_isUpdating);
            _isUpdating = true;

            var pointers = _pointerContainer.Pointers.Values;
            foreach (var pointer in pointers)
                pointer.SetOverDataDirty();

            _input.UpdateInput(_changes);

            FrameStarted?.InvokeHandleExceptions();

            foreach (var pointer in pointers)
                pointer.INTERNAL_UpdatePosition();

            CommitChanges();

            FrameFinished?.InvokeHandleExceptions();

            // 만약 업데이트 도중에 TouchManager 가 비활성화되었다면, 즉시 쌓인 변경사항을 반영함.
            if (enabled == false)
            {
                _logger.Info("비활성화가 감지되었습니다. 즉시 변경사항을 반영합니다.");
                CommitChanges();
                Assert.IsTrue(_pointerContainer.Empty());
                Assert.IsTrue(_changes.Empty());
            }

            _isUpdating = false;
        }

        public void Activate()
        {
            _logger.Info(nameof(Activate));

            // XXX: Update 에서 이벤트 전파 도중에 Deactivate 후 Activate 가 호출되는 경우.
            // 아직 Cancelled 가 commit 되지 않아, _pointerContainer 및 _changes 에 남은 원소가 생기게 됨.
            Assert.IsTrue(_isUpdating || _pointerContainer.Empty());
            Assert.IsTrue(_isUpdating || _changes.Empty(), _changes.ToString());
            enabled = true;
        }

        public void Deactivate()
        {
            _logger.Info(nameof(Deactivate));

            _input.Deactivate(_changes);
            if (_isUpdating == false)
            {
                CommitChanges();
                Assert.IsTrue(_pointerContainer.Empty(), _pointerContainer.ToString());
                Assert.IsTrue(_changes.Empty());
            }
            enabled = false;
        }

        readonly List<(Pointer, PointerChange)> _tmpChanges = new();

        void CommitChanges()
        {
            // 변경사항 모으기.
            _tmpChanges.Clear();
            _changes.Flush(_pointerContainer, _tmpChanges);

            // 변경사항 전처리.
            for (var i = 0; i < _tmpChanges.Count; i++)
            {
                var (pointer, change) = _tmpChanges[i];

                if (change.Released == false)
                {
                    // Released 추가설정 조건. (AND)
                    // 1) 눌려있었거나 눌릴 예정이면서,
                    // 2) 포인터가 제거 혹은 취소되었다.
                    var shouldRelease = (pointer.Pressing || change.Pressed) && (change.Removed || change.Cancelled);
                    if (shouldRelease)
                    {
                        change.Released = true;
                        _tmpChanges[i] = (pointer, change);
                    }
                }
            }

            // UI 먼저 업데이트.
            foreach (var (pointer, change) in _tmpChanges)
                _uiInputModule.ProcessTouchEvents(pointer, change);

            // 제스쳐 등 업데이트.
            foreach (var (pointer, change) in _tmpChanges)
            {
                Assert.IsTrue(pointer.Id.IsValid());

                if (change.Added)
                {
                    // _logger.Info("Added: " + pointer.Id);
                    PointerAdded?.InvokeHandleExceptions(pointer);
                }

                if (change.Updated)
                    PointerUpdated?.InvokeHandleExceptions(pointer);

                if (change.Pressed)
                {
                    Assert.IsFalse(pointer.Pressing);
                    // _logger.Info("Pressed: " + pointer.Id);
                    pointer.Pressing = true;
                    var hit = pointer.GetOverData();
                    pointer.INTERNAL_SetPressData(hit);
                    PointerPressed?.InvokeHandleExceptions(pointer);
                }

                if (change.Released)
                {
                    Assert.IsTrue(pointer.Pressing);
                    // _logger.Info("Released: " + pointer.Id);
                    pointer.Pressing = false;
                    PointerReleased?.InvokeHandleExceptions(pointer);
                    pointer.INTERNAL_ClearPressData();
                }

                if (change.Removed)
                {
                    Assert.IsTrue(pointer.Id.IsValid());
                    // _logger.Info("Removed: " + pointer.Id);
                    PointerRemoved?.InvokeHandleExceptions(pointer);
                    pointer.InputSource.INTERNAL_DiscardPointer(pointer, false);
                }

                if (change.Cancelled)
                {
                    Assert.IsTrue(pointer.Id.IsValid());
                    // _logger.Info("Cancelled: " + pointer.Id);
                    PointerCancelled?.InvokeHandleExceptions(pointer);
                    pointer.InputSource.INTERNAL_DiscardPointer(pointer, true);
                }
            }
        }

        public void CancelPointer(Pointer pointer)
        {
            pointer.InputSource.CancelPointer(pointer, _changes);
        }

        public FakeInputSource GetFakeInputSource() => _input.FakeInputSource;
    }
}