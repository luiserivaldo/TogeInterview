using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class NpcCutsceneMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    private Coroutine moveRoutine;
    private SpriteRenderer spriteRenderer;
    private Vector3 homePosition;

    public Vector3 HomePosition => homePosition;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        homePosition = transform.position;
    }

    public void SetHomePosition(Vector3 home)
    {
        homePosition = home;
        transform.position = home;
    }

    public void MoveTo(Vector3 target, Action onComplete = null)
    {
        MoveAlongPath(new List<Vector3> { target }, onComplete);
    }

    public void MoveAlongPath(IList<Vector3> positions, Action onComplete = null)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine(positions, onComplete));
    }

    public void ReturnHome(Action onComplete = null)
    {
        MoveTo(homePosition, onComplete);
    }

    private IEnumerator MoveRoutine(IList<Vector3> positions, Action onComplete)
    {
        if (positions != null)
        {
            for (int i = 0; i < positions.Count; i++)
            {
                Vector3 target = positions[i];
                while (Vector3.Distance(transform.position, target) > 0.02f)
                {
                    Vector3 previous = transform.position;
                    transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                    UpdateFacing(transform.position - previous);
                    yield return null;
                }

                transform.position = target;
                yield return null;
            }
        }

        moveRoutine = null;
        onComplete?.Invoke();
    }

    private void UpdateFacing(Vector3 direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction.x) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX = direction.x > 0f;
    }
}
