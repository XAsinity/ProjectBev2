using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureArrayCreator
{
    [MenuItem("Assets/Create/Texture2D Array From Selection")]
    public static void CreateTextureArray()
    {
        var textures = Selection.GetFiltered<Texture2D>(SelectionMode.DeepAssets);

        if (textures.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please select texture files first!", "OK");
            return;
        }

        int width = textures[0].width;
        int height = textures[0].height;
        TextureFormat format = textures[0].format;

        Texture2DArray textureArray = new Texture2DArray(width, height, textures.Length, format, textures[0].mipmapCount > 1);

        for (int i = 0; i < textures.Length; i++)
        {
            Graphics.CopyTexture(textures[i], 0, textureArray, i);
        }

        textureArray.Apply();

        // Create TextureArrays folder if it doesn't exist
        if (!Directory.Exists("Assets/TextureArrays"))
        {
            Directory.CreateDirectory("Assets/TextureArrays");
        }

        string path = "Assets/TextureArrays/MyTreeTextureArray.asset";
        AssetDatabase.CreateAsset(textureArray, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Success!", $"Texture Array created at:\n{path}", "OK");
    }
}