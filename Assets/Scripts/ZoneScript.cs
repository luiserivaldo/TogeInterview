using UnityEngine;

public class ZoneScript : MonoBehaviour
{
    public Transform targetCameraAnchor; // assign the zone's camera anchor here
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            CameraManager.Instance.MoveCameraTo(targetCameraAnchor);
        }
    }
}
