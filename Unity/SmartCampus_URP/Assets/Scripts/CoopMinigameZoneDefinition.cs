using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider))]
public sealed class CoopMinigameZoneDefinition : MonoBehaviour
{
    [Header("Zone Metadata")]
    [SerializeField] [Min(1)] private int miniGameNumber = 1;
    [SerializeField] private string displayName = string.Empty;

    [Header("Validation")]
    [SerializeField] [Min(0.1f)] private float maxAcceptedAccuracyMeters = 15f;
    [SerializeField] [Min(0.01f)] private float insideToleranceMeters = 0.25f;

    [Header("Debug")]
    [SerializeField] private Color gizmoColor = new(0.2f, 0.6f, 1f, 0.25f);
    [SerializeField] private Collider zoneCollider;

    public int MiniGameNumber => miniGameNumber;
    public int MiniGameIndex => Mathf.Max(0, miniGameNumber - 1);
    public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
    public float MaxAcceptedAccuracyMeters => maxAcceptedAccuracyMeters;
    public Collider ZoneCollider => zoneCollider;

    private void Reset()
    {
        EnsureCollider();
    }

    private void OnValidate()
    {
        EnsureCollider();
        miniGameNumber = Mathf.Max(1, miniGameNumber);
        maxAcceptedAccuracyMeters = Mathf.Max(0.1f, maxAcceptedAccuracyMeters);
        insideToleranceMeters = Mathf.Max(0.01f, insideToleranceMeters);
    }

    public bool Contains(Vector3 worldPosition)
    {
        if (zoneCollider == null || !zoneCollider.enabled)
        {
            return false;
        }

        var closestPoint = zoneCollider.ClosestPoint(worldPosition);
        return Vector3.SqrMagnitude(closestPoint - worldPosition) <= insideToleranceMeters * insideToleranceMeters;
    }

    private void OnDrawGizmos()
    {
        if (zoneCollider == null)
        {
            EnsureCollider();
        }

        if (zoneCollider == null)
        {
            return;
        }

        Gizmos.color = gizmoColor;
        if (zoneCollider is BoxCollider boxCollider)
        {
            var matrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            Gizmos.matrix = matrix;
            return;
        }

        Gizmos.DrawWireCube(zoneCollider.bounds.center, zoneCollider.bounds.size);
    }

    private void EnsureCollider()
    {
        zoneCollider ??= GetComponent<BoxCollider>();
        zoneCollider ??= GetComponent<Collider>();
        if (zoneCollider == null)
        {
            zoneCollider = gameObject.AddComponent<BoxCollider>();
        }

        zoneCollider.isTrigger = true;

        if (zoneCollider is BoxCollider boxCollider)
        {
            var size = boxCollider.size;
            boxCollider.size = new Vector3(
                Mathf.Max(1f, size.x),
                Mathf.Max(1f, size.y),
                Mathf.Max(1f, size.z));
        }
    }
}
