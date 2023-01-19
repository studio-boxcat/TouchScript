/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Text;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.UI;

namespace TouchScript.Behaviors.Cursors
{
    /// <summary>
    /// Abstract class for pointer cursors with text.
    /// </summary>
    /// <typeparam name="T">Pointer type.</typeparam>
    /// <seealso cref="TouchScript.Behaviors.Cursors.PointerCursor" />
    public abstract class TextPointerCursor : PointerCursor
    {
        #region Public properties

        /// <summary>
        /// Should the value of <see cref="Pointer.Id"/> be shown on screen on the cursor.
        /// </summary>
        public bool ShowPointerId = true;

        /// <summary>
        /// The link to UI.Text component.
        /// </summary>
        public Text Text;

        #endregion

        #region Private variables

        private static StringBuilder stringBuilder = new StringBuilder(64);

        #endregion

        #region Protected methods

        /// <inheritdoc />
        protected override void updateOnce(Pointer pointer)
        {
            base.updateOnce(pointer);

            if (Text == null) return;
            if (!textIsVisible())
            {
                Text.enabled = false;
                return;
            }

            Text.enabled = true;
            stringBuilder.Length = 0;
            generateText(pointer, stringBuilder);

            Text.text = stringBuilder.ToString();
        }

        /// <summary>
        /// Generates text for pointer.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <param name="str">The string builder to use.</param>
        protected virtual void generateText(Pointer pointer, StringBuilder str)
        {
            if (ShowPointerId)
            {
                str.Append("Id: ");
                str.Append(pointer.Id);
            }
        }

        /// <summary>
        /// Indicates if text should be visible.
        /// </summary>
        /// <returns><c>True</c> if pointer text should be displayed; <c>false</c> otherwise.</returns>
        protected virtual bool textIsVisible()
        {
            return ShowPointerId;
        }

        /// <summary>
        /// Typed version of <see cref="getPointerHash"/>. Returns a hash of a cursor state.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <returns>Integer hash.</returns>
        protected virtual uint gethash(Pointer pointer)
        {
            return (uint) state;
        }

        /// <inheritdoc />
        protected sealed override uint getPointerHash(Pointer pointer)
        {
            return gethash(pointer);
        }

        #endregion
    }

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

        #region Public properties

        /// <summary>
        /// Cursor size in pixels.
        /// </summary>
        public float Size
        {
            get { return size; }
            set
            {
                size = value;
                if (size > 0)
                {
                    rect.sizeDelta = Vector2.one * size;
                }
                else
                {
                    size = 0;
                    rect.sizeDelta = Vector2.one * defaultSize;
                }
            }
        }

        #endregion

        #region Private variables

        /// <summary>
        /// Current cursor state.
        /// </summary>
        protected CursorState state;

        /// <summary>
        /// CUrrent cursor state data.
        /// </summary>
        protected object stateData;

        /// <summary>
        /// Cached RectTransform.
        /// </summary>
        protected RectTransform rect;

        /// <summary>
        /// Cursor size.
        /// </summary>
        protected float size = 0;

        /// <summary>
        /// Initial cursor size in pixels.
        /// </summary>
        protected float defaultSize;

        /// <summary>
        /// Last data hash.
        /// </summary>
        protected uint hash = uint.MaxValue;

        private CanvasGroup group;

        #endregion

        #region Public methods

        /// <summary>
        /// Initializes (resets) the cursor.
        /// </summary>
        /// <param name="parent"> Parent container. </param>
        /// <param name="pointer"> Pointer this cursor represents. </param>
        public void Init(RectTransform parent, Pointer pointer)
        {
            hash = uint.MaxValue;
            group = GetComponent<CanvasGroup>();

            show();
            rect.SetParent(parent);
            rect.SetAsLastSibling();
            state = CursorState.Released;

            UpdatePointer(pointer);
        }

        /// <summary>
        /// Updates the pointer. This method is called when the pointer is moved.
        /// </summary>
        /// <param name="pointer"> Pointer this cursor represents. </param>
        public void UpdatePointer(Pointer pointer)
        {
            rect.anchoredPosition = pointer.Position;
            var newHash = getPointerHash(pointer);
            if (newHash != hash) updateOnce(pointer);
            hash = newHash;

            update(pointer);
        }

        /// <summary>
        /// Sets the state of the cursor.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <param name="newState">The new state.</param>
        /// <param name="data">State data.</param>
        public void SetState(Pointer pointer, CursorState newState, object data = null)
        {
            state = newState;
            stateData = data;

            var newHash = getPointerHash(pointer);
            if (newHash != hash) updateOnce(pointer);
            hash = newHash;
        }

        /// <summary>
        /// Hides this instance.
        /// </summary>
        public void Hide()
        {
            hide();
        }

        #endregion

        #region Unity methods

        private void Awake()
        {
            rect = transform as RectTransform;
            if (rect == null)
            {
                Debug.LogError("PointerCursor must be on an UI element!");
                enabled = false;
                return;
            }
            rect.anchorMin = rect.anchorMax = Vector2.zero;
            defaultSize = rect.sizeDelta.x;
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// Hides (clears) this instance.
        /// </summary>
        protected virtual void hide()
        {
            group.alpha = 0;
#if UNITY_EDITOR
            gameObject.name = "Inactive Pointer";
#endif
        }

        /// <summary>
        /// Shows this instance.
        /// </summary>
        protected virtual void show()
        {
            group.alpha = 1;
#if UNITY_EDITOR
            gameObject.name = "Pointer";
#endif
        }

        /// <summary>
        /// This method is called once when the cursor is initialized.
        /// </summary>
        /// <param name="pointer"> The pointer. </param>
        protected virtual void updateOnce(Pointer pointer) {}

        /// <summary>
        /// This method is called every time when the pointer changes.
        /// </summary>
        /// <param name="pointer"> The pointer. </param>
        protected virtual void update(Pointer pointer) {}

        /// <summary>
        /// Returns pointer hash.
        /// </summary>
        /// <param name="pointer">The pointer.</param>
        /// <returns>Integer hash value.</returns>
        protected virtual uint getPointerHash(Pointer pointer)
        {
            return (uint) state;
        }

        #endregion
    }
}