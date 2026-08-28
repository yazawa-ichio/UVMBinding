# TASK-019: Converter / Provider の型フィルタ

| | |
| --- | --- |
| Phase | 3（コア品質・並行トラック） |
| 状態 | 未着手 |
| 依存 | TASK-017, TASK-018, TASK-022 |
| 規模 | 中 |

## 目的

Inspector の Converter / Provider 選択で、型が合わない候補を除外する。

> **2026-08-28 変更。** 当初 [TASK-003](TASK-003-bindingPropertyRedesign.md) の前提としていたが、[TASK-022](TASK-022-castConverterConsolidation.md) が Cast 90 型を 1 型に集約し、数値変換のペア選択が不要になるため、**前提から外して並行トラックへ移した**。
>
> 候補数は TASK-022 完了時点で 130 → 41 に減る。フィルタはそこからさらに絞るための改善であり、ブロッカーではない。

## 背景

`ConverterList` は `TypeCache.GetTypesDerivedFrom<IConverter>()` を全件並べるだけで、**型による絞り込みを一切していない**。

| 種別 | 型数 | TASK-022 後 |
| --- | --- | --- |
| Cast（生成） | 90 | **1** |
| BoolSwitch（生成） | 10 | 10 |
| Comparison（生成） | 10 | 10 |
| 手書き | 約 20 | 約 20 |
| **合計** | **約 130** | **約 41** |

TASK-022 後も **41 型が 1 つの Popup に並ぶ。** `IConverter.GetInputType()` / `GetOutputType()` は実装されているのに、Editor 側で一度も使われていない。

`PropertyProviderDrawer` も同じく `TypeCache.GetTypesDerivedFrom<IPropertyProvider>()` を全件並べる。具象が 2 つしかないため今は顕在化していないが、[TASK-018](TASK-018-propertyProviderBuilder.md) で汎用 Provider が使えるようになると同じ問題になる。

## 変更内容

### 出力型によるフィルタ（本タスクの主眼）

Binder は `Binder<T>` を継承しているため、**リフレクションで `T` を取得できる**。`GetOutputType()` が `T` に代入可能な Converter だけを候補に出す。

```
Binder<string> に対する候補 → GetOutputType() が string に代入可能なもののみ
```

これだけで 130 型が数個〜十数個に減る。**依存タスクなしで実装できる。**

### 入力型によるフィルタ（後続の改善）

ViewModel の宣言型が分かれば `GetInputType()` でも絞れる。ただし path から宣言型を引くには [TASK-005](TASK-005-reservationViewModelAccessor.md)（`ReservationViewModelName` の公開）が必要。

**本タスクには含めない。** TASK-005 完了後に追加する。

### Provider 側

`PropertyProviderDrawer` にも同じ仕組みを入れる。Provider は `GetInfos()` が出力型を返すため、これを使う。

### フィルタの解除

型が合わない Converter を意図的に選びたい場合に備え、**フィルタを外す操作を残す**。フィルタは補助であり、選択を禁止するものではない。

## 対象ファイル

- `Unity/Assets/UVMBinding/Editor/Drawer/TypeList.cs`
- `Unity/Assets/UVMBinding/Editor/Drawer/BinderDrawer.cs`
- `Unity/Assets/UVMBinding/Editor/Drawer/PropertyProviderDrawer.cs`

## 完了条件

- `TextBinder`（`Binder<string, Text>`）の候補に bool 出力の Converter が現れないこと
- 候補数が 41 から実用的な数に減っていること（実測して記録する）
- `ConcatenateConverter` と `CastConverter` が候補に残ること（入出力型が実行時に決まるため除外しない）
- フィルタを外して全件表示に切り替えられること
- 既存の Converter 選択・Concatenate の編集が壊れていないこと（手動確認）

## 備考

`ConcatenateConverter` は `GetInputType()` / `GetOutputType()` が構成要素に依存し、要素が空のとき `null` を返す。**フィルタ実装時に null を考慮すること。**

TASK-022 の `CastConverter` も入出力型がバインド時に決まるため、同様に除外しない。
