using System.Collections;
using UnityEngine;

public class NPCPointerMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float arrivalThreshold = 0.02f;

    private Transform locationPointer;

    private void Awake()
    {
        EnsurePointer();
    }

    public IEnumerator MoveTo(Vector3 destination)
    {
        EnsurePointer();

        Vector3 snappedDestination = SnapToGrid(destination);
        locationPointer.position = snappedDestination;

        while (Vector3.Distance(transform.position, locationPointer.position) > arrivalThreshold)
        {
            transform.position = Vector3.MoveTowards(transform.position, locationPointer.position, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = snappedDestination;
        locationPointer.position = snappedDestination;
    }

    private void EnsurePointer()
    {
        if (locationPointer != null)
        {
            return;
        }

        GameObject pointerObject = new GameObject($"{name}_CutscenePointer");
        pointerObject.hideFlags = HideFlags.HideInHierarchy;
        locationPointer = pointerObject.transform;
        locationPointer.position = SnapToGrid(transform.position);
    }

    private static Vector3 SnapToGrid(Vector3 position)
    {
        return new Vector3(
            Mathf.Round(position.x),
            Mathf.Round(position.y),
            position.z);
    }
}
