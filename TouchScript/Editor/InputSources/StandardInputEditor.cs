/*
 * @author Valentin Simonov / http://va.lent.in/
 */

using TouchScript.InputSources;
using UnityEditor;
using UnityEngine;
using TouchScript.Editor.EditorUI;

namespace TouchScript.Editor.InputSources
{
    [CustomEditor(typeof(StandardInput), true)]
    internal sealed class StandardInputEditor : InputSourceEditor
    {
        public static readonly GUIContent TEXT_GENERAL_HEADER = new GUIContent("General", "General settings.");

        public static readonly GUIContent TEXT_EMULATE_MOUSE = new GUIContent("Emulate Second Mouse Pointer", "If selected, you can press ALT to make a stationary mouse pointer. This is used to simulate multi-touch.");

        public static readonly GUIContent TEXT_HELP = new GUIContent("This component gathers input data from various devices like touch, mouse and pen on all platforms.");

        private SerializedProperty basicEditor;

        private SerializedProperty emulateSecondMousePointer;

        private SerializedProperty generalProps;

        private StandardInput instance;

        protected override void OnEnable()
        {
            base.OnEnable();

            instance = target as StandardInput;
            basicEditor = serializedObject.FindProperty("basicEditor");
            emulateSecondMousePointer = serializedObject.FindProperty("emulateSecondMousePointer");

            generalProps = serializedObject.FindProperty("generalProps");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.UpdateIfRequiredOrScript();

            GUILayout.Space(5);

            if (basicEditor.boolValue)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(emulateSecondMousePointer, TEXT_EMULATE_MOUSE);
                if (EditorGUI.EndChangeCheck())
                {
                    instance.EmulateSecondMousePointer = emulateSecondMousePointer.boolValue;
                }

                if (GUIElements.BasicHelpBox(TEXT_HELP))
                {
                    basicEditor.boolValue = false;
                    Repaint();
                }
            }
            else
            {
                drawGeneral();
            }

            serializedObject.ApplyModifiedProperties();
            base.OnInspectorGUI();
        }

        private void drawGeneral()
        {
            var display = GUIElements.Header(TEXT_GENERAL_HEADER, generalProps);
            if (display)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(emulateSecondMousePointer, TEXT_EMULATE_MOUSE);
                if (EditorGUI.EndChangeCheck())
                {
                    instance.EmulateSecondMousePointer = emulateSecondMousePointer.boolValue;
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}