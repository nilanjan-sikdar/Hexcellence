using UnityEngine;
using UnityEditor;
using System.IO;

public class GenerateSolidHexagon
{
    [InitializeOnLoadMethod]
    private static void Generate()
    {
        if (SessionState.GetBool("SolidHexGenerated", false)) return;

        string path = "Assets/_assets/SolidHexagon.png";
        
        int size = 256;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        
        float cx = size / 2f;
        float cy = size / 2f;
        float R = size * 0.48f; // Leave a small margin
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                
                // Pointy-topped hexagon distance
                float d1 = Mathf.Abs(dx);
                float d2 = Mathf.Abs(dx * 0.5f + dy * 0.8660254f);
                float d3 = Mathf.Abs(dx * -0.5f + dy * 0.8660254f);
                
                float maxD = Mathf.Max(d1, d2, d3) * 1.1547005f;
                
                if (maxD < R)
                {
                    // Anti-aliased border
                    if (maxD > R - 8f) // 8 pixel border
                    {
                        // Blend black border with white interior
                        float borderFactor = Mathf.Clamp01((maxD - (R - 8f)) / 2f);
                        pixels[y * size + x] = Color.Lerp(Color.white, Color.black, borderFactor);
                    }
                    else
                    {
                        pixels[y * size + x] = Color.white;
                    }

                    // Outer anti-aliasing against transparent
                    if (maxD > R - 1f)
                    {
                        float alpha = Mathf.Clamp01(R - maxD);
                        Color c = pixels[y * size + x];
                        c.a = alpha;
                        pixels[y * size + x] = c;
                    }
                }
                else
                {
                    pixels[y * size + x] = Color.clear;
                }
            }
        }
        
        tex.SetPixels(pixels);
        tex.Apply();
        
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();
        
        Debug.Log("Generated SolidHexagon.png!");
        SessionState.SetBool("SolidHexGenerated", true);
    }
}
