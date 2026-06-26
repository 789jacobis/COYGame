using TMPro;
using UnityEditor;
using UnityEngine;

public static class CoyFontUtility
{
    [MenuItem("Tools/COY/Repair Current Scene")]
    public static void RepairCurrentScene()
    {
        ResetTmpToDefaultFont();
        CoySceneVisualUtility.EnsureCourtSprites();
        RepairBattleUiReferences();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("COY scene repaired with default TMP font references and sprite court visuals.");
    }

    private static void ResetTmpToDefaultFont()
    {
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null)
        {
            Debug.LogWarning("TMP default font asset is missing; text font references were not changed.");
            return;
        }

        foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            text.font = defaultFont;
            text.fontSharedMaterial = defaultFont.material;
            text.SetAllDirty();
            EditorUtility.SetDirty(text);
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/UI/CardView.prefab");
        if (prefab != null)
        {
            foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                text.font = defaultFont;
                text.fontSharedMaterial = defaultFont.material;
                text.SetAllDirty();
                EditorUtility.SetDirty(text);
            }

            PrefabUtility.SavePrefabAsset(prefab);
        }
    }

    public static void RepairBattleUiReferences()
    {
        var ui = Object.FindFirstObjectByType<COYGame.BattleUI>(FindObjectsInactive.Include);
        var controller = Object.FindFirstObjectByType<COYGame.BattleController>(FindObjectsInactive.Include);
        if (ui == null)
        {
            return;
        }

        var so = new SerializedObject(ui);
        so.FindProperty("controller").objectReferenceValue = controller;

        var court = GameObject.Find("Court");
        if (court != null)
        {
            so.FindProperty("playArea").objectReferenceValue = court.GetComponent<RectTransform>();
        }

        var cardPrefab = AssetDatabase.LoadAssetAtPath<COYGame.CardView>("Assets/Game/UI/CardView.prefab");
        so.FindProperty("cardViewPrefab").objectReferenceValue = cardPrefab;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(ui);

        DisableRaycast("CardPreview");
    }

    public static void DisableRaycast(string rootName)
    {
        var root = GameObject.Find(rootName);
        if (root == null)
        {
            return;
        }

        foreach (var graphic in root.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            graphic.raycastTarget = false;
            EditorUtility.SetDirty(graphic);
        }
    }
}
