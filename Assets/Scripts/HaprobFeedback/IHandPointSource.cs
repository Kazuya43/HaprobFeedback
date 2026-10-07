using System.Collections.Generic;

namespace HaprobFeedback
{
    /// <summary>手の点群(ワールド座標, メートル)を供給するソース。</summary>
    public interface IHandPointSource
    {
        /// <summary>最新フレームの手の点群。呼び出し側は変更しないこと。</summary>
        IReadOnlyList<UnityEngine.Vector3> HandPoints { get; }

        /// <summary>HandPoints が更新されるたびに増えるフレーム番号。</summary>
        int Version { get; }
    }
}
