# TASK-028: 多態性を許す範囲の決定

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **仕様確定 / 実装未着手** |
| 依存 | TASK-003 |
| 規模 | 小〜中 |

## 背景

[TASK-003](TASK-003-bindingPropertyRedesign.md) で 1 path = 1 型にした結果、**型が完全一致
しない場合の扱いが未確定のまま残った。**

## 用語：ここでの「多態性」

**CLR の代入互換のみ**を指す。

| 組み合わせ | 可否 |
| --- | --- |
| 派生 → 基底 | 可 |
| 実装型 → インターフェース | 可 |
| 値型 → `object`（ボックス化） | 可 |
| `int` → `float` / `int` → `long` / `T` → `T?` | **不可** |

**数値変換は多態性の範囲外。** Converter の責務であり、本タスクでは扱わない
（[TASK-022](TASK-022-castConverterConsolidation.md) / [TASK-026](TASK-026-implicitCast.md)）。

---

## 目標仕様：どこで型を跨げるか

**原則：多態性は View 側でのみ許す。ViewModel 側は完全一致のみ。**

**ViewModel は値の持ち主**であり、型が揺れると持ち主が誰か分からなくなる。
**View は値の消費者**であり、受け取れる形が複数あってよい。

### マトリクス

登録されている型を **S**、API に渡す型を **T** とする。

| 外から使う API | S == T | 代入可（安全な向き） | 代入不可 |
| --- | --- | --- | --- |
| `IViewModel.Set<T>` / `GeneralViewModel.Set<T>` | 反映される | **`Log.Error` + 無視** | `Log.Error` + 無視 |
| `IViewModel.Get<T>` / `GeneralViewModel.Get<T>` | 値が返る | **`Log.Error` + 既定値** | `Log.Error` + 既定値 |
| `Property<T>` | 読み書きできる | **`Log.Error` + 無効** | `Log.Error` + 無効 |
| `PropertyContainer.Subscribe<T>` | 届く | **`Log.Error` + 購読されない** | `Log.Error` + 購読されない |
| `PropertyContainer.SubscribeAssignable<T>` | 届く | **届く** | **届かない（無言）** |
| Binder の結線（`Binder<T>`） | 結線される | **結線される** | 結線されない（`Log.Debug`） |
| Converter を挟んだ結線 | — | — | **変換できれば結線される** |

「安全な向き」は操作ごとに違う。

| 操作 | 安全な向き |
| --- | --- |
| 読み取り（`Get` / 購読 / Binder への配信） | **S が T に代入可**（登録 `Derived` → 要求 `Base`） |
| 書き込み（`Set` / Binder からの書き戻し） | **T が S に代入可**（要求 `Derived` → 登録 `Base`） |

### この形にする理由

**ViewModel 側で代入可を許さない。** 許すと `[Bind]` の宣言型と実際に流れる型が食い違っても
気付けず、1 path = 1 型にした意味が薄れる。型が違うなら Converter を挟むか、
[TASK-007](TASK-007-structuralLint.md) の静的検査で潰す。

**`SubscribeAssignable<T>` だけ例外的に代入可を許す。** 名前のとおり「代入可なら受け取る」ための
API であり、型を前提にしない購読として既に成立している。

**View 側は代入可を許す。** 1 つの ViewModel を複数の View が別の粒度で見るのは正常な使い方で、
`Binder<Base>` が `Derived` の値を表示できないと共通 UI が書けない。

**代入不可のときだけ挙動が分かれる。** ViewModel 側は `Log.Error`、View 側は無言または
`Log.Debug`。**View 側は「結線しない」が正常な選択肢**（同じ prefab を複数の ViewModel で
使い回す）であり、毎回 Error を出すと実用にならない。

---

## 補足：型を変えたい場合

ViewModel 側は完全一致のみなので、**一度確定した型は変えられない。** 型が静的に保証されない
`GeneralViewModel` に限り、登録を解除する `UnSet` を用意する。

```csharp
vm.Set("P1", 10);     // int で確定
vm.UnSet("P1");       // 解除
vm.Set("P1", "Text"); // string で登録し直す
```

**原則：`UnSet` した path は「一度も使っていない状態」に戻る。**

| 観測点 | `UnSet` 直後 | 再登録後 |
| --- | --- | --- |
| `Get<T>` / `Set<T>` | 未登録として扱う。`Get<T>` は T で登録する | 通常どおり |
| Binder | 未結線に戻る | 結線し直される |
| `SubscribeAssignable<T>` | 届かない | **届く**（path に対する購読のため） |
| `Subscribe<T>` / `Property<T>` | **無効。取り直しが必要** | 無効のまま |

`Subscribe<T>` と `Property<T>` を無効にするのは、**取得時の型を前提にしたハンドル**だから。
`UnSet` はその前提を壊す操作なので、自動で繋ぎ直さず取り直させる。

`[Bind]` / `Property<T>` は宣言で型が決まるため、`ViewModel` 本体や `PropertyContainer` の
public API に `UnSet` は出さない。

---

## 実装メモ

**ここから下は目標仕様ではなく、実装時に効いてくる制約。**

### 型付きハンドルを多態にできない理由

`IBindingProperty<T>` は `T Value { get; set; }` を持つため**不変（invariant）**であり、
`BindingProperty<Base>` を `IBindingProperty<Derived>` として返せない。

したがって `Property<T>` / `Subscribe<T>` は、**仕様として選ぶまでもなく完全一致しか実現できない。**
上表でこれらを完全一致のみとしたのは、この制約とも一致する。

### 目標仕様と現状の差

| 項目 | 現状 | 差 |
| --- | --- | --- |
| `Set` / `Get` / `Subscribe` / `Property<T>` の型不一致 | 目標どおり | 無し |
| `SubscribeAssignable` | 目標どおり | 無し |
| Binder の結線 | 目標どおり | 無し |
| `UnSet` | **存在しない** | **新規** |
| `UnSet` 後の `Subscribe<T>` / `Property<T>` | （実装すれば）解除前の実体を掴んだままになる | **無効化する仕組みが要る** |
| `UnSet` 直後の Binder | （実装すれば）最後の値を表示し続ける | **未結線に戻す処理が要る** |

**実装が必要なのは `UnSet` とその周辺のみ。** 多態性の扱い自体は現状で目標仕様を満たしている。

## 完了条件

- `GeneralViewModel.UnSet` を実装し、「補足」の表の挙動を満たすこと
- 型不一致の各ケースについてテストを追加すること
- [TASK-003](TASK-003-bindingPropertyRedesign.md) の設計節の「`T == object` なら常に受け入れる」を
  撤回済みとして直すこと
