# TASK-028: 多態性を許す範囲の決定

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **目標確定 / 実現方法を検討中** |
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

## 実現方法

### 制約の出どころ

**`IBindingProperty<T>` を外部に露出していること。**

この型は `T Value { get; set; }` を持つため**不変（invariant）**であり、
`BindingProperty<Base>` を `IBindingProperty<Derived>` として返せない。
型を跨げないのはこれが理由。

**クラスがジェネリックであること自体は制約ではない。** 露出をやめ、値ベースの読み書きに
一本化すれば、目標仕様は満たせる。

### 現状のアロケーション

変更の影響を測る基準として記録する。

| # | 箇所 | 定常状態 |
| --- | --- | --- |
| 1 | 値の変更時の `ValueHolder<T>.Use()` | **無し。** プールが最大 5 個を保持。`UseScope` は構造体。購読者が居なければプール操作自体を行わない（[TASK-016](TASK-016-writePathAllocation.md)） |
| 2 | `IValueHolder.TryGetValue<T>` の ↑ 経路 | **値型のボックス化 1 回。** 通知のたび |
| 3 | `Binder.TryUpdate` の非完全一致経路 | **値型のボックス化 1 回。** 値の変更のたび、Binder ごと |
| 4 | 購読時のクロージャ | 購読ごとに 1 回。定常では発生しない |
| 5 | `Log.Trace` の文字列整形 | 有効時のみ。既定では通らない |

**定常状態で効くのは 2 と 3 のボックス化。** どちらも型消去に由来し、どちらの案でも消えない。
**3 が最も効く**（`ReflectionViewModel` は全プロパティが `object` 格納のため、結線した
すべての Binder が該当する）。

---

### 案 A：`IBindingProperty<T>` を利用者向け API から外す

`internal` のまま残し、層 1・層 2 の API を値ベースに一本化する。

```
BindingProperty                     … 非ジェネリック。Path / Hash
└ BindingProperty<TValue>           … 値・型付きイベント・比較子
   └ ObjectBindingProperty          … 型消去用の特例
ValueHolder<TValue>                 … 通知のたびにプールから借りる使い捨て
```

**クラス構成は現状のまま。** `Binder.TryUpdate` も現在のコードのままでよい。

### 案 B：値を `ValueHolder` に集約する

`BindingProperty` を非ジェネリックにし、値を `IValueHolder` に持たせる。

```
BindingProperty                     … 非ジェネリック。Path / Hash / IValueHolder
ValueHolder<TValue>                 … 値の格納そのもの。path ごとに 1 つ
```

`IValueHolder` には既に `TryGetValue<T>` があり、**目標仕様の `Get<T>` の「=」と「↑」が
そのまま実装されている。** 対称に `TrySetValue<T>` を足せば「=」と「↓」も揃う。

```csharp
public bool TrySetValue<T>(T value)
{
    if (this is ValueHolder<T> h) { h.Value = value; return true; }   // = ボックス化なし
    if (Assignable<T, TValue>.Is) { Value = (TValue)(object)value; return true; }  // ↓
    return false;
}
```

---

### 比較

| 項目 | 案 A | 案 B |
| --- | --- | --- |
| 目標仕様を満たすか | **満たす** | **満たす** |
| `IBindingProperty<T>` の露出 | 無くなる | 無くなる |
| `ObjectBindingProperty` | 残る | **除去できる** |
| `ValueHolder` のプール | 残る | **撤去できる**（CPU の手数が減る） |
| クラス階層 | 現状のまま | **浅くなる** |
| `Binder.TryUpdate` | **変更不要** | ジェネリックなインターフェースメソッド経由になる |
| path あたりのオブジェクト | 1 | **2**（許容と判断済み） |
| 間接参照 | 1 段 | **2 段** |
| AOT の実体化 | 現状のまま | **組み合わせが増える** |

### 案 B にのみ伴うリスク

| 分類 | 内容 |
| --- | --- |
| **CPU** | 値の読み取りが `IValueHolder.TryGetValue<T>` 経由になる。**インターフェース上のジェネリックメソッド**で、IL2CPP では呼び出しごとに実体の解決が入る。しかも**デフォルトインターフェース実装**で段数が増える。**Binder ごと・値の変更ごとに効く** |
| CPU | 間接参照が 1 段増える |
| **AOT** | `ValueHolder<T>.TryGetValue<U>` は**ジェネリック型のジェネリックメソッド**。値型の組み合わせごとに実体が要り、IL2CPP が生成し損ねる可能性が現在より高い |
| 移設 | `BindingProperty<T>` が持つ**型付きイベント**（`Action<T>`）と**比較子**（`EqualityComparer<TValue>.Default`）の持ち先を決める必要がある |
| **既存挙動の維持** | `ObjectBindingProperty.IsAssignable<T>` は**実行時の値の型**を見ている。`Binder.Bind` がこれを使うため、`ReflectionViewModel`（全プロパティが `object`）の結線がこれに依存する。**同じ特例を `ValueHolder<object>` に持たせないと結線しなくなる** |

### 判断

**案 A を先に実装し、目標仕様を満たした状態で計測する。** そのうえで案 B に進むかを決める。

案 B は**仕様の要求ではなく構造の整理**であり、上記のリスクを負う価値があるかは計測で判断する。
**悪化するなら案 A で止める。**

---

### 段取り

**第 1 段階（案 A）— 目標仕様を満たす**

1. 層 3 に値ベースの読み書きを足す（`TrySetValue<T>` と実行時判定）
2. 層 2 の API を値ベースに一本化する。`Get<T>` が型付きハンドルを返す形をやめる
3. 層 1 の `Get<T>` / `Set<T>` を ↑ ↓ 対応にし、型が合わなければ例外を投げる
4. `TryGetAs<T>` / `TrySet<T>` を追加する
5. 購読 API を改名する（`Subscribe` / `SubscribeStrict`）。`SubscribeStrict` は `internal` にする
6. `GeneralViewModel.UnSet` を追加する
7. `IBindingProperty<T>` を利用者向け API から外す

**ここまでで目標仕様を満たす。**

**第 2 段階（案 B）— 構造を寄せる。実施は計測後に判断**

8. `BindingProperty` を非ジェネリックにし、`IValueHolder` を持たせる
9. `ObjectBindingProperty` を除去する
10. `ValueHolder` のプールを撤去する

**検証**

11. `make test` / `make test-edit` / `make verify-strip`
12. 性能比較（「完了条件・性能」）

**各段を独立したコミットにする。** 問題が出た段だけ戻せるようにする（[A-13](../INDEX.md) の方針）。

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

**第 1 段階（案 A）を実装して測り、そのうえで第 2 段階（案 B）に進むかを決める。**
第 2 段階は仕様の要求ではないため、性能が悪化するなら実施しない。
