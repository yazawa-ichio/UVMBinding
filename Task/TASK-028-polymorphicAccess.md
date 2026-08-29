# TASK-028: 多態性を許す範囲の決定

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **仕様確定 / 実現方法を検討中** |
| 依存 | TASK-003 |
| 規模 | 小〜中 |

## 背景

[TASK-003](TASK-003-bindingPropertyRedesign.md) で 1 path = 1 型にした結果、**型が完全一致
しない場合の扱いが未確定のまま残った。**

**1 path = 1 型が縛るのは「格納する型」であって、代入の可否ではない。**
`Animal` の枠に `Dog` を入れるのは通常の代入であり、これを拒むのは誤り。

## 用語

### 「代入可」

**CLR の代入互換のみ**を指す。`int` → `float` などの数値変換は含まない
（Converter の責務。[TASK-022](TASK-022-castConverterConsolidation.md) /
[TASK-026](TASK-026-implicitCast.md)）。

### 型の関係

格納されている型を **S**、API に渡す型を **T** とする。

| 記号 | 関係 | 例 | 自然な操作 |
| --- | --- | --- | --- |
| **=** | `T == S` | — | 読み書きとも |
| **↓** | T が S に代入可 | S=`Animal`, T=`Dog` | **書き込み**（`Set<Dog>`） |
| **↑** | S が T に代入可 | S=`Dog`, T=`Animal` | **読み取り**（`Get<Animal>`） |
| **×** | どちらでもない | S=`int`, T=`string` | 無し |

### 型が合わないときは例外を投げる

**型の不一致はコードの誤り**であり、C# の代入規則に反する操作を要求している。
**`InvalidCastException` を投げる。**

| 項目 | 内容 |
| --- | --- |
| 例外 | `InvalidCastException` |
| メッセージ | path・格納されている型・要求された型 |

**落とさずに扱いたい場合のために `Try` 版を用意する。** 標準ライブラリと同じ組み方
（`Dictionary` の添字は投げ、`TryGetValue` は投げない）。

| 投げる | 投げない |
| --- | --- |
| `Get<T>` / `Set<T>` | `TryGetAs<T>` / `TrySet<T>` |
| `Property<T>` の取得 | — |
| `Subscribe` 系の購読 | — |

**View 側の結線は例外を投げない。** 同じ prefab を複数の ViewModel で使い回す際、
「結線しない」は正常な結果であるため（`Log.Debug` のみ）。

#### TASK-003 の方針からの変更

[TASK-003](TASK-003-bindingPropertyRedesign.md) では「出荷後に落とさないため `Log.Error` して
無視する」と決めたが、**本タスクで例外を投げる方針に変更する（2026-08-29）。**

落とさずに済ませたい箇所は `Try` 版を使う。これにより
**層 3 の `SetObject` が例外を投げる現在の挙動とも一貫する**（従来は不整合として扱っていた）。

---

## 目標仕様

### 外から使う API

| API | 層 | 役割 |
| --- | --- | --- |
| `IViewModel.Set<T>` / `Get<T>` | ViewModel | 値の読み書き |
| `IViewModel.TryGetAs<T>` / `TrySet<T>` | ViewModel | **落とさずに試す**（新規） |
| `GeneralViewModel.Set<T>` / `Get<T>` / `TryGetAs<T>` / `TrySet<T>` | ViewModel | 同上 |
| `GeneralViewModel.UnSet` | ViewModel | **格納型の解除**（新規） |
| `Property<T>` | ViewModel | 型付きハンドル |
| `PropertyContainer.Subscribe<T>` | Container | 購読（↑ を許す） |
| `Binder<T>` の結線 | View | — |

### 型の関係ごとの挙動

