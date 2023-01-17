/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Hit;
using UnityEngine;
using System.Collections;
using TouchScript.Core;
using TouchScript.Pointers;

namespace TouchScript.Layers
{
    /// <summary>
    /// Base class for all pointer layers. Used to check if some object is hit by a pointer.
    /// <seealso cref="ITouchManager"/>
    /// <seealso cref="HitData"/>
    /// <seealso cref="Pointer"/>
    /// </summary>
    /// <remarks>
    /// <para>In <b>TouchScript</b> it's a layer's job to determine if a pointer on the screen hits anything in Unity's 3d/2d world.</para>
    /// <para><see cref="ILayerManager"/> keeps a sorted list of all layers in <see cref="ILayerManager.Layers"/> which it queries when a new pointer appears. It's a layer's job to return <see cref="HitResult.Hit"/> if this pointer hits an object. Layers can even be used to "hit" objects outside of Unity's 3d world, for example <b>Scaleform</b> integration is implemented this way.</para>
    /// <para>Layers can be configured in a scene using <see cref="TouchManager"/> or from code using <see cref="ITouchManager"/> API.</para>
    /// <para>If you want to route pointers and manually control which objects they should "pointer" it's better to create a new layer extending <see cref="TouchLayer"/>.</para>
    /// </remarks>
    public abstract class TouchLayer : MonoBehaviour
    {
        #region Public properties

        /// <summary>
        /// Layers screen to world projection normal.
        /// </summary>
        public virtual Vector3 WorldProjectionNormal => transform.forward;

        #endregion

        #region Private variables

        /// <summary>
        /// The layer projection parameters.
        /// </summary>
        protected ProjectionParams layerProjectionParams;

        /// <summary>
        /// Layer manager.
        /// </summary>
        protected LayerManagerInstance layerManager;

        #endregion

        #region Public methods

        /// <summary>
        /// Gets the projection parameters of this layer which might depend on a specific pointer data.
        /// </summary>
        /// <param name="pointer"> Pointer to retrieve projection parameters for. </param>
        /// <returns></returns>
        public virtual ProjectionParams GetProjectionParams(Pointer pointer)
        {
            return layerProjectionParams;
        }

        /// <summary>
        /// Checks if a point in screen coordinates hits something in this layer.
        /// </summary>
        /// <param name="pointer">Pointer.</param>
        /// <param name="hit">Hit result.</param>
        /// <returns><c>true</c>, if an object is hit, <see cref="HitResult.Miss"/>; <c>false</c> otherwise.</returns>
        public virtual HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            hit = default;
            if (enabled == false || gameObject.activeInHierarchy == false) return HitResult.Miss;
            return HitResult.Hit;
        }

        #endregion

        #region Unity methods

        /// <summary>
        /// Unity Awake callback.
        /// </summary>
        protected virtual void Awake()
        {
            if (!Application.isPlaying) return;

            layerManager = LayerManager.Instance;
            layerProjectionParams = createProjectionParams();
            StartCoroutine(lateAwake());
        }

        private IEnumerator lateAwake()
        {
            yield return null;

            // Add ourselves after TouchManager finished adding layers in order
            if (layerManager != null) layerManager.AddLayer(this, -1, false);
        }

        // To be able to turn layers off
        private void Start() {}

        /// <summary>
        /// Unity OnDestroy callback.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (!Application.isPlaying) return;

            StopAllCoroutines();
            if (layerManager != null) layerManager.RemoveLayer(this);
        }

        #endregion

        #region Protected functions

        /// <summary>
        /// Creates projection parameters.
        /// </summary>
        /// <returns> Created <see cref="ProjectionParams"/> instance.</returns>
        protected virtual ProjectionParams createProjectionParams()
        {
            return new ProjectionParams();
        }

        #endregion
    }
}