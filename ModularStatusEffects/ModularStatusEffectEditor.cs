using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ModularStatusEffects;

[CustomEditor(typeof(ModularStatusEffect))]
public class ModularStatusEffectEditor : Editor {

    SerializedProperty displayIconProp;
    SerializedProperty displayNameProp;
    SerializedProperty baseDurationProp;
    SerializedProperty reApplyTypeProp;
    SerializedProperty visualEffectTypeProp;
    SerializedProperty visualEffectProp;
    SerializedProperty attachVFXProp;
    SerializedProperty soundEffectTypeProp;
    SerializedProperty soundEffectProp;
    SerializedProperty modulesProp;

    void OnEnable() {

        displayIconProp = serializedObject.FindProperty("displayIcon");
        displayNameProp = serializedObject.FindProperty("displayName");
        baseDurationProp = serializedObject.FindProperty("baseDuration");
        reApplyTypeProp = serializedObject.FindProperty("reApplyType");
        visualEffectTypeProp = serializedObject.FindProperty("visualEffectType");
        visualEffectProp = serializedObject.FindProperty("visualEffect");
        attachVFXProp = serializedObject.FindProperty("attachVFX");
        soundEffectTypeProp = serializedObject.FindProperty("soundEffectType");
        soundEffectProp = serializedObject.FindProperty("soundEffect");
        modulesProp = serializedObject.FindProperty("modules");

    }

    public override void OnInspectorGUI() {

        serializedObject.Update();

        EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(displayIconProp);
        EditorGUILayout.PropertyField(displayNameProp);
        EditorGUILayout.PropertyField(baseDurationProp);
        EditorGUILayout.PropertyField(reApplyTypeProp);

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Visual", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(visualEffectTypeProp);
        if (visualEffectTypeProp.intValue != 0) {
            EditorGUILayout.PropertyField(visualEffectProp);
            EditorGUILayout.PropertyField(attachVFXProp);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Audio", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(soundEffectTypeProp);
        if (soundEffectTypeProp.intValue != 0) {
            EditorGUILayout.PropertyField(soundEffectProp);
        }

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Components", EditorStyles.boldLabel);

        for (int i = 0; i < modulesProp.arraySize; i++) {
            SerializedProperty moduleProp = modulesProp.GetArrayElementAtIndex(i);
            SerializedProperty componentProp = moduleProp.FindPropertyRelative("component");
            string componentName = componentProp.objectReferenceValue != null ? componentProp.objectReferenceValue.name : "None";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(componentProp, GUIContent.none); // Display component as property field

            if (GUILayout.Button("Remove Module", GUILayout.Width(120))) {
                modulesProp.DeleteArrayElementAtIndex(i);
                break;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(moduleProp.FindPropertyRelative("isPercentage"));
            EditorGUILayout.PropertyField(moduleProp.FindPropertyRelative("value"));
            EditorGUILayout.PropertyField(moduleProp.FindPropertyRelative("interval"));
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
        }

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Add Module", GUILayout.Width(120))) {
            modulesProp.arraySize++;
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();


        serializedObject.ApplyModifiedProperties();
    }
}

[CustomPropertyDrawer(typeof(StatusCCType))] 
public class StatusCCTypeDrawer : PropertyDrawer {

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

        EditorGUI.BeginChangeCheck();

        int newValue = EditorGUI.MaskField(position, label, property.intValue, System.Enum.GetNames(typeof(StatusCCType)));

        if (EditorGUI.EndChangeCheck())
            property.intValue = newValue;

    }

}