| API | **=** | **↓** T→S | **↑** S→T | **×** |
| --- | --- | --- | --- | --- |
| `Set<T>` | 反映 | **反映** | **例外** | **例外** |
| `Get<T>` | 値 | **例外** | **値（値型はボックス化）** | **例外** |
| `TrySet<T>` | `true` + 反映 | **`true` + 反映** | `false` | `false` |
| `TryGetAs<T>` | `true` + 値 | **実行時の値が T なら `true`** | **`true` + 値** | `false` |
| `Property<T>` | 読み書き | **例外** | **例外** | **例外** |
| `Subscribe<T>`（↑ 許容） | 届く | — | **届く** | **例外**（※） |
| `SubscribeStrict<T>` | 届く | — | 例外 | 例外 |
| `Binder<T>` の結線 | 結線 | — | **結線** | 結線されない（`Log.Debug`） |

**`Try` 版は例外を投げず、ログも出さない。** 失敗が正常な結果に含まれるため。

**※ 購読は登録前に行うのが通常**（View が先に購読し、後から ViewModel が値を入れる）。
そのため**購読時点で格納型が決まっていない場合は判定できず、例外も投げられない。**
判定できるのは既に登録済みの path を購読する場合のみ。

`UnSet` は型に依存しないため上表に含めない（後述）。

### なぜ `Set` は ↓ を許し ↑ を拒むか

**代入の向きがそのまま可否になる。** `Animal` の枠に `Dog` を入れるのは通常の代入。
逆に `Dog` の枠に `Animal` を入れるのは通常の代入でも不可。

`Get` はその逆。`Dog` の枠から `Animal` として取り出すのは通常の代入。

**特別な規則を足していない。C# の代入規則をそのまま path に持ち込んでいる。**

### なぜ `Try` 版を足すか

**ダウンキャストで値を取り出す手段が無い。**

`Animal` として格納した値の実体が `Dog` のとき、`Get<Dog>` は静的な型（`Animal`）だけを見て
例外になる。**実行時の値を見れば取り出せる**が、その経路が存在しない。

```csharp
vm.Set<Animal>("Pet", new Dog());
vm.Get<Dog>("Pet");                    // 例外。静的には Animal → Dog は不可
vm.TryGetAs<Dog>("Pet", out var dog);  // true。実行時の値が Dog なので取り出せる
```

`TrySet<T>` は書き込み側。型が静的に保証されない `GeneralViewModel` で、
**落とさずに書き込みを試す**ために要る。

#### 命名（未決）

読み取り側は「**実行時の値の型を見てキャストする**」ことが分かる名前にしたい。

| 案 | 読み | 備考 |
| --- | --- | --- |
| **`TryGetAs<T>`** | 「T として取り出せるか試す」 | C# の `as` 演算子と同じ含意。**推奨** |
| `TryCast<T>` | 「T にキャストできるか試す」 | 取り出す動作が名前に出ない |
| `TryGet<T>` | 「取り出せるか試す」 | **静的な型で判定するのか実行時の値で判定するのか読めない** |

書き込み側は `TrySet<T>` でよい。判定に実行時の値が関わらないため。

### `Property<T>` が完全一致のみな理由

`Property<T>` は**読み書き両方**を持つ。↑ を許すと書き込みが、↓ を許すと読み取りが
成立しなくなる。**両立しないため完全一致のみとする。**

---

## 購読 API の命名 → **決定**

| 変更前 | 変更後 | 挙動 |
| --- | --- | --- |
| `SubscribeAssignable<T>` | **`Subscribe<T>`** | ↑ を許す。**既定** |
| `Subscribe<T>` | **`SubscribeStrict<T>`** | 完全一致のみ |

**↑ を許す方を既定にする（2026-08-29 決定）。** 理由は 2 つ。

1. **他の API と揃う。** `Get` も Binder も ↑ を許すのに、購読だけ完全一致が既定なのは説明しにくい
2. **完全一致を使うケースが極端。** 名前が長い方を特殊な用途に充てる

### `SubscribeStrict` は利用者向けの一覧から外す

**実プロジェクトでの利用は 0 件**（[TASK-003](TASK-003-bindingPropertyRedesign.md) の調査。
検出された `Subscribe<` は `EventBroker` に対するもの）。パッケージ内部では
`Collection<T>` / `ValueCollection<T>` が使っている。

