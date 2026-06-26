using System.Collections.Generic;
using System.IO;
using COYGame;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class CoySceneVisualUtility
{
    private const string ScenePath = "Assets/Game/Scenes/BattleScene.unity";
    private const string SpritePath = "Assets/Game/Art/Sprites/WhiteSquare.png";
    private const string VisualRootName = "CourtSpriteVisuals";
    private const float PixelsToWorld = 10f / 1080f;

    public static void EnsureCourtSprites(Dictionary<string, PlayerData> players = null)
    {
        players ??= LoadPlayers();
        EnsureWhiteSprite();
        ConfigureCamera();

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        var root = FindOrCreateRoot();

        Sprite(root.transform, "CourtFloor", sprite, new Color(0.83f, 0.48f, 0.25f), Vector2.zero, new Vector2(1920, 1080), -100);
        Sprite(root.transform, "PaintLeft", sprite, new Color(0.75f, 0.25f, 0.2f), new Vector2(-470, 0), new Vector2(210, 430), -90);
        Sprite(root.transform, "PaintRight", sprite, new Color(0.75f, 0.25f, 0.2f), new Vector2(470, 0), new Vector2(210, 430), -90);
        Sprite(root.transform, "MidCourt", sprite, new Color(0.95f, 0.76f, 0.32f), Vector2.zero, new Vector2(10, 620), -80);

        Hoop(root.transform, "PlayerHoop", sprite, new Vector2(-500, 235));
        Hoop(root.transform, "EnemyHoop", sprite, new Vector2(500, 235));
        Players(root.transform, sprite, players);
        HideLegacyCanvasVisuals();

        EditorUtility.SetDirty(root);
    }

    [MenuItem("Tools/COY/Apply Sprite Visuals To Battle Scene")]
    public static void ApplySpriteVisualsToBattleScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath);
        EnsureCourtSprites();
        CoyFontUtility.RepairBattleUiReferences();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("COY battle scene sprite visuals applied.");
    }

    public static void HideLegacyCanvasVisuals()
    {
        SetUiImageAlpha("Court", 0f, true);
        SetUiImageAlpha("PaintLeft", 0f, false);
        SetUiImageAlpha("PaintRight", 0f, false);
        SetUiImageAlpha("MidCourt", 0f, false);

        foreach (var name in new[] { "PlayerHoop", "EnemyHoop", "Curry", "Green", "Klay", "LeBron", "Doncic", "Reaves" })
        {
            var go = GameObject.Find(name);
            if (go != null && go.GetComponentInParent<Canvas>() != null)
            {
                go.SetActive(false);
                EditorUtility.SetDirty(go);
            }
        }
    }

    private static Dictionary<string, PlayerData> LoadPlayers()
    {
        var players = new Dictionary<string, PlayerData>();
        foreach (var guid in AssetDatabase.FindAssets("t:PlayerData", new[] { "Assets/Game/Data/Players" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var player = AssetDatabase.LoadAssetAtPath<PlayerData>(path);
            if (player != null)
            {
                players[player.name] = player;
            }
        }

        return players;
    }

    private static void EnsureWhiteSprite()
    {
        EnsureFolder("Assets/Game", "Art");
        EnsureFolder("Assets/Game/Art", "Sprites");

        if (!File.Exists(SpritePath))
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath, ImportAssetOptions.ForceUpdate);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 1f;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static void ConfigureCamera()
    {
        var camera = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (camera == null)
        {
            return;
        }

        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.2f, 0.24f);
        EditorUtility.SetDirty(camera);
    }

    private static GameObject FindOrCreateRoot()
    {
        var root = GameObject.Find(VisualRootName);
        if (root != null)
        {
            return root;
        }

        root = new GameObject(VisualRootName);
        root.transform.position = Vector3.zero;
        return root;
    }

    private static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Color color, Vector2 pixelPosition, Vector2 pixelSize, int order)
    {
        var child = parent.Find(name);
        var go = child != null ? child.gameObject : new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(pixelPosition.x * PixelsToWorld, pixelPosition.y * PixelsToWorld, 0f);
        go.transform.localScale = new Vector3(pixelSize.x * PixelsToWorld, pixelSize.y * PixelsToWorld, 1f);

        var renderer = go.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = go.AddComponent<SpriteRenderer>();
        }

        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        EditorUtility.SetDirty(go);
        return renderer;
    }

    private static void Hoop(Transform parent, string name, Sprite sprite, Vector2 pixelPosition)
    {
        var root = FindOrCreateChild(parent, name);
        root.transform.localPosition = new Vector3(pixelPosition.x * PixelsToWorld, pixelPosition.y * PixelsToWorld, 0f);
        Sprite(root.transform, "Backboard", sprite, new Color(1f, 0.92f, 0.55f), Vector2.zero, new Vector2(88, 150), -70);
        Sprite(root.transform, "Rim", sprite, new Color(1f, 0.35f, 0.2f), new Vector2(0, 40), new Vector2(95, 18), -60);
        Sprite(root.transform, "Net", sprite, Color.white, new Vector2(0, -8), new Vector2(52, 95), -50);
        EditorUtility.SetDirty(root);
    }

    private static void Players(Transform parent, Sprite sprite, IReadOnlyDictionary<string, PlayerData> players)
    {
        var names = new[] { "Curry", "Green", "Klay", "LeBron", "Doncic", "Reaves" };
        var positions = new[]
        {
            new Vector2(-260, -105), new Vector2(-125, -95), new Vector2(10, -105),
            new Vector2(185, -105), new Vector2(315, -95), new Vector2(445, -105)
        };

        for (var i = 0; i < names.Length; i++)
        {
            var player = players != null && players.TryGetValue(names[i], out var data) ? data : null;
            var color = player != null ? player.placeholderColor : Color.gray;
            var root = FindOrCreateChild(parent, names[i]);
            root.transform.localPosition = new Vector3(positions[i].x * PixelsToWorld, positions[i].y * PixelsToWorld, 0f);
            Sprite(root.transform, "Token", sprite, color, Vector2.zero, new Vector2(72, 72), -40);
            DeleteChild(root.transform, "Name");
            EditorUtility.SetDirty(root);
        }
    }

    private static GameObject FindOrCreateChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
        {
            return child.gameObject;
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void DeleteChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null)
        {
            Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void SetUiImageAlpha(string name, float alpha, bool keepRaycast)
    {
        var go = GameObject.Find(name);
        if (go == null || go.GetComponentInParent<Canvas>() == null)
        {
            return;
        }

        var image = go.GetComponent<Image>();
        if (image == null)
        {
            return;
        }

        var color = image.color;
        color.a = alpha;
        image.color = color;
        image.raycastTarget = keepRaycast;
        EditorUtility.SetDirty(image);
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
