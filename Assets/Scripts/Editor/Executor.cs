using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System;

using Object = UnityEngine.Object;

public class Executor : EditorWindow {
    MonoScript monoScript = null;
    MonoBehaviour monoBehaviour = null;
    Type type;
    string methodName = "Execute";
    Dictionary<string, object> values = new Dictionary<string, object>();

    [MenuItem("Window/Executor")]
    static void Init() {
        Executor window = (Executor)EditorWindow.GetWindow(typeof(Executor));
        window.Show();
    }

    void ClearState() {
        monoScript = null;
        monoBehaviour = null;
        type = null;
        values.Clear();
    }

    void OnGUI() {
        var scriptInput = EditorGUILayout.ObjectField("Script", monoScript ? (Object)monoScript : monoBehaviour, typeof(Object), true);

        if(scriptInput == null) {
            ClearState();
            EditorGUILayout.HelpBox("Select any script from the project or MonoBehaviour from the scene.", MessageType.Info);
            return;
        } else if(scriptInput is MonoScript && monoScript != scriptInput) {
            ClearState();
            monoScript = (MonoScript)scriptInput;
            type = monoScript.GetClass();
        } else if(scriptInput is MonoBehaviour && monoBehaviour != scriptInput) {
            ClearState();
            monoBehaviour = (MonoBehaviour)scriptInput;
            type = monoBehaviour.GetType();
        }

        methodName = EditorGUILayout.TextField("Method name", methodName);
        GUILayout.Space(10);
        
        if(type != null) {
            var method = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            if(method == null) {
                EditorGUILayout.HelpBox($"Couldn't find '{methodName}' method. Please make sure you have one in the script.", MessageType.Error, true);
                return;
            }

            var methodParams = method.GetParameters();
            foreach(var param in methodParams) {
                if(param.ParameterType.GetTypeInfo().IsArray) {
                    DrawArray(param.ParameterType.GetTypeInfo(), param.Name);
                } else {
                    DrawField(param.ParameterType.GetTypeInfo(), param.Name);
                }
            }

            GUILayout.Space(10);
            if(GUILayout.Button("Execute")) {
                method.Invoke(monoBehaviour ? monoBehaviour : monoScript.GetClass().GetConstructor(Type.EmptyTypes).Invoke(new object[]{}), values.Values.ToArray());
            }

        }
    }

    void DrawArray(TypeInfo type, string name) {
        // TODO: handle arrays, ideally using reordable lists
    }

    void DrawField(TypeInfo type, string name) {
        var label = ObjectNames.NicifyVariableName(name);

        if(type == typeof(bool))
            values[name] = EditorGUILayout.Toggle(label, (values.ContainsKey(name) ? (bool)values[name] : false));
        else if(type == typeof(string))
            values[name] = EditorGUILayout.TextField(label, (values.ContainsKey(name) ? (string)values[name] : ""));
        else if(type == typeof(int))
            values[name] = EditorGUILayout.IntField(label, (values.ContainsKey(name) ? (int)values[name] : 0));
        else if(type == typeof(float))
            values[name] = EditorGUILayout.FloatField(label, (values.ContainsKey(name) ? (float)values[name] : 0));
        else if(type == typeof(Vector3))
            values[name] = EditorGUILayout.Vector3Field(label, (values.ContainsKey(name) ? (Vector3)values[name] : Vector3.zero));
        else if(type == typeof(Quaternion))
            values[name] = Quaternion.Euler(EditorGUILayout.Vector3Field(label, (values.ContainsKey(name) ? ((Quaternion)values[name]).eulerAngles : Vector3.zero)));
        else if(type.IsSubclassOf(typeof(Object)))
            values[name] = EditorGUILayout.ObjectField(label, (values.ContainsKey(name) ? (Object)values[name] : null), type, true);
        else 
            Debug.LogError("Unhendled field type of type: " + type);
    }

}
