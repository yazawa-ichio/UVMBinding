# TASK-027: Transform 系 Binder と uGUI レイアウトの競合

| | |
| --- | --- |
| Phase | 3（コア品質） |
| 状態 | **AnchoredPositionBinder を追加して対応済み / PositionBinder 自体は未修正** |
| 依存 | なし |
| 規模 | 小〜中 |

## 現象

`Position3DBinder` を `RectTransform` に付けると、**同じ環境で実行しても結果が変わる**ことがある。

[TASK-023](TASK-023-showcase.md) のショーケース画面 11 で、`Position` に `Vector3.zero` をバインドした
オブジェクトの `anchoredPosition` が、実行のたびに `0` と `50` の間で揺れた。Editor 実行と
プレイヤー実行の間でも一致しなかった。

## 原因

`Position3DBinder` は `transform.localPosition` に書く。

```csharp
transform.localPosition = m_Offset + value;
```

**`RectTransform` の `localPosition` は、アンカーと親の解決済み矩形から導かれる値**である。
アンカーが端に寄っている場合、同じ `localPosition` でも親の幅が変われば `anchoredPosition` が変わる。

バインドの書き込みはポーリング（`Canvas.preWillRenderCanvases`）で走るため、**親のレイアウトが
確定する前に書くことがある**。その場合、書いた時点の親の幅で `anchoredPosition` が決まってしまう。

レイアウトグループの下にある `RectTransform` では親の幅が実行時に決まるため、結果が安定しない。

## 影響範囲

`Transform` に直接書く Binder すべてが対象になりうる。

| Binder | 書き込み先 |
| --- | --- |
| `Position3DBinder` | `localPosition` / `position` |
| `RotationBinder` | 回転 |
| `ScaleBinder` | `localScale` |

**`ScaleBinder` は影響を受けない。** `localScale` はレイアウトが書き換えないため。
`Position3DBinder` が最も影響を受ける。

利用側では UI 要素の移動演出に使われる可能性が高く、**レイアウトグループの下に置くと再現性のない
挙動になる**。

## 対応

### 採用: 専用の Binder を追加する（案 B）

`AnchoredPositionBinder` を追加した。`Position3DBinder` は変更していない。

```csharp
public class AnchoredPositionBinder : Binder<Vector2, RectTransform>
{
	[SerializeField]
	Vector2 m_Offset;

	protected override void UpdateValue(Vector2 value)
	{
		Target.anchoredPosition = m_Offset + value;
	}
}
```

`anchoredPosition` は親の大きさに依存しないため、レイアウトが確定する前に書いても結果が変わらない。

**既存の利用箇所に影響しない**ことを優先した。`Position3DBinder` の挙動を変えると、
`localPosition` 基準で座標を決めていた箇所の見た目が変わってしまう。

### 検証

ショーケース画面 11 の対象を `AnchoredPositionBinder` に置き換え、**端アンカーのまま**に戻した。

| Binder | アンカー | 結果 |
| --- | --- | --- |
| `Position3DBinder` | 端 | **実行のたびに `0` と `50` で揺れる** |
| `Position3DBinder` | 中央 | 安定（回避策） |
| `AnchoredPositionBinder` | 端 | **安定。`40` → `240` とバインド値がそのまま出る** |

Editor 実行 2 回と IL2CPP プレイヤー実行で一致することを確認済み。

### 採らなかった案

**`Position3DBinder` の挙動を `anchoredPosition` に変える案は採らない。** 破壊的変更になる。

**ドキュメントに制限として書くだけの案も採らない。** [A-13](../INDEX.md) の方針
（利用側に制限を課さない）に反する。

### 名前を変えた

`PositionBinder` を **`Position3DBinder` に改名**した。`AnchoredPositionBinder` との対比を
名前で示すため。ファイルと `.meta` を一緒に移動して GUID を保っているので、**既存の prefab /
scene の参照は切れない**（`UVMBinding.AppTests/Resources/Root.prefab` で確認）。

**コードから型名で参照している箇所はコンパイルエラーになる。** 利用側の対応が必要。

なお `3DPositionBinder` は C# の識別子として無効（数字始まり）なため `Position3DBinder` とした。

### ショーケースでの使い分け

| 画面 | Binder | アンカー | 意図 |
| --- | --- | --- | --- |
| 08 演出系 | `Position3DBinder` | **中央** | 親の幅への依存を外した正しい使い方 |
| 11 その他 | `AnchoredPositionBinder` | **端** | レイアウト下でも成立することを示す |

画面 08 も当初は端アンカーだったが、**基準値が Editor と実機で割れた**（`160` と `50`）ため
中央に変更した。この問題は特定の画面に固有ではなく、`Position3DBinder` を `RectTransform` に
使えば常に起こりうる。

## 残っている課題

**`Position3DBinder` を `RectTransform` に対して使うと、依然として結果が安定しない。**
`AnchoredPositionBinder` という正しい選択肢はできたが、誤った選択をしても警告は出ない。

対応の候補は以下。**どれも未着手。**

| 案 | 内容 |
| --- | --- |
| 静的検査 | [TASK-007](TASK-007-lint.md) の lint で `RectTransform` 上の `Position3DBinder` を検出する |
| 実行時警告 | `Position3DBinder` の `OnBind` で `RectTransform` を検出したら一度だけ警告する |
| 何もしない | 選択を利用側に委ねる |

**`RotationBinder` は未確認。** `localRotation` はレイアウトが書き換えないため影響を受けない
見込みだが、実測していない。

**`Vector3ToVector2` / `Vector2ToVector3` を追加した。** `Position3DBinder`（`Vector3`）から
`AnchoredPositionBinder`（`Vector2`）へ移行する際、ViewModel 側の型を変えずに済む。

**軸の対応付けは任意に選べる。** 出力の各成分をどの入力軸から取るかを個別に指定する形にした。
3D キャラの位置を 2D マップへ落とす場合の XZ 平面のような用途に対応するため。符号の反転と
`Zero` も選べるが、**係数による拡大・せん断は扱わない**（必要なら Concatenate で別の Converter を
繋ぐ）。

| 設定 | 結果 |
| --- | --- |
| `X = X` / `Y = Y`（既定） | XY 平面 |
| `X = X` / `Y = Z` | **XZ 平面** |
| `X = X` / `Y = MinusZ` | XZ 平面で奥行きを反転 |

逆変換では**参照していない軸が 0 になる。** `TryInverseConvert` には変換前の値が渡ってこないため
復元できない。

## 完了条件

- ~~ショーケースのアンカー回避を戻しても基準値が安定すること~~ 済（`AnchoredPositionBinder` で成立）
- `RectTransform` に対する `Position3DBinder` の誤用を検出できること（未着手）
- `RotationBinder` についても同様の問題がないか確認すること（未着手）
