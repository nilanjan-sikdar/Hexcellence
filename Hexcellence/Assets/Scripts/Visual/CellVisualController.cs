using UnityEngine;
using System.Collections;

/// <summary>
/// Listens to cell value changes and updates visual appearance (colors and animations)
/// based on the configured <see cref="NumberColorPalette"/>.
/// </summary>
public class CellVisualController : MonoBehaviour
{
    #region Constants

    /// <summary>Duration of the placement scale punch animation in seconds.</summary>
    private const float PLACE_ANIMATION_DURATION = 0.2f;

    /// <summary>Peak scale multiplier for the placement animation.</summary>
    private const float PLACE_PUNCH_SCALE = 1.15f;

    /// <summary>Duration of the merge scale punch animation in seconds.</summary>
    private const float MERGE_ANIMATION_DURATION = 0.3f;

    /// <summary>Peak scale multiplier for the merge animation.</summary>
    private const float MERGE_PUNCH_SCALE = 1.35f;

    /// <summary>Duration of the white flash during merge animation in seconds.</summary>
    private const float MERGE_FLASH_DURATION = 0.08f;

    /// <summary>Base scale value for cells.</summary>
    private const float BASE_SCALE = 1.0f;

    #endregion

    #region Serialized Fields

    [Header("References")]
    [Tooltip("The color palette used to determine cell colors based on their number values.")]
    [SerializeField] private NumberColorPalette colorPalette;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Subscribes to the cell value changed event.
    /// </summary>
    private void OnEnable()
    {
        HexCell.OnCellValueChanged += HandleCellValueChanged;
    }

    /// <summary>
    /// Unsubscribes from the cell value changed event.
    /// </summary>
    private void OnDisable()
    {
        HexCell.OnCellValueChanged -= HandleCellValueChanged;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles a cell value change by updating the cell's color and triggering
    /// the appropriate animation (place or merge).
    /// </summary>
    /// <param name="cell">The cell whose value changed.</param>
    /// <param name="prevValue">The previous value of the cell.</param>
    /// <param name="newValue">The new value of the cell.</param>
    private void HandleCellValueChanged(HexCell cell, int prevValue, int newValue)
    {
        if (colorPalette == null)
        {
            colorPalette = Resources.Load<NumberColorPalette>("NumberColorPalette");
            if (colorPalette == null)
            {
                Debug.LogWarning("[CellVisualController] NumberColorPalette is not assigned and could not be loaded from Resources.");
                return;
            }
        }

        // Update the cell color
        if (newValue == 0)
        {
            cell.SetDefaultColor(colorPalette.GetEmptyColor());
        }
        else
        {
            cell.SetDefaultColor(colorPalette.GetColor(newValue));
        }

        // Determine and trigger the appropriate animation
        if (newValue > prevValue && prevValue > 0)
        {
            // Merge: value increased from a non-zero state
            StartCoroutine(MergeAnimation(cell));
        }
        else if (newValue > 0 && prevValue == 0)
        {
            // Placement: cell went from empty to occupied
            StartCoroutine(PlaceAnimation(cell));
        }
    }

    #endregion

    #region Animation Coroutines

    /// <summary>
    /// Plays a quick scale punch animation for piece placement.
    /// Scales from 1.0 → 1.15 → 1.0 over 0.2 seconds.
    /// </summary>
    /// <param name="cell">The cell to animate.</param>
    /// <returns>Coroutine enumerator.</returns>
    private IEnumerator PlaceAnimation(HexCell cell)
    {
        if (cell == null)
        {
            yield break;
        }

        Transform target = cell.transform;
        Vector3 originalScale = Vector3.one * BASE_SCALE;
        Vector3 punchTargetScale = Vector3.one * PLACE_PUNCH_SCALE;
        float halfDuration = PLACE_ANIMATION_DURATION * 0.5f;
        float elapsed = 0f;

        // Scale up
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            target.localScale = Vector3.Lerp(originalScale, punchTargetScale, t);
            yield return null;
        }

        target.localScale = punchTargetScale;

        // Scale back down
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            target.localScale = Vector3.Lerp(punchTargetScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    /// <summary>
    /// Plays a larger scale punch animation with a brief white flash for merging.
    /// Scales from 1.0 → 1.35 → 1.0 over 0.3 seconds with a white flash at the peak.
    /// </summary>
    /// <param name="cell">The cell to animate.</param>
    /// <returns>Coroutine enumerator.</returns>
    private IEnumerator MergeAnimation(HexCell cell)
    {
        if (cell == null)
        {
            yield break;
        }

        Transform target = cell.transform;
        SpriteRenderer spriteRenderer = cell.CellSpriteRenderer;
        Vector3 originalScale = Vector3.one * BASE_SCALE;
        Vector3 punchTargetScale = Vector3.one * MERGE_PUNCH_SCALE;

        // Store the current color to restore after the flash
        Color targetColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

        float halfDuration = MERGE_ANIMATION_DURATION * 0.5f;
        float elapsed = 0f;

        // Scale up phase
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            target.localScale = Vector3.Lerp(originalScale, punchTargetScale, t);
            yield return null;
        }

        target.localScale = punchTargetScale;

        // Brief white flash at peak
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
            yield return new WaitForSeconds(MERGE_FLASH_DURATION);
            spriteRenderer.color = targetColor;
        }

        // Scale back down phase
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / halfDuration));
            target.localScale = Vector3.Lerp(punchTargetScale, originalScale, t);
            yield return null;
        }

        target.localScale = originalScale;
    }

    #endregion
}

// Refresh
