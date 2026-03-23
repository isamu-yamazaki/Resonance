using UnityEngine;
using UnityEditor;

public class FogNoiseGenerator : EditorWindow
{
    int resolution = 64;
    float scale = 4f;

    [MenuItem("Tools/Fog Noise Generator")]
    static void Open() => GetWindow<FogNoiseGenerator>("Fog Noise");

    void OnGUI()
    {
        resolution = EditorGUILayout.IntField("Resolution", resolution);
        scale = EditorGUILayout.FloatField("Scale", scale);

        if (GUILayout.Button("Generate"))
            Generate();
    }

    void Generate()
    {
        Texture3D texture = new Texture3D(resolution, resolution, resolution, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Repeat;
        texture.filterMode = FilterMode.Trilinear;

        Color[] colors = new Color[resolution * resolution * resolution];

        for (int z = 0; z < resolution; z++)
        for (int y = 0; y < resolution; y++)
        for (int x = 0; x < resolution; x++)
        {
            float fx = (float)x / resolution * scale;
            float fy = (float)y / resolution * scale;
            float fz = (float)z / resolution * scale;

            float r = Mathf.PerlinNoise(fx, fy);
            float g = Mathf.PerlinNoise(fy, fz);
            float b = Mathf.PerlinNoise(fz, fx);
            float a = Mathf.PerlinNoise(fx + fy, fz + fx);

            colors[x + y * resolution + z * resolution * resolution] = new Color(r, g, b, a);
        }

        texture.SetPixels(colors);
        texture.Apply();

        AssetDatabase.CreateAsset(texture, "Assets/FogNoise3D.asset");
        AssetDatabase.SaveAssets();
        Debug.Log("Generated FogNoise3D.asset");
    }
}
