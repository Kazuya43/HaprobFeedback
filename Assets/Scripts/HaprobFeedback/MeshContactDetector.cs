using System.Collections.Generic;
using UnityEngine;

namespace HaprobFeedback
{
    /// <summary>接触判定の結果。</summary>
    public struct ContactResult
    {
        public bool touching;
        public Vector3 point;      // 接触代表点(ワールド)
        public Vector3 normal;     // メッシュ表面の法線(ワールド)
        public float depth;        // 最大めり込み量(m)。表面上/外側の近接は0
        public int contactCount;   // 接触と判定された点数
    }

    /// <summary>
    /// オブジェクトのメッシュ(三角形)と手の点群の距離から接触を判定する。
    /// 各点について最近接三角形点を求め、距離 &lt; contactDistance(または内側)なら接触。
    /// 非凸メッシュも可。三角形数×点数の総当たりなので、低ポリ(~数千三角形)向け。
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    public class MeshContactDetector : MonoBehaviour
    {
        public MonoBehaviour handPointSource; // IHandPointSource を実装したもの

        [Tooltip("この距離以内の点を接触とみなす(m)。深度ノイズに合わせて調整")]
        public float contactDistance = 0.01f;

        [Tooltip("この点数以上で接触とする(ノイズ除去)")]
        public int minContactPoints = 5;

        public ContactResult Latest { get; private set; }
        public event System.Action<ContactResult> OnContact;

        IHandPointSource _src;
        Mesh _mesh;
        Vector3[] _verts;
        int[] _tris;
        Vector3[] _worldVerts;
        int _lastVersion = -1;

        void Start()
        {
            _src = handPointSource as IHandPointSource;
            _mesh = GetComponent<MeshFilter>().sharedMesh;
            _verts = _mesh.vertices;
            _tris = _mesh.triangles;
            _worldVerts = new Vector3[_verts.Length];
        }

        void Update()
        {
            if (_src == null || _src.Version == _lastVersion) return;
            _lastVersion = _src.Version;

            var t = transform;
            for (int i = 0; i < _verts.Length; i++) _worldVerts[i] = t.TransformPoint(_verts[i]);

            var result = Detect(_src.HandPoints);
            Latest = result;
            OnContact?.Invoke(result);
        }

        ContactResult Detect(IReadOnlyList<Vector3> points)
        {
            Vector3 sumP = Vector3.zero, sumN = Vector3.zero;
            float maxDepth = 0f;
            int count = 0;

            for (int p = 0; p < points.Count; p++)
            {
                var q = points[p];
                float best = float.MaxValue;
                Vector3 bestC = default, bestN = default;

                for (int i = 0; i < _tris.Length; i += 3)
                {
                    var a = _worldVerts[_tris[i]];
                    var b = _worldVerts[_tris[i + 1]];
                    var c = _worldVerts[_tris[i + 2]];
                    var cp = ClosestPointOnTriangle(q, a, b, c);
                    float d = (q - cp).sqrMagnitude;
                    if (d < best)
                    {
                        best = d; bestC = cp;
                        bestN = Vector3.Cross(b - a, c - a).normalized; // 時計回り=表(Unity)
                    }
                }

                float dist = Mathf.Sqrt(best);
                bool inside = Vector3.Dot(q - bestC, bestN) < 0f; // 表面の裏側=めり込み
                if (dist <= contactDistance || inside && dist <= 0.05f)
                {
                    count++;
                    sumP += bestC; sumN += bestN;
                    if (inside) maxDepth = Mathf.Max(maxDepth, dist);
                }
            }

            if (count < minContactPoints) return default;
            return new ContactResult
            {
                touching = true,
                point = sumP / count,
                normal = sumN.normalized,
                depth = maxDepth,
                contactCount = count
            };
        }

        // Ericson, Real-Time Collision Detection 5.1.5
        static Vector3 ClosestPointOnTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            var ab = b - a; var ac = c - a; var ap = p - a;
            float d1 = Vector3.Dot(ab, ap), d2 = Vector3.Dot(ac, ap);
            if (d1 <= 0 && d2 <= 0) return a;
            var bp = p - b;
            float d3 = Vector3.Dot(ab, bp), d4 = Vector3.Dot(ac, bp);
            if (d3 >= 0 && d4 <= d3) return b;
            float vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0) return a + ab * (d1 / (d1 - d3));
            var cpv = p - c;
            float d5 = Vector3.Dot(ab, cpv), d6 = Vector3.Dot(ac, cpv);
            if (d6 >= 0 && d5 <= d6) return c;
            float vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0) return a + ac * (d2 / (d2 - d6));
            float va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
                return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
            float denom = 1f / (va + vb + vc);
            return a + ab * (vb * denom) + ac * (vc * denom);
        }

        void OnDrawGizmos()
        {
            if (!Latest.touching) return;
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Latest.point, 0.01f);
            Gizmos.DrawRay(Latest.point, Latest.normal * 0.05f);
        }
    }
}
