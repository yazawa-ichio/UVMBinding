# TASK-003: BindingProperty の再設計（1 path = 1 値）

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **完了** |
| 依存 | TASK-001, TASK-002, TASK-016（TASK-022 は先行推奨） |
| 規模 | **大** |

## 目的

path に対して型ごとにプロパティを登録する仕様を廃止し、**1 path = 1 値**の制約を導入する。`BindingProperty.Next` のチェーンを削除する。

## 背景

### 現状

`PropertyContainer` は `Dictionary<string, BindingProperty>` を持ち、同一 path に対して型の異なる `BindingProperty<T>` を `Next` の連結リストで保持している。

**これらは独立した値である。** `Set<int>("Hp", 10)` は `BindingProperty<int>` のみを更新し、同じ path の `BindingProperty<float>` には何も起きない。

### なぜ今まで手が付かなかったか

- **パフォーマンス** — 1 path 1 値にすると、型を跨ぐ読み取りでボックス化が避けられないように見えた
- **購読時の基盤型指定** — 購読者ごとに型が違う場合、型ごとのプロパティが必要に見えた

### なぜ今できるか

**購読側は既に解決済み。** `IValueHolder.TryGetValue<T>()` が型消去された読み取りを実装しており、完全一致時はボックス化しない。

```csharp
bool TryGetValue<T>(out T value)
{
    if (this is ValueHolder<T> h) { value = h.Value; return true; }  // 完全一致・ボックス化なし
    if (IsAssignable<T>())        { value = (T)GetObject(); return true; }
    value = default; return false;
}
```

`SubscribeAssignableHandle<T>` は既に `PropertyContainer.OnChangedValue`（path + `IValueHolder`）を購読し、型を配信時に解決している。**型ごとのプロパティを必要としていない。**

チェーンが残っている理由は pull 側（`Get<T>`）と `Binder.Bind` のみ。

### 現に起きている不具合

`Collection<T>` がチェーンに隠された型不一致を起こしている。

```csharp
m_VM.Property.Subscribe<List<T>>(Path, OnNotifyChanged);   // List<T> を購読
m_VM.Set<List<IViewModel>>(Path, m_List);                  // List<IViewModel> に書き込み
```

チェーンに 2 エントリができ、**`Collection<T>.OnChanged` は一度も発火しない**。`CollectionBinder` が `IsAssignable` 経由で動くため表面化していない。

**1 path = 1 型であれば起こり得なかった不具合。** 修正自体は [TASK-001](TASK-001-collectionEnumerator.md) で先行して行う。

---

## 設計

```
1 path = 1 BindingProperty<T>    T は最初の登録で確定。Next は廃止

書き込み  Set<U>(path, val)      U == T           → 直接代入（ボックス化なし）
                                 T == object      → 常に受け入れる（型消去ケース）
                                 それ以外の不一致  → Log.Error して無視

読み取り  TryGetValue<U>(path)   U == T           → 直接（ボックス化なし）
                                 T が U に代入可   → (U)GetObject()（ボックス化あり）
                                 不一致           → false

購読      container.OnChangedValue に一本化（SubscribeAssignable が既にこの形）

型変換    Binder 層の Converter の責務。PropertyContainer は型を跨がない
```

### 要点：型変換を Binder 層に押し出す

`PropertyContainer` が型を跨ぐ責務を持っていたことが複雑さの根源だった。**Converter という正しい置き場が既にある。**

### `BindingProperty<object>` の扱い

`ObjectBindingProperty` は型消去のケースであり、**任意の型の書き込みを受け入れる**。`ReflectionViewModel` が `ShouldCreateProperty` 経由で生成するのはこれ。読み取りは `IsAssignable` が実行時の値の型を見るため従来通り動く。

### 型不一致時の挙動

**例外を投げず、`Log.Error` して無視する。** 理由は、出荷したゲームで型不一致が起きたときに落とさないため。

型不一致は**静的検査で防ぐ**（[TASK-007](TASK-007-structuralLint.md)）。実行時は落とさず記録する、という方針を全体で揃える。