| 案 | 内容 |
| --- | --- |
| **案 1** | **`internal` にする。** 利用者向けの API 一覧から外れる。必要になれば public に上げられる（逆は破壊的） |
| 案 2 | public のまま残し、ドキュメントで「通常は `Subscribe` を使う」と書く |

**案 1 を推す。**

### 両者の違いは厳密さだけではない

| | `Subscribe<T>`（↑ 許容） | `SubscribeStrict<T>` |
| --- | --- | --- |
| 通知の元 | **path** | プロパティの実体 |
| 通知ごとの処理 | path の比較 + 型判定 | 直接呼び出し |
| `UnSet` 後 | **生き残る** | 無効になる |

**`SubscribeStrict` の方が安い。** 内部で使う理由はここにある。

---

## 補足：`UnSet`

1 path = 1 型のため、**格納する型そのものを変える手段が無い。**
型が静的に保証されない `GeneralViewModel` に限り `UnSet` を用意する。

```csharp
vm.Set("P1", 10);     // int で格納
vm.UnSet("P1");       // 解除
vm.Set("P1", "Text"); // string で格納し直す
```

**原則：`UnSet` した path は「一度も使っていない状態」に戻る。**

| 観測点 | `UnSet` 直後 | 再登録後 |
| --- | --- | --- |
| `Set<T>` / `Get<T>` / `TryGetAs<T>` / `TrySet<T>` | 未登録として扱う。`Get<T>` は T で格納型を決める | 通常どおり |
| `Binder<T>` | 未結線に戻る | 結線し直される |
| `Subscribe<T>`（↑ 許容） | 届かない | **届く**（path に対する購読のため） |
| `SubscribeStrict<T>` / `Property<T>` | **無効。取り直しが必要** | 無効のまま |

`[Bind]` / `Property<T>` は宣言で型が決まるため、`ViewModel` 本体や `PropertyContainer` の
public API に `UnSet` は出さない。

---

## 実現方法：値を `ValueHolder` に集約する

### 何が複雑さの原因か

**`BindingProperty<TValue>` が型を持っていること。**

`IBindingProperty<T>` は `T Value { get; set; }` を持つため**不変（invariant）**であり、
`BindingProperty<Base>` を `IBindingProperty<Derived>` として返せない。この 1 点から
以下がすべて派生している。

- `PropertyContainer.Get<T>` が完全一致しか返せない
- `Property<T>` / `SubscribeStrict<T>` が完全一致に縛られる
- 型消去のために `ObjectBindingProperty` という別クラスが要る
- 型を跨ぐ通知のために `ValueHolder` を**別途プールして使い捨てる**

**1 path = 1 型になったことで、格納を型消去された 1 つの入れ物に寄せられる。**

### 構造

```
現在
  BindingProperty                     … 非ジェネリック。Path / Hash
  └ BindingProperty<TValue>           … 値・型付きイベント・比較子
     └ ObjectBindingProperty          … 型消去用の特例
  ValueHolder<TValue>                 … 通知のたびにプールから借りる使い捨て

案
  BindingProperty                     … 非ジェネリック。Path / Hash / IValueHolder
  └ （派生なし）
  ValueHolder<TValue>                 … 値の格納そのもの。path ごとに 1 つ
```

**`BindingProperty` からジェネリクスが消える。** 型は `IValueHolder` の実装型が持つ。

### 目標仕様との対応

`IValueHolder` には既に読み取り側が実装されている。

```csharp
bool TryGetValue<T>(out T value)
{
    if (this is ValueHolder<T> h) { value = h.Value; return true; }  // = 完全一致・ボックス化なし
    if (IsAssignable<T>())        { value = (T)GetObject(); return true; }  // ↑ 代入可
    value = default; return false;
}
```

**これは目標仕様の `Get<T>` の「=」と「↑」そのもの。** 足りないのは以下。

