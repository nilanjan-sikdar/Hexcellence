using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that maps number values to their display colors.
/// Provides a fast dictionary lookup with a fallback gradient for unmapped values.
/// </summary>
[CreateAssetMenu(menuName = "Hexcellence/Number Color Palette", fileName = "NumberColorPalette")]
public class NumberColorPalette : ScriptableObject
{
    #region Nested Types

    /// <summary>
    /// Associates a number value with its display color.
    /// </summary>
    [Serializable]
    public struct NumberColorEntry
    {
        /// <summary>The number value to map.</summary>
        [Tooltip("The number value.")]
        public int number;

        /// <summary>The display color for this number.</summary>
        [Tooltip("The color for this number.")]
        public Color color;
    }

    #endregion

    #region Constants

    /// <summary>Maximum number used to normalize the fallback gradient evaluation.</summary>
    private const float FALLBACK_GRADIENT_MAX = 128f;

    #endregion

    #region Serialized Fields

    [Header("Color Mappings")]
    [Tooltip("Maps number values to their display colors.")]
    [SerializeField] private List<NumberColorEntry> colorEntries = new List<NumberColorEntry>
    {
        new NumberColorEntry { number = 1,  color = new Color(0.18f,  0.545f, 0.341f, 1f) },  // #2E8B57 sea green
        new NumberColorEntry { number = 2,  color = new Color(0.608f, 0.349f, 0.714f, 1f) },  // #9B59B6 purple
        new NumberColorEntry { number = 3,  color = new Color(0.204f, 0.596f, 0.859f, 1f) },  // #3498DB blue
        new NumberColorEntry { number = 4,  color = new Color(0.557f, 0.267f, 0.678f, 1f) },  // #8E44AD dark purple
        new NumberColorEntry { number = 8,  color = new Color(0.102f, 0.737f, 0.612f, 1f) },  // #1ABC9C teal
        new NumberColorEntry { number = 16, color = new Color(0.902f, 0.494f, 0.133f, 1f) },  // #E67E22 orange
        new NumberColorEntry { number = 32, color = new Color(0.906f, 0.298f, 0.235f, 1f) },  // #E74C3C red
        new NumberColorEntry { number = 64, color = new Color(0.953f, 0.612f, 0.071f, 1f) },  // #F39C12 gold
    };

    [Header("Defaults")]
    [Tooltip("Color for empty hex cells.")]
    [SerializeField] private Color emptyColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Tooltip("Fallback gradient for numbers not in the mapping. Evaluated by (number / 128) clamped to 0-1.")]
    [SerializeField] private Gradient fallbackGradient;

    #endregion

    #region Private Fields

    /// <summary>Cached dictionary for fast color lookups by number value.</summary>
    private Dictionary<int, Color> colorLookup;

    #endregion

    #region Private Methods

    /// <summary>
    /// Builds the internal dictionary from the serialized color entries list.
    /// Called lazily on first access.
    /// </summary>
    private void BuildLookup()
    {
        colorLookup = new Dictionary<int, Color>(colorEntries.Count);
        foreach (NumberColorEntry entry in colorEntries)
        {
            colorLookup[entry.number] = entry.color;
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the display color for a given number value.
    /// Returns the mapped color if one exists, otherwise evaluates the fallback gradient.
    /// </summary>
    /// <param name="number">The number value to look up.</param>
    /// <returns>The color associated with the number, or a fallback color.</returns>
    public Color GetColor(int number)
    {
        if (colorLookup == null)
        {
            BuildLookup();
        }

        if (colorLookup.TryGetValue(number, out Color color))
        {
            return color;
        }

        // Use fallback gradient if available
        if (fallbackGradient != null)
        {
            float normalizedValue = Mathf.Clamp01(number / FALLBACK_GRADIENT_MAX);
            return fallbackGradient.Evaluate(normalizedValue);
        }

        return new Color(0.2f, 0.2f, 0.2f, 1f);
    }

    /// <summary>
    /// Gets the color used for empty (unoccupied) hex cells.
    /// </summary>
    /// <returns>The empty cell color.</returns>
    public Color GetEmptyColor()
    {
        return emptyColor;
    }

    #endregion
}

// Refresh