`Get<T>` は不一致時に `null` を返す。呼び出し側（`SetImpl` 等）は null チェックで no-op とする。

---

## 影響範囲

### `Next` を辿っている箇所

- `PropertyContainer.cs:81`（`SubscribeAssignable`）, `:111`, `:115`（`Get<T>`）, `:177`（`SetDirty`）, `:191`（`SetAllDirty`）, `:212`（`GetAllImpl`）
- `Binding.cs:148`（`m_Extend` の走査）

`EventBroker` にも `Next` があるが**別機構であり本タスクの対象外**（イベント名に対する型違いのイベント）。

### `Get<T>` に依存している箇所

- `ViewModel.GetImpl<T>` / `SetImpl<T>` — ILPP が `[Bind]` の宣言型から生成するため型は一貫している
- `Property<T>` のコンストラクタ
- `PropertyContainer.Subscribe<T>` / `Unsubscribe<T>`
- `ViewModelPropertyExtension.Set<T>`
- `Collection<T>` / `ValueCollection<T>`（TASK-001 で型を揃える）
- `GeneralViewModel.Get<T>` / `Set<T>` — **手書きのため型不一致が起こりうる。要確認**

### `Binder<T>.Bind(IBindingProperty)` の 3 分岐

```csharp
if (m_Converter != null && m_Converter.TryConvert(prop, ref m_Property)) { ... }
else if (prop is IBindingProperty<T> ret) { ... }
else if (prop.IsAssignable<T>()) { ... }
else { Log.Debug("Not Found BindingProperty ..."); }
```

**この 3 分岐自体は変わらない。** `Binder.Bind` は `Get<T>` を経由せず、渡された 1 つのプロパティを判定するだけなので、チェーン廃止の影響を直接は受けない。

影響が出るのは **`Binding.Bind` が `GetAllImpl` で列挙するプロパティ数が減る**点。チェーンに 2 件以上あった場合、これまでは同じ path の全エントリが順に `BindToBinder` へ渡され、最後に成功したものが勝っていた。1 件になると、その 1 件で判定が決まる。

**チェーンに 2 件以上できるのは、同じ path に対して `Get<T1>` と `Get<T2>` の両方が呼ばれた場合のみ。** 該当するのは `Property<T>` / `Subscribe<T>` / `Collection<T>` / `GeneralViewModel` であり、Binder 側からは発生しない。

### 移行棚卸しの範囲（調査で縮小）

当初「Converter が必須になるケースが増える」と見積もっていたが、[TASK-022](TASK-022-castConverterConsolidation.md) の調査で**過大評価と判明した**。

- `IsAssignable` は数値の暗黙変換を通さない（`typeof(float).IsAssignableFrom(typeof(int))` は false）。数値の型不一致は**現在も Converter が必須**であり、チェーン廃止で新たに必要になるわけではない
- そして Cast Converter の使用箇所は**リポジトリ内に 0 件**。つまり数値型の不一致バインドが存在しない

**したがって棚卸しの主対象は `Get<T>` の呼び出し側**であり、Binder / prefab 側ではない。

prefab 側も一応確認するが、**互換性を守るためではなく、壊れる箇所を把握して直すため**（決定 A-9）。[TASK-007](TASK-007-structuralLint.md) の Lint 項目「変換手段が無い型不一致」として自動化できるため、**Lint が先に動くならそちらを使う**。

### その他

- `ReflectionViewModel` — 全て `object` 型。上記の特例で従来通り動くはず
- `ViewModelPropertyExtension` の第二 `PropertyContainer`
- `Binding.m_Extend`

---

## 調査結果（2026-08-29）

### チェーンは実際にはほとんど作られていない

`Get<T>` がチェーンを継ぎ足す箇所に一時的な記録を入れ、実測した。

| 対象 | チェーン生成 |
| --- | --- |
| PlayMode テスト 34 件 | **1 件** |
| ショーケース全 11 画面（操作込み） | **0 件** |

