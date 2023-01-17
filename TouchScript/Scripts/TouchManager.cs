/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using TouchScript.Core;
using TouchScript.Devices.Display;
using TouchScript.Layers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TouchScript
{
    /// <summary>
    /// A facade object to configure and hold parameters for an instance of <see cref="ITouchManager"/>. Contains constants used throughout the library.
    /// <seealso cref="ITouchManager"/>
    /// </summary>
    /// <remarks>
    /// <para>An instance of <see cref="TouchManager"/> may be added to a Unity scene to hold (i.e. serialize them to the scene) parameters needed to configure an instance of <see cref="ITouchManager"/> used in application. Which can be accessed via <see cref="TouchManager.Instance"/> static property.</para>
    /// <para>Though it's not required it is a convenient way to configure <b>TouchScript</b> for your scene. You can use different configuration options for different scenes.</para>
    /// </remarks>
    [AddComponentMenu("TouchScript/Touch Manager")]
    [HelpURL("http://touchscript.github.io/docs/html/T_TouchScript_TouchManager.htm")]
    public sealed class TouchManager : MonoBehaviour
    {
        #region Constants

        /// <summary>
        /// Centimeter to inch ratio to be used in DPI calculations.
        /// </summary>
        public const float CM_TO_INCH = 0.393700787f;

        /// <summary>
        /// The value used to represent an unknown state of a screen position. Use <see cref="TouchManager.IsInvalidPosition"/> to check if a point has unknown value.
        /// </summary>
        public static readonly Vector2 INVALID_POSITION = new Vector2(float.NaN, float.NaN);

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the instance of <see cref="ITouchManager"/> implementation used in the application.
        /// </summary>
        /// <value>An instance of <see cref="ITouchManager"/> which is in charge of global pointer input control in the application.</value>
        public static TouchManagerInstance Instance => TouchManagerInstance.Instance;

        #endregion

        #region Public methods

        /// <summary>
        /// Determines whether a Vector2 represents an invalid position, i.e. if it is equal to <see cref="INVALID_POSITION"/>.
        /// </summary>
        /// <param name="position">Screen position.</param>
        /// <returns><c>true</c> if position is invalid; otherwise, <c>false</c>.</returns>
        public static bool IsInvalidPosition(Vector2 position)
        {
            return float.IsNaN(position.x) && float.IsNaN(position.y);
        }

        #endregion

        #region Private variables

		[SerializeField]
        private Object displayDevice;

        [SerializeField]
        private List<TouchLayer> layers = new();

        #endregion

        #region Unity

        private void Awake()
        {
            var touchManager = Instance;
            if (touchManager == null) return;

            touchManager.DisplayDevice = displayDevice as IDisplayDevice;

            var layerManager = LayerManager.Instance;
            if (layerManager == null) return;
            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer != null) layerManager.AddLayer(layer, i);
            }
        }

        #endregion
    }
}
