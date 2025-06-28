using UnityEngine;

public enum ConnectionDirection
{
    Up,
    Down,
    Left,
    Right
}
[RequireComponent(typeof(BoxCollider))]
public class PathConnection : MonoBehaviour
{
    [Header("Paths Connection Info")]
    public int fromPath;
    public int toPath;
    public float probabilityToChooseSecondary = 0.3f;
    public bool reconnectToMain = true;
    public ConnectionDirection connectionDirection;
    void Reset()
    {
        ApplyColliderSettings();
    }

    void OnEnable()
    {
        Invoke(nameof(ApplyColliderSettings), 5f);
    }

    private void ApplyColliderSettings()
    {
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            col.center = Vector3.zero;
            col.size = new Vector3(3f, 1f, 3f);
        }
    }
    // Prioriza claramente camino principal (pathIndex 1)
    public int GetNextPathIndex()
    {
        if (toPath == 1)
            return toPath;

        return Random.value < probabilityToChooseSecondary ? toPath : fromPath;
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.25f, Vector3.one * 0.5f);

        // Flecha que indica hacia qué camino se conecta
        Vector3 direction = Vector3.zero;
        switch (connectionDirection)
        {
            case ConnectionDirection.Up: direction = Vector3.forward; break;
            case ConnectionDirection.Down: direction = Vector3.back; break;
            case ConnectionDirection.Left: direction = Vector3.left; break;
            case ConnectionDirection.Right: direction = Vector3.right; break;
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * 0.5f, direction * 0.5f);
    }
#endif

}