唯一の 1 件は `SubscribeTests` の「PropertyのHandleで解除出来る」で、`Subscribe<int>` と
`Subscribe<string>` を同じ path に張って両方が独立に発火することを確かめている。
**廃止する仕様そのものを検証しているテスト**であり、本タスクで書き換える対象。

### 利用側にチェーンを作る経路が無い

実プロジェクトを静的に調べた結果は以下。

| 観点 | 結果 |
| --- | --- |
| `PropertyContainer.Get<T>` / `Set<T>` の直接呼び出し | **0 件**（`Get<T>` の検出 11 件はすべて別 API） |
| `GeneralViewModel` | **0 件** |
| `ViewModelPropertyExtension` / PropertyProvider | **0 件**（コード・prefab とも） |
| `Property<T>` 宣言 | 10 件。すべて 1 path に 1 型 |
| `Collection<T>` | 15 件 |

`Subscribe<T>` の 1 件は `EventBroker` に対するもので**本タスクの対象外**。

**チェーンは同一 path が異なる T で 2 回触られたときにしかできない。** 宣言箇所が
path ごとに 1 つしかない以上、実プロジェクトでは発生し得ない。

### 型を決めるのは「最初に触った側」

プロパティは `Get<T>` で**遅延生成**される。`[Bind]` の事前登録は無い。
したがって 1 path = 1 型にすると、**型は最初にその path を触ったコードで確定する**。

ただし上記のとおり **path ごとに型を持つ箇所は 1 つしかない**ため、実害は無い。
`Binder.Bind` は `Get<T>` を経由せず、渡された 1 つのプロパティを判定するだけなので
View 側から型が確定することもない。

### `ReflectionViewModel` は影響を受けない

書き込みは `property.SetObject(value)`、つまり**型消去されたインターフェース経由**で行っており
`Get<T>` を通らない。`ObjectBindingProperty` の `TValue` は `object` なのでキャストも常に成功する。

---

## 移行手順

1. TASK-001 で `Collection<T>` の型不一致を修正する（チェーンなしでも動く状態にする）
2. TASK-016 で書き込みパスの `ValueHolder` ガードを入れる
3. **既存 prefab の型不一致を棚卸しする。** 宣言型と Binder 型が異なり Converter が無い箇所を列挙する
4. `Get<T>` を型一致必須に変更し、`Next` を削除する
5. `SetDirty` / `SetAllDirty` / `GetAllImpl` / `SubscribeAssignable` からチェーン走査を削除する
6. `Binding.m_Extend` の走査を修正する
7. 型不一致のログを入れ、既存テストと[ショーケース](TASK-023-showcase.md)で不一致が出ないことを確認する

## 実装結果（2026-08-29）

### 変更点

| 対象 | 変更 |
| --- | --- |
| `BindingProperty` | `Next` フィールドを削除 |
| `PropertyContainer.Get<T>` | 型が合わなければ `Log.Error` して **null を返す** |
| `PropertyContainer.Subscribe` / `Unsubscribe` | null ガード |
| `PropertyContainer.SubscribeAssignable` | チェーン走査を削除 |
| `PropertyContainer.SetDirty` / `SetAllDirty` / `GetAllImpl` | チェーン走査を削除 |
| `ViewModel.GetImpl` / `SetImpl` | null ガード。読み取りは `default` を返す |
| `Property<T>` | null ガード。`Value` の読み書きと `SetDirty` |
| `ViewModelPropertyExtension.Set<T>` | null ガード |
| `Binding` | `m_Extend` の走査を削除 |

### `Binding.m_Extend` は死んでいた

`m_Extend` は**宣言と読み出ししか存在せず、代入箇所が無かった**。常に null であり、
チェーン走査のループは一度も実行されていない。フィールドごと削除した。

### 検証

| 項目 | 結果 |
| --- | --- |
| PlayMode テスト | **43 / 43**（新規 9 件を含む） |
| EditMode テスト | 1 / 1 |
| ショーケース（IL2CPP + Stripping High） | **基準値と完全一致** |

