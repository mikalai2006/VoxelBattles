using UnityEditor;
using UnityEngine;

namespace Mikalai2006.VoxelMap
{
    [CustomEditor(typeof(WFCCreatorTiles))]
    public class WFCCreatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            WFCCreatorTiles target = (WFCCreatorTiles)base.target;

            DrawDefaultInspector();

            if (GUILayout.Button("AnalyseSockets"))
            {
                target.AnalyseSockets();
            }

        }
    }
}