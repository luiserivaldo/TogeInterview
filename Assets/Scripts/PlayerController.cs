using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] public float moveSpeed = 10f; // player speed, default 10f
    [SerializeField] private Transform locationPointer;  // pointer for grid
    [SerializeField] private LayerMask wallLayer;
    [SerializeField] private LayerMask monsterLayer;
    [SerializeField] private LayerMask shopLayer;
    [SerializeField] private LayerMask interactableLayer;
    private Vector3 lastPosition;
    private Vector3 movementDirection; // Track the direction of movement for proper bump back
    private float bumpTimer = 0f;
    private float bumpDuration = 0.2f;
    private bool isBumping = false;
    private Vector3 bumpStart;
    private Vector3 bumpTarget;
    private float inputCooldown = 0f;  // cooldown between bumps to prevent re-input
    private float inputCooldownDuration = 0.2f;

    void Start()
    {
        locationPointer.parent = null; // move away from parent to allow easier transform
        lastPosition = transform.position; // Initialize lastPosition
        movementDirection = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        // set speed of movement towards tiles
        float movementAmount = moveSpeed * Time.deltaTime;

        // timer to block input
        if (inputCooldown > 0f)
        {
            inputCooldown -= Time.deltaTime;
        }

        // If currently bumping, interpolate and return early
        if (isBumping)
        {
            bumpTimer += Time.deltaTime;
            float t = Mathf.Clamp01(bumpTimer / bumpDuration);
            float ease = Mathf.Pow(t, 0.5f); // smoother deceleration
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

        // movement per input. 0.5 = 1 tile 
        if (Vector3.Distance(transform.position, locationPointer.position) <= .05f && !isBumping && inputCooldown <= 0f)
        {
            // register direction through input
            float horizontalInput = Input.GetAxisRaw("Horizontal");
            float verticalInput = Input.GetAxisRaw("Vertical");

            // Disable UI texts on movement
            Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input != Vector2.zero)
            {
                UIManager.Instance.HideMessage(); // Hide sign text on player movement
                UIManager.Instance.HideBountyBoard();
            }

            // move in X axis based on horizontal input, prioritise horizontal movement over vertical
            if (Mathf.Abs(horizontalInput) == 1f)
            {
                Vector3 targetPos = locationPointer.position + new Vector3(horizontalInput, 0f, 0f);
                if (!Physics2D.OverlapCircle(targetPos, 0.2f, wallLayer))
                {
                    lastPosition = locationPointer.position; // Store current position before moving
                    movementDirection = new Vector3(horizontalInput, 0f, 0f); // Store movement direction
                    locationPointer.position = targetPos;
                }
            }
            // move in Y axis based on vertical input
            else if (Mathf.Abs(verticalInput) == 1f)
            {
                Vector3 targetPos = locationPointer.position + new Vector3(0f, verticalInput, 0f);
                if (!Physics2D.OverlapCircle(targetPos, 0.2f, wallLayer))
                {
                    lastPosition = locationPointer.position; // Store current position before moving
                    movementDirection = new Vector3(0f, verticalInput, 0f); // Store movement direction
                    locationPointer.position = targetPos;
                }
            }
        }

        // Check for collisions continuously while moving (allowing slight overlap)
        if (!isBumping)
        {
            // check for monster collision 
            Collider2D monsterHit = Physics2D.OverlapCircle(transform.position, 0.2f, monsterLayer);
            if (monsterHit != null && monsterHit.TryGetComponent(out MonsterClass monster))
            {
                Debug.Log("Hit a monster.");
                GameManager.Instance.StartCombat(GetComponent<PlayerClass>(), monster);
                BumpBack();
            }

            // check for shop collision 
            Collider2D shopHit = Physics2D.OverlapCircle(transform.position, 0.2f, shopLayer);
            if (shopHit != null && shopHit.TryGetComponent(out ShopClass shop))
            {
                Debug.Log("Hit a shop.");
                shop.TryInteract(GetComponent<PlayerClass>());
                BumpBack();
            }

            // check for interactable collision 
            Collider2D interactableHit = Physics2D.OverlapCircle(transform.position, 0.2f, interactableLayer);
            if (interactableHit != null && interactableHit.TryGetComponent(out InteractableObject interactable))
            {
                Debug.Log("Hit an interactable object.");
                GameManager.Instance.InteractWithObject(interactable);

                // Only show message if it's a sign
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

    // Detects whether player collides with a MonsterClass GameObject
    private bool MonsterAtPosition(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, 0.2f);
        if (hit != null && hit.GetComponent<MonsterClass>() != null)
        {
            return true;
        }
        return false;
    }

    // Detects whether player collides with a Shop GameObject
    private bool ShopAtPosition(Vector3 position)
    {
        Collider2D hit = Physics2D.OverlapCircle(position, 0.2f);
        if (hit != null && hit.GetComponent<ShopManager>() != null)
        {
            return true;
        }
        return false;
    }

    // "Bump" effect - bumps back to original position before the movement
    public void BumpBack()
    {
        inputCooldown = inputCooldownDuration;
        isBumping = true;
        bumpStart = transform.position; // Start from current overlapping position
        bumpTarget = SnapToGrid(lastPosition); // Go back to the position before the movement
        
        // Update locationPointer to the bump target
        locationPointer.position = bumpTarget;
        bumpTimer = 0f;
    }

    // Ensures player bounces back to grid
    private Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x * 2f) / 2f,
            Mathf.Round(pos.y * 2f) / 2f,
            pos.z
        );
    }
}