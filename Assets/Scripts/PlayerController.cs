using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(GridMovement))]
public class PlayerController : MonoBehaviour
{
    private enum InteractionSelectionState
    {
        None,
        SingleCandidate,
        MultiCandidateReady,
        MultiCandidateSelecting,
    }

    private sealed class InteractionCandidate
    {
        public GameObject RootObject;
        public SpriteRenderer Indicator;
        public Vector3 Position;
        public Action<PlayerClass> Interact;
    }

    [Header("Movement")]
    [SerializeField, Min(0.01f)] public float initialMoveSpeedOverride = 8f;
    [SerializeField] private Transform locationPointer;
    [SerializeField] private SpriteRenderer playerIndicatorRenderer;
    [SerializeField] private float inputDeadZone = 0.1f;

    [Header("Collision Layers")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask monsterLayer;
    [SerializeField] private LayerMask shopLayer;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Interaction Detection")]
    [SerializeField] private float interactionRadius = 0.2f;
    [SerializeField, Min(0.1f)] private float nearbyInteractableRange = 1.05f;

    [Header("Bump Back")]
    [SerializeField] private float bumpDuration = 0.2f;
    [SerializeField] private float inputCooldownDuration = 0.2f;

    private readonly List<InteractionCandidate> nearbyCandidates = new();

    private GridMovement gridMovement;
    private PlayerClass playerClass;

    private float bumpTimer;
    private float inputCooldown;

    private bool isBumping;
    private bool interactionHandledForCurrentStep;
    private bool selectionNavigationHeld;

    private Vector3 bumpStart;
    private Vector3 bumpTarget;

    private int selectedCandidateIndex = -1;
    private InteractionSelectionState interactionSelectionState;

    public LayerMask WallLayer => wallLayer;
    public GridMovement Movement => gridMovement;

    private void Awake()
    {
        gridMovement = GetComponent<GridMovement>();
        playerClass = GetComponent<PlayerClass>();

        gridMovement.MoveSpeed = initialMoveSpeedOverride;
        gridMovement.BlockingLayers = wallLayer;

        AutoAssignPlayerIndicator();
    }

    private void Start()
    {
        if (locationPointer != null)
        {
            locationPointer.SetParent(null);
            locationPointer.position = transform.position;
        }

        gridMovement.SetLastCommittedPosition(transform.position);
        RefreshNearbyInteractables(forceClear: false);
    }

    private void Update()
    {
        if (GameManager.IsGameplayLocked)
        {
            ExitSelectionMode(false);
            RefreshNearbyInteractables(forceClear: true);
            CancelMovementAndSnap();
            return;
        }

        UpdateInputCooldown();

        if (isBumping)
        {
            RefreshNearbyInteractables(forceClear: true);
            UpdateBumpBack();
            return;
        }

        UpdateLocationPointer();
        RefreshNearbyInteractables(forceClear: false);

        if (interactionSelectionState == InteractionSelectionState.MultiCandidateSelecting)
        {
            HandleSelectionModeInput();
            return;
        }

        HandleStationaryInteractionInput();

        if (interactionSelectionState == InteractionSelectionState.MultiCandidateSelecting)
        {
            return;
        }

        CheckCurrentTileInteractions();
        ReadMovementInput();
    }

    public Vector3 GetSafeReturnPosition()
    {
        return gridMovement.SnapToGrid(gridMovement.LastCommittedPosition);
    }

    public void PrepareForBattleReturn()
    {
        inputCooldown = inputCooldownDuration;
        isBumping = false;
        bumpTimer = 0f;

        gridMovement.Stop();

        Vector3 safePosition = GetSafeReturnPosition();

        bumpStart = safePosition;
        bumpTarget = safePosition;

        if (locationPointer != null)
        {
            locationPointer.position = safePosition;
        }
    }

    public void BumpBack()
    {
        inputCooldown = inputCooldownDuration;
        isBumping = true;
        bumpTimer = 0f;

        gridMovement.Stop();

        bumpStart = transform.position;
        bumpTarget = GetSafeReturnPosition();

        if (locationPointer != null)
        {
            locationPointer.position = bumpTarget;
        }
    }

    private void ReadMovementInput()
    {
        if (gridMovement.IsMoving || inputCooldown > 0f)
        {
            return;
        }

        Vector2 movementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if (movementInput.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            return;
        }

        UIManager.Instance.HideMessage();
        UIManager.Instance.HideBountyBoard();

        if (!gridMovement.TryMove(movementInput))
        {
            return;
        }

        interactionHandledForCurrentStep = false;
        ExitSelectionMode(false);
        RefreshNearbyInteractables(forceClear: true);
        UpdateLocationPointer();
    }

    private void HandleStationaryInteractionInput()
    {
        if (!CanUseNearbyInteractables() || !IsInteractionSubmitPressed())
        {
            return;
        }

        if (interactionSelectionState == InteractionSelectionState.SingleCandidate)
        {
            InteractWithCandidate(0);
            return;
        }

        if (interactionSelectionState == InteractionSelectionState.MultiCandidateReady)
        {
            EnterSelectionMode();
        }
    }

    private void HandleSelectionModeInput()
    {
        if (!CanUseNearbyInteractables())
        {
            ExitSelectionMode(false);
            RefreshNearbyInteractables(forceClear: false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.Escape))
        {
            ExitSelectionMode(true);
            return;
        }

        Vector2 navigationInput = GetCardinalInput();
        if (navigationInput.sqrMagnitude < 0.01f)
        {
            selectionNavigationHeld = false;
        }
        else if (!selectionNavigationHeld)
        {
            selectionNavigationHeld = true;
            int direction = (navigationInput.x > 0f || navigationInput.y < 0f) ? 1 : -1;
            MoveSelection(direction);
        }

        if (!IsInteractionSubmitPressed())
        {
            return;
        }

        if (selectedCandidateIndex >= nearbyCandidates.Count)
        {
            ExitSelectionMode(true);
            return;
        }

        InteractWithCandidate(selectedCandidateIndex);
    }

    private void EnterSelectionMode()
    {
        if (nearbyCandidates.Count <= 1)
        {
            return;
        }

        interactionSelectionState = InteractionSelectionState.MultiCandidateSelecting;
        selectedCandidateIndex = 0;
        selectionNavigationHeld = false;
        UpdateIndicatorVisuals();
    }

    private void ExitSelectionMode(bool restoreNearbyState)
    {
        interactionSelectionState = restoreNearbyState
            ? InteractionSelectionState.MultiCandidateReady
            : InteractionSelectionState.None;
        selectedCandidateIndex = -1;
        selectionNavigationHeld = false;
        UpdateIndicatorVisuals();
    }

    private void MoveSelection(int direction)
    {
        int totalOptions = nearbyCandidates.Count + 1;
        if (totalOptions <= 1)
        {
            return;
        }

        if (selectedCandidateIndex < 0)
        {
            selectedCandidateIndex = 0;
        }
        else
        {
            selectedCandidateIndex = (selectedCandidateIndex + direction + totalOptions) % totalOptions;
        }

        UpdateIndicatorVisuals();
    }

    private void InteractWithCandidate(int index)
    {
        if (index < 0 || index >= nearbyCandidates.Count)
        {
            return;
        }

        InteractionCandidate candidate = nearbyCandidates[index];
        inputCooldown = inputCooldownDuration;
        ExitSelectionMode(false);
        RefreshNearbyInteractables(forceClear: true);
        UIManager.Instance.HideMessage();
        UIManager.Instance.HideBountyBoard();
        candidate.Interact?.Invoke(playerClass);
        RefreshNearbyInteractables(forceClear: false);
    }

    private void RefreshNearbyInteractables(bool forceClear)
    {
        if (forceClear || !CanUseNearbyInteractables())
        {
            nearbyCandidates.Clear();
            interactionSelectionState = InteractionSelectionState.None;
            selectedCandidateIndex = -1;
            UpdateIndicatorVisuals();
            return;
        }

        CollectNearbyCandidates();

        if (interactionSelectionState == InteractionSelectionState.MultiCandidateSelecting && nearbyCandidates.Count > 1)
        {
            int maxIndex = nearbyCandidates.Count;
            selectedCandidateIndex = Mathf.Clamp(selectedCandidateIndex, 0, maxIndex);
            UpdateIndicatorVisuals();
            return;
        }

        if (nearbyCandidates.Count == 0)
        {
            interactionSelectionState = InteractionSelectionState.None;
            selectedCandidateIndex = -1;
        }
        else if (nearbyCandidates.Count == 1)
        {
            interactionSelectionState = InteractionSelectionState.SingleCandidate;
            selectedCandidateIndex = 0;
        }
        else
        {
            interactionSelectionState = InteractionSelectionState.MultiCandidateReady;
            selectedCandidateIndex = -1;
        }

        UpdateIndicatorVisuals();
    }

    private void CollectNearbyCandidates()
    {
        nearbyCandidates.Clear();

        int combinedMask = interactableLayer.value | shopLayer.value;
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, nearbyInteractableRange, combinedMask);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        HashSet<GameObject> seenObjects = new();

        foreach (Collider2D hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            GameObject rootObject = hit.gameObject;
            if (!seenObjects.Add(rootObject))
            {
                continue;
            }

            if (rootObject.TryGetComponent(out InteractableObject interactable))
            {
                nearbyCandidates.Add(new InteractionCandidate
                {
                    RootObject = rootObject,
                    Indicator = interactable.IndicatorRenderer,
                    Position = interactable.transform.position,
                    Interact = interactable.TryInteract,
                });
                continue;
            }

            if (rootObject.TryGetComponent(out ShopClass shop))
            {
                if (!shop.CanInteract)
                {
                    UIManager.Instance?.ApplyIndicatorState(
                        shop.IndicatorRenderer,
                        UIManager.InteractIndicatorState.Hidden
                    );

                    continue;
                }

                nearbyCandidates.Add(new InteractionCandidate
                {
                    RootObject = rootObject,
                    Indicator = shop.IndicatorRenderer,
                    Position = shop.transform.position,
                    Interact = shop.TryInteract,
                });
            }
        }

        nearbyCandidates.Sort(CompareCandidatesClockwise);
    }

