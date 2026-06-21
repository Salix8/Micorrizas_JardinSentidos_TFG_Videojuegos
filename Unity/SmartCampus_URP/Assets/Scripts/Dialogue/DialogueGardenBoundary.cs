using UnityEngine;

namespace SmartCampus.Dialogue
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DialogueGardenBoundary : MonoBehaviour
    {
        [SerializeField] [Min(0.01f)] private float insideToleranceMeters = 0.25f;
        [SerializeField] private BoxCollider boundaryCollider;
        [SerializeField] private Color gizmoColor = new(0.2f, 0.8f, 0.35f, 0.25f);

        public Collider BoundaryCollider => boundaryCollider;

        private void Reset()
        {
            EnsureCollider();
        }

        private void OnValidate()
        {
            insideToleranceMeters = Mathf.Max(0.01f, insideToleranceMeters);
            EnsureCollider();
        }

        public bool Contains(Vector3 worldPosition)
        {
            if (boundaryCollider == null || !boundaryCollider.enabled)
            {
                return false;
            }

            var closestPoint = boundaryCollider.ClosestPoint(worldPosition);
            return Vector3.SqrMagnitude(closestPoint - worldPosition) <= insideToleranceMeters * insideToleranceMeters;
        }

        private void OnDrawGizmos()
        {
            EnsureCollider();
            if (boundaryCollider == null)
            {
                return;
            }

            var previousMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(boundaryCollider.center, boundaryCollider.size);
            Gizmos.matrix = previousMatrix;
        }

        private void EnsureCollider()
        {
            boundaryCollider ??= GetComponent<BoxCollider>();
            if (boundaryCollider == null)
            {
                boundaryCollider = gameObject.AddComponent<BoxCollider>();
            }

            boundaryCollider.isTrigger = true;
        }
    }
}
