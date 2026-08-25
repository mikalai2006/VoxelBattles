// using System.Collections.Generic;
// using UnityEditor;
// using UnityEngine;

// [CustomEditor(typeof(ObjectPool))]
// public class ObjectPoolEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         ObjectPool target = (ObjectPool)base.target;

//         // Draw default Inspector properties for properties not handled below
//         DrawDefaultInspector();

//         // Add custom controls
//         EditorGUILayout.Space();

//         EditorGUILayout.LabelField("Pool info (Only Editor)");
//         GUILayout.Label($"Count despawn: {target.Pool.Count}");

//         List<GameObject> childs = new List<GameObject>();
//         foreach (Transform child in target.transform)
//         {
//             childs.Add(child.gameObject);
//         }

//         GUILayout.Label($"Count spawn: {childs.Count}");
        
//         // foreach (Object obj in Selection.objects)
//         // {
//         //     string path = AssetDatabase.GetAssetPath(obj);
//         //     string fileExtension = Path.GetExtension(path);

//         //     if (fileExtension == ".vox")
//         //     {
//         //         paths.Add(path);
//         //     }
//         // }
//         // target.OnSetFiles(paths.ToArray());

//         // if (paths.Count > 0)
//         // {
//         //     foreach (Object obj in Selection.objects)
//         //     {
//         //         string path = AssetDatabase.GetAssetPath(obj);

//         //         string fileExtension = Path.GetExtension(path);
//         //         if (fileExtension == ".vox")
//         //         {
//         //             GUIStyle myLabelStyle = new GUIStyle();
//         //             myLabelStyle.normal.textColor = Color.green;
//         //             GUILayout.Label($"Name: {obj.name}, Path: {path}", myLabelStyle);
//         //         }
//         //         else
//         //         {
//         //             GUIStyle myLabelStyle = new GUIStyle();
//         //             myLabelStyle.normal.textColor = Color.yellow;
//         //             GUILayout.Label($"ONLY VOX ({fileExtension})", myLabelStyle);
//         //         }
//         //     }
//         //     if (GUILayout.Button("Create data"))
//         //     {
//         //         target.OnCreateData();
//         //     }
//         // }
//         // else
//         // {
//         //     GUIStyle myLabelStyle = new GUIStyle();
//         //     myLabelStyle.normal.textColor = Color.yellow;
//         //     GUILayout.Label("Выберите во вкладке проекта файлы VOX");
//         // }

//         // // Apply changes if any
//         // if (GUI.changed)
//         // {
//         //     EditorUtility.SetDirty(target);
//         // }

//     }
// }
