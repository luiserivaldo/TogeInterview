using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 10f;
    [SerializeField] private Transform locationPointer;
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask monsterLayer;
    [SerializeField] private LayerMask shopLayer;
    [SerializeField] private LayerMask interactableLayer;
    private Vector3 lastPosition;
    private Vector3 movementDirection;
    private float bumpTimer = 0f;
    private float bumpDuration = 0.2f;
    private bool isBumping = false;
    private Vector3 bumpStart;
    private Vector3 bumpTarget;
    private float inputCooldown = 0f;
    private float inputCooldownDuration = 0.2f;

    public LayerMask WallLayer => wallLayer;

    void Start()
    {
        locationPointer.parent = null;
        lastPosition = transform.position;
        movementDirection = Vector3.zero;
    }

    void Update()
    {
        if (GameManager.IsGameplayLocked)
        {
            CancelMovementAndSnap();
            return;
        }

        float movementAmount = moveSpeed * Time.deltaTime;

        if (inputCooldown > 0f)
        {
            inputCooldown -= Time.deltaTime;
        }

        if (isBumping)
        {
            bumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(bumpTimer / bumpDuration);
            float ease = Mathf.Pow(t, 0.5f);
            transform.position = Vector3.Lerp(bumpStart, bumpTarget, ease);

            if (t >= 1f)
            {
                isBumping = false;
                bumpTimer = 0f;
                transform.position = bumpTarget;
                locationPointer.position = bumpTarget;
            }

            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, locationPointer.position, movementAmount);

        if (Vector3.Distance(transform.position, locationPointer.position) <= .05f && !isBumping && inputCooldown <= 0f)
        {
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input != Vector2.zero)
            {
                UIManager.Instance.HideMessage();
                UIManager.Instance.HideBountyBoard();
            }

            if (Mathf.Abs(horizontalInput) == 1f)
            {
                Vector3 targetPos = locationPointer.position + new Vector3(horizontalInput, 0f, 0f);
                if (!Physics2D.OverlapCircle(targetPos, 0.2f, wallLayer))
                {
                    lastPosition = locationPointer.position;
                    movementDirection = new Vector3(horizontalInput, 0f, 0f);
                    locationPointer.position = targetPos;
                }
            }
            else if (Mathf.Abs(verticalInput) == 1f)
            {
                Vector3 targetPos = locationPointer.position + new Vector3(0f, verticalInput, 0f);
                if (!Physics2D.OverlapCircle(targetPos, 0.2f, wallLayer))
                {
                    lastPosition = locationPointer.position;
                    movementDirection = new Vector3(0f, verticalInput, 0f);
                    locationPointer.position = targetPos;
                }
            }
        }

        if (!isBumping)
        {
            Collider2D monsterHit = Physics2D.OverlapCircle(transform.position, 0.2f, monsterLayer);
            if (monsterHit != null && monsterHit.TryGetComponent(out MonsterClass monster))
            {
                Debug.Log("Hit a monster.");
                BattleManager.Instance.StartBattle(GetComponent<PlayerClass>(), monster);
                return;
            }

            Collider2D shopHit = Physics2D.OverlapCircle(transform.position, 0.2f, shopLayer);
            if (shopHit != null && shopHit.TryGetComponent(out ShopClass shop))
            {
                Debug.Log("Hit a shop.");
                shop.TryInteract(GetComponent<PlayerClass>());
                BumpBack();
            }

            Collider2D interactableHit = Physics2D.OverlapCircle(transform.position, 0.2f, interactableLayer);
            if (interactableHit != null && interactableHit.TryGetComponent(out InteractableObject interactable))
            {
                Debug.Log("Hit an interactable object.");
                GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
                if (gameManager != null)
                {
                    gameManager.InteractWithObject(interactable);
                }

                if (GameManager.IsGameplayLocked)
                {
                    return;
                }

                if (interactable.objectType == InteractableObject.InteractableType.Sign)
                {
                    UIManager.Instance.ShowMessage(interactable.GetSignMessage());
                    if (interactable.signType == InteractableObject.SignType.BountyBoardSign)
                    {
                        UIManager.Instance.ShowBountyBoard();
                    }
                }

                BumpBack();
            }
        }
    }

    public Vector3 GetSafeReturnPosition()
    {
        return SnapToGrid(lastPosition);
    }

    public void PrepareForBattleReturn()
    {
        inputCooldown = inputCooldownDuration;
        isBumping = false;
        bumpTimer = 0f;

        Vector3 safePosition = GetSafeReturnPosition();
        bumpStart = safePosition;
        bumpTarget = safePosition;
        locationPointer.position = safePosition;
    }

    private bool MonsterAtPosition(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, 0.2f);
        if (hit != null && hit.GetComponent<MonsterClass>() != null)
        {
            return true;
        }
        return false;
    }

    private bool ShopAtPosition(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, 0.2f);
        if (hit != null && hit.GetComponent<ShopManager>() != null)
        {
            return true;
        }
        return false;
    }

    public void BumpBack()
    {
        inputCooldown = inputCooldownDuration;
        isBumping = true;
        bumpStart = transform.position;
        bumpTarget = SnapToGrid(lastPosition);

        locationPointer.position = bumpTarget;
        bumpTimer = 0f;
    }

    private Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x * 2f) / 2f,
            Mathf.Round(pos.y * 2f) / 2f,
            pos.z
        );
    }

    private void CancelMovementAndSnap()
    {
        inputCooldown = 0f;
        isBumping = false;
        bumpTimer = 0f;
        movementDirection = Vector3.zero;

        Vector3 snappedPosition = SnapToGrid(transform.position);
        transform.position = snappedPosition;

        if (locationPointer != null)
        {
            locationPointer.position = snappedPosition;
        }
    }
}
