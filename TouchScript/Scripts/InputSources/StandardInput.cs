/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Core;
using TouchScript.InputSources.InputHandlers;
using TouchScript.Pointers;
using UnityEngine;

namespace TouchScript.InputSources
{
    /// <summary>
    /// Processes standard input events (mouse, pointer, pen) on all platforms.
    /// Initializes proper inputs automatically. Replaces old Mobile and Mouse inputs.
    /// </summary>
    [AddComponentMenu("TouchScript/Input Sources/Standard Input")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_InputSources_StandardInput.htm")]
    public sealed class StandardInput : InputSource
    {
        #region Private variables

        private static StandardInput instance;

        private MouseHandler mouseHandler;
        private TouchHandler touchHandler;

        #endregion

        #region Public methods

        /// <inheritdoc />
        public override bool UpdateInput()
        {
            if (base.UpdateInput()) return true;

            var handled = false;
            if (touchHandler != null)
            {
                handled = touchHandler.UpdateInput();
            }
            if (mouseHandler != null)
            {
                if (handled) mouseHandler.CancelMousePointer();
                else handled = mouseHandler.UpdateInput();
            }

            return handled;
        }

        /// <inheritdoc />
        public override bool CancelPointer(Pointer pointer, bool shouldReturn)
        {
            base.CancelPointer(pointer, shouldReturn);

            var handled = false;
            if (touchHandler != null) handled = touchHandler.CancelPointer(pointer, shouldReturn);
            if (mouseHandler != null && !handled) handled = mouseHandler.CancelPointer(pointer, shouldReturn);

            return handled;
        }

        #endregion

        #region Internal methods

        /// <inheritdoc />
        public override void INTERNAL_DiscardPointer(Pointer pointer)
        {
            base.INTERNAL_DiscardPointer(pointer);

            var handled = false;
            if (touchHandler != null) handled = touchHandler.DiscardPointer(pointer);
            if (mouseHandler != null && !handled) handled = mouseHandler.DiscardPointer(pointer);
        }

        /// <inheritdoc />
        public override void INTERNAL_UpdateResolution()
        {
            base.INTERNAL_UpdateResolution();

            if (touchHandler != null) touchHandler.UpdateResolution(screenWidth, screenHeight);
            if (mouseHandler != null) mouseHandler.UpdateResolution(screenWidth, screenHeight);
        }

        #endregion

        #region Unity

        /// <inheritdoc />
        protected override void OnDisable()
        {
            disableMouse();
            disableTouch();
            base.OnDisable();
        }

        #endregion

        #region Protected methods

        /// <inheritdoc />
        protected override void init()
        {
            if (instance != null) Destroy(instance);
            instance = this;

            Input.simulateMouseWithTouches = false;

            enableMouse();
            enableTouch();
        }

        #endregion

        #region Private functions

        private void enableMouse()
        {
            mouseHandler = new MouseHandler(this, TouchManagerInstance.Instance);
            Debug.Log("[TouchScript] Initialized Unity mouse input.");
        }

        private void disableMouse()
        {
            if (mouseHandler != null)
            {
                mouseHandler.Dispose();
                mouseHandler = null;
            }
        }

        private void enableTouch()
        {
            touchHandler = new TouchHandler(this, TouchManagerInstance.Instance);
            Debug.Log("[TouchScript] Initialized Unity touch input.");
        }

        private void disableTouch()
        {
            if (touchHandler != null)
            {
                touchHandler.Dispose();
                touchHandler = null;
            }
        }

        #endregion
    }
}