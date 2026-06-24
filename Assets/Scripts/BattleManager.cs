using System.Collections;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private static BattleManager instance;

    [Header("Transition")]
    [SerializeField] private float entryDuration = 0.25f;
    [SerializeField] private float exitDuration = 0.2f;
    [SerializeField] private float scaleMultiplier = 1.75f;
    [SerializeField] [Range(0.05f, 0.45f)] private float heroViewportX = 0.25f;
    [SerializeField] [Range(0.55f, 0.95f)] private float enemyViewportX = 0.75f;
    [SerializeField] [Range(0.1f, 0.9f)] private float combatViewportY = 0.5f;

    private PlayerClass activePlayer;
    private MonsterClass activeMonster;
    private PlayerController activePlayerController;

    private bool battleActive;
    private bool transitionRunning;

    private Vector3 playerStartPosition;
    private Vector3 monsterStartPosition;
    private Vector3 playerStartScale;
    private Vector3 monsterStartScale;

    public static BattleManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<BattleManager>();

                if (instance == null)
                {
                    GameObject managerObject = new GameObject(nameof(BattleManager));
                    instance = managerObject.AddComponent<BattleManager>();
                }
            }

            return instance;
        }
    }

    public bool IsBattleActive => battleActive;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    public void StartBattle(PlayerClass player, MonsterClass monster)
    {
        if (battleActive || transitionRunning || player == null || monster == null)
        {
            return;
        }

        activePlayer = player;
        activeMonster = monster;
        activePlayerController = player.GetComponent<PlayerController>();

        playerStartPosition = activePlayer.transform.position;
        monsterStartPosition = activeMonster.transform.position;
        playerStartScale = activePlayer.transform.localScale;
        monsterStartScale = activeMonster.transform.localScale;

        battleActive = true;
        transitionRunning = true;

        if (activePlayerController != null)
        {
            activePlayerController.enabled = false;
        }

        StartCoroutine(BeginBattleRoutine());
    }

    public void OnAttackPressed()
    {
        if (!battleActive || transitionRunning)
        {
            return;
        }

        UIManager.Instance.SetCombatLog("Attack is not implemented yet.");
    }

    public void OnItemPressed()
    {
        if (!battleActive || transitionRunning)
        {
            return;
        }

        UIManager.Instance.SetCombatLog("Item is not implemented yet.");
    }

    public void OnRunPressed()
    {
        if (!battleActive || transitionRunning)
        {
            return;
        }

        StartCoroutine(ExitBattleRoutine());
    }

    private IEnumerator BeginBattleRoutine()
    {
        UIManager.Instance.SetBattleButtonsInteractable(false);

        Vector3 playerTarget = GetViewportWorldPosition(heroViewportX, combatViewportY, playerStartPosition.z);
        Vector3 monsterTarget = GetViewportWorldPosition(enemyViewportX, combatViewportY, monsterStartPosition.z);

        yield return AnimateCombatants(
            playerStartPosition,
            playerTarget,
            playerStartScale,
            playerStartScale * scaleMultiplier,
            monsterStartPosition,
            monsterTarget,
            monsterStartScale,
            monsterStartScale * scaleMultiplier,
            entryDuration);

        UIManager.Instance.ShowBattleUI();
        UIManager.Instance.BindBattle(activePlayer, activeMonster);
        UIManager.Instance.SetCombatLog($"A {activeMonster.DisplayName} has appeared!");
        UIManager.Instance.SetBattleButtonsInteractable(true);

        transitionRunning = false;
    }

    private IEnumerator ExitBattleRoutine()
    {
        transitionRunning = true;
        UIManager.Instance.SetBattleButtonsInteractable(false);
        UIManager.Instance.SetCombatLog("You ran away.");

        yield return AnimateCombatants(
            activePlayer.transform.position,
            playerStartPosition,
            activePlayer.transform.localScale,
            playerStartScale,
            activeMonster.transform.position,
            monsterStartPosition,
            activeMonster.transform.localScale,
            monsterStartScale,
            exitDuration);

        UIManager.Instance.ShowOverworldUI();

        if (activePlayerController != null)
        {
            activePlayerController.enabled = true;
        }

        activePlayer = null;
        activeMonster = null;
        activePlayerController = null;
        battleActive = false;
        transitionRunning = false;
    }

    private Vector3 GetViewportWorldPosition(float viewportX, float viewportY, float worldZ)
    {
        Camera targetCamera = Camera.main;
        if (targetCamera == null)
        {
            Vector3 fallback = (playerStartPosition + monsterStartPosition) * 0.5f;
            fallback.z = worldZ;
            return fallback;
        }

        float cameraDistance = Mathf.Abs(worldZ - targetCamera.transform.position.z);
        Vector3 position = targetCamera.ViewportToWorldPoint(new Vector3(viewportX, viewportY, cameraDistance));
        position.z = worldZ;
        return position;
    }

    private IEnumerator AnimateCombatants(
        Vector3 playerFrom,
        Vector3 playerTo,
        Vector3 playerScaleFrom,
        Vector3 playerScaleTo,
        Vector3 monsterFrom,
        Vector3 monsterTo,
        Vector3 monsterScaleFrom,
        Vector3 monsterScaleTo,
        float duration)
    {
        if (activePlayer == null || activeMonster == null)
        {
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            activePlayer.transform.position = Vector3.Lerp(playerFrom, playerTo, eased);
            activePlayer.transform.localScale = Vector3.Lerp(playerScaleFrom, playerScaleTo, eased);

            activeMonster.transform.position = Vector3.Lerp(monsterFrom, monsterTo, eased);
            activeMonster.transform.localScale = Vector3.Lerp(monsterScaleFrom, monsterScaleTo, eased);

            yield return null;
        }

        activePlayer.transform.position = playerTo;
        activePlayer.transform.localScale = playerScaleTo;

        activeMonster.transform.position = monsterTo;
        activeMonster.transform.localScale = monsterScaleTo;
    }
}