    private int CompareCandidatesClockwise(InteractionCandidate left, InteractionCandidate right)
    {
        float leftAngle = GetClockwiseAngleFromUp(left.Position - transform.position);
        float rightAngle = GetClockwiseAngleFromUp(right.Position - transform.position);

        int angleComparison = leftAngle.CompareTo(rightAngle);
        if (angleComparison != 0)
        {
            return angleComparison;
        }

        float leftDistance = (left.Position - transform.position).sqrMagnitude;
        float rightDistance = (right.Position - transform.position).sqrMagnitude;
        return leftDistance.CompareTo(rightDistance);
    }

    private void UpdateIndicatorVisuals()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        foreach (InteractionCandidate candidate in nearbyCandidates)
        {
            UIManager.Instance.ApplyIndicatorState(candidate.Indicator, UIManager.InteractIndicatorState.Hidden);
        }

        UIManager.Instance.ApplyIndicatorState(playerIndicatorRenderer, UIManager.InteractIndicatorState.Hidden);

        switch (interactionSelectionState)
        {
            case InteractionSelectionState.SingleCandidate:
                if (nearbyCandidates.Count > 0)
                {
                    UIManager.Instance.ApplyIndicatorState(nearbyCandidates[0].Indicator, UIManager.InteractIndicatorState.Discoverable);
                }
                break;

            case InteractionSelectionState.MultiCandidateReady:
                UIManager.Instance.ApplyIndicatorState(playerIndicatorRenderer, UIManager.InteractIndicatorState.PlayerMultiTarget);
                break;

            case InteractionSelectionState.MultiCandidateSelecting:
                for (int i = 0; i < nearbyCandidates.Count; i++)
                {
                    UIManager.Instance.ApplyIndicatorState(
                        nearbyCandidates[i].Indicator,
                        i == selectedCandidateIndex
                            ? UIManager.InteractIndicatorState.Selected
                            : UIManager.InteractIndicatorState.Discoverable);
                }

                if (selectedCandidateIndex >= nearbyCandidates.Count)
                {
                    UIManager.Instance.ApplyIndicatorState(playerIndicatorRenderer, UIManager.InteractIndicatorState.PlayerMultiTarget);
                }
                break;
        }
    }

    private bool CanUseNearbyInteractables()
    {
        return !GameManager.IsGameplayLocked &&
               !isBumping &&
               !gridMovement.IsMoving &&
               inputCooldown <= 0f;
    }

    private void CheckCurrentTileInteractions()
    {
        if (isBumping || interactionHandledForCurrentStep)
        {
            return;
        }

        Collider2D monsterHit = Physics2D.OverlapCircle(
            transform.position,
            interactionRadius,
            monsterLayer
        );

        if (monsterHit == null || !monsterHit.TryGetComponent(out MonsterClass monster))
        {
            return;
        }

        interactionHandledForCurrentStep = true;

        Debug.Log("Hit a monster.");
        BattleManager.Instance.StartBattle(playerClass, monster);
    }

    private void UpdateInputCooldown()
    {
        if (inputCooldown <= 0f)
        {
            return;
        }

        inputCooldown -= Time.deltaTime;

        if (inputCooldown < 0f)
        {
            inputCooldown = 0f;
        }
    }

    private void UpdateBumpBack()
    {
        bumpTimer += Time.deltaTime;

        float duration = Mathf.Max(bumpDuration, 0.0001f);
        float t = Mathf.Clamp01(bumpTimer / duration);
        float easedTime = Mathf.Sqrt(t);

        transform.position = Vector3.Lerp(bumpStart, bumpTarget, easedTime);

        if (t < 1f)
        {
            return;
        }

        isBumping = false;
        bumpTimer = 0f;

        gridMovement.SetPositionImmediate(bumpTarget, true);
        interactionHandledForCurrentStep = true;

        if (locationPointer != null)
        {
            locationPointer.position = bumpTarget;
        }
    }

    private void UpdateLocationPointer()
    {
        if (locationPointer == null)
        {
            return;
        }

        locationPointer.position = gridMovement.IsMovingStep
            ? gridMovement.StepTarget
            : transform.position;
    }

    private void CancelMovementAndSnap()
    {
        gridMovement.Stop();

        inputCooldown = 0f;
        isBumping = false;
        bumpTimer = 0f;
        interactionHandledForCurrentStep = true;

        Vector3 safePosition = GetSafeReturnPosition();

        gridMovement.SetPositionImmediate(safePosition, true);

        if (locationPointer != null)
        {
            locationPointer.position = safePosition;
        }
    }

    private void AutoAssignPlayerIndicator()
    {
        if (playerIndicatorRenderer != null)
        {
            return;
        }

        Transform indicator = transform.Find("UiIndicator");
        if (indicator != null)
        {
            playerIndicatorRenderer = indicator.GetComponent<SpriteRenderer>();
        }
    }

    private Vector2 GetCardinalInput()
    {
        Vector2 rawInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        if (rawInput.sqrMagnitude < inputDeadZone * inputDeadZone)
        {
            return Vector2.zero;
        }

        if (Mathf.Abs(rawInput.x) > Mathf.Abs(rawInput.y))
        {
            return new Vector2(Mathf.Sign(rawInput.x), 0f);
        }

        return new Vector2(0f, Mathf.Sign(rawInput.y));
    }

    private static float GetClockwiseAngleFromUp(Vector3 offset)
    {
        float angle = Mathf.Atan2(offset.x, offset.y) * Mathf.Rad2Deg;
        return angle < 0f ? angle + 360f : angle;
    }

    private static bool IsInteractionSubmitPressed()
    {
        return Input.GetKeyDown(KeyCode.Z)
            || Input.GetKeyDown(KeyCode.Space)
            || Input.GetButtonDown("Submit");
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        initialMoveSpeedOverride = Mathf.Max(0.01f, initialMoveSpeedOverride);
        inputDeadZone = Mathf.Clamp01(inputDeadZone);
        interactionRadius = Mathf.Max(0.01f, interactionRadius);
        nearbyInteractableRange = Mathf.Max(0.1f, nearbyInteractableRange);
        bumpDuration = Mathf.Max(0.01f, bumpDuration);
        inputCooldownDuration = Mathf.Max(0f, inputCooldownDuration);
        AutoAssignPlayerIndicator();
    }
#endif
}
