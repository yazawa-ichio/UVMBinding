# TASK-028: 多態性を許す範囲の決定

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **仕様検討中** |
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

## 実装メモ

**ここから下は目標仕様ではなく、実装時に効いてくる制約。**

### 型付きハンドルを多態にできない理由

`IBindingProperty<T>` は `T Value { get; set; }` を持つため**不変（invariant）**であり、
`BindingProperty<Base>` を `IBindingProperty<Derived>` としても返せない。
`Property<T>` / `Subscribe<T>` が完全一致のみなのはこの制約による。

### 目標仕様と現状の差

| 項目 | 現状 | 差 |
| --- | --- | --- |
| `Set<T>` の ↓ | `Log.Error` + 無視 | **許すよう変更が必要** |
| `Get<T>` の ↑ | `Log.Error` + 既定値 | **許すよう変更が必要** |
| 型不一致（↑↓ 逆方向・×） | `Log.Error` + 無視 / 既定値 | **例外を投げるよう変更が必要** |
| `TryGetAs<T>` / `TrySet<T>` | 存在しない | **新規** |
| `UnSet` | 存在しない | **新規** |
| ↑ を許す購読 / Binder の結線 | 目標どおり | 無し |
| 層 3 の `SetObject` | 例外を投げる | **目標どおり。変更不要** |
| 購読 API の命名 | `Subscribe` / `SubscribeAssignable` | **`SubscribeStrict` / `Subscribe` へ改名** |
| `SubscribeStrict` の公開範囲 | public | **`internal` にする** |

## 完了条件

- 上表の差をすべて解消すること
- 型の関係 4 種（= ↓ ↑ ×）それぞれについて、例外版と `Try` 版の両方のテストを追加すること
- [TASK-003](TASK-003-bindingPropertyRedesign.md) の設計節を実装と一致させること
