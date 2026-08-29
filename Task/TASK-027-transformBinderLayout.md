# TASK-027: Transform 系 Binder と uGUI レイアウトの競合

| | |
| --- | --- |
| Phase | 3（コア品質） |
| 状態 | 未着手 |
| 依存 | なし |
| 規模 | 小〜中 |

## 現象

`PositionBinder` を `RectTransform` に付けると、**同じ環境で実行しても結果が変わる**ことがある。

[TASK-023](TASK-023-showcase.md) のショーケース画面 11 で、`Position` に `Vector3.zero` をバインドした
オブジェクトの `anchoredPosition` が、実行のたびに `0` と `50` の間で揺れた。Editor 実行と
プレイヤー実行の間でも一致しなかった。

## 原因

`PositionBinder` は `transform.localPosition` に書く。

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
| `PositionBinder` | `localPosition` / `position` |
| `RotationBinder` | 回転 |
| `ScaleBinder` | `localScale` |

**`ScaleBinder` は影響を受けない。** `localScale` はレイアウトが書き換えないため。
`PositionBinder` が最も影響を受ける。

利用側では UI 要素の移動演出に使われる可能性が高く、**レイアウトグループの下に置くと再現性のない
挙動になる**。

## 対応案

### 案 A: `RectTransform` の場合は `anchoredPosition` に書く

```csharp
if (transform is RectTransform rect)
{
    rect.anchoredPosition = m_Offset + value;
}
```

`anchoredPosition` は親の幅に依存しないため、いつ書いても結果が同じになる。
**ただし既存の利用箇所の見た目が変わる**（`localPosition` 基準で座標を決めていた場合）。

### 案 B: 専用の Binder を追加する

`AnchoredPositionBinder` を別に用意し、`PositionBinder` は現状のまま残す。
破壊的変更にならないが、**どちらを使うべきかの判断を利用側に押し付ける**。

### 案 C: ドキュメントに制限として書くだけ

[A-13](../INDEX.md) の方針（利用側に制限を課さない）に反するため、**採らない。**

## 判断の材料

**実プロジェクトでの `PositionBinder` の使用箇所を数えてから決める。** 使用が少なければ案 A、
多ければ案 B が現実的。

## 暫定対応

ショーケース側でアンカーを中央に変更し、`localPosition` が親の幅に依存しないようにした。
**これは検証の基準値を安定させるための回避であり、本体の修正ではない。**

## 完了条件

- `RectTransform` に対する `PositionBinder` の結果が実行ごとに変わらないこと
- ショーケースのアンカー回避を戻しても基準値が安定すること
- `RotationBinder` についても同様の問題がないか確認すること
