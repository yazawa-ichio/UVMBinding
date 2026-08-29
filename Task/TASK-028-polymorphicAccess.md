# TASK-028: 多態性を許す範囲の決定

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **仕様検討中 / 未着手** |
| 依存 | TASK-003 |
| 規模 | 小〜中 |

## 背景

[TASK-003](TASK-003-bindingPropertyRedesign.md) で 1 path = 1 型にした結果、**型が完全一致
しない場合の扱いが未確定のまま残った**。

設計文書には「登録が `object` なら任意の型の書き込みを受け入れる」と書いたが、**実装は
`Get<T>` の完全一致だけ**になっている。`ReflectionViewModel` は全プロパティが `object` かつ
`ViewModel` の派生なので、`Set<int>("X", 1)` が `Log.Error` になって無視される経路が残っている。

## 用語：ここでの「多態性」

`IsAssignable` は `Type.IsAssignableFrom` である。**CLR の代入互換のみ**を指す。

| 組み合わせ | 可否 |
| --- | --- |
| 派生 → 基底 | 可 |
| 実装型 → インターフェース | 可 |
| 値型 → `object`（ボックス化） | 可 |
| `int` → `float` | **不可** |
| `int` → `long` | **不可** |
| `T` → `T?` | **不可** |

**数値変換は多態性の範囲外。** それは Converter（[TASK-022](TASK-022-castConverterConsolidation.md)）の
責務であり、本タスクでは扱わない。

## 決められない理由：ジェネリクスの不変性

`Get<T>` は `IBindingProperty<T>` を返す。この型は `T Value { get; set; }` を持つため
**不変（invariant）**であり、`BindingProperty<Base>` を `IBindingProperty<Derived>` としても
`IBindingProperty<object>` としても返せない。

読み取りだけなら共変にできるが、`Value` の setter があるため不可能。**型付きハンドルを返す限り
多態性は入らない。**

したがって決めるべきは「**ハンドルを返さない経路で、どこまで型を跨がせるか**」になる。

## 現状の経路

| 経路 | 型の扱い | 備考 |
| --- | --- | --- |
| `Get<T>` | 完全一致のみ | 不一致は `Log.Error` して `null` |
| `Property<T>` / `Subscribe<T>` | 完全一致のみ | `Get<T>` に依存 |
| `SubscribeAssignable<T>` | **代入可を許す** | 型消去チャネル経由。既に動いている |
| `IBindingProperty.GetObject()` | 型消去 | `IsAssignable<T>()` で可否を判定できる |
| `IBindingProperty.SetObject(object)` | 型消去 | **不正な型では `InvalidCastException` を投げる** |
| `Binder.Bind` | 代入可を許す | `prop.IsAssignable<T>()` で判定。View 側は既に多態 |

**読み取り側は既に多態を許している。** 揃っていないのは書き込み側と、値の直接取得。

## 論点

### 論点 1：書き込みで代入可を許すか

登録が `Base`、書き込みが `Derived` の場合。

| 案 | 挙動 |
| --- | --- |
| **A-1** | 完全一致のみ。`Derived` の書き込みは `Log.Error` |
| **A-2** | 代入可なら許す（`Derived` → `Base` は通る） |
| **A-3** | 登録が `object` のときだけ許す |

**A-3 は TASK-003 の設計文書に書いた内容**だが、`Base` / インターフェースのケースを拾えない。

実プロジェクトで `Set<Derived>` を `Base` の path に書く形が発生しうるかは未調査。
**調査してから決めるのが望ましい。**

### 論点 2：値の直接取得を用意するか

`Get<T>` はハンドルを返すため多態にできないが、**値だけを返す経路**なら可能。

```
bool TryGetValue<T>(string path, out T value)
```

完全一致は非ボックス、代入可はボックス化して通す、不可は `Log.Error`。

**用意しない場合**、利用側は `IBindingProperty.GetObject()` を使うことになるが、これは
`internal` なので**外部からは触れない**。つまり用意しないと多態な読み取り手段が
`SubscribeAssignable` しかなくなる。

### 論点 3：`SetObject` が例外を投げる

`BindingProperty<TValue>.SetObject(object val)` は `Value = (TValue)val;` であり、
**型が違えば `InvalidCastException` が飛ぶ**。

[TASK-003](TASK-003-bindingPropertyRedesign.md) で決めた「出荷後に落とさない。`Log.Error` して
無視する」方針と食い違っている。**`Get<T>` だけ方針を守り、`SetObject` は守っていない。**

`ReflectionViewModel` は `SetObject` で書き込むため、データソース側の型が変わると落ちる。

| 案 | 挙動 |
| --- | --- |
| **C-1** | 現状維持。例外を投げる |
| **C-2** | 型を検査し、不正なら `Log.Error` して無視する |

**C-2 を推す。** 方針を全体で揃えるべきで、ここだけ例外なのは説明がつかない。

### 論点 4：公開範囲

`PropertyContainer` は public クラスだが、`Get<T>` を含む型付き API は `internal`。
新しい経路を public にするかは別途決める。

public にすると「型を跨ぐ読み書き」が正規の手段になり、静的検査（[TASK-007](TASK-007-structuralLint.md)）で
不一致を潰す方針と多少ぶつかる。

### 論点 5：型を変えて登録し直す手段

1 path = 1 型のため、一度確定した型は変えられない。型が静的に保証されない
`GeneralViewModel` では詰まる可能性がある。

| 案 | 挙動 |
| --- | --- |
| **E-1** | 用意しない。型を変えたい場合は別の path を使う |
| **E-2** | `GeneralViewModel` にのみ解放（`RemoveProperty`）を用意する |

**E-2 の場合の副作用：** 解放してもバインド済みの `Binder` は古いインスタンスを保持する。
同じ path に新しいプロパティが作られた時点で `OnNewProperty` 経由で貼り直される。
`SubscribeAssignable` の購読は `m_SubscribePaths` に残るため、再登録された値も届く。

## 決める順序

1. **論点 3**（`SetObject` の例外）— 方針の一貫性の問題であり、他と独立に決められる
2. **論点 1**（書き込みの多態）— 実プロジェクトでの発生有無を調査してから
3. **論点 2**（値の直接取得）— 論点 1 が決まれば従属的に決まる
4. **論点 4**（公開範囲）— 実装時に決めればよい
5. **論点 5**（解放）— 独立。必要になった時点でよい

## 完了条件

- 上記 5 論点それぞれについて、採る案と理由を本ファイルに記録すること
- 決定に沿って実装し、テストを追加すること
- [TASK-003](TASK-003-bindingPropertyRedesign.md) の設計節を、実装と一致する内容に直すこと
