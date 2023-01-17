using Sirenix.OdinInspector;
using TouchScript.Core;
using TouchScript.Layers.UI;
using UnityEngine;

namespace TouchScript
{
    public class TouchManagerBootstrapper : MonoBehaviour
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        TouchScriptInputModule _touchScriptInputModule;
        [SerializeField, Required, ChildGameObjectsOnly]
        TouchManagerInstance _touchManager;
        [SerializeField, Required, ChildGameObjectsOnly]
        GestureManager _gestureManager;

        void Awake()
        {
            TouchScriptInputModule.Instance = _touchScriptInputModule;
            TouchManagerInstance.Instance = _touchManager;
            GestureManager.Instance = _gestureManager;
        }

        void OnDestroy()
        {
            TouchScriptInputModule.Instance = null;
            TouchManagerInstance.Instance = null;
            GestureManager.Instance = null;
        }
    }
}