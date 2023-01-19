using System.Collections.Generic;
using TouchScript.Hit;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TouchScript.Layers.UI
{
    internal sealed partial class TouchScriptInputModule
    {
        /// <summary>
        /// Basically, copied code from UI Input Module which handles all UI pointer processing logic.
        /// Last update: df1947cd (5.4f3)
        /// </summary>
        class UIStandardInputModule
        {
            protected TouchScriptInputModule input;

            public UIStandardInputModule(TouchScriptInputModule input)
            {
                this.input = input;
            }

            #region Unchanged from PointerInputModule

            private Dictionary<int, PointerEventData> m_PointerData = new Dictionary<int, PointerEventData>(10);

            public bool IsPointerOverGameObject(int pointerId)
            {
                var lastPointer = GetLastPointerEventData(pointerId);
                if (lastPointer != null)
                    return lastPointer.pointerEnter != null;
                return false;
            }

            protected bool GetPointerData(int id, out PointerEventData data, bool create)
            {
                if (!m_PointerData.TryGetValue(id, out data) && create)
                {
                    data = new PointerEventData()
                    {
                        pointerId = id,
                    };
                    m_PointerData.Add(id, data);
                    return true;
                }
                return false;
            }

            protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
            {
                // Selection tracking
                var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
                // if we have clicked something new, deselect the old thing
                // leave 'selection handling' up to the press event though.
                if (selectHandlerGO != input.eventSystem.currentSelectedGameObject)
                    input.eventSystem.SetSelectedGameObject(null, pointerEvent);
            }

            protected PointerEventData GetLastPointerEventData(int id)
            {
                PointerEventData data;
                GetPointerData(id, out data, false);
                return data;
            }

            private static bool ShouldStartDrag(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold)
            {
                if (!useDragThreshold)
                    return true;

                return (pressPos - currentPos).sqrMagnitude >= threshold * threshold;
            }

            private bool SendUpdateEventToSelectedObject()
            {
                if (input.eventSystem.currentSelectedGameObject == null)
                    return false;

                var data = input.GetBaseEventData();
                ExecuteEvents.Execute(input.eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
                return data.used;
            }

            #endregion

            public void Process()
            {
                bool usedEvent = SendUpdateEventToSelectedObject();

                // touch needs to take precedence because of the mouse emulation layer
                //                if (!ProcessTouchEvents() && Input.mousePresent)
                //                    ProcessMouseEvent();
            }

            #region Changed

            protected void RemovePointerData(int id)
            {
                m_PointerData.Remove(id);
            }

            private void convertRaycast(RaycastHitUI old, ref RaycastResult current)
            {
                current.module = old.Raycaster;
                current.gameObject = old.Target == null ? null : old.Target.gameObject;
                current.depth = old.Depth;
                current.index = old.GraphicIndex;
                current.sortingLayer = old.SortingLayer;
                current.sortingOrder = old.SortingOrder;
            }

            #endregion

            #region Event processors

            public virtual void ProcessAdded(List<Pointer> pointers)
            {
                var raycast = new RaycastResult();
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();

                    // Don't update the pointer if it is not over an UI element
                    if (over.IsNotUI()) continue;

                    GetPointerData((int) pointer.Id, out var data, true);
                    data.Reset();
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;

                    data.position = pointer.Position;
                    data.delta = Vector2.zero;
                    convertRaycast(over.RaycastHitUI, ref raycast);
                    raycast.screenPosition = data.position;
                    data.pointerCurrentRaycast = raycast;

                    input.HandlePointerExitAndEnter(data, currentOverGo);
                }
            }

            public virtual void ProcessUpdated(List<Pointer> pointers)
            {
                var raycast = new RaycastResult();
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();

                    // Don't update the pointer if it is pressed not over an UI element
                    if (pointer.Button.Pressed)
                    {
                        var press = pointer.GetPressData();
                        if (press.IsNotUI()) continue;
                    }

                    PointerEventData data;
                    GetPointerData((int) pointer.Id, out data, true);

                    // If not over an UI element this and previous frame, don't process further.
                    // Need to check the previous hover state to properly process leaving a UI element.
                    if (over.IsNotUI())
                    {
                        if (data.hovered.Count == 0) continue;
                    }

                    data.Reset();
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;

                    data.position = pointer.Position;
                    data.delta = pointer.Position - pointer.PreviousPosition;
                    convertRaycast(over.RaycastHitUI, ref raycast);
                    raycast.screenPosition = data.position;
                    data.pointerCurrentRaycast = raycast;

                    input.HandlePointerExitAndEnter(data, currentOverGo);

                    bool moving = data.IsPointerMoving();

                    if (moving && data.pointerDrag != null
                               && !data.dragging
                               && ShouldStartDrag(data.pressPosition, data.position, input.eventSystem.pixelDragThreshold, data.useDragThreshold))
                    {
                        ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.beginDragHandler);
                        data.dragging = true;
                    }

                    // Drag notification
                    if (data.dragging && moving && data.pointerDrag != null)
                    {
                        // Before doing drag we should cancel any pointer down state
                        // And clear selection!
                        if (data.pointerPress != data.pointerDrag)
                        {
                            ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);

                            data.eligibleForClick = false;
                            data.pointerPress = null;
                        }
                        ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.dragHandler);
                    }

                    if (!Mathf.Approximately(pointer.ScrollDelta.sqrMagnitude, 0.0f))
                    {
                        data.scrollDelta = pointer.ScrollDelta;
                        var scrollHandler = ExecuteEvents.GetEventHandler<IScrollHandler>(currentOverGo);
                        ExecuteEvents.ExecuteHierarchy(scrollHandler, data, ExecuteEvents.scrollHandler);
                    }
                }
            }

            public virtual void ProcessPressed(List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();
                    // Don't update the pointer if it is not over an UI element
                    if (over.IsNotUI()) continue;

                    PointerEventData data;
                    GetPointerData((int) pointer.Id, out data, true);
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;

                    data.eligibleForClick = true;
                    data.delta = Vector2.zero;
                    data.dragging = false;
                    data.useDragThreshold = true;
                    data.position = pointer.Position;
                    data.pressPosition = pointer.Position;
                    data.pointerPressRaycast = data.pointerCurrentRaycast;

                    DeselectIfSelectionChanged(currentOverGo, data);

                    if (data.pointerEnter != currentOverGo)
                    {
                        // send a pointer enter to the touched element if it isn't the one to select...
                        input.HandlePointerExitAndEnter(data, currentOverGo);
                        data.pointerEnter = currentOverGo;
                    }

                    // search for the control that will receive the press
                    // if we can't find a press handler set the press
                    // handler to be what would receive a click.
                    var newPressed = ExecuteEvents.ExecuteHierarchy(currentOverGo, data, ExecuteEvents.pointerDownHandler);

                    // didnt find a press handler... search for a click handler
                    if (newPressed == null)
                        newPressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                    // Debug.Log("Pressed: " + newPressed);

                    data.pointerPress = newPressed;

                    // Save the drag handler as well
                    data.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(currentOverGo);

                    if (data.pointerDrag != null)
                        ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.initializePotentialDrag);
                }
            }

            public virtual void ProcessReleased(List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    var press = pointer.GetPressData();
                    // Don't update the pointer if it is was not pressed over an UI element
                    if (press.IsNotUI()) continue;

                    var over = pointer.GetOverData();

                    GetPointerData((int) pointer.Id, out var data, true);
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;

                    ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);
                    var pointerUpHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);
                    if (data.pointerPress == pointerUpHandler && data.eligibleForClick)
                    {
                        ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
                    }
                    else if (data.pointerDrag != null && data.dragging)
                    {
                        ExecuteEvents.ExecuteHierarchy(currentOverGo, data, ExecuteEvents.dropHandler);
                    }

                    data.eligibleForClick = false;
                    data.pointerPress = null;

                    if (data.pointerDrag != null && data.dragging)
                        ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.endDragHandler);

                    data.dragging = false;
                    data.pointerDrag = null;

                    // send exit events as we need to simulate this on touch up on touch device
                    ExecuteEvents.ExecuteHierarchy(data.pointerEnter, data, ExecuteEvents.pointerExitHandler);
                    data.pointerEnter = null;

                    // redo pointer enter / exit to refresh state
                    // so that if we moused over somethign that ignored it before
                    // due to having pressed on something else
                    // it now gets it.
                    if (currentOverGo != data.pointerEnter)
                    {
                        input.HandlePointerExitAndEnter(data, null);
                        input.HandlePointerExitAndEnter(data, currentOverGo);
                    }
                }
            }

            public virtual void ProcessCancelled(List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();

                    PointerEventData data;
                    GetPointerData((int) pointer.Id, out data, true);
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;

                    ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);

                    if (data.pointerDrag != null && data.dragging)
                    {
                        ExecuteEvents.ExecuteHierarchy(currentOverGo, data, ExecuteEvents.dropHandler);
                    }

                    data.eligibleForClick = false;
                    data.pointerPress = null;

                    if (data.pointerDrag != null && data.dragging)
                        ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.endDragHandler);

                    data.dragging = false;
                    data.pointerDrag = null;

                    // send exit events as we need to simulate this on touch up on touch device
                    ExecuteEvents.ExecuteHierarchy(data.pointerEnter, data, ExecuteEvents.pointerExitHandler);
                    data.pointerEnter = null;
                }
            }

            public virtual void ProcessRemoved(List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();
                    // Don't update the pointer if it is not over an UI element
                    if (over.IsNotUI()) continue;

                    GetPointerData((int) pointer.Id, out var data, true);

                    if (data.pointerEnter) ExecuteEvents.ExecuteHierarchy(data.pointerEnter, data, ExecuteEvents.pointerExitHandler);
                    RemovePointerData((int) pointer.Id);
                }
            }

            #endregion
        }
    }
}