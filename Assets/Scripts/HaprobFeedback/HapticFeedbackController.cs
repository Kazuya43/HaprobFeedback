using UnityEngine;

namespace HaprobFeedback
{
    /// <summary>接触結果を触覚出力に橋渡しする。めり込み量→強度、接触の開始/終了で出力制御。</summary>
    public class HapticFeedbackController : MonoBehaviour
    {
        public MeshContactDetector detector;
        public MonoBehaviour hapticOutput; // IHapticOutput

        [Tooltip("この深さ(m)で強度1.0")]
        public float fullIntensityDepth = 0.02f;
        public float minIntensity = 0.3f;

        [Tooltip("接触が切れてから停止するまでの猶予(s)。ノイズによるチラつき防止")]
        public float releaseDelay = 0.1f;

        IHapticOutput _out;
        float _lastTouchTime = -1f;
        bool _active;

        void Start()
        {
            _out = hapticOutput as IHapticOutput;
            detector.OnContact += OnContact;
        }

        void OnDestroy() { if (detector != null) detector.OnContact -= OnContact; }

        void OnContact(ContactResult r)
        {
            if (_out == null || !r.touching) return;
            _lastTouchTime = Time.time;
            _active = true;
            float k = Mathf.Clamp01(r.depth / fullIntensityDepth);
            _out.Present(r.point, Mathf.Lerp(minIntensity, 1f, k));
        }

        void Update()
        {
            if (_active && Time.time - _lastTouchTime > releaseDelay)
            {
                _out?.Stop();
                _active = false;
            }
        }
    }
}
