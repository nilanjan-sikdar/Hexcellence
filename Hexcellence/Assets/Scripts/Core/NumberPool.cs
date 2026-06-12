using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Manages the pool of available numbers for piece generation.
/// Numbers can be discovered during gameplay, expanding the pool over time.
/// </summary>
public class NumberPool : MonoBehaviour
{
    #region Events

    /// <summary>
    /// Fired when a new number is discovered and added to the pool.
    /// Parameter: the newly discovered number value.
    /// </summary>
    public static event Action<int> OnNumberDiscovered;

    #endregion

    #region Serialized Fields

    [Header("Initial Configuration")]
    [Tooltip("The starting set of numbers available for piece generation.")]
    [SerializeField] private List<int> initialPool = new List<int> { 1, 2, 3 };

    #endregion

    #region Private Fields

    /// <summary>Runtime copy of the available number pool.</summary>
    private List<int> currentPool;

    #endregion

    #region Properties

    /// <summary>
    /// Gets a read-only view of the current number pool.
    /// </summary>
    public IReadOnlyList<int> CurrentPool => currentPool;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes the runtime pool from the initial configuration.
    /// </summary>
    private void Awake()
    {
        currentPool = new List<int>(initialPool);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Returns a random number from the current pool.
    /// </summary>
    /// <returns>A randomly selected number from the available pool.</returns>
    public int GetRandomNumber()
    {
        if (currentPool == null || currentPool.Count == 0)
        {
            Debug.LogWarning("[NumberPool] Current pool is empty. Returning 1 as fallback.");
            return 1;
        }

        int randomIndex = UnityEngine.Random.Range(0, currentPool.Count);
        return currentPool[randomIndex];
    }

    /// <summary>
    /// Discovers a new number and adds it to the pool if not already present.
    /// Fires <see cref="OnNumberDiscovered"/> when a new number is added.
    /// </summary>
    /// <param name="number">The number value to discover.</param>
    public void DiscoverNumber(int number)
    {
        if (currentPool == null)
        {
            currentPool = new List<int>(initialPool);
        }

        if (currentPool.Contains(number))
        {
            return;
        }

        currentPool.Add(number);
        OnNumberDiscovered?.Invoke(number);
    }

    /// <summary>
    /// Checks whether a number is currently in the pool.
    /// </summary>
    /// <param name="number">The number value to check.</param>
    /// <returns><c>true</c> if the number exists in the current pool; otherwise <c>false</c>.</returns>
    public bool HasNumber(int number)
    {
        return currentPool != null && currentPool.Contains(number);
    }

    /// <summary>
    /// Resets the pool back to its initial configuration.
    /// </summary>
    public void ResetPool()
    {
        currentPool = new List<int>(initialPool);
    }

    #endregion
}

// Refresh
