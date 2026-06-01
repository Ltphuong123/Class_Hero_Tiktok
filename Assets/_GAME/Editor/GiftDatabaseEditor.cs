#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(GiftDatabase))]
public class GiftDatabaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GiftDatabase db = (GiftDatabase)target;

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Import", EditorStyles.boldLabel);

        XlsxParser.DebugMode = EditorGUILayout.Toggle("Debug Mode (xem log chi tiết)", XlsxParser.DebugMode);

        if (GUILayout.Button("🔍  Debug Parse (xem log)", GUILayout.Height(26)))
        {
            string debugPath = EditorUtility.OpenFilePanel("Chọn file xlsx để debug", "", "xlsx");
            if (!string.IsNullOrEmpty(debugPath))
            {
                XlsxParser.DebugMode = true;
                var list = XlsxParser.Parse(debugPath);
                Debug.Log($"[GiftDB Debug] Parsed {list.Count} rows. First 3:");
                for (int i = 0; i < Mathf.Min(3, list.Count); i++)
                {
                    var g = list[i];
                    Debug.Log($"  [{g.id}] name='{g.name}' diamond={g.diamond} imageUrl='{g.imageUrl}'");
                }
                XlsxParser.DebugMode = false;
            }
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("📥  Import from XLSX", GUILayout.Height(30)))
        {
            string path = EditorUtility.OpenFilePanel(
                "Chọn file gifts_with_images.xlsx",
                Path.Combine(Application.dataPath, "_GAME/Scripts/TiktokEvent"),
                "xlsx");

            if (!string.IsNullOrEmpty(path))
                db.LoadFromXlsx(path);
        }

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField($"Tổng số quà: {db.gifts?.Count ?? 0}", EditorStyles.helpBox);

        if (db.gifts != null && db.gifts.Count > 0)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Xem trước (10 quà đầu):", EditorStyles.boldLabel);

            int preview = Mathf.Min(10, db.gifts.Count);
            for (int i = 0; i < preview; i++)
            {
                var g = db.gifts[i];
                EditorGUILayout.LabelField($"  [{g.id}]  {g.name}  —  {g.diamond} 💎",
                    EditorStyles.miniLabel);
            }

            if (db.gifts.Count > 10)
                EditorGUILayout.LabelField($"  ... và {db.gifts.Count - 10} quà khác",
                    EditorStyles.miniLabel);
        }
    }
}
#endif
