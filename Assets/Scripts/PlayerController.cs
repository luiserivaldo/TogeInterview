using UnityEngine;

[RequireComponent(typeof(GridMovement))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.01f)] public float initialMoveSpeedOverride = 8f; // Movement speed is controlled by GridMovement.cs ; this overrides that value
    [SerializeField] private Transform locationPointer;
    [SerializeField] private float inputDeadZone = 0.1f;

    [Header("Collision Layers")]
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask monsterLayer;
    [SerializeField] private LayerMask shopLayer;
    [SerializeField] private LayerMask interactableLayer;

    [Header("Interaction Detection")]
    [SerializeField] private float interactionRadius = 0.2f;

    [Header("Bump Back")]
    [SerializeField] private float bumpDuration = 0.2f;
    [SerializeField] private float inputCooldownDuration = 0.2f;

    private GridMovement gridMovement;

    private float bumpTimer;
    private float inputCooldown;

    private bool isBumping;
    private bool interactionHandledForCurrentStep;

    private Vector3 bumpStart;
    private Vector3 bumpTarget;

    public LayerMask WallLayer => wallLayer;
    public GridMovement Movement => gridMovement;

    private void Awake()
    {
        gridMovement = GetComponent<GridMovement>();

        gridMovement.MoveSpeed = initialMoveSpeedOverride;
        gridMovement.BlockingLayers = wallLayer;
    }

    private void Start()
    {
        if (locationPointer != null)
        {
            locationPointer.SetParent(null);
            locationPointer.position = transform.position;
        }

        gridMovement.SetLastCommittedPosition(
            transform.position
        );
    }

    private void Update()
    {
        if (GameManager.IsGameplayLocked)
        {
            CancelMovementAndSnap();
            return;
        }

        UpdateInputCooldown();

        if (isBumping)
        {
            UpdateBumpBack();
            return;
        }

        UpdateLocationPointer();
        CheckCurrentTileInteractions();
        ReadMovementInput();
    }

    public Vector3 GetSafeReturnPosition()
    {
        return gridMovement.SnapToGrid(
            gridMovement.LastCommittedPosition
        );
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
        if (gridMovement.IsMoving ||
            inputCooldown > 0f)
        {
            return;
        }

        Vector2 movementInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
                                            Input.GetAxisRaw("Vertical")
        );

        if (movementInput.sqrMagnitude <
            inputDeadZone * inputDeadZone)
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
        UpdateLocationPointer();
    }

    private void CheckCurrentTileInteractions()
    {
        if (isBumping ||
            interactionHandledForCurrentStep)
        {
            return;
        }

        Collider2D monsterHit = Physics2D.OverlapCircle(
            transform.position,
            interactionRadius,
            monsterLayer
        );

        if (monsterHit != null &&
            monsterHit.TryGetComponent(
                out MonsterClass monster))
        {
            interactionHandledForCurrentStep = true;

            Debug.Log("Hit a monster.");

            BattleManager.Instance.StartBattle(
                GetComponent<PlayerClass>(),
                                               monster
            );

            return;
        }

        Collider2D shopHit = Physics2D.OverlapCircle(
            transform.position,
            interactionRadius,
            shopLayer
        );

        if (shopHit != null &&
            shopHit.TryGetComponent(
                out ShopClass shop))
        {
            interactionHandledForCurrentStep = true;

            Debug.Log("Hit a shop.");

            shop.TryInteract(
                GetComponent<PlayerClass>()
            );

            BumpBack();
            return;
        }

        Collider2D interactableHit =
        Physics2D.OverlapCircle(
            transform.position,
            interactionRadius,
            interactableLayer
        );

        if (interactableHit == null ||
            !interactableHit.TryGetComponent(
                out InteractableObject interactable))
        {
            return;
        }

        interactionHandledForCurrentStep = true;

        Debug.Log("Hit an interactable object.");

        GameManager gameManager =
        Object.FindFirstObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.InteractWithObject(interactable);
        }

        if (GameManager.IsGameplayLocked)
        {
            return;
        }

        if (interactable.objectType ==
            InteractableObject.InteractableType.Sign)
        {
            UIManager.Instance.ShowMessage(
                interactable.GetSignMessage()
            );

            if (interactable.signType ==
                InteractableObject.SignType.BountyBoardSign)
            {
                UIManager.Instance.ShowBountyBoard();
            }
        }

        BumpBack();
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

        float duration = Mathf.Max(
            bumpDuration,
            0.0001f
        );

        float t = Mathf.Clamp01(
            bumpTimer / duration
        );

        float easedTime = Mathf.Sqrt(t);

        transform.position = Vector3.Lerp(
            bumpStart,
            bumpTarget,
            easedTime
        );

        if (t < 1f)
        {
            return;
        }

        isBumping = false;
        bumpTimer = 0f;

        gridMovement.SetPositionImmediate(
            bumpTarget,
            true
        );

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

        locationPointer.position =
        gridMovement.IsMovingStep
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

        Vector3 safePosition =
        GetSafeReturnPosition();

        gridMovement.SetPositionImmediate(
            safePosition,
            true
        );

        if (locationPointer != null)
        {
            locationPointer.position =
            safePosition;
        }
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        initialMoveSpeedOverride = Mathf.Max(0.01f, initialMoveSpeedOverride);
        inputDeadZone = Mathf.Clamp01(inputDeadZone);
        interactionRadius =
        Mathf.Max(0.01f, interactionRadius);
        bumpDuration = Mathf.Max(0.01f, bumpDuration);
        inputCooldownDuration =
        Mathf.Max(0f, inputCooldownDuration);
    }
    #endif
}
