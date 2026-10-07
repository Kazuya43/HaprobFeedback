# HaprobFeedback

RealSense で手の点群を取得し、オブジェクトのメッシュとの接触を判定して AUTD3 で触覚フィードバックを返す Unity システム。

```
RealSenseHandPointSource ─(手の点群, ワールド座標)→ MeshContactDetector ─(接触点/法線/めり込み)→ HapticFeedbackController ─→ AutdHapticOutput (AUTD3)
```

## 必要なもの
- Unity 2021.3 以降
- RealSense Unity wrapper (`realsense.unitypackage`, librealsense releases) をインポート
- autd3-unity (AUTD3Sharp) をインポート → `AutdHapticOutput.cs` の TODO を使用バージョンの API に合わせて有効化し、`dryRun` を false に

## シーン構成
1. RealSense Device プレハブ(`RsDevice`, Depth を有効)を配置
2. 空オブジェクトに `RealSenseHandPointSource` を付与。`frameProvider`=RsDevice, `cameraTransform`=カメラ姿勢(実カメラと一致するよう配置)。Gizmo の水色の箱が手の検出範囲
3. 触れさせたいオブジェクトに `MeshContactDetector`(MeshFilter 必須)。`handPointSource` に 2 を指定
4. 空オブジェクトに `AutdHapticOutput`(`autdOrigin`=AUTD振動子面中心、面がXZ・法線+Y)
5. `HapticFeedbackController` に detector と output を指定

## 注意・既知の制約
- **手の検出は簡易(深度の箱切り出し)**。箱内に手以外が入ると誤検出するため、背景/腕の処理が必要なら `RealSenseHandPointSource` を差し替える(`IHandPointSource` 実装)。
- 接触判定は三角形総当たり。重いメッシュは低ポリの判定用メッシュを使うか、`stride` を上げる。
- カメラ-AUTD-Unity 間のキャリブレーション(Transform の位置合わせ)精度が提示位置精度を決める。
- AUTD 部分は実機/ライブラリ未検証(この環境では API 資料を取得できなかったため雛形)。RealSense 部分もハード未検証。
