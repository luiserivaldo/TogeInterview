using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    private const string HeroDisplayName = "Hero";

    private static BattleManager instance;

    [Header("Transition")]
    [SerializeField] private float entryDuration = 0.25f;
    [SerializeField] private float exitDuration = 0.2f;
    [SerializeField] private float scaleMultiplier = 1.75f;
    [SerializeField] [Range(0.05f, 0.45f)] private float heroViewportX = 0.25f;
    [SerializeField] [Range(0.55f, 0.95f)] private float enemyViewportX = 0.75f;
    [SerializeField] [Range(0.1f, 0.9f)] private float combatViewportY = 0.5f;

    [Header("Turn Combat")]
    [SerializeField] private float actionPauseDuration = 0.05f;
    [SerializeField] private float defeatFadeDuration = 0.4f;
    [SerializeField] private float conclusionPauseDuration = 0.2f;
    [SerializeField] private float attackLungeDistance = 0.45f;
    [SerializeField] private float attackLungeDuration = 0.12f;
    [SerializeField] private float hitFlashDuration = 0.16f;
    [SerializeField] private float hitShakeMagnitude = 0.08f;
    [SerializeField] private Color damageTint = new(1f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color defeatTint = new(0.62f, 0.62f, 0.62f, 1f);

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
    private int playerBattleMaxHp;
    private bool suppressOpeningBattleLog;
    private bool openingActionsDeferred;

    private readonly List<SpriteRenderer> hiddenCreatureRenderers = new();
    private readonly List<SpriteSortingState> activeBattleSpriteStates = new();
    private AudioManager audioManager;

    public enum BattleResult
    {
        Victory,
        RanAway,
        Defeat,
    }

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
                instance = UnityEngine.Object.FindFirstObjectByType<BattleManager>();

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
    public event Action<BattleResult, MonsterClass> BattleEnded;

    public void SetOpeningTutorialMode(bool enabled)
    {
        suppressOpeningBattleLog = enabled;

        if (!enabled)
        {
            openingActionsDeferred = false;
        }
    }

    public void CompleteOpeningTutorial()
    {
        suppressOpeningBattleLog = false;

        if (!battleActive || !openingActionsDeferred || UIManager.Instance == null || activeMonster == null)
        {
            openingActionsDeferred = false;
            return;
        }

        UIManager.Instance.SetCombatLog($"A <color=red>{activeMonster.DisplayName}</color> has appeared!");
        UIManager.Instance.AppendCombatLog($"<color=green>{HeroDisplayName}</color> moves first.");
        UIManager.Instance.SetBattleButtonsInteractable(true);
        UIManager.Instance.SelectBattleDefaultAction();
        openingActionsDeferred = false;
    }

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

        audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
    }

    public void StartBattle(PlayerClass player, MonsterClass monster)
    {
        if (GameManager.IsGameplayLocked || battleActive || transitionRunning || player == null || monster == null)
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
        playerBattleMaxHp = Mathf.Max(1, activePlayer.maxHp);
        playerReturnPosition = playerStartPosition;
        monsterReturnPosition = monsterStartPosition;

        if (activePlayerController != null)
        {
            playerReturnPosition = activePlayerController.GetSafeReturnPosition();
            activePlayerController.PrepareForBattleReturn();
            activePlayerController.enabled = false;
        }

        ResetCombatantAlpha(activePlayer.gameObject);
        ResetCombatantAlpha(activeMonster.gameObject);
        ResetCombatantColor(activePlayer.gameObject);
        ResetCombatantColor(activeMonster.gameObject);

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

        StartCoroutine(ResolvePlayerActionRoutine(BattleAction.Attack));
    }

    public void OnItemPressed()
    {
        if (!battleActive || transitionRunning)
        {
            return;
        }

        StartCoroutine(ResolvePlayerActionRoutine(BattleAction.Item));
    }

    public void OnRunPressed()
    {
        if (!battleActive || transitionRunning)
        {
            return;
        }

        StartCoroutine(ExitBattleRoutine("You ran away.", false, false, BattleResult.RanAway));
    }

    private enum BattleAction
    {
        Attack,
        Item,
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
        UIManager.Instance.BindBattle(activePlayer, activeMonster, true, playerBattleMaxHp);

        if (suppressOpeningBattleLog)
        {
            openingActionsDeferred = true;
            UIManager.Instance.SetCombatLog(string.Empty);
        }
        else
        {
            UIManager.Instance.SetCombatLog($"A <color=red>{activeMonster.DisplayName}</color> has appeared!");
            UIManager.Instance.AppendCombatLog($"<color=green>{HeroDisplayName}</color> moves first.");
            UIManager.Instance.SetBattleButtonsInteractable(true);
            UIManager.Instance.SelectBattleDefaultAction();
        }

        transitionRunning = false;
    }

    private IEnumerator ResolvePlayerActionRoutine(BattleAction action)
    {
        transitionRunning = true;
        UIManager.Instance.SetBattleButtonsInteractable(false);

        if (action == BattleAction.Attack)
        {
            yield return AnimateAttackRoutine(activePlayer.transform, activeMonster.transform);

            int damage = activeMonster.ApplyDamage(activePlayer.attack);
            PlayBattleSfx("damage", "combat");
            yield return HitReactRoutine(activeMonster.gameObject, activeMonster.IsDefeated);
            UIManager.Instance.AppendCombatLog($"<color=green>{HeroDisplayName}</color> attacks <color=red>{activeMonster.DisplayName}</color> for <color=red>{damage}</color> damage!");
            UIManager.Instance.BindBattle(activePlayer, activeMonster, false, playerBattleMaxHp);
        }
        else
        {
            UIManager.Instance.AppendCombatLog($"<color=green>{HeroDisplayName}</color> tries to use an item, but nothing happens.");
            UIManager.Instance.BindBattle(activePlayer, activeMonster, false, playerBattleMaxHp);
        }

        if (activeMonster.IsDefeated)
        {
            yield return HandleEnemyDefeatRoutine();
            yield break;
        }

        yield return new WaitForSeconds(actionPauseDuration);

        yield return AnimateAttackRoutine(activeMonster.transform, activePlayer.transform);
        int incomingDamage = activePlayer.ApplyDamage(activeMonster.attack);
        PlayBattleSfx("damage", "combat");
        yield return HitReactRoutine(activePlayer.gameObject, activePlayer.IsDefeated);
        UIManager.Instance.AppendCombatLog($"<color=red>{activeMonster.DisplayName}</color> attacks <color=green>{HeroDisplayName}</color> for {incomingDamage}!");
        UIManager.Instance.BindBattle(activePlayer, activeMonster, activePlayer.IsDefeated ? false : true, playerBattleMaxHp);

        if (activePlayer.IsDefeated)
        {
            yield return HandlePlayerDefeatRoutine();
            yield break;
        }

        yield return new WaitForSeconds(actionPauseDuration);

        UIManager.Instance.SetBattleButtonsInteractable(true);
        UIManager.Instance.SelectBattleDefaultAction();
        UIManager.Instance.BindBattle(activePlayer, activeMonster, true, playerBattleMaxHp);
        transitionRunning = false;
    }

    private IEnumerator HandleEnemyDefeatRoutine()
    {
        UIManager.Instance.BindBattle(activePlayer, activeMonster, false, playerBattleMaxHp);
        UIManager.Instance.AppendCombatLog($"<color=red>{activeMonster.DisplayName}</color> has been defeated! Earn <color=yellow>{activeMonster.money}</color> GP!");
        PlayBattleSfx("enemyDefeat", "kill");

        yield return new WaitForSeconds(conclusionPauseDuration);

        GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.MonsterKilled(activeMonster);
        }

        yield return ExitBattleRoutine(string.Empty, true, true, BattleResult.Victory);
    }

    private IEnumerator HandlePlayerDefeatRoutine()
    {
        UIManager.Instance.BindBattle(activePlayer, activeMonster, false, playerBattleMaxHp);
        UIManager.Instance.AppendCombatLog($"<color=green>{HeroDisplayName}</color> has been defeated...");

        yield return FadeCombatantRoutine(activePlayer.gameObject, 1f, 0f, defeatFadeDuration);
        yield return new WaitForSeconds(conclusionPauseDuration);

        SetOverworldCreatureSpritesVisible(true);
        RestoreActiveBattleSpriteSorting();
        UIManager.Instance.ShowOverworldUI();

        battleActive = false;
        transitionRunning = false;
        hiddenCreatureRenderers.Clear();
        MonsterClass defeatedMonster = activeMonster;
        activePlayer = null;
        activeMonster = null;
        activePlayerController = null;

        BattleEnded?.Invoke(BattleResult.Defeat, defeatedMonster);

        GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.GameOver();
        }
    }

    private IEnumerator ExitBattleRoutine(string logMessage, bool deactivateMonster, bool grantPlayerTurnOnExit, BattleResult result)
    {
        transitionRunning = true;
        UIManager.Instance.SetBattleButtonsInteractable(false);

        if (!string.IsNullOrWhiteSpace(logMessage))
        {
            UIManager.Instance.AppendCombatLog(logMessage);
        }

        UIManager.Instance.ShowOverworldUI();
        SetOverworldCreatureSpritesVisible(true);

        if (deactivateMonster && activeMonster != null && activeMonster.IsDefeated)
        {
            SetCombatantRenderersEnabled(activeMonster.gameObject, false);
        }

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
        ResetCombatantAlpha(activePlayer.gameObject);
        ResetCombatantAlpha(activeMonster.gameObject);
        ResetCombatantColor(activePlayer.gameObject);
        ResetCombatantColor(activeMonster.gameObject);

        if (deactivateMonster && activeMonster != null)
        {
            activeMonster.gameObject.SetActive(false);
        }

        if (activePlayerController != null)
        {
            activePlayerController.enabled = true;
        }

        MonsterClass completedMonster = activeMonster;
        activePlayer = null;
        activeMonster = null;
        activePlayerController = null;
        hiddenCreatureRenderers.Clear();
        battleActive = false;
        transitionRunning = false;
        openingActionsDeferred = false;
        suppressOpeningBattleLog = false;

        if (grantPlayerTurnOnExit)
        {
            UIManager.Instance.UpdateUI();
        }

        BattleEnded?.Invoke(result, completedMonster);
    }

    private IEnumerator FadeCombatantRoutine(GameObject combatant, float fromAlpha, float toAlpha, float duration)
    {
        if (combatant == null)
        {
            yield break;
        }

        SpriteRenderer[] renderers = combatant.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
        {
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = Mathf.Lerp(fromAlpha, toAlpha, t);
            SetRenderersAlpha(renderers, alpha);
            yield return null;
        }

        SetRenderersAlpha(renderers, toAlpha);
    }

    private void ResetCombatantAlpha(GameObject combatant)
    {
        if (combatant == null)
        {
            return;
        }

        SpriteRenderer[] renderers = combatant.GetComponentsInChildren<SpriteRenderer>(true);
        SetRenderersAlpha(renderers, 1f);
        SetRenderersEnabled(renderers, true);
    }

    private void ResetCombatantColor(GameObject combatant)
    {
        if (combatant == null)
        {
            return;
        }

        SpriteRenderer[] renderers = combatant.GetComponentsInChildren<SpriteRenderer>(true);
        SetRenderersColor(renderers, Color.white);
    }

    private void SetRenderersAlpha(SpriteRenderer[] renderers, float alpha)
    {
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Color color = renderer.color;
            color.a = alpha;
            renderer.color = color;
        }
    }


    private IEnumerator AnimateAttackRoutine(Transform attacker, Transform target)
    {
        if (attacker == null || target == null)
        {
            yield break;
        }

        Vector3 startPosition = attacker.position;
        Vector3 direction = (target.position - startPosition).normalized;
        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            yield break;
        }

        Vector3 lungeTarget = startPosition + (direction * attackLungeDistance);
        yield return MoveTransformRoutine(attacker, startPosition, lungeTarget, attackLungeDuration);
        yield return MoveTransformRoutine(attacker, lungeTarget, startPosition, attackLungeDuration);
    }

    private IEnumerator HitReactRoutine(GameObject combatant, bool defeated)
    {
        if (combatant == null)
        {
            yield break;
        }

        SpriteRenderer[] renderers = combatant.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0)
        {
            yield break;
        }

        Dictionary<SpriteRenderer, Color> originalColors = CacheRendererColors(renderers);
        Transform combatantTransform = combatant.transform;
        Vector3 originalPosition = combatantTransform.position;

        float elapsed = 0f;
        while (elapsed < hitFlashDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / hitFlashDuration);
            float tintStrength = 1f - progress;

            ApplyTint(renderers, originalColors, damageTint, tintStrength);
            Vector2 shakeOffset = UnityEngine.Random.insideUnitCircle * hitShakeMagnitude;
            combatantTransform.position = new Vector3(originalPosition.x + shakeOffset.x, originalPosition.y + shakeOffset.y, originalPosition.z);
            yield return null;
        }

        combatantTransform.position = originalPosition;

        if (defeated)
        {
            SetRenderersColor(renderers, defeatTint);
            yield break;
        }

        RestoreRendererColors(originalColors);
    }

    private static IEnumerator MoveTransformRoutine(Transform target, Vector3 from, Vector3 to, float duration)
    {
        if (target == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            target.position = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            target.position = Vector3.Lerp(from, to, progress);
            yield return null;
        }

        target.position = to;
    }

    private static Dictionary<SpriteRenderer, Color> CacheRendererColors(SpriteRenderer[] renderers)
    {
        Dictionary<SpriteRenderer, Color> colors = new();
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                colors[renderer] = renderer.color;
            }
        }

        return colors;
    }

    private static void RestoreRendererColors(Dictionary<SpriteRenderer, Color> originalColors)
    {
        foreach (KeyValuePair<SpriteRenderer, Color> entry in originalColors)
        {
            if (entry.Key != null)
            {
                entry.Key.color = entry.Value;
            }
        }
    }

    private static void ApplyTint(SpriteRenderer[] renderers, Dictionary<SpriteRenderer, Color> originalColors, Color tintColor, float tintStrength)
    {
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null || !originalColors.TryGetValue(renderer, out Color baseColor))
            {
                continue;
            }

            Color targetColor = Color.Lerp(baseColor, tintColor, tintStrength);
            targetColor.a = baseColor.a;
            renderer.color = targetColor;
        }
    }

    private void SetCombatantRenderersEnabled(GameObject combatant, bool enabled)
    {
        if (combatant == null)
        {
            return;
        }

        SpriteRenderer[] renderers = combatant.GetComponentsInChildren<SpriteRenderer>(true);
        SetRenderersEnabled(renderers, enabled);
    }

    private static void SetRenderersEnabled(SpriteRenderer[] renderers, bool enabled)
    {
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.enabled = enabled;
            }
        }
    }

    private static void SetRenderersColor(SpriteRenderer[] renderers, Color color)
    {
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            Color updatedColor = color;
            updatedColor.a = renderer.color.a;
            renderer.color = updatedColor;
        }
    }

    private void PlayBattleSfx(string primaryKey, string fallbackKey)
    {
        audioManager ??= UnityEngine.Object.FindFirstObjectByType<AudioManager>();
        if (audioManager == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(primaryKey) && audioManager.HasSfx(primaryKey))
        {
            audioManager.PlaySFX(primaryKey);
            return;
        }

        if (!string.IsNullOrWhiteSpace(fallbackKey))
        {
            audioManager.PlaySFX(fallbackKey);
        }
    }

    private void SetOverworldCreatureSpritesVisible(bool visible)
    {
        if (!visible)
        {
            hiddenCreatureRenderers.Clear();

            MonsterClass[] monsters = UnityEngine.Object.FindObjectsByType<MonsterClass>(FindObjectsSortMode.None);
            foreach (MonsterClass monster in monsters)
            {
                if (monster == null || monster == activeMonster)
                {
                    continue;
                }

                AddCreatureRenderers(monster.gameObject);
            }

            AddGroupedMonsterRenderers();

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

    private void AddGroupedMonsterRenderers()
    {
        Transform[] allTransforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform sceneTransform in allTransforms)
        {
            if (sceneTransform == null || sceneTransform.name != "Monsters")
            {
                continue;
            }

            SpriteRenderer[] renderers = sceneTransform.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (activePlayer != null && renderer.transform.IsChildOf(activePlayer.transform))
                {
                    continue;
                }

                if (activeMonster != null && renderer.transform.IsChildOf(activeMonster.transform))
                {
                    continue;
                }

                if (renderer.enabled && !hiddenCreatureRenderers.Contains(renderer))
                {
                    hiddenCreatureRenderers.Add(renderer);
                }
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
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
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
        Renderer[] renderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
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
