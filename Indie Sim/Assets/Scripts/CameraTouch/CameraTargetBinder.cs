using UnityEngine;
using System.Collections;

/// <summary>
/// Scene-local. Finds the player at runtime and hands it to CinemachineCursorLead
/// (CameraLead), which owns the actual vcam Follow-target setup. Needed because
/// the player isn't guaranteed to exist in the scene when this vcam's own Start()
/// runs — it may still be arriving via DontDestroyOnLoad today, or be
/// runtime-spawned later (post Phase 6). Polls until a player exists, binds once.
/// </summary>
public class CameraTargetBinder : MonoBehaviour
{
    [SerializeField] private CinemachineCursorLead cameraLead;
    [SerializeField] private float pollInterval = 0.1f;

    private void Start()
    {
        if (cameraLead == null)
            cameraLead = GetComponent<CinemachineCursorLead>();

        StartCoroutine(BindWhenPlayerExists());
    }

    private IEnumerator BindWhenPlayerExists()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        while (player == null)
        {
            yield return new WaitForSeconds(pollInterval);
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (cameraLead != null)
            cameraLead.BindPlayer(player.transform);
    }
}
