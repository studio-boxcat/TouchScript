/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Behaviors.Cursors.UI;
using UnityEngine;

namespace TouchScript.Behaviors.Cursors
{
    /// <summary>
    /// Cursor for mouse pointers.
    /// </summary>
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_Behaviors_Cursors_MouseCursor.htm")]
    public class MouseCursor : PointerCursor
    {
        #region Public properties

        /// <summary>
        /// Default cursor sub object.
        /// </summary>
        public TextureSwitch DefaultCursor;

        /// <summary>
        /// Pressed cursor sub object.
        /// </summary>
        public TextureSwitch PressedCursor;

        #endregion

        #region Protected methods

        /// <inheritdoc />
        protected override void UpdateOnce()
        {
            switch (_state)
            {
                case CursorState.Released:
                case CursorState.Over:
                    DefaultCursor.Show();
                    PressedCursor.Hide();
                    break;
                case CursorState.Pressed:
                case CursorState.OverPressed:
                    DefaultCursor.Hide();
                    PressedCursor.Show();
                    break;
            }

            base.UpdateOnce();
        }

        #endregion
    }
}