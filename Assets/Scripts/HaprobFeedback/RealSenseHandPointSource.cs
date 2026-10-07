using System.Collections.Generic;
using Intel.RealSense;
using UnityEngine;

namespace HaprobFeedback
{
    /// <summary>
    /// RealSense の深度から点群を作り、ワークスペース(箱)内の点だけを「手の点群」として出力する。
    /// RealSense Unity wrapper (realsense.unitypackage) の RsDevice を使う。
    /// 手の領域は、カメラ前方の箱(near/far + 横幅)で切り出す簡易セグメンテーション。
    /// </summary>
    public class RealSenseHandPointSource : MonoBehaviour, IHandPointSource
    {
        [Tooltip("シーン上の RsFrameProvider(RsDevice 等)")]
        public RsFrameProvider frameProvider;

        [Tooltip("カメラのシーン上の姿勢。点群をワールドへ変換する。キャリブレーション結果をここに反映")]
        public Transform cameraTransform;

        [Header("手の切り出し(カメラ座標, m)")]
        public float nearZ = 0.20f;
        public float farZ = 0.60f;
        public float halfWidth = 0.25f;
        public float halfHeight = 0.20f;

        [Tooltip("N点に1点だけ使う(負荷軽減)")]
        [Range(1, 32)] public int stride = 4;

        readonly List<Vector3> _points = new List<Vector3>(4096);
        readonly List<Vector3> _pending = new List<Vector3>(4096);
        readonly object _lock = new object();
        PointCloud _pc;
        Vector3[] _verts;
        volatile bool _dirty;
        int _version;

        public IReadOnlyList<Vector3> HandPoints => _points;
        public int Version => _version;

        void Start()
        {
            _pc = new PointCloud();
            if (frameProvider == null) frameProvider = FindObjectOfType<RsFrameProvider>();
            frameProvider.OnNewSample += OnNewSample;
        }

        void OnDestroy()
        {
            if (frameProvider != null) frameProvider.OnNewSample -= OnNewSample;
            _pc?.Dispose();
        }

        // RealSense のスレッドから呼ばれる。Unity API(Transform)は触らない。
        void OnNewSample(Frame frame)
        {
            using (var fs = frame.As<FrameSet>())
            using (var depth = fs.DepthFrame)
            {
                if (depth == null) return;
                using (var pf = _pc.Process(depth))
                using (var pts = pf.As<Points>())
                {
                    if (_verts == null || _verts.Length != pts.Count) _verts = new Vector3[pts.Count];
                    pts.CopyVertices(_verts);
                }
            }

            lock (_lock)
            {
                _pending.Clear();
                for (int i = 0; i < _verts.Length; i += stride)
                {
                    // RealSense: x右, y下, z前 → Unity: x右, y上, z前
                    var v = new Vector3(_verts[i].x, -_verts[i].y, _verts[i].z);
                    if (v.z < nearZ || v.z > farZ) continue; // 0 (無効)もここで落ちる
                    if (Mathf.Abs(v.x) > halfWidth || Mathf.Abs(v.y) > halfHeight) continue;
                    _pending.Add(v);
                }
                _dirty = true;
            }
        }

        void Update()
        {
            if (!_dirty) return;
            lock (_lock)
            {
                _points.Clear();
                foreach (var p in _pending)
                    _points.Add(cameraTransform != null ? cameraTransform.TransformPoint(p) : p);
                _dirty = false;
            }
            _version++;
        }

        void OnDrawGizmosSelected()
        {
            if (cameraTransform == null) return;
            Gizmos.matrix = cameraTransform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            float d = (nearZ + farZ) * 0.5f;
            Gizmos.DrawWireCube(new Vector3(0, 0, d), new Vector3(halfWidth * 2, halfHeight * 2, farZ - nearZ));
        }
    }
}
