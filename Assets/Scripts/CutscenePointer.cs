using UnityEngine;

[DisallowMultipleComponent]
public class CutscenePointer : MonoBehaviour
{
    private CutsceneEvent ownerEvent;
    private CutsceneManager ownerManager;

    public Vector3 PointerPosition => transform.position;

    public void Bind(CutsceneEvent cutsceneEvent, CutsceneManager cutsceneManager)
    {
        ownerEvent = cutsceneEvent;
        ownerManager = cutsceneManager;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ownerEvent != null && ownerManager != null)
        {
            ownerManager.TryStartEvent(ownerEvent, this, other);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
