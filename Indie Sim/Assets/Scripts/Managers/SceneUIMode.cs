using UnityEngine;

/// <summary>
/// Per-scene marker read by CursorController and CustomCrosshair to decide
/// menu vs gameplay presentation, instead of comparing scene names as strings
/// (the three-spellings-of-one-scene bug this replaces).
/// </summary>
public class SceneUIMode : MonoBehaviour
{
    [SerializeField] private bool showCursor;
    public bool ShowCursor => showCursor;
}
