/*
 * @author Valentin Simonov / http://va.lent.in/
 */

#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TouchScript.Behaviors.Cursors.UI
{
    /// <summary>
    /// Generates a texture with a circle gradient.
    /// </summary>
    [GraphicPropertyHide(GraphicPropertyFlag.Color | GraphicPropertyFlag.Raycast)]
    public class GradientTexture : Graphic
    {
        [FormerlySerializedAs("Gradient")]
        [SerializeField]
        private Gradient _gradient;
        [NonSerialized]
        private Texture2D _texture;

        public override Texture mainTexture
        {
            get
            {
                if (_texture is null) Refresh();
                return _texture;
            }
        }

        private void Start() => Refresh();

        private void OnDestroy()
        {
            if (_texture is not null)
            {
                DestroyImmediate(_texture);
                _texture = null;
            }
        }

        private void Refresh()
        {
            _texture ??= CreateTexture();
            ApplyGradient(_texture, _gradient);
        }

        protected override void OnPopulateMesh(MeshBuilder mb) =>
            mb.SetUp_Quad_FullUV(rectTransform.rect, color);

        private static Texture2D CreateTexture()
        {
            return new Texture2D(128, 1, TextureFormat.ARGB32, false)
            {
                name = "",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave // XXX: To prevent it from being destroyed when the scene is reloaded.
            };
        }

        private static void ApplyGradient(Texture2D tex, Gradient gradient)
        {
            var res = tex.width;
            var colors = new Color[res];
            for (var i = 0; i < res; i++)
            {
                var t = i / (float) res;
                colors[i] = gradient.Evaluate(t);
            }
            tex.SetPixels(colors);
            tex.Apply(false, true);
        }
    }
}
#endif