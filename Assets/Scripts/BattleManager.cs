using System.Collections;
using System.Collections.Generic;
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
    private Vector3 playerReturnPosition;
    private Vector3 monsterReturnPosition;
    private Vector3 playerStartScale;
    private Vector3 monsterStartScale;

    private readonly List<SpriteRenderer> hiddenCreatureRenderers = new();
    private readonly List<SpriteSortingState> activeBattleSpriteStates = new();

    private struct SpriteSortingState
    {
        public SpriteRenderer Renderer;
        public int SortingLayerId;
        public int SortingOrder;
    }

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
        playerReturnPosition = playerStartPosition;
        monsterReturnPosition = monsterStartPosition;

        if (activePlayerController != null)
        {
            playerReturnPosition = activePlayerController.GetSafeReturnPosition();
            activePlayerController.PrepareForBattleReturn();
            activePlayerController.enabled = false;
        }

        battleActive = true;
        transitionRunning = true;

        ElevateActiveBattleSprites();
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

        SetOverworldCreatureSpritesVisible(false);
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
        UIManager.Instance.ShowOverworldUI();
        SetOverworldCreatureSpritesVisible(true);

        yield return AnimateCombatants(
            activePlayer.transform.position,
            playerReturnPosition,
            activePlayer.transform.localScale,
            playerStartScale,
            activeMonster.transform.position,
            monsterReturnPosition,
            activeMonster.transform.localScale,
            monsterStartScale,
            exitDuration);

        RestoreActiveBattleSpriteSorting();

        if (activePlayerController != null)
        {
            activePlayerController.enabled = true;
        }

        activePlayer = null;
        activeMonster = null;
        activePlayerController = null;
        hiddenCreatureRenderers.Clear();
        battleActive = false;
        transitionRunning = false;
    }

    private void SetOverworldCreatureSpritesVisible(bool visible)
    {
        if (!visible)
        {
            hiddenCreatureRenderers.Clear();

            MonsterClass[] monsters = Object.FindObjectsByType<MonsterClass>(FindObjectsSortMode.None);
            foreach (MonsterClass monster in monsters)
            {
                if (monster == null || monster == activeMonster)
                {
                    continue;
                }

                AddCreatureRenderers(monster.gameObject);
            }

            foreach (SpriteRenderer renderer in hiddenCreatureRenderers)
            {
                renderer.enabled = false;
            }

            return;
        }

        foreach (SpriteRenderer renderer in hiddenCreatureRenderers)
        {
            if (renderer != null)
            {
                renderer.enabled = true;
            }
        }
    }

    private void AddCreatureRenderers(GameObject creature)
    {
        if (creature == null)
        {
            return;
        }

        if ((activePlayer != null && creature == activePlayer.gameObject) ||
            (activeMonster != null && creature == activeMonster.gameObject))
        {
            return;
        }

        SpriteRenderer[] renderers = creature.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.enabled && !hiddenCreatureRenderers.Contains(renderer))
            {
                hiddenCreatureRenderers.Add(renderer);
            }
        }
    }

    private void ElevateActiveBattleSprites()
    {
        activeBattleSpriteStates.Clear();

        int topSortingLayerId = GetTopSortingLayerId();
        int topSortingOrder = GetTopSortingOrderForLayer(topSortingLayerId) + 100;

        ElevateCombatantSprites(activePlayer != null ? activePlayer.gameObject : null, topSortingLayerId, topSortingOrder);
        ElevateCombatantSprites(activeMonster != null ? activeMonster.gameObject : null, topSortingLayerId, topSortingOrder + 1);
    }

    private void ElevateCombatantSprites(GameObject combatant, int sortingLayerId, int sortingOrder)
    {
        if (combatant == null)
        {
            return;
        }

        SpriteRenderer[] renderers = combatant.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            activeBattleSpriteStates.Add(new SpriteSortingState
            {
                Renderer = renderer,
                SortingLayerId = renderer.sortingLayerID,
                SortingOrder = renderer.sortingOrder,
            });

            renderer.sortingLayerID = sortingLayerId;
            renderer.sortingOrder = sortingOrder;
        }
    }

    private void RestoreActiveBattleSpriteSorting()
    {
        foreach (SpriteSortingState state in activeBattleSpriteStates)
        {
            if (state.Renderer == null)
            {
                continue;
            }

            state.Renderer.sortingLayerID = state.SortingLayerId;
            state.Renderer.sortingOrder = state.SortingOrder;
        }

        activeBattleSpriteStates.Clear();
    }

    private int GetTopSortingLayerId()
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int bestLayerId = 0;
        int bestLayerValue = int.MinValue;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            int layerValue = SortingLayer.GetLayerValueFromID(renderer.sortingLayerID);
            if (layerValue > bestLayerValue)
            {
                bestLayerValue = layerValue;
                bestLayerId = renderer.sortingLayerID;
            }
        }

        return bestLayerId;
    }

    private int GetTopSortingOrderForLayer(int sortingLayerId)
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        int bestSortingOrder = 0;

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.sortingLayerID != sortingLayerId)
            {
                continue;
            }

            if (renderer.sortingOrder > bestSortingOrder)
            {
                bestSortingOrder = renderer.sortingOrder;
            }
        }

        return bestSortingOrder;
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
