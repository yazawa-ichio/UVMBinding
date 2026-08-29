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
`PropertyContainer.Get<T>` の完全一致だけ**になっている。`ReflectionViewModel` は全プロパティが
`object` かつ `ViewModel` の派生なので、`Set<int>("X", 1)` が `Log.Error` になって無視される
経路が残っている。

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

---

## 層の整理

`Get<T>` / `Set<T>` という名前が複数の層に存在するため、**どの層の話かを常に明示する。**

### 層 1: ViewModel

利用側が直接書く層。`[Bind]` の実体もここを通る。

| API | 公開範囲 | 呼び出す先 |
| --- | --- | --- |
| `IViewModel.Get<T>` / `Set<T>` | **public** | `ViewModel.GetImpl<T>` / `SetImpl<T>` |
| `ViewModel.GetImpl<T>` / `SetImpl<T>` | protected | `PropertyContainer.Get<T>` |
| `GeneralViewModel.Get<T>` / `Set<T>` | **public** | 同上（薄いラッパ） |
| `ViewModel.Property` | protected | 層 2 への入口 |

**ILPP が生成する `[Bind]` プロパティは `GetImpl` / `SetImpl` を呼ぶ。** 宣言型がそのまま T に
なるため、この経路では型が一貫している。

### 層 2: PropertyContainer

path とプロパティの対応を持つ層。

| API | 公開範囲 | 型の扱い |
| --- | --- | --- |
| `Subscribe<T>` | **public** | 完全一致のみ（`Get<T>` に依存） |
| `Unsubscribe<T>` | **public** | 完全一致のみ（同上） |
| `SubscribeAssignable<T>` | **public** | **代入可を許す** |
| `SetDirty` / `SetAllDirty` | **public** | 型に無関係 |
| `Get<T>` | internal | 完全一致のみ。不一致は `Log.Error` して `null` |
| `TryGet` | internal | 型消去。`IBindingProperty` を返す |
| `GetAll` / `GetAllImpl` | internal | 型消去 |

**public な型付き経路は `Subscribe<T>` / `Unsubscribe<T>` の 2 つだけ。** 値の読み書きは
層 1 経由でしか行えない。

### 層 3: BindingProperty

値そのものを持つ層。**インターフェースも実装もすべて `internal`。** 利用側からは触れない。

| API | 型の扱い |
| --- | --- |
| `IBindingProperty<T>.Value` | 完全一致。`get` / `set` を持つため**不変（invariant）** |
| `IBindingProperty.GetObject()` | 型消去 |
| `IBindingProperty.SetObject(object)` | 型消去。**不正な型では `InvalidCastException` を投げる** |
| `IBindingProperty.IsAssignable<T>()` | 代入可否の判定 |

### 層をまたぐ制約

**層 3 の `IBindingProperty<T>` が不変であることが、すべての制約の source になっている。**

`Value` に setter があるため、`BindingProperty<Base>` を `IBindingProperty<Derived>` としても
`IBindingProperty<object>` としても返せない。したがって：

- **層 2 の `Get<T>` は多態にできない。** ハンドルを返すため
- **層 2 の `Subscribe<T>` も多態にできない。** `Get<T>` に依存しているため
- **層 1 の `GetImpl` / `SetImpl` は多態にできる。** 値しか扱わないため

**多態性を入れられるのは層 1 と、層 2 に値だけを扱う API を足した場合に限られる。**

---

## 論点

### 論点 1（層 1）：`ViewModel.Set<T>` で代入可を許すか

登録が `Base`、書き込みが `Derived` の場合の `IViewModel.Set<Derived>(path, val)`。

| 案 | 挙動 |
| --- | --- |
| **A-1** | 完全一致のみ。`Derived` の書き込みは `Log.Error` して無視（**現状**） |
| **A-2** | 代入可なら許す。`Derived` → `Base` は通る |
| **A-3** | 登録が `object` のときだけ許す |

**A-3 は TASK-003 の設計文書に書いた内容**だが、`Base` / インターフェースのケースを拾えない。

`ReflectionViewModel` を救うだけなら A-3 で足りる。`Set<Derived>` を `Base` の path に書く形が
実プロジェクトで発生しうるかは**未調査**。

### 論点 2（層 1）：`ViewModel.Get<T>` で代入可を許すか

