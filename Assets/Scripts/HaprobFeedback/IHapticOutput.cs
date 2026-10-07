using UnityEngine;

namespace HaprobFeedback
{
    /// <summary>触覚出力の抽象。AUTD以外(ログ等)に差し替え可能。</summary>
    public interface IHapticOutput
    {
        /// <summary>ワールド座標 point に強度 intensity(0-1) で触覚を提示する。</summary>
        void Present(Vector3 worldPoint, float intensity);

        /// <summary>提示を止める。</summary>
        void Stop();
    }
}
