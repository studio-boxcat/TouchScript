/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using System.Collections.Generic;
using System.Linq;
using TouchScript.Devices.Display;
using TouchScript.Editor.EditorUI;
using TouchScript.Layers;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Reflection;

namespace TouchScript.Editor
{
    [CustomEditor(typeof(TouchManager))]
    internal sealed class TouchManagerEditor : UnityEditor.Editor
    {
        public static readonly GUIContent TEXT_LAYERS_HELP = new GUIContent("Layers at the top get to process pointer input first.");
        public static readonly GUIContent TEXT_LAYERS_HEADER = new GUIContent("Pointer Layers", "Sorted array of Pointer Layers in the scene.");
        public static readonly GUIContent TEXT_DEFAULTS_HEADER = new GUIContent("Defaults", "Default actions when some of TouchScript components are not present in the scene.");

        public static readonly GUIContent TEXT_DEBUG_MODE = new GUIContent("Debug", "Turns on debug mode.");
        public static readonly GUIContent TEXT_DISPLAY_DEVICE = new GUIContent("Display Device", "Display device properties where such parameters as target DPI are stored.");
        public static readonly GUIContent TEXT_CREATE_CAMERA_LAYER = new GUIContent("Create Camera Layer", "Indicates if TouchScript should create a CameraLayer for you if no layers present in a scene. This is usually a desired behavior but sometimes you would want to turn this off if you are using TouchScript only to get input from some device.");
        public static readonly GUIContent TEXT_CREATE_STANDARD_INPUT = new GUIContent("Create Standard Input", "");

        public static readonly GUIContent TEXT_HELP = new GUIContent("This component holds TouchScript configuration options for a scene.");

        private TouchManager instance;
        private ReorderableList layersList;
        private SerializedProperty basicEditor;
        private SerializedProperty debugMode;

        private SerializedProperty layers, displayDevice, shouldCreateCameraLayer, shouldCreateStandardInput;

        private void OnEnable()
        {
            instance = target as TouchManager;

            basicEditor = serializedObject.FindProperty("basicEditor");
            debugMode = serializedObject.FindProperty("debugMode");
            layers = serializedObject.FindProperty("layers");
            displayDevice = serializedObject.FindProperty("displayDevice");
            shouldCreateCameraLayer = serializedObject.FindProperty("shouldCreateCameraLayer");
            shouldCreateStandardInput = serializedObject.FindProperty("shouldCreateStandardInput");

            refresh();

            layersList = new ReorderableList(serializedObject, layers, true, false, false, false);
            layersList.headerHeight = 0;
            layersList.footerHeight = 0;
            layersList.drawElementCallback += (rect, index, active, focused) =>
            {
                rect.height = 16;
                rect.y += 2;
                if (index >= layers.arraySize) return;
                var layer = layers.GetArrayElementAtIndex(index).objectReferenceValue as TouchLayer;
                if (layer == null)
                {
                    EditorGUI.LabelField(rect, "null");
                    return;
                }
                EditorGUI.LabelField(rect, layer.Name + " (" + layer.gameObject.name + ")");
            };
        }

        public override void OnInspectorGUI()
        {
#if UNITY_5_6_OR_NEWER
            serializedObject.UpdateIfRequiredOrScript();
#else
            serializedObject.UpdateIfDirtyOrScript();
#endif

            GUILayout.Space(5);

            if (basicEditor.boolValue)
            {
                drawLayers();

                if (GUIElements.BasicHelpBox(TEXT_HELP))
                {
                    basicEditor.boolValue = false;
                    Repaint();
                }
            }
            else
            {
                drawDefaults();
                drawLayers();
                drawDebug();
            }

            GUILayout.Label("v. " + TouchManager.VERSION + (string.IsNullOrEmpty(TouchManager.VERSION_SUFFIX) ? "" : " " + TouchManager.VERSION_SUFFIX), GUIElements.SmallTextRight);

            serializedObject.ApplyModifiedProperties();
        }

        private void drawDefaults()
        {
            var display = GUIElements.Header(TEXT_DEFAULTS_HEADER, shouldCreateCameraLayer);
            if (display)
            {
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
                {
                    EditorGUILayout.PropertyField(shouldCreateCameraLayer, TEXT_CREATE_CAMERA_LAYER);
                    EditorGUILayout.PropertyField(shouldCreateStandardInput, TEXT_CREATE_STANDARD_INPUT);
                }

                var r = EditorGUILayout.GetControlRect(true, 16f, EditorStyles.objectField);
                var label = EditorGUI.BeginProperty(r, TEXT_DISPLAY_DEVICE, displayDevice);
                EditorGUI.BeginChangeCheck();
                r = EditorGUI.PrefixLabel(r, label);
                var newDevice = EditorGUI.ObjectField(r, instance.DisplayDevice as Object, typeof(IDisplayDevice), true) as IDisplayDevice;
                if (EditorGUI.EndChangeCheck())
                {
                    instance.DisplayDevice = newDevice;
                    EditorUtility.SetDirty(instance);
                }
                EditorGUI.EndProperty();

                EditorGUI.indentLevel--;
            }
        }

        private void drawLayers()
        {
            var display = GUIElements.Header(TEXT_LAYERS_HEADER, layers);
            if (display)
            {
                EditorGUILayout.LabelField(TEXT_LAYERS_HELP, GUIElements.HelpBox);
                EditorGUI.indentLevel++;
                using (new EditorGUI.DisabledGroupScope(Application.isPlaying))
                {
                    layersList.DoLayoutList();
                }
                EditorGUI.indentLevel--;
            }
        }

        private void drawDebug()
        {
            if (debugMode == null) return;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(debugMode, TEXT_DEBUG_MODE);
            if (EditorGUI.EndChangeCheck()) instance.DebugMode = debugMode.boolValue;
        }

        private void refresh()
        {
            if (Application.isPlaying)
            {
                layers.arraySize = 0;
                var layerManager = LayerManager.Instance;
                if (layerManager != null) 
                {
                    LayerManager.Instance.ForEach((l) =>
                                {
                                    layers.arraySize++;
                                    layers.GetArrayElementAtIndex(layers.arraySize - 1).objectReferenceValue = l;
                                    return true;
                                });
                }
            }
            else
            {
                var allLayers = FindObjectsOfType(typeof(TouchLayer)).Cast<TouchLayer>().ToList();
                var toRemove = new List<int>();
                for (var i = 0; i < layers.arraySize; i++)
                {
                    var layer = layers.GetArrayElementAtIndex(i).objectReferenceValue as TouchLayer;
                    if (layer == null || allLayers.IndexOf(layer) == -1) toRemove.Add(i);
                    else allLayers.Remove(layer);
                }

                for (var i = toRemove.Count - 1; i >= 0; i--)
                {
                    var index = toRemove[i];
                    layers.GetArrayElementAtIndex(index).objectReferenceValue = null;
                    layers.DeleteArrayElementAtIndex(index);
                }

                for (var i = 0; i < allLayers.Count; i++)
                {
                    layers.arraySize++;
                    layers.GetArrayElementAtIndex(layers.arraySize - 1).objectReferenceValue = allLayers[i];
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}