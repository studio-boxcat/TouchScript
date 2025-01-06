using Sirenix.OdinInspector;
using TouchScript.Core;
using UnityEngine;
using UnityEngine.Assertions;

namespace TouchScript
{
    public class TouchManagerBootstrapper : MonoBehaviour
    {
        [SerializeField, Required, ChildGameObjectsOnly]
        private TouchManager _touchManager;
        [SerializeField, Required, ChildGameObjectsOnly]
        private GestureManager _gestureManager;

        private void Awake()
        {
            Assert.IsNull(TouchManager.Instance);
            Assert.IsNull(GestureManager.Instance);

            Input.simulateMouseWithTouches = false;
            TouchManager.Instance = _touchManager;
            GestureManager.Instance = _gestureManager;
        }

        private void OnDestroy()
        {
            Assert.IsTrue(ReferenceEquals(_touchManager, TouchManager.Instance));
            Assert.IsTrue(ReferenceEquals(_gestureManager, GestureManager.Instance));

            TouchManager.Instance = null;
            GestureManager.Instance = null;
        }
    }
}