**ショーケースの基準値が 1 行も変わらなかった。** 446 件の状態（操作前後・色・座標・活性・入力値）が
再設計の前後で同一であり、[TASK-023](TASK-023-showcase.md) の基準値が回帰検出として機能した。

### 書き換えたテスト

`SubscribeTests.PropertyのHandleで解除出来る` は `Subscribe<int>` と `Subscribe<string>` を
同じ path に張り、両方が独立に発火することを検証していた。**廃止した仕様そのもの**のため、
1 path = 1 型を前提とする内容に書き換えた。

### 新規テスト（`SinglePropertyTypeTests`）

型不一致で `Log.Error` が出て無視されること、`Subscribe` / `Unsubscribe` が例外にならないこと、
`BindingProperty<object>` が任意の型を受け入れること、`SubscribeAssignable` の 3 ケース
（完全一致・代入可能・不一致）、path ごとにプロパティが 1 つしか作られないこと、
完全一致の書き込みでボックス化しないこと。

**アロケーション計測では `Logger.Log.Level` を一時的に `Error` へ落としている。**
`Trace` が有効だとログの整形でアロケーションが発生するため。既定は Editor で `Debug`、
プレイヤーで `Warning` なので通常は通らない経路。

## 完了条件

- `BindingProperty.Next` が存在しないこと
- 同一 path に異なる型を登録しようとした際、`Log.Error` が出て無視されるテスト
- `BindingProperty<object>` が任意の型の書き込みを受け入れるテスト
- `SubscribeAssignable<T>` が型消去経由で正しく値を受け取るテスト（完全一致・代入可能・不一致の 3 ケース）
- 完全一致の読み取りでボックス化が発生しないこと（アロケーション計測）
- `Collection<T>.OnChanged` が発火すること
- **既存テスト全通過**（`BindingTests` / `BinderTests` / `ReflectionTests` / `SubscribeTests`）
- **[ショーケース](TASK-023-showcase.md)の画面 1〜7 のチェックリストをすべて満たすこと**（Console にエラー・例外が出ないことを含む）

## リスク

- **本計画で最も規模が大きい。** `PropertyContainer` / `BindingProperty` / `Binding` / `Binder` に横断的に手が入る
- `GeneralViewModel` の手書き利用箇所は型の一貫性が保証されていない。ただし**実プロジェクトでの直接利用は確認されなかった**ためリスクは小さい
- `ReflectionViewModel` は全プロパティが `object` 型。特例の扱いを誤ると広範囲に影響する。クラス名としての直接利用は確認されなかったが、**`IView.Bind(object)` 拡張メソッド経由で暗黙に生成される経路がある**（実プロジェクトに十数箇所）。この経路を見落とさないこと

**prefab が壊れるリスクは当初想定より小さく、かつ互換性は維持しない方針**（決定 A-9）。影響の主対象は `Get<T>` の呼び出し側であり、Binder 側の結線判定は変わらない。

ただし手順 3 の確認は省略しないこと。**「壊れてよい」ことと「壊れたまま気付かない」ことは別。**

## 先行推奨：TASK-022

[TASK-022](TASK-022-castConverterConsolidation.md)（Cast Converter の集約）は `IConverter.TryConvert` のシグネチャを変更し、`Binder.Bind` にも手が入る。**本タスクと同じ箇所を触るため、先に済ませると二度手間にならない。**

ただし TASK-022 は SerializeReference のジェネリック対応検証で分岐するため、結果次第で規模が変わる。**ハード依存にはしない。** 先行できなければ本タスクを先に進めてよい。

## 備考

破壊的変更は許容される方針（2026-08-28 決定）のため、API の変更に制約はない。

[TASK-013](TASK-013-versionGate.md)（version ゲート）も `PropertyContainer` に手を入れるため、**本タスクを先に完了させる**こと。逆順だと version カウンタを再設計後に入れ直すことになる。
