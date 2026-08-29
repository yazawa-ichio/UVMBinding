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

### 「Error」の意味

本ドキュメントで **Error** と書いた場合は以下を指す。**例外は投げない。**

| 項目 | 内容 |
| --- | --- |
| 出力 | `Log.Error`。Unity の `LogType.Error` として出る |
| 内容 | path・格納されている型・要求された型 |
| 操作の結果 | 書き込みは**無視**、読み取りは**既定値**を返す |
| テスト | Unity のテストランナーでは失敗扱いになる |

理由は [TASK-003](TASK-003-bindingPropertyRedesign.md) の方針。**出荷後に落とさない。**
型の不一致は [TASK-007](TASK-007-structuralLint.md) の静的検査で潰す。

---

## 目標仕様

### 外から使う API

| API | 層 | 役割 |
| --- | --- | --- |
| `IViewModel.Set<T>` / `Get<T>` | ViewModel | 値の読み書き |
| `IViewModel.TryGet<T>` | ViewModel | **実行時の値の型で取り出す**（新規） |
| `GeneralViewModel.Set<T>` / `Get<T>` / `TryGet<T>` | ViewModel | 同上 |
| `GeneralViewModel.UnSet` | ViewModel | **格納型の解除**（新規） |
| `Property<T>` | ViewModel | 型付きハンドル |
| `PropertyContainer.Subscribe` 系 | Container | 購読 |
| `Binder<T>` の結線 | View | — |

### 型の関係ごとの挙動

| API | **=** | **↓** T→S | **↑** S→T | **×** |
| --- | --- | --- | --- | --- |
| `Set<T>` | 反映 | **反映** | Error + 無視 | Error + 無視 |
| `Get<T>` | 値 | Error + 既定値 | **値（値型はボックス化）** | Error + 既定値 |
| `TryGet<T>` | `true` + 値 | **実行時の値が T なら `true`** | **`true` + 値** | `false` + 既定値。**Error は出さない** |
| `Property<T>` | 読み書き | Error + 無効 | Error + 無効 | Error + 無効 |
| `Subscribe` 系 | 届く | — | **届く** | 届かない |
| `Binder<T>` の結線 | 結線 | — | **結線** | 結線されない（`Log.Debug`） |

`UnSet` は型に依存しないため上表に含めない（後述）。

### なぜ `Set` は ↓ を許し ↑ を拒むか

**代入の向きがそのまま可否になる。** `Animal` の枠に `Dog` を入れるのは通常の代入。
逆に `Dog` の枠に `Animal` を入れるのは通常の代入でも不可。

`Get` はその逆。`Dog` の枠から `Animal` として取り出すのは通常の代入。

**特別な規則を足していない。C# の代入規則をそのまま path に持ち込んでいる。**

### なぜ `TryGet<T>` を足すか

**↓ の向き（ダウンキャスト）で値を取り出す手段が無い。**

`Animal` として格納した値の実体が `Dog` のとき、`Get<Dog>` は静的な型（`Animal`）だけを見て
Error になる。**実行時の値を見れば取り出せる**が、その経路が存在しない。

```csharp
vm.Set<Animal>("Pet", new Dog());
vm.Get<Dog>("Pet");            // Error。静的には Animal → Dog は不可
vm.TryGet<Dog>("Pet", out var dog);  // true。実行時の値が Dog なので取り出せる
```

**`TryGet` は失敗しても Error を出さない。** 「型を確かめて取り出す」ための API であり、
失敗が正常な結果に含まれる。

### `Property<T>` が完全一致のみな理由

`Property<T>` は**読み書き両方**を持つ。↑ を許すと書き込みが、↓ を許すと読み取りが
成立しなくなる。**両立しないため完全一致のみとする。**

---

## 命名の検討（未決）

現在の購読 API は以下。

| 現在の名前 | 挙動 |
| --- | --- |
| `Subscribe<T>` | 完全一致のみ |
| `SubscribeAssignable<T>` | ↑ を許す |

**上表のとおり `Get` も Binder も ↑ を許すため、購読だけ完全一致が既定なのは一貫しない。**
名前を入れ替える案がある。

| 案 | 既定 | 厳密 |
| --- | --- | --- |
| 現状 | `Subscribe<T>`（完全一致） | — |
| **案 X** | **`Subscribe<T>`（↑ を許す）** | `SubscribeStrict<T>` |

**案 X の方が自然**と考える。理由は 3 つ。

1. **他の API と揃う。** `Get` / Binder が ↑ を許すのに購読だけ違うのは説明しにくい
2. **既定が安全側。** 完全一致は「型が変わったら黙って届かなくなる」ため、既定にすると事故が起きる
3. **名前が長い方が特殊** という原則に合う

ただし両者は**厳密さ以外にも違う**。

| | `Subscribe<T>` | `SubscribeAssignable<T>` |
| --- | --- | --- |
| 通知の元 | プロパティの実体 | path |
| 型が変わったとき | 届かなくなる | 代入可なら届く |

**この差を名前で表すべきかは未決。** `Strict` は厳密さしか表さない。

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
| `Set<T>` / `Get<T>` / `TryGet<T>` | 未登録として扱う。`Get<T>` は T で格納型を決める | 通常どおり |
| `Binder<T>` | 未結線に戻る | 結線し直される |
| `SubscribeAssignable<T>` | 届かない | **届く**（path に対する購読のため） |
| `Subscribe<T>` / `Property<T>` | **無効。取り直しが必要** | 無効のまま |

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
| `Set<T>` の ↓ | **Error + 無視** | **許すよう変更が必要** |
| `Get<T>` の ↑ | **Error + 既定値** | **許すよう変更が必要** |
| `TryGet<T>` | 存在しない | **新規** |
| `UnSet` | 存在しない | **新規** |
| `SubscribeAssignable` / Binder の結線 | 目標どおり | 無し |
| 購読 API の命名 | `Subscribe` / `SubscribeAssignable` | **案 X を採るなら変更** |

## 完了条件

- 上表の差をすべて解消すること
- 型の関係 4 種（= ↓ ↑ ×）それぞれについてテストを追加すること
- [TASK-003](TASK-003-bindingPropertyRedesign.md) の設計節を実装と一致させること
