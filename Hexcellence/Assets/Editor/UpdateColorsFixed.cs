using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class UpdateColorsFixed
{
    [InitializeOnLoadMethod]
    private static void FixColors()
    {
        if (SessionState.GetBool("ColorsFixedProperly", false)) return;

        string palettePath = "Assets/Resources/NumberColorPalette.asset";
        NumberColorPalette palette = AssetDatabase.LoadAssetAtPath<NumberColorPalette>(palettePath);

        if (palette != null)
        {
            SerializedObject so = new SerializedObject(palette);

            // Empty Cell: distinct semi-transparent deep blue, NO white/black.
            so.FindProperty("emptyColor").colorValue = new Color(0.15f, 0.35f, 0.75f, 0.5f);

            SerializedProperty entriesProp = so.FindProperty("colorEntries");
            entriesProp.ClearArray();

            // Distinct, vibrant colors for each number without using white or black
            AddEntry(entriesProp, 1, new Color(0.98f, 0.40f, 0.40f)); // 1: Vibrant Red-Pink
            AddEntry(entriesProp, 2, new Color(1.00f, 0.65f, 0.00f)); // 2: Vibrant Orange
            AddEntry(entriesProp, 3, new Color(0.95f, 0.85f, 0.10f)); // 3: Vibrant Yellow
            AddEntry(entriesProp, 4, new Color(0.20f, 0.80f, 0.20f)); // 4: Vibrant Green
            AddEntry(entriesProp, 8, new Color(0.00f, 0.75f, 1.00f)); // 8: Vibrant Cyan
            AddEntry(entriesProp, 16, new Color(0.50f, 0.30f, 1.00f)); // 16: Vibrant Purple
            AddEntry(entriesProp, 32, new Color(1.00f, 0.20f, 0.60f)); // 32: Vibrant Magenta
            AddEntry(entriesProp, 64, new Color(0.60f, 0.00f, 0.00f)); // 64: Deep Crimson
            AddEntry(entriesProp, 128, new Color(0.00f, 0.00f, 0.60f)); // 128: Deep Navy
            AddEntry(entriesProp, 256, new Color(0.00f, 0.50f, 0.00f)); // 256: Deep Forest Green
            AddEntry(entriesProp, 512, new Color(0.60f, 0.40f, 0.00f)); // 512: Deep Bronze
            AddEntry(entriesProp, 1024, new Color(0.60f, 0.00f, 0.60f)); // 1024: Deep Violet

            so.ApplyModifiedProperties();
            
            Debug.Log("Vibrant Colors Applied to Palette! (Excluded white and black)");
            SessionState.SetBool("ColorsFixedProperly", true);
        }
        else
        {
            Debug.LogError("Could not find NumberColorPalette.asset at " + palettePath);
        }
    }

    private static void AddEntry(SerializedProperty arrayProp, int value, Color color)
    {
        int index = arrayProp.arraySize;
        arrayProp.InsertArrayElementAtIndex(index);
        SerializedProperty element = arrayProp.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("number").intValue = value;
        element.FindPropertyRelative("color").colorValue = color;
    }
}
