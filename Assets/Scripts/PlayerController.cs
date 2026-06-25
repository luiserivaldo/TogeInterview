using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public enum ControlMode
    {
        Player,
        Locked,
        Scripted
    }

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
    private readonly Queue<Vector3> scriptedPath = new Queue<Vector3>();
    private Action scriptedMoveCompleted;
    private ControlMode controlMode = ControlMode.Player;
    private bool scriptedMonsterCollisionEnabled;

    void Start()
    {
        locationPointer.parent = null; // move away from parent to allow easier transform
        lastPosition = transform.position; // Initialize lastPosition
        movementDirection = Vector3.zero;
    }

    void Update()
    {
        float movementAmount = moveSpeed * Time.deltaTime;

        if (inputCooldown > 0f)
        {
            inputCooldown -= Time.deltaTime;
        }

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

        if (controlMode == ControlMode.Scripted)
        {
            AdvanceScriptedMovement();
        }

        if (controlMode == ControlMode.Player &&
            Vector3.Distance(transform.position, locationPointer.position) <= .05f &&
            inputCooldown <= 0f)
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

        if (!isBumping && controlMode != ControlMode.Locked)
        {
            bool allowMonsterCollision = controlMode == ControlMode.Player || scriptedMonsterCollisionEnabled;
            if (allowMonsterCollision)
            {
                Collider2D monsterHit = Physics2D.OverlapCircle(transform.position, 0.2f, monsterLayer);
                if (monsterHit != null && monsterHit.TryGetComponent(out MonsterClass monster))
                {
                    Debug.Log("Hit a monster.");
                    GameManager.Instance.StartCombat(GetComponent<PlayerClass>(), monster);
                    BumpBack();
                    ClearScriptedMovement();
                }
            }

            if (controlMode == ControlMode.Player)
            {
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
                    GameManager.Instance.InteractWithObject(interactable);

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

    public ControlMode CurrentControlMode => controlMode;
    public LayerMask WallLayer => wallLayer;
    public LayerMask MonsterLayer => monsterLayer;
    public LayerMask ShopLayer => shopLayer;
    public LayerMask InteractableLayer => interactableLayer;
    public Vector3 GridPosition => locationPointer.position;
    public bool IsBusy => isBumping || Vector3.Distance(transform.position, locationPointer.position) > 0.05f || scriptedPath.Count > 0;

    public void SetControlMode(ControlMode mode)
    {
        controlMode = mode;
        if (mode != ControlMode.Scripted)
        {
            scriptedPath.Clear();
            scriptedMoveCompleted = null;
            scriptedMonsterCollisionEnabled = false;
        }
    }

    public void MoveToPosition(Vector3 target, Action onComplete = null, bool allowMonsterCollision = false)
    {
        List<Vector3> generatedPath = GeneratePath(locationPointer.position, target);
        MoveAlongPath(generatedPath, onComplete, allowMonsterCollision);
    }

    public void MoveAlongPath(IList<Vector3> path, Action onComplete = null, bool allowMonsterCollision = false)
    {
        scriptedPath.Clear();
        if (path != null)
        {
            for (int i = 0; i < path.Count; i++)
            {
                scriptedPath.Enqueue(path[i]);
            }
        }

        scriptedMoveCompleted = onComplete;
        scriptedMonsterCollisionEnabled = allowMonsterCollision;
        controlMode = ControlMode.Scripted;
        AdvanceScriptedMovement();
    }

    public void ClearScriptedMovement()
    {
        scriptedPath.Clear();
        scriptedMoveCompleted = null;
        scriptedMonsterCollisionEnabled = false;
        if (controlMode == ControlMode.Scripted)
        {
            controlMode = ControlMode.Locked;
        }
    }

    public bool CanOccupy(Vector3 position, bool ignoreMonsters = false)
    {
        if (Physics2D.OverlapCircle(position, 0.2f, wallLayer))
        {
            return false;
        }

        if (Physics2D.OverlapCircle(position, 0.2f, shopLayer))
        {
            return false;
        }

        if (Physics2D.OverlapCircle(position, 0.2f, interactableLayer))
        {
            return false;
        }

        if (!ignoreMonsters && Physics2D.OverlapCircle(position, 0.2f, monsterLayer))
        {
            return false;
        }

        return true;
    }

    private void AdvanceScriptedMovement()
    {
        if (controlMode != ControlMode.Scripted)
        {
            return;
        }

        if (Vector3.Distance(transform.position, locationPointer.position) > 0.05f || isBumping)
        {
            return;
        }

        if (scriptedPath.Count > 0)
        {
            Vector3 nextPosition = SnapToGrid(scriptedPath.Dequeue());
            lastPosition = locationPointer.position;
            movementDirection = nextPosition - locationPointer.position;
            locationPointer.position = nextPosition;
            return;
        }

        Action completed = scriptedMoveCompleted;
        scriptedMoveCompleted = null;
        scriptedMonsterCollisionEnabled = false;
        controlMode = ControlMode.Locked;
        completed?.Invoke();
    }

    private List<Vector3> GeneratePath(Vector3 start, Vector3 target)
    {
        List<Vector3> path = new List<Vector3>();
        Vector3 cursor = SnapToGrid(start);
        Vector3 snappedTarget = SnapToGrid(target);

        while (!Mathf.Approximately(cursor.x, snappedTarget.x))
        {
            cursor += new Vector3(Mathf.Sign(snappedTarget.x - cursor.x), 0f, 0f);
            path.Add(cursor);
        }

        while (!Mathf.Approximately(cursor.y, snappedTarget.y))
        {
            cursor += new Vector3(0f, Mathf.Sign(snappedTarget.y - cursor.y), 0f);
            path.Add(cursor);
        }

        return path;
    }

    private Vector3 SnapToGrid(Vector3 pos)
    {
        return new Vector3(
            Mathf.Round(pos.x * 2f) / 2f,
            Mathf.Round(pos.y * 2f) / 2f,
            pos.z
        );
    }
}
