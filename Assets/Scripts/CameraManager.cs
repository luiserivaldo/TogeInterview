using System.Collections;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;
    public Transform cameraTransform;
    public float moveSpeed = 5f;

    private void Awake()
    {
        // Ensure only one CameraManager exists on the scene
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void MoveCameraTo(Transform targetAnchor)
    {
        StopAllCoroutines();
        StartCoroutine(MoveToPosition(targetAnchor.position));
    }

    private IEnumerator MoveToPosition(Vector3 targetPos)
    {
        targetPos.z = cameraTransform.position.z; // keep original z
        while (Vector3.Distance(cameraTransform.position, targetPos) > 0.05f)
        {
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetPos, Time.deltaTime * moveSpeed);
            yield return null;
        }

        cameraTransform.position = targetPos; // Snap to exact position
    }
}
