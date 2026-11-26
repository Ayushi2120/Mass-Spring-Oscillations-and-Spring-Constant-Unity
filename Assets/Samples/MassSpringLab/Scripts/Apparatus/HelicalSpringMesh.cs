using UnityEngine;

namespace MassSpringLab.Apparatus
{
    /// <summary>
    /// Generates a 3D tubular helical spring mesh between a fixed top local Y (0)
    /// and a target bottom attachment transform.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class HelicalSpringMesh : MonoBehaviour
    {
        [Header("Geometry")]
        [SerializeField] private int coils = 12;
        [SerializeField] private float coilRadius = 0.022f;
        [SerializeField] private float wireRadius = 0.0028f;
        [SerializeField] private int segmentsPerCoil = 20;
        [SerializeField] private int radialSegments = 8;
        [SerializeField] private float defaultLength = 0.12f;

        [Header("Endpoints")]
        [SerializeField] private Transform bottomHook;

        private Mesh _mesh;
        private float _lastLength = -1f;

        private void OnEnable()
        {
            EnsureMesh();
            RebuildIfNeeded(force: true);
        }

        private void LateUpdate()
        {
            RebuildIfNeeded(force: false);
        }

        public void RebuildIfNeeded(bool force = false)
        {
            float targetLength = defaultLength;
            if (bottomHook != null)
            {
                Vector3 localBottom = transform.InverseTransformPoint(bottomHook.position);
                targetLength = Mathf.Max(0.04f, -localBottom.y);
            }

            if (!force && Mathf.Abs(targetLength - _lastLength) < 0.0002f)
            {
                return;
            }

            _lastLength = targetLength;
            BuildHelixMesh(targetLength);
        }

        private void EnsureMesh()
        {
            MeshFilter mf = GetComponent<MeshFilter>();
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "HelicalSpringProceduralMesh" };
                _mesh.MarkDynamic();
                mf.sharedMesh = _mesh;
            }
        }

        private void BuildHelixMesh(float length)
        {
            EnsureMesh();

            int totalSteps = Mathf.Max(16, coils * segmentsPerCoil);
            int ringVerts = radialSegments + 1;
            int vertCount = (totalSteps + 1) * ringVerts;

            Vector3[] vertices = new Vector3[vertCount];
            Vector3[] normals = new Vector3[vertCount];
            int[] triangles = new int[totalSteps * radialSegments * 6];

            float totalAngle = coils * Mathf.PI * 2f;

            for (int i = 0; i <= totalSteps; i++)
            {
                float t = (float)i / totalSteps;
                float angle = t * totalAngle;
                float y = -t * length;

                Vector3 center = new Vector3(
                    Mathf.Cos(angle) * coilRadius,
                    y,
                    Mathf.Sin(angle) * coilRadius);

                // Tangent along the helix
                float dAngle = totalAngle;
                Vector3 tangent = new Vector3(
                    -Mathf.Sin(angle) * coilRadius * dAngle,
                    -length,
                    Mathf.Cos(angle) * coilRadius * dAngle).normalized;

                Vector3 radialOut = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)).normalized;
                Vector3 binormal = Vector3.Cross(tangent, radialOut).normalized;
                Vector3 normalRef = Vector3.Cross(binormal, tangent).normalized;

                int baseIdx = i * ringVerts;
                for (int r = 0; r <= radialSegments; r++)
                {
                    float phi = ((float)r / radialSegments) * Mathf.PI * 2f;
                    Vector3 offsetDir = (normalRef * Mathf.Cos(phi) + binormal * Mathf.Sin(phi)).normalized;
                    vertices[baseIdx + r] = center + offsetDir * wireRadius;
                    normals[baseIdx + r] = offsetDir;
                }
            }

            int triIdx = 0;
            for (int i = 0; i < totalSteps; i++)
            {
                int row0 = i * ringVerts;
                int row1 = (i + 1) * ringVerts;
                for (int r = 0; r < radialSegments; r++)
                {
                    int a = row0 + r;
                    int b = row0 + r + 1;
                    int c = row1 + r;
                    int d = row1 + r + 1;

                    triangles[triIdx++] = a;
                    triangles[triIdx++] = c;
                    triangles[triIdx++] = b;

                    triangles[triIdx++] = b;
                    triangles[triIdx++] = c;
                    triangles[triIdx++] = d;
                }
            }

            _mesh.Clear();
            _mesh.vertices = vertices;
            _mesh.normals = normals;
            _mesh.triangles = triangles;
            _mesh.RecalculateBounds();
        }
    }
}
