/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using Sirenix.OdinInspector;
using TouchScript.Core;
using TouchScript.Hit;
using TouchScript.Pointers;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.EventSystems;

namespace TouchScript.Layers.UI
{
    /// <summary>
    /// An implementation of a Unity UI Input Module which lets TouchScript interact with the UI and EventSystem.
    /// </summary>
    internal sealed partial class TouchScriptInputModule : BaseInputModule
    {
        #region Private variables

        [SerializeField, Required, ChildGameObjectsOnly]
        TouchManager _touchManager;
        private UIStandardInputModule ui;

        #endregion

        #region Public methods

        protected override void Awake()
        {
            base.Awake();

            ui = new UIStandardInputModule(this);
            _touchManager.PointerAdded += ui.ProcessAdded;
            _touchManager.PointerUpdated += ui.ProcessUpdated;
            _touchManager.PointerPressed += ui.ProcessPressed;
            _touchManager.PointerReleased += ui.ProcessReleased;
            _touchManager.PointerRemoved += ui.ProcessRemoved;
            _touchManager.PointerCancelled += ui.ProcessCancelled;
        }

        public override void Process()
        {
            ui.Process();
        }

        public override bool IsPointerOverGameObject(int pointerId)
        {
            return ui.IsPointerOverGameObject(pointerId);
        }

        public override bool ShouldActivateModule() => true;

        public override bool IsModuleSupported() => true;

        public override void DeactivateModule()
        {
        }

        public override void ActivateModule()
        {
        }

        public override void UpdateModule()
        {
        }

        #endregion

        #region Copy-pasted code from UI

        #endregion
    }
}