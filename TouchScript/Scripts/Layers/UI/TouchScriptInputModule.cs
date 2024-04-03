/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.InputSources;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;
using Logger = TouchScript.Utils.Logger;

namespace TouchScript.Layers.UI
{
    /// <summary>
    /// An implementation of a Unity UI Input Module which lets TouchScript interact with the UI and EventSystem.
    /// </summary>
    sealed class TouchScriptInputModule : BaseInputModule
    {
        readonly Logger _logger = new(nameof(TouchScriptInputModule));

        public override bool ShouldActivateModule() => true;

        #region From PointerInputModule

        readonly Dictionary<int, PointerEventData> m_PointerData = new();

        PointerEventData AddPointerData(int id)
        {
            // _logger.Info("AddPointerData: " + id);
            var data = new PointerEventData { pointerId = id };
            m_PointerData.Add(id, data);
#if UNITY_EDITOR
            if (m_PointerData.Count > 10)
                _logger.Error("Too many tracked pointers. Might be something wrong with the input.");
#endif
            return data;
        }

        PointerEventData GetPointerData(int id)
        {
            return m_PointerData[id];
        }

        void RemovePointerData(PointerEventData data)
        {
            // _logger.Info("RemovePointerData: " + data.pointerId);
            m_PointerData.Remove(data.pointerId);
        }

        PointerEventData GetTouchPointerEventData(Pointer p, PointerChange change)
        {
            // XXX: CancelledOnly 면서 포인터가 없어서 생성해야하는 상황은 호출되어서는 안 됨.
            Assert.IsFalse(change.CancelledOnly() && !m_PointerData.ContainsKey((int) p.Id));

            var id = (int) p.Id;
            var pointerData = change.Added ? AddPointerData(id) : GetPointerData(id);

            pointerData.Reset();

            if (change.Added)
                pointerData.position = p.Position;

            if (change.Pressed)
                pointerData.delta = Vector2.zero;
            else
                pointerData.delta = p.Position - pointerData.position;

            pointerData.position = p.Position;

            pointerData.button = PointerEventData.InputButton.Left;

            var over = p.GetOverData();
            if (change.Cancelled || over.Collider == null)
            {
                pointerData.pointerCurrentRaycast = default;
            }
            else
            {
                pointerData.pointerCurrentRaycast = new RaycastResult(over.Collider, over.Raycaster, over.ScreenPosition);
            }

            // pointerData.radius = Vector2.one * input.radius;
            // pointerData.radiusVariance = Vector2.one * input.radiusVariance;

            return pointerData;
        }

        static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
        {
            if (!useDragThreshold)
                return true;

            return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
        }

        void ProcessMove(PointerEventData pointerEvent)
        {
            var targetGO = (Cursor.lockState == CursorLockMode.Locked ? null : pointerEvent.pointerCurrentRaycast.gameObject);
            HandlePointerExitAndEnter(pointerEvent, targetGO);
        }

