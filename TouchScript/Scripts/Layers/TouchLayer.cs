/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.Hit;
using UnityEngine;
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
        [SerializeField]
        int _priority;

        public int Priority => _priority;

        #region Public methods

        /// <summary>
        /// Gets the projection parameters of this layer which might depend on a specific pointer data.
        /// </summary>
        /// <returns></returns>
        public abstract ProjectionParams GetProjectionParams();

        public virtual HitResult Hit(Vector2 screenPosition, out HitData hit)
        {
            hit = default;
            if (enabled == false || gameObject.activeInHierarchy == false) return HitResult.Miss;
            return HitResult.Hit;
        }

        #endregion

        #region Unity methods

        void OnEnable()
        {
            LayerManager.AddLayer(this);
        }

        void OnDisable()
        {
            LayerManager.RemoveLayer(this);
        }

        #endregion
    }
}