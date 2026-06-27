using System;
using UnityEngine;

[DisallowMultipleComponent]
public class GridMovement : MonoBehaviour
{
    [Header("Grid Movement")]
    [SerializeField, Min(0.01f)] private float gridSize = 1f;
    [SerializeField, Min(0.01f)] private float moveSpeed = 5f;

    [Header("Collision")]
    [SerializeField] private LayerMask blockingLayers;
    [SerializeField, Min(0.01f)] private float collisionRadius = 0.2f;

    [Header("Grid Alignment")]
    [Tooltip(
        "Preserves the actor's initial position as its grid offset. " +
        "Enable this when actors can begin on half-unit coordinates."
    )]
    [SerializeField] private bool preserveInitialGridOffset = true;

    private Vector3 gridOrigin;
    private Vector3 stepTarget;
    private Vector3 finalTarget;
    private Vector3 lastCommittedPosition;

    private bool movingStep;
    private bool movingToTarget;

    public bool IsMoving => movingStep || movingToTarget;
    public bool IsMovingStep => movingStep;
    public bool IsMovingToTarget => movingToTarget;

    public float GridSize => gridSize;
    public Vector3 StepTarget => stepTarget;
    public Vector3 FinalTarget => finalTarget;
    public Vector3 LastCommittedPosition => lastCommittedPosition;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0.01f, value);
    }

    public LayerMask BlockingLayers
    {
        get => blockingLayers;
        set => blockingLayers = value;
    }

    public event Action MovementCompleted;
    public event Action MovementBlocked;

    private void Awake()
    {
        gridOrigin = preserveInitialGridOffset
        ? transform.position
        : Vector3.zero;

        stepTarget = transform.position;
        finalTarget = transform.position;
        lastCommittedPosition = transform.position;
    }

    private void Update()
    {
        if (movingStep)
        {
            UpdateCurrentStep();
            return;
        }

        if (movingToTarget)
        {
            RequestNextDestinationStep();
        }
    }

    /// <summary>
    /// Requests one grid step from any two-dimensional movement vector.
    /// The source may be keyboard axes, a controller stick, a D-pad,
    /// touch input, AI, or another gameplay system.
    /// </summary>
    public bool TryMove(Vector2 movementInput)
    {
        if (IsMoving)
        {
            return false;
        }

        return TryStartVectorStep(movementInput);
    }

    /// <summary>
    /// Moves through repeated cardinal grid steps until the supplied world
    /// position is reached.
    /// </summary>
    public bool MoveToGridPosition(Vector3 destination)
    {
        if (IsMoving)
        {
            return false;
        }

        finalTarget = SnapToGrid(destination);
        finalTarget.z = transform.position.z;

        if (HasReached(finalTarget))
        {
            transform.position = finalTarget;
            stepTarget = finalTarget;
            lastCommittedPosition = finalTarget;

            MovementCompleted?.Invoke();
            return true;
        }

        movingToTarget = true;
        RequestNextDestinationStep();
        return true;
    }

    /// <summary>
    /// Stops current movement at the actor's current world position.
    /// </summary>
    public void Stop()
    {
        movingStep = false;
        movingToTarget = false;

        stepTarget = transform.position;
        finalTarget = transform.position;
    }

    /// <summary>
    /// Stops movement and places the actor immediately at a world position.
    /// </summary>
    public void SetPositionImmediate(
        Vector3 position,
        bool updateCommittedPosition = true)
    {
        Stop();

        transform.position = position;
        stepTarget = position;
        finalTarget = position;

        if (updateCommittedPosition)
        {
            lastCommittedPosition = position;
        }
    }

    /// <summary>
    /// Overrides the last safe grid position used by return and bump logic.
    /// </summary>
    public void SetLastCommittedPosition(Vector3 position)
    {
        lastCommittedPosition = position;
    }

    public Vector3 SnapToGrid(Vector3 position)
    {
        float originX = preserveInitialGridOffset ? gridOrigin.x : 0f;
        float originY = preserveInitialGridOffset ? gridOrigin.y : 0f;

        float snappedX =
        Mathf.Round((position.x - originX) / gridSize) *
        gridSize +
        originX;

        float snappedY =
        Mathf.Round((position.y - originY) / gridSize) *
        gridSize +
        originY;

        return new Vector3(
            snappedX,
            snappedY,
            position.z
        );
    }

    private void UpdateCurrentStep()
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            stepTarget,
            moveSpeed * Time.deltaTime
        );

        if (!HasReached(stepTarget))
        {
            return;
        }

        transform.position = stepTarget;
        movingStep = false;

        if (!movingToTarget)
        {
            MovementCompleted?.Invoke();
        }
    }

    private void RequestNextDestinationStep()
    {
        Vector3 difference = finalTarget - transform.position;

        bool reachedX = Mathf.Abs(difference.x) <= 0.001f;
        bool reachedY = Mathf.Abs(difference.y) <= 0.001f;

        if (reachedX && reachedY)
        {
            transform.position = finalTarget;
            stepTarget = finalTarget;
            lastCommittedPosition = finalTarget;

            movingStep = false;
            movingToTarget = false;

            MovementCompleted?.Invoke();
            return;
        }

        Vector2 movementVector = new Vector2(
            difference.x,
            difference.y
        );

        if (TryStartVectorStep(movementVector))
        {
            return;
        }

        movingStep = false;
        movingToTarget = false;
        finalTarget = transform.position;
        stepTarget = transform.position;

        Debug.LogWarning(
            $"{name} could not continue toward its grid destination."
        );

        MovementBlocked?.Invoke();
    }

    private bool TryStartVectorStep(Vector2 movementInput)
    {
        if (movingStep || movementInput.sqrMagnitude < 0.01f)
        {
            return false;
        }

        Vector2 cardinalDirection =
        GetCardinalDirection(movementInput);

        Vector3 worldDirection = new Vector3(
            cardinalDirection.x,
            cardinalDirection.y,
            0f
        );

        Vector3 targetPosition =
        transform.position +
        worldDirection * gridSize;

        return BeginStep(targetPosition);
    }

    private bool BeginStep(Vector3 targetPosition)
    {
        targetPosition = SnapToGrid(targetPosition);
        targetPosition.z = transform.position.z;

        if (IsPositionBlocked(targetPosition))
        {
            return false;
        }

        lastCommittedPosition = transform.position;
        stepTarget = targetPosition;
        movingStep = true;

        return true;
    }

    private bool IsPositionBlocked(Vector3 position)
    {
        if (blockingLayers.value == 0)
        {
            return false;
        }

        return Physics2D.OverlapCircle(
            position,
            collisionRadius,
            blockingLayers
        ) != null;
    }

    private bool HasReached(Vector3 position)
    {
        return (transform.position - position).sqrMagnitude
        <= 0.0001f;
    }

    private static Vector2 GetCardinalDirection(Vector2 input)
    {
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return new Vector2(
                Mathf.Sign(input.x),
                               0f
            );
        }

        return new Vector2(
            0f,
            Mathf.Sign(input.y)
        );
    }

    #if UNITY_EDITOR
    private void OnValidate()
    {
        gridSize = Mathf.Max(0.01f, gridSize);
        moveSpeed = Mathf.Max(0.01f, moveSpeed);
        collisionRadius = Mathf.Max(0.01f, collisionRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            Application.isPlaying ? stepTarget : transform.position,
            collisionRadius
        );
    }
    #endif
}