登録が `Derived`、読み取りが `Base` の場合の `IViewModel.Get<Base>(path)`。

| 案 | 挙動 |
| --- | --- |
| **B-1** | 完全一致のみ。不一致は `Log.Error` して `default`（**現状**） |
| **B-2** | 代入可ならボックス化して返す |

論点 1 と対になる。**書き込みだけ許して読み取りを許さない構成は説明がつかない**ため、
1 と 2 は揃えるべき。

### 論点 3（層 2）：値だけを扱う API を足すか

層 2 に `Get<T>` とは別に、ハンドルを返さない経路を用意するか。

```
bool TryGetValue<T>(string path, out T value)
bool TrySetValue<T>(string path, T value)
```

**論点 1・2 を A-1 / B-1（完全一致のみ）にするなら不要。**
A-2 / B-2 を採るなら、層 1 の実装先としてここに置くのが自然。層 1 に判定ロジックを直接書くと
`ViewModel` と `GeneralViewModel` の両方に同じ処理が要る。

公開範囲は別途決める（後述の論点 5）。

### 論点 4（層 3）：`SetObject` が例外を投げる

`BindingProperty<TValue>.SetObject(object val)` は `Value = (TValue)val;` であり、
**型が違えば `InvalidCastException` が飛ぶ**。

[TASK-003](TASK-003-bindingPropertyRedesign.md) で決めた「出荷後に落とさない。`Log.Error` して
無視する」方針と食い違っている。**層 2 の `Get<T>` だけ方針を守り、層 3 は守っていない。**

`ReflectionViewModel` は層 3 の `SetObject` で書き込むため、データソース側の型が変わると落ちる。

| 案 | 挙動 |
| --- | --- |
| **C-1** | 現状維持。例外を投げる |
| **C-2** | 型を検査し、不正なら `Log.Error` して無視する |

**C-2 を推す。** 方針を全体で揃えるべきで、ここだけ例外なのは説明がつかない。
**他の論点と独立に決められる。**

### 論点 5（層 2）：新しい API の公開範囲

論点 3 で API を足す場合、`public` にするか `internal` に留めるか。

`public` にすると「型を跨ぐ読み書き」が層 2 の正規の手段になり、静的検査
（[TASK-007](TASK-007-structuralLint.md)）で不一致を潰す方針と多少ぶつかる。

`internal` に留めれば、利用側から見える多態な経路は層 1 の `Get<T>` / `Set<T>` と
層 2 の `SubscribeAssignable<T>` だけになる。

### 論点 6（層 1）：型を変えて登録し直す手段

1 path = 1 型のため、一度確定した型は変えられない。型が静的に保証されない
`GeneralViewModel` では詰まる可能性がある。

| 案 | 挙動 |
| --- | --- |
| **E-1** | 用意しない。型を変えたい場合は別の path を使う |
| **E-2** | `GeneralViewModel` にのみ解放（`RemoveProperty`）を用意する |

`[Bind]` / `Property<T>` は宣言で型が決まるため、解放する理由が無い。**用意するなら
`GeneralViewModel` 限定。**

**E-2 の場合の副作用：** 解放してもバインド済みの `Binder` は古いインスタンスを保持する。
同じ path に新しいプロパティが作られた時点で `OnNewProperty` 経由で貼り直される。
`SubscribeAssignable` の購読は `m_SubscribePaths` に残るため、再登録された値も届く。

---

## 決める順序

| 順 | 論点 | 層 | 理由 |
| --- | --- | --- | --- |
| 1 | 論点 4（`SetObject` の例外） | 3 | 方針の一貫性の問題。他と独立に決められる |
| 2 | 論点 1・2（読み書きの多態） | 1 | 対になっており同時に決める。実プロジェクトの調査が必要 |
| 3 | 論点 3（値だけの API） | 2 | 論点 1・2 が決まれば従属的に決まる |
| 4 | 論点 5（公開範囲） | 2 | 実装時に決めればよい |
| 5 | 論点 6（解放） | 1 | 独立。必要になった時点でよい |

## 完了条件

- 上記 6 論点それぞれについて、採る案と理由を本ファイルに記録すること
- 決定に沿って実装し、テストを追加すること
- [TASK-003](TASK-003-bindingPropertyRedesign.md) の設計節を、実装と一致する内容に直すこと