| 目標仕様 | 追加するもの |
| --- | --- |
| `Set<T>` の「=」「↓」 | `IValueHolder.TrySetValue<T>` |
| `TryGetAs<T>`（実行時の値で判定） | `IValueHolder` に実行時の型を見る経路 |
| 型が合わないときの例外 | 呼び出し側（層 1・層 2）で `false` を例外に変換 |

`TrySetValue<T>` は `ValueHolder<TValue>` 側にこう書ける。

```csharp
public bool TrySetValue<T>(T value)
{
    if (this is ValueHolder<T> h) { h.Value = value; return true; }   // = ボックス化なし
    if (Assignable<T, TValue>.Is) { Value = (TValue)(object)value; return true; }  // ↓
    return false;
}
```

**参照型の ↓ はキャストのみでボックス化しない。** 値型の ↓ は格納が `object` の場合しか
起こらず、その場合ボックス化は避けられない（現在も同じ）。

### アロケーションの現状（実装を追った結果）

**「プール撤去でアロケーションが減る」は誤り。** プールはアロケーションを防ぐために存在し、
実際に防いでいる。

| # | 箇所 | 定常状態でのアロケーション |
| --- | --- | --- |
| 1 | 値の変更時の `ValueHolder<T>.Use()` | **無い。** プールが最大 5 個を保持。`UseScope` は構造体で `using` から直接 `Dispose` が呼ばれる。さらに購読者が居なければプール操作自体を行わない（[TASK-016](TASK-016-writePathAllocation.md)） |
| 2 | **`IValueHolder.TryGetValue<T>` の ↑ 経路** | **`(T)GetObject()` で値型がボックス化する。** 通知のたび |
| 3 | **`Binder.TryUpdate` の非完全一致経路** | **`(T)m_Property.GetObject()` で値型がボックス化する。** 値が変わるたび、Binder ごと |
| 4 | 購読時のクロージャ | 購読ごとに 1 回。定常では発生しない |
| 5 | `Log.Trace` の文字列整形 | 有効時のみ。既定（Editor=Debug / Player=Warning）では通らない |

**定常状態で効くのは 2 と 3 のボックス化だけ。** どちらも**型消去に由来**しており、
プールとは無関係。`ValueHolder` へ集約しても ↑ 経路のボックス化は消えない。

**3 が最も効く。** `ReflectionViewModel` は全プロパティが `object` 格納なので、
**結線したすべての Binder が値の変更ごとにボックス化する。**

### プールを外して得られるのはアロケーションではなく手数

値の変更 1 回あたり `ThreadLocal<Queue<...>>.Value` のアクセス、`Queue` の出し入れ、
返却時の初期化が不要になる。**アロケーションは変わらない。**

### 得られるもの

| 項目 | 効果 |
| --- | --- |
| `IBindingProperty<T>` | **不要になる。** 不変性の制約が消える |
| `ObjectBindingProperty` | **不要になる。** `ValueHolder<object>` で足りる |
| `PropertyContainer.Get<T>` | 型付きハンドルを返す必要が無くなり、値の読み書きに一本化できる |
| 通知時の `ValueHolder` プール | **不要になる。** 格納がそのまま `IValueHolder` なので借りる必要が無い |
| 型付きハンドル（`Property<T>`） | `ValueHolder<T>` を直接持てばよい。完全一致で取得できたときのみ |

**プール（`ValueHolder<T>.Use` / `UseScope`）が丸ごと不要になる**のが大きい。
現在は値が変わるたびにプールから借りて返している。

### 検証が要る点

**未検証。実装前に確かめる。**

