using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VoxParser))]
public class VoxParserEditor : Editor
{
    public override void OnInspectorGUI()
        {
        VoxParser target = (VoxParser)base.target;
        
        target.meshFilter = target.GetComponent<MeshFilter>();
        target.meshRenderer = target.GetComponent<MeshRenderer>();

        // Draw default Inspector properties for properties not handled below
        DrawDefaultInspector();

        // Add custom controls
        EditorGUILayout.Space(); // Add some spacing
        // myPlayer.damage = EditorGUILayout.IntSlider("Damage", myPlayer.damage, 0, 100);
        // myPlayer.armor = EditorGUILayout.IntField("Armor Value", myPlayer.armor);

        // // Conditional display
        // myPlayer.showAdvancedSettings = EditorGUILayout.Toggle("Show Advanced Settings", myPlayer.showAdvancedSettings);
        // if (myPlayer.showAdvancedSettings)
        // {
        //     EditorGUILayout.ObjectField("Gun Object", myPlayer.gun, typeof(GameObject), true);
        // }

            List<string> paths = new List<string>();
            
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                string fileExtension = Path.GetExtension(path);
                
                if (fileExtension == ".vox")
                {
                    paths.Add(path);
                }
            }
            target.OnSetFiles(paths.ToArray());

        if (paths.Count > 0)
        {
            foreach (Object obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);

                string fileExtension = Path.GetExtension(path);
                if (fileExtension == ".vox")
                {
                    GUIStyle myLabelStyle = new GUIStyle();
                    myLabelStyle.normal.textColor = Color.green;
                    GUILayout.Label($"Name: {obj.name}, Path: {path}", myLabelStyle);
                }
                else
                {
                    GUIStyle myLabelStyle = new GUIStyle();
                    myLabelStyle.normal.textColor = Color.yellow;
                    GUILayout.Label($"ONLY VOX ({fileExtension})", myLabelStyle);
                }
            }
            if (GUILayout.Button("Create data"))
            {
                target.OnCreateData();
            }
            }
        else
        {
            GUIStyle myLabelStyle = new GUIStyle();
            myLabelStyle.normal.textColor = Color.yellow;
            myLabelStyle.wordWrap = true;
            GUILayout.Label("Зафиксируйте в инспекторе этот компонент и выберите во вкладке проекта файлы VOX", myLabelStyle);
        }

        // Apply changes if any
        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }
    }
}
