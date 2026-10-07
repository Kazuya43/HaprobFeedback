using UnityEngine;
// ↓ autd3-unity (AUTD3Sharp) を導入後に有効化。API はバージョンで異なるため、
//   使用バージョンのサンプル(Controller.Open / Focus / Sine)に合わせて下の TODO を修正すること。
// using AUTD3Sharp;
// using AUTD3Sharp.Gain;
// using AUTD3Sharp.Modulation;

namespace HaprobFeedback
{
    /// <summary>
    /// AUTD3 による触覚提示。接触点に焦点(Focus)を作り、強度で振幅を変える。
    /// 座標系: autdOrigin の ローカル座標を AUTD 座標へ変換する。
    ///   Unity(左手系, Y上, m) → AUTD(右手系, Z上, mm): (x, y, z) → (x, z, y)*1000 を既定とする。
    ///   autdOrigin は「AUTDの振動子面の中心」に置き、面を XZ 平面・法線を +Y に合わせる。
    /// </summary>
    public class AutdHapticOutput : MonoBehaviour, IHapticOutput
    {
        public Transform autdOrigin;

        [Tooltip("振幅変調の周波数(Hz)。皮膚感覚が強い 150-250Hz 付近")]
        public float modulationFreq = 150f;

        [Tooltip("接触点を振動子面からこの高さ(m)以内に制限(焦点が届く範囲)")]
        public Vector2 reachRangeY = new Vector2(0.05f, 0.30f);

        public bool dryRun = true; // true: AUTD に送らずログのみ

        // Controller _autd;  // TODO: AUTD3Sharp.Controller

        void Start()
        {
            if (dryRun) return;
            // TODO: _autd = Controller.Open(new[]{ new AUTD3(Vector3.zero) }, new Simulator(...) or SOEM(...));
            //       _autd.Send(new Sine(freq: modulationFreq));  // 変調はバージョンにより Send の形が異なる
        }

        public void Present(Vector3 worldPoint, float intensity)
        {
            var local = autdOrigin != null ? autdOrigin.InverseTransformPoint(worldPoint) : worldPoint;
            if (local.y < reachRangeY.x || local.y > reachRangeY.y) { Stop(); return; }

            // Unity(x,y,z)[m] → AUTD(x,y,z)[mm]
            var autdMm = new Vector3(local.x, local.z, local.y) * 1000f;
            intensity = Mathf.Clamp01(intensity);

            if (dryRun)
            {
                Debug.Log($"[AUTD dry] focus=({autdMm.x:F1},{autdMm.y:F1},{autdMm.z:F1})mm intensity={intensity:F2}");
                return;
            }
            // TODO: _autd.Send(new Focus(autdMm){ Intensity = (byte)(intensity*255) });
        }

        public void Stop()
        {
            if (dryRun) return;
            // TODO: _autd.Send(new Null());
        }

        void OnDestroy()
        {
            Stop();
            // TODO: _autd?.Dispose();
        }
    }
}