| 項目 | 内容 |
| --- | --- |
| **`object` 格納の代入可判定** | 現在 `ObjectBindingProperty.IsAssignable<T>` は**実行時の値の型**を見る。`Binder.Bind` がこれを使うため、`ReflectionViewModel`（全プロパティが `object`）の結線がこれに依存している。**静的判定に変えると結線しなくなる。** `ValueHolder<object>` に同じ特例が要る |
| 型付きイベント | `BindingProperty<T>.OnChanged`（`Action<T>`）を誰が持つか。`ValueHolder<TValue>` に移すか、型消去チャネルに寄せるか |
| 比較子 | `EqualityComparer<TValue>.Default` による差分検出を `ValueHolder<TValue>` に移す |
| **ジェネリックなインターフェースメソッド** | `IValueHolder.TryGetValue<T>` は**インターフェース経由のジェネリックメソッド**。IL2CPP では呼び出しごとに解決が入り、直接呼び出しより遅い可能性がある。**実測する** |
| アロケーション | プール撤去で減るはずだが**実測していない**。[TASK-025](TASK-025-strippingVerification.md) の基準値と `Is.Not.AllocatingGCMemory` で確認する |
| AOT | `ValueHolder<T>` の実体化数は `BindingProperty<T>` と同じはずだが、IL2CPP ビルドで確認する |

### 代替案との比較が要る

**不変性の問題は `IBindingProperty<T>` を「公開しない」だけでも解ける。**
クラスがジェネリックであること自体は問題ではない。

| 案 | 内容 | path あたりのオブジェクト数 |
| --- | --- | --- |
| **案 A** | `IBindingProperty<T>` を API から外し、値ベースの読み書きに一本化する。クラス構成は現状のまま | 1（現状と同じ） |
| **案 B** | 値を `ValueHolder` に集約する。`BindingProperty` は非ジェネリックになる | **2**（`BindingProperty` + `ValueHolder<T>`） |

**案 B は path ごとにオブジェクトが 1 つ増える。**

`ValueHolder<T>` に `Path` / `Hash` / イベントを持たせて 1 つに統合する形もあるが、
それは実質的に `BindingProperty<T>` の改名であり、**案 A に戻る。**

**どちらを採るかは実測してから決める。**

### 段取り

1. `IValueHolder` に `TrySetValue<T>` と実行時判定の経路を足す（既存構造のまま）
2. `BindingProperty` を非ジェネリックにし、`IValueHolder` を持たせる
3. `IBindingProperty<T>` / `ObjectBindingProperty` を除去
4. 層 2 の API を値ベースに一本化し、層 1 から例外を投げる
5. 購読 API を改名（`Subscribe` / `SubscribeStrict`）
6. `UnSet` を追加
7. `make verify-strip` と PlayMode テストで回帰を確認

**各段を独立したコミットにする。** 問題が出た段だけ戻せるようにする
（[A-13](../INDEX.md) の方針）。

## 完了条件

### 機能

- 型の関係 4 種（= ↓ ↑ ×）それぞれについて、例外版と `Try` 版の両方のテストを追加すること
- `GeneralViewModel.UnSet` を実装し、「補足」の表の挙動を満たすこと
- 購読 API を `Subscribe`（↑ 許容）/ `SubscribeStrict`（完全一致）に改名すること
- [TASK-003](TASK-003-bindingPropertyRedesign.md) の設計節を実装と一致させること
- `make verify-strip` の基準値が一致すること

### 性能

**現行実装との比較を取り、悪化していないことを示すこと。** 数字を本ファイルに記録する。

| 指標 | 測り方 | 判定 |
| --- | --- | --- |
| 値変更 1 回のアロケーション（完全一致） | `Is.Not.AllocatingGCMemory` | **0 であること** |
| 値変更 1 回のアロケーション（↑ 経路・値型） | 同上 | **現行と同じかそれ以下**（現行はボックス化 1 回） |
| 値変更 1 回の所要時間 | Binder を N 個結線して計測 | **現行比で悪化しないこと** |
| path あたりのメモリ | オブジェクト数と実測 | 増える場合は理由を記録すること |
| IL2CPP 実機での所要時間 | [TASK-025](TASK-025-strippingVerification.md) のプレイヤーで計測 | **現行比で悪化しないこと** |

**案 A と案 B の両方を測ってから採用を決める。** 片方だけ実装して比較なしに進めない。
