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
            EventSystem eventSystem => input.eventSystem;

            public UIStandardInputModule(TouchScriptInputModule input)
            {
                this.input = input;
            }

            #region From PointerInputModule

            protected Dictionary<int, PointerEventData> m_PointerData = new Dictionary<int, PointerEventData>();

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

            protected void RemovePointerData(PointerEventData data)
            {
                m_PointerData.Remove(data.pointerId);
            }

            protected void DeselectIfSelectionChanged(GameObject currentOverGo, BaseEventData pointerEvent)
            {
                // Selection tracking
                var selectHandlerGO = ExecuteEvents.GetEventHandler<ISelectHandler>(currentOverGo);
                // if we have clicked something new, deselect the old thing
                // leave 'selection handling' up to the press event though.
                if (selectHandlerGO != eventSystem.currentSelectedGameObject)
                    eventSystem.SetSelectedGameObject(null, pointerEvent);
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

            #endregion

            #region From StandaloneInputModule

            protected bool SendUpdateEventToSelectedObject()
            {
                if (eventSystem.currentSelectedGameObject == null)
                    return false;

                var data = GetBaseEventData();
                ExecuteEvents.Execute(eventSystem.currentSelectedGameObject, data, ExecuteEvents.updateSelectedHandler);
                return data.used;
            }

            public void Process()
            {
                // if (!eventSystem.isFocused && ShouldIgnoreEventsOnNoFocus())
                //     return;

                SendUpdateEventToSelectedObject();

                // touch needs to take precedence because of the mouse emulation layer
                // if (!ProcessTouchEvents() && input.mousePresent)
                //     ProcessMouseEvent();
            }

            #endregion

            #region Changed

            BaseEventData GetBaseEventData() => input.GetBaseEventData();

            void HandlePointerExitAndEnter(PointerEventData currentPointerData, GameObject newEnterTarget)
                => input.HandlePointerExitAndEnter(currentPointerData, newEnterTarget);

            private static void ConvertRaycast(RaycastHitUI old, ref RaycastResult current)
            {
                current.gameObject = old.Target == null ? null : old.Target.gameObject;
                current.module = old.Raycaster;
                current.depth = old.Depth;
                current.index = old.GraphicIndex;
                current.sortingLayer = old.SortingLayer;
                current.sortingOrder = old.SortingOrder;
            }

            #endregion

            #region Event processors

            // XXX: PointerInputModule.GetTouchPointerEventData() 을 변형함.
            public virtual void ProcessAdded(List<Pointer> pointers)
            {
                var raycast = new RaycastResult();
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();

                    // Don't update the pointer if it is not over an UI element
                    if (over.IsNotUI()) continue;

                    GetPointerData((int) pointer.Id, out var pointerEvent, true);
                    pointerEvent.Reset();
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;

                    pointerEvent.position = pointer.Position;
                    pointerEvent.delta = Vector2.zero;
                    pointerEvent.button = PointerEventData.InputButton.Left;
                    ConvertRaycast(over.RaycastHitUI, ref raycast);
                    raycast.screenPosition = pointerEvent.position;
                    pointerEvent.pointerCurrentRaycast = raycast;

                    input.HandlePointerExitAndEnter(pointerEvent, currentOverGo);
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

                    GetPointerData((int) pointer.Id, out var pointerEvent, true);

                    // If not over an UI element this and previous frame, don't process further.
                    // Need to check the previous hover state to properly process leaving a UI element.
                    if (over.IsNotUI())
                    {
                        if (pointerEvent.hovered.Count == 0) continue;
                    }

                    pointerEvent.Reset();

                    pointerEvent.position = pointer.Position;
                    pointerEvent.delta = pointer.Position - pointer.PreviousPosition;
                    ConvertRaycast(over.RaycastHitUI, ref raycast);
                    raycast.screenPosition = pointerEvent.position;
                    pointerEvent.pointerCurrentRaycast = raycast;

                    ProcessMove(pointerEvent);
                    ProcessDrag(pointerEvent);
                }
            }

            // XXX: PointerInputModule.ProcessMove() 를 변형함.
            protected virtual void ProcessMove(PointerEventData pointerEvent)
            {
                var targetGO = (Cursor.lockState == CursorLockMode.Locked ? null : pointerEvent.pointerCurrentRaycast.gameObject);
                HandlePointerExitAndEnter(pointerEvent, targetGO);
            }

            // XXX: PointerInputModule.ProcessDrag() 를 변형함.
            protected virtual void ProcessDrag(PointerEventData pointerEvent)
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

            // XXX: StandaloneInputModule.ProcessTouchPress() 를 변형함.
            public virtual void ProcessPressed(List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();
                    // Don't update the pointer if it is not over an UI element
                    if (over.IsNotUI()) continue;
                    GetPointerData((int) pointer.Id, out var pointerEvent, true);
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;

                    pointerEvent.eligibleForClick = true;
                    pointerEvent.delta = Vector2.zero;
                    pointerEvent.dragging = false;
                    pointerEvent.useDragThreshold = true;
                    pointerEvent.position = pointer.Position; // XXX: Changed
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
            }

            // XXX: StandaloneInputModule.ProcessTouchPress() 를 변형함.
            public virtual void ProcessReleased(List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    var press = pointer.GetPressData();
                    // Don't update the pointer if it is was not pressed over an UI element
                    if (press.IsNotUI()) continue;
                    var over = pointer.GetOverData();
                    GetPointerData((int) pointer.Id, out var pointerEvent, true);
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;


                    // Debug.Log("Executing pressup on: " + pointer.pointerPress);
                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

                    // Debug.Log("KeyCode: " + pointer.eventData.keyCode);

                    // see if we mouse up on the same element that we clicked on...
                    var pointerClickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentOverGo);

                    // PointerClick and Drop events
                    if (pointerEvent.pointerClick == pointerClickHandler && pointerEvent.eligibleForClick)
                    {
                        ExecuteEvents.Execute(pointerEvent.pointerClick, pointerEvent, ExecuteEvents.pointerClickHandler);
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

                    // redo pointer enter / exit to refresh state
                    // so that if we moused over something that ignored it before
                    // due to having pressed on something else
                    // it now gets it.
                    if (currentOverGo != pointerEvent.pointerEnter)
                    {
                        HandlePointerExitAndEnter(pointerEvent, null);
                        HandlePointerExitAndEnter(pointerEvent, currentOverGo);
                    }
                }
            }

            // XXX: StandaloneInputModule.ProcessTouchPress() 를 변형함.
            public virtual void ProcessCancelled(List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();
                    GetPointerData((int) pointer.Id, out var pointerEvent, true);
                    var target = over.Target;
                    var currentOverGo = target == null ? null : target.gameObject;


                    ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);

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

            public virtual void ProcessRemoved(List<Pointer> pointers)
            {
                foreach (var pointer in pointers)
                {
                    var over = pointer.GetOverData();
                    // Don't update the pointer if it is not over an UI element
                    if (over.IsNotUI()) continue;
                    GetPointerData((int) pointer.Id, out var pointerEvent, true);

                    if (pointerEvent.pointerEnter) ExecuteEvents.ExecuteHierarchy(pointerEvent.pointerEnter, pointerEvent, ExecuteEvents.pointerExitHandler);
                    RemovePointerData(pointerEvent);
                }
            }

            #endregion
        }
    }
}