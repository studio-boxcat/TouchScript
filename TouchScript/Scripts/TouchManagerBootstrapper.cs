using Sirenix.OdinInspector;
using TouchScript.Core;
using UnityEngine;

namespace TouchScript
{
    public class TouchManagerBootstrapper : MonoBehaviour
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        TouchManager _touchManager;
        [SerializeField, Required, ChildGameObjectsOnly]
        GestureManager _gestureManager;

        void Awake()
        {
            TouchManager.Instance = _touchManager;
            GestureManager.Instance = _gestureManager;
        }

        void OnDestroy()
        {
            TouchManager.Instance = null;
            GestureManager.Instance = null;
        }
    }
}