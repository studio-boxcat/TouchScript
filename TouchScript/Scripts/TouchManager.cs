/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System;
using System.Collections.Generic;
using TouchScript.Core;
using TouchScript.Devices.Display;
using TouchScript.Layers;
using TouchScript.Pointers;
using TouchScript.Utils.Attributes;
using UnityEngine;
using UnityEngine.Events;
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
    public sealed class TouchManager : DebuggableMonoBehaviour
    {
        #region Constants

#if TOUCHSCRIPT_DEBUG
        public const int DEBUG_GL_START = int.MinValue;
        public const int DEBUG_GL_TOUCH = DEBUG_GL_START;
#endif

        /// <summary>
        /// Event implementation in Unity EventSystem for pointer events.
        /// </summary>
        [Serializable]
        public class PointerEvent : UnityEvent<IList<Pointer>> {}

        /// <summary>
        /// Event implementation in Unity EventSystem for frame events.
        /// </summary>
        /// <seealso cref="UnityEngine.Events.UnityEvent" />
        [Serializable]
        public class FrameEvent : UnityEvent {}

        /// <summary>
        /// Centimeter to inch ratio to be used in DPI calculations.
        /// </summary>
        public const float CM_TO_INCH = 0.393700787f;

        /// <summary>
        /// Inch to centimeter ratio to be used in DPI calculations.
        /// </summary>
        public const float INCH_TO_CM = 1 / CM_TO_INCH;

        /// <summary>
        /// The value used to represent an unknown state of a screen position. Use <see cref="TouchManager.IsInvalidPosition"/> to check if a point has unknown value.
        /// </summary>
        public static readonly Vector2 INVALID_POSITION = new Vector2(float.NaN, float.NaN);

        /// <summary>
        /// TouchScript version.
        /// </summary>
        public static readonly Version VERSION = new Version(9, 0);

        /// <summary>
        /// TouchScript version suffix.
        /// </summary>
        public static readonly string VERSION_SUFFIX = "";

        #endregion

        #region Public properties

        /// <summary>
        /// Gets the instance of <see cref="ITouchManager"/> implementation used in the application.
        /// </summary>
        /// <value>An instance of <see cref="ITouchManager"/> which is in charge of global pointer input control in the application.</value>
        public static ITouchManager Instance
        {
            get { return TouchManagerInstance.Instance; }
        }

        /// <summary>
        /// Gets or sets current display device.
        /// </summary>
        /// <value>Object which holds properties of current display device, like DPI and others.</value>
        /// <remarks>A shortcut for <see cref="ITouchManager.DisplayDevice"/> which is also serialized into scene.</remarks>
        public IDisplayDevice DisplayDevice
        {
            get
            {
                if (Instance == null) return displayDevice as IDisplayDevice;
                return Instance.DisplayDevice;
            }
            set
            {
                if (Instance == null)
                {
                    displayDevice = value as Object;
                    return;
                }
                Instance.DisplayDevice = value;
            }
        }

        /// <summary>
        /// Indicates if TouchScript should create a CameraLayer for you if no layers present in a scene.
        /// </summary>
        /// <value><c>true</c> if a CameraLayer should be created on startup; otherwise, <c>false</c>.</value>
        /// <remarks>This is usually a desired behavior but sometimes you would want to turn this off if you are using TouchScript only to get pointer input from some device.</remarks>
        public bool ShouldCreateCameraLayer
        {
            get { return shouldCreateCameraLayer; }
            set { shouldCreateCameraLayer = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether a <see cref="TouchScript.InputSources.StandardInput"/> should be created in scene if no inputs present.
        /// </summary>
        /// <value> <c>true</c> if StandardInput should be created; otherwise, <c>false</c>. </value>
        /// <remarks>This is usually a desired behavior but sometimes you would want to turn this off.</remarks>
        public bool ShouldCreateStandardInput
        {
            get { return shouldCreateStandardInput; }
            set { shouldCreateStandardInput = value; }
        }

#if TOUCHSCRIPT_DEBUG

        /// <inheritdoc />
        public override bool DebugMode
        {
            get { return base.DebugMode; }
            set
            {
                base.DebugMode = value;
                if (Application.isPlaying) (Instance as TouchManagerInstance).DebugMode = value;
            }
        }

#endif

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

#pragma warning disable 0414

        [SerializeField]
        [HideInInspector]
        private bool basicEditor = true;

#pragma warning restore 0414

		[SerializeField]
        private Object displayDevice;

        [SerializeField]
        [ToggleLeft]
        private bool shouldCreateCameraLayer = true;

        [SerializeField]
        [ToggleLeft]
        private bool shouldCreateStandardInput = true;

        [SerializeField]
        private List<TouchLayer> layers = new List<TouchLayer>();

        #endregion

        #region Unity

        private void Awake()
        {
            var touchManager = Instance;
            if (touchManager == null) return;

#if TOUCHSCRIPT_DEBUG
            if (DebugMode) (touchManager as TouchManagerInstance).DebugMode = true;
#endif

            touchManager.DisplayDevice = displayDevice as IDisplayDevice;
            touchManager.ShouldCreateCameraLayer = ShouldCreateCameraLayer;
            touchManager.ShouldCreateStandardInput = ShouldCreateStandardInput;

            var layerManager = LayerManager.Instance;
            if (layerManager == null) return;
            for (var i = 0; i < layers.Count; i++)
            {
                var layer = layers[i];
                if (layer != null) layerManager.AddLayer(layer, i);
            }
        }

		[ContextMenu("Basic Editor")]
		private void switchToBasicEditor()
		{
            basicEditor = true;
		}

        #endregion
    }
}
