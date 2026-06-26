using UnityEngine;

[DisallowMultipleComponent]
public class CutsceneMovePointer : MonoBehaviour
{
    public Vector3 Position => transform.position;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.25f);
    }
}
