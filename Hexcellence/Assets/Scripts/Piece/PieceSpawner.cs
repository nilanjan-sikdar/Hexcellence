using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Spawns <see cref="NumberPiece"/> instances at a configurable spawn point.
/// Picks two random numbers from the <see cref="NumberPool"/> for each piece
/// and assigns a random initial rotation.
/// Handles returning pieces to the spawn point on invalid placements.
/// </summary>
public class PieceSpawner : MonoBehaviour
{
    // ───────────────────────── Serialized Fields ─────────────────────────

    [Header("References")]
    [Tooltip("The NumberPiece prefab to instantiate.")]
    [SerializeField] private NumberPiece piecePrefab;

    [Tooltip("The NumberPool used to pick random numbers.")]
    [SerializeField] private NumberPool numberPool;

    [Tooltip("The color palette for coloring piece tiles.")]
    [SerializeField] private NumberColorPalette colorPalette;

    [Tooltip("Reference to HexGridManager to get the hex outer radius.")]
    [SerializeField] private HexGridManager gridManager;

    [Header("Spawn Configuration")]
    [Tooltip("World-space position where new pieces appear.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Duration in seconds for the piece return animation on invalid drop.")]
    [SerializeField] private float returnAnimDuration = 0.3f;

    // ───────────────────────── Private Fields ────────────────────────────

    /// <summary>The currently active piece in play.</summary>
    private NumberPiece currentPiece;

    // ───────────────────────── Properties ────────────────────────────────

    /// <summary>The currently active NumberPiece, or null if none.</summary>
    public NumberPiece CurrentPiece => currentPiece;

    /// <summary>The world position of the spawn point.</summary>
    public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

    // ───────────────────────── Events ────────────────────────────────────

    /// <summary>
    /// Fired when a new piece is spawned and ready for player interaction.
    /// Parameter: the spawned NumberPiece.
    /// </summary>
    public static event Action<NumberPiece> OnPieceSpawned;

    /// <summary>
    /// Fired when a piece finishes returning to the spawn point after invalid drop.
    /// </summary>
    public static event Action OnPieceReturnComplete;

    // ───────────────────────── Public Methods ───────────────────────────

    /// <summary>
    /// Spawns a new NumberPiece at the spawn point with two random numbers
    /// and a random orientation. Destroys any existing piece first.
    /// </summary>
    public void SpawnNewPiece()
    {
        // Clean up previous piece if it exists
        if (currentPiece != null)
        {
            Destroy(currentPiece.gameObject);
            currentPiece = null;
        }

        if (numberPool == null)
        {
            numberPool = Resources.Load<NumberPool>("NumberPool");
        }

        if (colorPalette == null)
        {
            colorPalette = Resources.Load<NumberColorPalette>("NumberColorPalette");
        }

        if (piecePrefab == null || numberPool == null)
        {
            Debug.LogError("[PieceSpawner] Missing piecePrefab or numberPool reference.");
            return;
        }

        int valA = numberPool.GetRandomNumber();
        int valB = numberPool.GetRandomNumber();
        int randomRotation = UnityEngine.Random.Range(0, 6);
        float hexRadius = gridManager != null ? gridManager.OuterRadius : 1f;

        currentPiece = Instantiate(piecePrefab, SpawnPosition, Quaternion.identity, transform);
        currentPiece.Initialize(valA, valB, randomRotation, hexRadius, colorPalette);

        OnPieceSpawned?.Invoke(currentPiece);
    }

    /// <summary>
    /// Smoothly returns the current piece to the spawn point position.
    /// Used when a placement is invalid.
    /// </summary>
    public void ReturnPieceToSpawn()
    {
        if (currentPiece != null)
        {
            StartCoroutine(ReturnAnimation(currentPiece.transform, SpawnPosition));
        }
    }

    /// <summary>
    /// Destroys the current piece. Called after successful placement.
    /// </summary>
    public void DestroyCurrentPiece()
    {
        if (currentPiece != null)
        {
            Destroy(currentPiece.gameObject);
            currentPiece = null;
        }
    }

    // ───────────────────────── Private Methods ──────────────────────────

    /// <summary>
    /// Coroutine that smoothly animates the piece back to the spawn position.
    /// </summary>
    private IEnumerator ReturnAnimation(Transform pieceTransform, Vector3 targetPos)
    {
        Vector3 startPos = pieceTransform.position;
        float elapsed = 0f;

        while (elapsed < returnAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / returnAnimDuration));
            pieceTransform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        pieceTransform.position = targetPos;
        OnPieceReturnComplete?.Invoke();
    }
}

// Refresh
