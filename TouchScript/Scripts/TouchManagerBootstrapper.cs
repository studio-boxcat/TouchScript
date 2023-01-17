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
        TouchManager _touchManager;
        [SerializeField, Required, ChildGameObjectsOnly]
        GestureManager _gestureManager;

        void Awake()
        {
            _touchScriptInputModule.Connect(_touchManager);
            TouchManager.Instance = _touchManager;
            GestureManager.Instance = _gestureManager;
        }

        void OnDestroy()
        {
            _touchScriptInputModule.Disconnect(_touchManager);
            TouchManager.Instance = null;
            GestureManager.Instance = null;
        }
    }
}