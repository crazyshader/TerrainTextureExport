using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Collections;
using System.Linq;

public class TerrainTextureExport : EditorWindow
{
    private enum TextureChannel
    {
        R = 0,
        G,
        B,
        A
    }

    private enum ExportType
    {
        Default,
        DiffuseAndNormal,
        IndexAndControl,
    }

    private enum TextureArrayType
    {
        Diffuse,
        Normal,
    }

    private class IndexAndControl
    {
        public int Index;
        public float TexIndex;
        public float TexBlend;
    }

    private Terrain _targetTerrain;
    private DefaultAsset _savePath;
    private ExportType _exportType;

    [MenuItem("Window/Terrain/TextureExport")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(TerrainTextureExport));
    }

    private void OnCreateTextureArray(List<Texture2D> textureList, string path, TextureArrayType textureType)
    {
        Texture2D texture = textureList[0];
        Texture2DArray textureArray = new Texture2DArray(texture.width, texture.height, textureList.Count, texture.format, texture.mipmapCount > 1)
        {
            
            anisoLevel = texture.anisoLevel,
            filterMode = texture.filterMode,
            wrapMode = texture.wrapMode
        };

        for (int i = 0; i < textureList.Count; i++)
            for (int m = 0; m < texture.mipmapCount; m++)
                Graphics.CopyTexture(textureList[i], 0, m, textureArray, i, m);

        var existing = AssetDatabase.LoadAssetAtPath<Texture2DArray>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(textureArray, existing);
        }
        else
        {
            AssetDatabase.CreateAsset(textureArray, path);
        }

        AssetDatabase.ImportAsset(path);
        AssetDatabase.Refresh();
    }

    private string GetSelectedPath()
    {
        string path = "Assets";
        foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
        {
            path = AssetDatabase.GetAssetPath(obj);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
                break;
            }
        }
        return path;
    }

    private void ExportDiffuseAndNormalMap(string path)
    {
        TerrainData terrainData = _targetTerrain.terrainData;
        TerrainLayer[] terrainLayerArray = terrainData.terrainLayers;

        List<Texture2D> diffuselist = new List<Texture2D>();
        List<Texture2D> normalMaplist = new List<Texture2D>();
        for (int i = 0; i < terrainLayerArray.Length; i++)
        {
            if (terrainLayerArray[i].diffuseTexture != null)
            {
                diffuselist.Add(terrainLayerArray[i].diffuseTexture);
            }
            if (terrainLayerArray[i].normalMapTexture)
            {
                normalMaplist.Add(terrainLayerArray[i].normalMapTexture);
            }
        }

        if (diffuselist.Count <= 0)
        {
            Debug.LogError("The number of terrain splat must be greater than 0.");
            return;
        }
        string relativePath = path.Replace(Application.dataPath, "");
        OnCreateTextureArray(diffuselist, relativePath + "/TerrainTexture_Diffuse.asset", TextureArrayType.Diffuse);

        if (diffuselist.Count != normalMaplist.Count)
        {
            Debug.LogError("The number of diffuse and the number of normal are inconsistent.");
        }
        else
        {
            OnCreateTextureArray(normalMaplist, relativePath + "/TerrainTexture_Normal.asset", TextureArrayType.Normal);
        }
    }

    private void ExportIndexAndControlMap(string path)
    {
        TerrainData terrainData = _targetTerrain.terrainData;
        TerrainLayer[] terrainLayArray = terrainData.terrainLayers;
        Texture2D[] alphaMapArray = terrainData.alphamapTextures;
        int textureNum = terrainLayArray.Length;
        float maxTexCount = textureNum - 1;
        int witdh = alphaMapArray[0].width;
        int height = alphaMapArray[0].height;

        Texture2D indexTex = new Texture2D(witdh, height, TextureFormat.RGBA32, false, true);
        Texture2D blendTex = new Texture2D(witdh, height, TextureFormat.RGBA32, false, true);

        Color curColor = Color.clear;
        Color indexColor = Color.clear;
        Color blendColor = Color.clear;
        float index = 0.0f;
        int controlMapNumber = 0;
        int controlChannelNum = 0;
        int colorChannel = 0;
        List<IndexAndControl> indexAndblendList = new List<IndexAndControl>(8);
        for (int i = 0; i < 8; i++)
        {
            indexAndblendList.Add(new IndexAndControl());
        }

        BitArray blendLayer = new BitArray(textureNum, false);
        for (int i = 0; i < textureNum; i++)
        {
            for (int j = 0; j < witdh && !blendLayer[i]; j++)
            {
                for (int k = 0; k < height && !blendLayer[i]; k++)
                {
                    controlMapNumber = (i % 16) / 4;
                    controlChannelNum = i % 4;
                    curColor = alphaMapArray[controlMapNumber].GetPixel(j, k);

                    switch ((TextureChannel)controlChannelNum)
                    {
                        case TextureChannel.R:
                            if (curColor.r > 0.0f)
                            {
                                blendLayer[i] = true;
                            }
                            break;
                        case TextureChannel.G:
                            if (curColor.g > 0.0f)
                            {
                                blendLayer[i] = true;
                            }
                            break;
                        case TextureChannel.B:
                            if (curColor.b > 0.0f)
                            {
                                blendLayer[i] = true;
                            }
                            break;
                        case TextureChannel.A:
                            if (curColor.a > 0.0f)
                            {
                                blendLayer[i] = true;
                            }
                            break;
                    }
                }
            }
        }

        for (int j = 0; j < witdh; j++)
        {
            for (int k = 0; k < height; k++)
            {
                for (int i = 0; i < textureNum; i++)
                {
                    controlMapNumber = (i % 16) / 4;
                    controlChannelNum = i % 4;
                    curColor = alphaMapArray[controlMapNumber].GetPixel(j, k);
                    index = (float)i / maxTexCount;

                    switch ((TextureChannel)controlChannelNum)
                    {
                        case TextureChannel.R:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.r;
                            break;
                        case TextureChannel.G:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.g;
                            break;
                        case TextureChannel.B:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.b;
                            break;
                        case TextureChannel.A:
                            indexAndblendList[i].Index = i;
                            indexAndblendList[i].TexIndex = index;
                            indexAndblendList[i].TexBlend = curColor.a;
                            break;
                    }
                }

                var indexAndblendPairs = from pair in indexAndblendList
                                         orderby pair.TexBlend descending
                                         select pair;
                var datas = indexAndblendPairs.Take(4).OrderByDescending(item => item.TexIndex);

                colorChannel = 0;
                indexColor = Color.clear;
                blendColor = Color.clear;

                foreach (var indexAndblend in datas)
                {
                    if (colorChannel < 4)
                    {
                        switch ((TextureChannel)colorChannel)
                        {
                            case TextureChannel.R:
                                indexColor.r = indexAndblend.TexIndex;
                                blendColor.r = indexAndblend.TexBlend;
                                break;
                            case TextureChannel.G:
                                indexColor.g = indexAndblend.TexIndex;
                                blendColor.g = indexAndblend.TexBlend;
                                break;
                            case TextureChannel.B:
                                indexColor.b = indexAndblend.TexIndex;
                                blendColor.b = indexAndblend.TexBlend;
                                break;
                            case TextureChannel.A:
                                indexColor.a = indexAndblend.TexIndex;
                                blendColor.a = indexAndblend.TexBlend;
                                break;
                        }
                        colorChannel++;
                    }
                }

                indexTex.SetPixel(j, k, indexColor);
                blendTex.SetPixel(j, k, blendColor);
            }
        }

        string assets = "Assets/";
        string absolutePath = Application.dataPath;
        if (path.Length > assets.Length)
        {
            absolutePath = Path.Combine(Application.dataPath, path.Substring(assets.Length));
        }

        indexTex.Apply();
        byte[] bytes = indexTex.EncodeToPNG();
        string indexTexturePath = Path.Combine(absolutePath, "TerrainTexture_Index.png");
        File.WriteAllBytes(indexTexturePath, bytes);
        AssetDatabase.ImportAsset(indexTexturePath);

        blendTex.Apply();
        byte[] blendBytes = blendTex.EncodeToPNG();
        string controlTexturePath = Path.Combine(absolutePath, "TerrainTexture_Control.png");
        File.WriteAllBytes(controlTexturePath, blendBytes);
        AssetDatabase.ImportAsset(controlTexturePath);

        AssetDatabase.Refresh();
    }
 
    private void OnExportTerrainTexture()
    {
        if (_targetTerrain == null)
        {
            Debug.LogError("Please select one terrain.");
            return;
        }

        //string path = GetSelectedPath();
        string path = "Assets";
        if (_savePath != null)
        {
            path = AssetDatabase.GetAssetPath(_savePath);
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }
        }
        else
        {
            Debug.LogError("Please select save path.");
        }

        if (_exportType == ExportType.DiffuseAndNormal)
        {
            ExportDiffuseAndNormalMap(path);
        }
        else if (_exportType == ExportType.IndexAndControl)
        {
            ExportIndexAndControlMap(path);
        }
        else
        {
            ExportDiffuseAndNormalMap(path);
            ExportIndexAndControlMap(path);
        }

        EditorUtility.UnloadUnusedAssetsImmediate();
        Debug.Log("The terrain export finished.");
    }

    private void OnGUI()
    {
        _targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", _targetTerrain, typeof(Terrain), true);
        _savePath = (DefaultAsset)EditorGUILayout.ObjectField("Save Path", _savePath, typeof(DefaultAsset), false);
        _exportType = (ExportType)EditorGUILayout.EnumPopup("Export Texture Type", _exportType);
        GUILayout.Space(10);

        if (GUILayout.Button("Export Terrain Texture"))
        {
            OnExportTerrainTexture();
        }
    }
}