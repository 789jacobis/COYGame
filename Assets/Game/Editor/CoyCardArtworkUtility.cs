using System.Collections.Generic;
using System.IO;
using COYGame;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CoyCardArtworkUtility
{
    private const string ArtFolder = "Assets/Game/Art/CardArt";
    private const string CardPrefabPath = "Assets/Game/UI/CardView.prefab";

    [MenuItem("Tools/COY/Generate Placeholder Card Artwork")]
    public static void GeneratePlaceholderCardArtwork()
    {
        EnsureFolder("Assets/Game/Art", "CardArt");
        var artwork = new Dictionary<string, Sprite>
        {
            ["Curry"] = CreateArtwork("Curry", new Color32(77, 153, 255, 255), new Color32(255, 218, 72, 255)),
            ["Green"] = CreateArtwork("Green", new Color32(46, 210, 65, 255), new Color32(27, 84, 39, 255)),
            ["Klay"] = CreateArtwork("Klay", new Color32(72, 213, 220, 255), new Color32(244, 244, 244, 255)),
            ["LeBron"] = CreateArtwork("LeBron", new Color32(220, 172, 38, 255), new Color32(95, 43, 155, 255)),
            ["Doncic"] = CreateArtwork("Doncic", new Color32(183, 178, 255, 255), new Color32(35, 82, 186, 255)),
            ["Reaves"] = CreateArtwork("Reaves", new Color32(244, 246, 87, 255), new Color32(88, 88, 88, 255)),
            ["Item"] = CreateArtwork("Item", new Color32(245, 245, 245, 255), new Color32(120, 120, 120, 255))
        };

        AssignPlayerArtwork(artwork);
        AssignItemArtwork(artwork["Item"]);
        EnsureCardPrefabUsesFullBackgroundArtwork();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("COY placeholder card artwork generated and assigned.");
    }

    private static Sprite CreateArtwork(string name, Color32 primary, Color32 accent)
    {
        var path = $"{ArtFolder}/{name}CardArt.png";
        var texture = new Texture2D(160, 250, TextureFormat.RGBA32, false);
        for (var y = 0; y < texture.height; y++)
        {
            for (var x = 0; x < texture.width; x++)
            {
                var diagonal = x + y > texture.height * 0.35f && x - y < texture.width * 0.65f;
                var border = x < 8 || y < 8 || x >= texture.width - 8 || y >= texture.height - 8;
                texture.SetPixel(x, y, border || diagonal ? accent : primary);
            }
        }

        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static void AssignPlayerArtwork(IReadOnlyDictionary<string, Sprite> artwork)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:PlayerData", new[] { "Assets/Game/Data/Players" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var player = AssetDatabase.LoadAssetAtPath<PlayerData>(path);
            if (player == null || !artwork.TryGetValue(player.name, out var sprite))
            {
                continue;
            }

            player.cardArtwork = sprite;
            EditorUtility.SetDirty(player);
        }
    }

    private static void AssignItemArtwork(Sprite itemSprite)
    {
        var rebound = AssetDatabase.LoadAssetAtPath<CardData>("Assets/Game/Data/Cards/Rebound.asset");
        if (rebound == null)
        {
            return;
        }

        rebound.artwork = itemSprite;
        EditorUtility.SetDirty(rebound);
    }

    private static void EnsureCardPrefabUsesFullBackgroundArtwork()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPrefabPath);
        if (prefab == null)
        {
            return;
        }

        var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
        var cardView = root.GetComponent<CardView>();
        var artwork = root.transform.Find("Artwork");
        if (artwork != null)
        {
            Object.DestroyImmediate(artwork.gameObject);
        }

        var body = root.transform.Find("Body")?.GetComponent<TMP_Text>();
        if (body != null)
        {
            body.rectTransform.anchoredPosition = new Vector2(0f, -42f);
            body.rectTransform.sizeDelta = new Vector2(138f, 88f);
            EditorUtility.SetDirty(body);
        }

        var so = new SerializedObject(cardView);
        so.FindProperty("artworkImage").objectReferenceValue = null;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