        void ProcessDrag(PointerEventData pointerEvent)
        {
            if (!pointerEvent.IsPointerMoving() ||
                Cursor.lockState == CursorLockMode.Locked ||
                pointerEvent.pointerDrag == null)
                return;

            if (!pointerEvent.dragging
                && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
            {
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
                pointerEvent.dragging = true;
            }

            // Drag notification
            if (pointerEvent.dragging)
            {
                // Before doing drag we should cancel any pointer down state
                // And clear selection!
                if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
                {
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                    pointerEvent.eligibleForClick = false;
                    pointerEvent.pointerPress = null;
                }
                ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
            }
        }

        void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
        {
            // Selection tracking
            var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
            // if we have clicked something new, deselect the old thing
            // leave 'selection handling' up to the press event though.
            if (selectHandlerGO != eventSystem.currentSelectedGameObject)
                eventSystem.SetSelectedGameObject(null, pointerEvent);
        }

        #endregion

        #region From StandaloneInputModule

        public override void Process()
        {
            // if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
            //     return;

            SendUpdateEventToSelectedObject();

            // touch needs to take precedence because of the mouse emulation layer
            // if (!ProcessTouchEvents() && input.mousePresent)
            //     ProcessMouseEvent();
        }

        public void ProcessTouchEvents(Pointer p, PointerChange change)
        {
            Assert.IsTrue(p.Id.IsValid());

            var pointer = GetTouchPointerEventData(p, change);

            ProcessTouchPress(pointer, change.Pressed, change.Released);

            if (change is { Removed: false, Cancelled: false })
            {
                ProcessMove(pointer);
                ProcessDrag(pointer);
            }
            else
                RemovePointerData(pointer);

            Assert.IsTrue(p.Id.IsValid());
        }

        void ProcessTouchPress(PointerEventData pointerEvent, bool pressed, bool released)
        {
            var currentOverGo = pointerEvent.pointerCurrentRaycast.gameObject;

            // PointerDown notification
            if (pressed)
            {
                pointerEvent.eligibleForClick = true;
                pointerEvent.delta = Vector2.zero;
                pointerEvent.dragging = false;
                pointerEvent.useDragThreshold = true;
                pointerEvent.pressPosition = pointerEvent.position;
                pointerEvent.pointerPressRaycast = pointerEvent.pointerCurrentRaycast;

                DeselectIfSelectionChanged(currentOverGo, pointerEvent);

                if (pointerEvent.pointerEnter != currentOverGo)
                {
                    // send a pointer enter to the touched element if it isn't the one to select...
                    HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    pointerEvent.pointerEnter = currentOverGo;
                }

                // search for the control that will receive the press
                // if we can't find a press handler set the press
                // handler to be what would receive a click.
                var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.pointerDownHandler);

                var newClick = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // didnt find a press handler... search for a click handler
                if (newPressed == null)
                    newPressed = newClick;

                // Debug.Log("Pressed: " + newPressed);

                pointerEvent.pointerPress = newPressed;
                pointerEvent.pointerClick = newClick;

                // Save the drag handler as well
                pointerEvent.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                if (pointerEvent.pointerDrag != null)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.initializePotentialDrag);
            }

            // PointerUp notification
            if (released)
            {
                // Debug.Log("Executing pressup on: " + pointer.pointerPress);
                ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

                // see if we mouse up on the same element that we clicked on...
                var pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                // PointerClick and Drop events
                if (pointerEvent.pointerClick == pointerClickHandler)
                {
                    if (pointerEvent.eligibleForClick)
                    {
                        _logger.Info("Execute click on: " + pointerClickHandler, pointerClickHandler);
                        ExecuteEvents.Execute(pointerEvent.pointerClick, pointerEvent, ExecuteEvents.pointerClickHandler);
                    }
                    else
                    {
                        _logger.Warning("Pointer ineligible for click: " + pointerClickHandler, pointerClickHandler);
                    }
                }
                else
                {
                    _logger.Warning("Pointer click handler mismatch: " + pointerClickHandler, pointerClickHandler);
                }

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                {
                    ExecuteEvents.ExecuteHierarchy(currentOverGo, pointerEvent, ExecuteEvents.dropHandler);
                }

                pointerEvent.eligibleForClick = false;
                pointerEvent.pointerPress = null;
                pointerEvent.pointerClick = null;

                if (pointerEvent.pointerDrag != null && pointerEvent.dragging)
                    ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.endDragHandler);

                pointerEvent.dragging = false;
                pointerEvent.pointerDrag = null;

                // send exit events as we need to simulate this on touch up on touch device
                ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                pointerEvent.pointerEnter = null;
            }
        }

        bool SendUpdateEventToSelectedObject()
        {
            if (eventSystem.currentSelectedGameObject == null)
                return false;

            var data = GetBaseEventData();
            ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
            return data.used;
        }

        #endregion
    }
}