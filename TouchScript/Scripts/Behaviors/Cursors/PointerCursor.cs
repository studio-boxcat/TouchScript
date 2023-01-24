/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using UnityEngine;

namespace TouchScript.Behaviors.Cursors
{
    /// <summary>
    /// Visual cursor implementation used by TouchScript.
    /// </summary>
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Behaviors_Cursors_PointerCursor.htm")]
    public class PointerCursor : MonoBehaviour
    {
        #region Consts

        /// <summary>
        /// Possible states of a cursor.
        /// </summary>
        public enum CursorState
        {
            /// <summary>
            /// Not pressed.
            /// </summary>
            Released,

            /// <summary>
            /// Pressed.
            /// </summary>
            Pressed,

            /// <summary>
            /// Over something.
            /// </summary>
            Over,

            /// <summary>
            /// Over and pressed.
            /// </summary>
            OverPressed
        }

        #endregion

        #region Private variables

        /// <summary>
        /// Current cursor state.
        /// </summary>
        protected CursorState _state;

        #endregion

        #region Public methods

        /// <summary>
        /// Initializes (resets) the cursor.
        /// </summary>
        public void Init(Vector2 position)
        {
            _state = CursorState.Released;
            ((RectTransform) transform).anchoredPosition = position;
            UpdateOnce();
        }

        /// <summary>
        /// Sets the state of the cursor.
        /// </summary>
        /// <param name="newState">The new state.</param>
        public void SetState(CursorState newState)
        {
            var oldState = _state;
            _state = newState;
            if (newState != oldState)
                UpdateOnce();
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// This method is called once when the cursor is initialized.
        /// </summary>
        protected virtual void UpdateOnce()
        {
        }

        #endregion
    }
}