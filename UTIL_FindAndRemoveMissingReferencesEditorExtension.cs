using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class UTIL_FindAndRemoveMissingReferencesEditorExtension : EditorWindow
{
    [MenuItem("Tools/Project Management/FindAndRemoveMissingReferences")]
    static public void FindMissing()
    {
        GetWindow<UTIL_FindAndRemoveMissingReferencesEditorExtension>();
    }

    protected List<GameObject> objectsWithMissingReferences;
    protected Vector2 currentScrollPos;
    private int fixedCounter = 0;

    private void OnEnable()
    {
        objectsWithMissingReferences = new List<GameObject>();
        currentScrollPos = Vector2.zero;
        fixedCounter = 0;
    }
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Crawl from ALL Assets"))
            CrawlFromALLAssets();
        if (GUILayout.Button("Crawl from Scene"))
            CrawlFromScene();
        if (GUILayout.Button("Try to parse(fix) the ones we found"))
            TryToRemoveMissingReferences();
        EditorGUILayout.EndHorizontal();

        currentScrollPos = EditorGUILayout.BeginScrollView(currentScrollPos);
        for (int i = 0; i < objectsWithMissingReferences.Count; ++i)
        {
            if (GUILayout.Button(objectsWithMissingReferences[i].name))
            {
                EditorGUIUtility.PingObject(objectsWithMissingReferences[i]);
            }
        }
        EditorGUILayout.EndScrollView();
    }

    void CrawlFromALLAssets()
    {
        var assetGUIDs = AssetDatabase.FindAssets("t:GameObject");
        objectsWithMissingReferences.Clear();

        Debug.Log("Found total of  " + assetGUIDs.Length + " parseable stuff in Assets");

        foreach (string assetGuiD in assetGUIDs)
        {
            GameObject obj = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(assetGuiD));

            RecursiveDepthSearch(obj);
        }
    }
    void RecursiveDepthSearch(GameObject rootGO)
    {
        Component[] components = rootGO.GetComponents<Component>();
        foreach (Component c in components)
        {
            if (c == null)
            {
                if (!objectsWithMissingReferences.Contains(rootGO))
                    objectsWithMissingReferences.Add(rootGO);
            }
        }

        foreach (Transform t in rootGO.transform)
        {
            RecursiveDepthSearch(t.gameObject);
        }
    }
    void CrawlFromScene()
    {
        objectsWithMissingReferences.Clear();

        for (int i = 0; i < SceneManager.sceneCount; ++i)
        {
            var rootGOs = SceneManager.GetSceneAt(i).GetRootGameObjects();

            Debug.Log("Found total of " + rootGOs.Length + " parseable stuff in scene " + i);

            foreach (GameObject obj in rootGOs)
            {
                RecursiveDepthSearch(obj);
            }
        }
    }
    void TryToRemoveMissingReferences()
    {
        foreach (GameObject rootGO in objectsWithMissingReferences)
        {
            Component[] components = rootGO.GetComponents<Component>();
            var r = 0;            
            for (var i = 0; i < components.Length; i++)
            {                
                if (components[i] != null) continue;

                var s = rootGO.name;
                var t = rootGO.transform;
                while (t.parent != null)
                {
                    s = t.parent.name + "/" + s;
                    t = t.parent;
                }

                Debug.Log(s + " has a missing script at " + i + " ; " + rootGO);

                var serializedObject = new SerializedObject(rootGO);
                var prop = serializedObject.FindProperty("m_Component");
                serializedObject.Update();
                prop.DeleteArrayElementAtIndex(i - r);
                r++;
                serializedObject.ApplyModifiedProperties();

                //no   //serializedObject.SetIsDifferentCacheDirty();                
                //no   //serializedObject.ApplyModifiedPropertiesWithoutUndo();
                fixedCounter++;
            }
            EditorUtility.SetDirty(rootGO);
        }
        Debug.Log("Fixed count: " + fixedCounter);
    }
}
