using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ✅ needed for Button

public class BossSceneManager : MonoBehaviour
{
    [Header("Boss (D1 — data-driven, not scene-baked)")]
    [SerializeField] private BossDefinition bossDefinition;
    [SerializeField] private Transform bossSpawnPoint;

    [Header("Victory Settings")]
    [SerializeField] private string victoryPanelName = "VictoryPanel";
    [SerializeField] private string continueButtonName = "nextscene"; // ✅ must match button's GameObject name
    [SerializeField] private string nextSceneName = "RoguelikeScene_2";

    private GameObject victoryPanel;
    private Canvas persistedCanvas;

    private void Start()
    {
        SpawnBoss();
        FindPersistedCanvas();
    }

    private void SpawnBoss()
    {
        if (bossDefinition == null || bossDefinition.bossPrefab == null)
        {
            Debug.LogError("[BossSceneManager] No BossDefinition/bossPrefab assigned!");
            return;
        }

        Vector3 spawnPosition = bossSpawnPoint != null ? bossSpawnPoint.position : Vector3.zero;
        Quaternion spawnRotation = bossSpawnPoint != null ? bossSpawnPoint.rotation : Quaternion.identity;
        Instantiate(bossDefinition.bossPrefab, spawnPosition, spawnRotation);
    }

    private void FindPersistedCanvas()
    {
        persistedCanvas = FindObjectOfType<Canvas>();

        if (persistedCanvas != null)
        {
            Transform panel = persistedCanvas.transform.Find(victoryPanelName);
            if (panel != null)
            {
                victoryPanel = panel.gameObject;
                victoryPanel.SetActive(false);

                // ✅ Find the button inside the panel and wire it in code
                Transform buttonTransform = panel.Find(continueButtonName);
                if (buttonTransform != null)
                {
                    Button continueButton = buttonTransform.GetComponent<Button>();
                    if (continueButton != null)
                    {
                        continueButton.onClick.RemoveAllListeners(); // clear any stale listeners
                        continueButton.onClick.AddListener(OnContinueButtonPressed);
                        Debug.Log("[BossSceneManager] Continue button wired successfully");
                    }
                    else
                        Debug.LogWarning("[BossSceneManager] No Button component on ContinueButton!");
                }
                else
                    Debug.LogWarning($"[BossSceneManager] '{continueButtonName}' not found inside VictoryPanel!");

                Debug.Log("[BossSceneManager] Victory panel found and ready");
            }
            else
                Debug.LogWarning($"[BossSceneManager] '{victoryPanelName}' not found in Canvas!");
        }
        else
            Debug.LogWarning("[BossSceneManager] No persisted Canvas found!");
    }

    public void OnBossDefeated()
    {
        if (victoryPanel == null)
            FindPersistedCanvas();

        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            Time.timeScale = 0f;
            Debug.Log("[BossSceneManager] Boss defeated — Victory panel shown!");
        }
        else
            Debug.LogWarning("[BossSceneManager] Victory panel still null on boss defeat!");
    }

    public void OnContinueButtonPressed()
    {
        Time.timeScale = 1f;
        Debug.Log($"[BossSceneManager] Loading next scene: {nextSceneName}");
        GameManager.Instance.LoadGame();
        victoryPanel.SetActive(false);
    }

    [ContextMenu("DEBUG - Defeat Boss")]
    public void DEBUG_DefeatBoss() => OnBossDefeated();

}