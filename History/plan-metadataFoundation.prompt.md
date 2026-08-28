## Plan: Binding Metadata Foundation

本体（UVMBinding Runtime / Editor）の改修計画。

バインディンググラフを**本体の外から読み取れる公開 API** として整備し、その上に検査機能（Lint）を載せる。
将来の HTML export（[plan-viewHtmlExportFoundation.prompt.md](plan-viewHtmlExportFoundation.prompt.md)、草案）も同じ基盤に乗せる。

## 背景：現状では外から読めない

パッケージは必要な情報をすべて保持しているが、**取り出す手段が無い。**

| 必要な情報 | 現在の保持場所 | 外部からの可読性 |
| --- | --- | --- |
| `Binder.Path` | `Binder<T>.Path`（public）だが `IBinder` は **internal** | **不可。** `IViewElement` から辿れない |
| イベント名 | `IViewEvent.Name`（public） | 可 |
| ViewModel 型宣言 | `View.m_ReservationViewModel`（private, `CS0414` 抑止付きで未使用） | **不可。** Editor が `SerializedObject` 経由で文字列引きしている |
| 現在値 | `Binder<T>.Get()`（protected） | **不可** |
| 派生値の依存元 | `PropertyProvider<...>.m_SourceKey1..5`（private） | **不可。** `ExtensionPropertyInfo` は Path と Type しか返さない |
| 要素の意味的役割 | どこにも無い | **不可** |
| 表示テキスト | Text / TMP コンポーネント側 | 統一アクセサ無し |

`InternalsVisibleTo` は `UVMBinding.Editor` と `UVMBinding.Tests` のみ。

### `SerializedObject` 文字列引きの問題

`m_ReservationViewModel` は Editor の 4 箇所でフィールド名の文字列リテラルとして参照されている。

- `Editor/Drawer/BinderDrawer.cs:63`
- `Editor/Drawer/BindingViewer.cs:152`, `:169`
- `Editor/Drawer/EventNameDrawer.cs:26`
- `Editor/Drawer/PropertyPathDrawer.cs:48`

フィールド名を変更すると 4 箇所が黙って壊れる。基盤整備と同時に解消する。

---

## Phase 0: バグ修正

基盤とは独立。安価なので先に片付ける。詳細は `analysis-bindingDesignReview.md`。

1. `Runtime/ViewModel/Property/ValueCollection.cs:140` — `GetList() as IEnumerator<T>` が null を返し `foreach` が NRE になる
2. `Runtime/ViewModel/Property/Collection.cs:141` — 非ジェネリック `GetEnumerator()` が同じ理由で null
3. `Runtime/ViewModel/Property/Collection.cs` — `CopyTo` が読み取り操作なのに `SetDirty()` を呼ぶ
4. `Runtime/ViewModel/Property/PropertyContainer.cs:61,65,80` — `UnityEngine.Debug.Log` のデバッグ出力残り

Phase 1 で `Collection` 周辺に触るため、1〜3 は先行して直す。

---

## Phase 1: メタデータ公開層

**この Phase が本計画の中心。** Lint も export も、ここが無いと着手できない。

### 設計方針

- **`InternalsVisibleTo` の拡張では解決しない。** ユーザーが自分のゲーム側アセンブリで定義した Binder も読み取り対象になるため、公開 API が要る。
- **読み取り専用インターフェースを、既存の可変 API とは別に切る。** `IBinder` は結線用の内部 API のまま残し、参照用の型を新設する。
- **追加する公開 API は最小限にする。** 後方互換の負債になる。
- **更新経路を汚さない。** 現在値の読み取りはボックス化するため、明示的インターフェース実装として隔離し、`TryUpdate()` の経路には一切載せない。

### 1.1 バインド情報の公開インターフェース

```csharp
namespace UVMBinding.Core
{
    // 読み取り専用。結線用の internal IBinder とは分離する
    public interface IBinderInfo
    {
        string Path { get; }
        System.Type BindType { get; }
        object GetBoundValue();   // ボックス化する。検査・出力時のみ使用
    }
}
```

`Binder<T>` に明示的インターフェース実装として追加する。`IViewEvent` は既に public で `Name` / `EventType()` を持つため、対称になる。

### 1.2 ViewModel 型宣言の公開

`View` に読み取りアクセサを追加する。

```csharp
public string ReservationViewModelName => m_ReservationViewModel;
```

`#pragma warning disable CS0414` を削除できる。
Editor 側の `SerializedObject.FindProperty("m_ReservationViewModel")` 4 箇所をこのアクセサに置き換える。

**注意。** `ViewModelSourceProvider` / `DataSourceProvider` / `ViewBinder` 経由でバインドされる View は、この宣言を持たない場合がある。Lint は「宣言が無い View は検査対象外」として扱い、**検査できなかった View を一覧で報告する**。黙って通さない。

### 1.3 派生依存の公開

`ExtensionPropertyInfo` に依存元を追加する。

```csharp
public readonly struct ExtensionPropertyInfo
{
    public readonly string Path;
    public readonly System.Type Type;
    public readonly string[] SourcePaths;   // 追加
}
```

`PropertyProvider<T1..T5, TResult>` は既に `m_SourceKey1..5` を保持しているため、`GetInfos()` から返すだけで済む。
`CombineBoolProvider` / `CombineStringProvider` は `m_SourceKeys[]` を持っているためそのまま返せる。

この情報は Lint（依存元パスの存在検査）と export（`<output for="...">`）の両方が使う。

### 1.4 意味的役割の宣言

**アトリビュートで与える。** 仮想プロパティにすると Runtime コアに概念を持ち込むことになるため避ける。

```csharp
[SemanticRole(SemanticRole.Slider)]
public class SliderBinder : Binder<float, Slider> { }
```

- ランタイムのフットプリントがゼロ
- 型ごとにキャッシュしてリフレクションで読む（検査・出力時のみ）
- ユーザー定義 Binder / サードパーティ Binder も同じ手段で宣言できる

語彙は ARIA role を基準とし、Unity 6 の `AccessibilityRole` enum と突き合わせる。演出系 Binder（`GraphicColorBinder` / `PositionBinder` / `AnimatorBoolBinder` 等）は `SemanticRole.None` とし、収集対象から除外する。

### 1.5 走査の単一入口

現状 `View.GetElements()` は**平坦な列**を返す。構造検査にも HTML 出力にも階層が要る。

- 走査結果として View の入れ子と GameObject 階層を保持した木を返す入口を作る
- 子 View の要素は親から除外される既存の挙動（`View.Prepare()`）をそのまま使う
- EditMode（ViewModel 未接続）と Runtime（実値あり）の 2 モードを同じ入口で切り替える

**配置。** 走査結果の型そのものは Runtime に置かず、`UVMBinding.Semantics`（新規 optional assembly）に置く。Runtime 側は 1.1〜1.4 の読み取り API を提供するのみに留め、「ボックス化しない・リフレクションしない」というコアの性格を維持する。

### 1.6 表示テキストの統一アクセサ

Lint（ラベル欠落検査）に必要。`Binder<TValue, TComponent>` の `Target` から取れるが、型ごとに異なる。

`SemanticRole` と同じくアトリビュート、または `IBinderInfo` に `string GetDisplayText()` を足すかを、1.4 の実装時に決める。
既存の `Editor/Drawer/ElementViewer.cs` の `GetName()` が近いルールを持っているため、参照する。

---

## Phase 2: 検査（Lint）

Phase 1 の API のみを使う。EditMode テストとして実装し、CI で落とせる形にする。

### 2.1 構造検査

- バインドパスが、宣言された ViewModel 型に存在しない
- 派生値の依存元パスが存在しない（1.3 を使う）
- View が dispatch するイベント名を ViewModel が購読していない
- ViewModel が公開しているイベントを、どの View も dispatch していない
- 同一 View 内でイベント名が衝突している
- ViewModel 型が宣言されておらず検査できない View の一覧

### 2.2 命名品質検査

**export 計画は「既存の命名が意味を持つこと」に賭けている。** その賭けが成立しているかを、export に着手する前にここで測る。

- `Click\d+` / `Button\d+` / `Value\d+` 等の無意味名をブロックリストで検出
- 対話可能な `SemanticRole` を持つ要素にラベルが無い（1.6 を使う）

**この検査の出力が、export 計画 Step 1 の実測データそのものになる。** 違反が多ければ、作るべきは exporter ではなく命名規約である。

### 2.3 実行時ビューア

既存の `Editor/Drawer/BindingViewer.cs` を拡張し、Play 中に未解決バインド・型不一致を一覧表示する。静的検査と同じ Phase 1 API を使う。

---

## Phase 3: コア品質（並行トラック）

export / Lint とは独立だが本体の品質に効く。Phase 1・2 と並行して進められる。
詳細は `analysis-bindingDesignReview.md`。

### 3.1 Collection の差分更新 + プーリング

規模が出た時点で確実に刺さるため、独立トラックの中では最優先。
`CollectionChangedEventArgs<T>`（Add / Remove / Replace / Move / Reset）の導入と、`Destroy` からプール返却への変更。

**Phase 1 との接点。** 差分更新の実装時に、`CollectionBinder` が件数と仮想化の有無を `IBinderInfo` 経由で公開できるようにしておく。

### 3.2 ViewModel 単位の version ゲート

`PropertyContainer` に version カウンタを持たせ、`Binding` が最後に見た値と比較して `TryBinderUpdate()` を丸ごと飛ばす。
実装が軽いため、先に入れて感触を見てよい。

実装上の注意（bool ではなく version にする理由、`TryRebind()` の別枠扱い、`ViewModelPropertyExtension` の第二コンテナ、`View.IsActive` への `activeInHierarchy` 追加）は `analysis-bindingDesignReview.md` の改善候補 3 に記載。

### 3.3 宣言的 computed（`[DependsOn]`）

```csharp
[Bind, DependsOn(nameof(First), nameof(Last))]
public string FullName => $"{First} {Last}";
```

ILPP が既に `[Bind]` を処理しているため、依存元 setter に `SetDirty(派生path)` を注入する形で実装できる。

**export への影響。** 宣言的に書ける派生が増えるほど、export 側で静的に理解できる範囲が広がる（export 計画の系統 2）。単なる記述の簡便さではなく、**基盤の解像度を上げる機能**として位置づける。1.3 の `SourcePaths` と同じ経路で公開する。

---

## Phase 4（保留）

以下は消費者が確定するまで着手しない。**使う先が無い API を先に生やさない。**

- `[Consequence]` 注釈（不可逆・有料操作のマーキング）— export 計画の系統 4
- 設計時プレビュー（EditMode で `ReflectionViewModel` を接続してシーンビューに反映）— 1.5 の EditMode 走査が前提
- `UVMBinding.Semantics` アセンブリでの HTML / JS 出力

---

## Implementation Steps

1. Phase 0 のバグ修正
2. 1.1 `IBinderInfo` の追加
3. 1.2 `ReservationViewModelName` の公開と、Editor 4 箇所の文字列引き置換
4. 1.3 `ExtensionPropertyInfo` への `SourcePaths` 追加
5. **2.1 構造検査の実装**（ここで基盤の妥当性が検証される）
6. 1.4 `SemanticRole` アトリビュートと組み込み Binder への付与
7. 1.6 表示テキストアクセサ
8. 2.2 命名品質検査
9. 1.5 走査の単一入口と `UVMBinding.Semantics` アセンブリの新設
10. 2.3 実行時ビューアの拡張

Phase 3 は 2 以降と並行。3.2 → 3.1 → 3.3 の順。

**Step 5 を早い位置に置いている理由。** 構造検査は 1.1〜1.3 だけで実装でき、そこで「公開 API が実用に足るか」が判明する。残りの公開 API を作り込む前に検証したい。

---

## Relevant Files

### 変更対象

- [Unity/Assets/UVMBinding/Runtime/Binder/Binder.cs](Unity/Assets/UVMBinding/Runtime/Binder/Binder.cs) — 1.1
- [Unity/Assets/UVMBinding/Runtime/Binder/IBinder.cs](Unity/Assets/UVMBinding/Runtime/Binder/IBinder.cs) — 1.1
- [Unity/Assets/UVMBinding/Runtime/View/View.cs](Unity/Assets/UVMBinding/Runtime/View/View.cs) — 1.2、3.2
- [Unity/Assets/UVMBinding/Runtime/PropertyProvider/ExtensionPropertyInfo.cs](Unity/Assets/UVMBinding/Runtime/PropertyProvider/ExtensionPropertyInfo.cs) — 1.3
- [Unity/Assets/UVMBinding/Runtime/PropertyProvider/IPropertyProvider.cs](Unity/Assets/UVMBinding/Runtime/PropertyProvider/IPropertyProvider.cs) — 1.3
- [Unity/Assets/UVMBinding/Runtime/PropertyProvider/CombineBoolProvider.cs](Unity/Assets/UVMBinding/Runtime/PropertyProvider/CombineBoolProvider.cs) — 1.3
- [Unity/Assets/UVMBinding/Runtime/ViewModel/Property/PropertyContainer.cs](Unity/Assets/UVMBinding/Runtime/ViewModel/Property/PropertyContainer.cs) — Phase 0、3.2
- [Unity/Assets/UVMBinding/Runtime/ViewModel/Property/Collection.cs](Unity/Assets/UVMBinding/Runtime/ViewModel/Property/Collection.cs) — Phase 0、3.1
- [Unity/Assets/UVMBinding/Runtime/ViewModel/Property/ValueCollection.cs](Unity/Assets/UVMBinding/Runtime/ViewModel/Property/ValueCollection.cs) — Phase 0
- [Unity/Assets/UVMBinding/Runtime/View/Binding.cs](Unity/Assets/UVMBinding/Runtime/View/Binding.cs) — 3.2
- [Unity/Assets/UVMBinding/Runtime/Binder/Binders/CollectionBinder.cs](Unity/Assets/UVMBinding/Runtime/Binder/Binders/CollectionBinder.cs) — 3.1

### Editor 側（文字列引きの置換）

- [Unity/Assets/UVMBinding/Editor/Drawer/BinderDrawer.cs](Unity/Assets/UVMBinding/Editor/Drawer/BinderDrawer.cs)
- [Unity/Assets/UVMBinding/Editor/Drawer/BindingViewer.cs](Unity/Assets/UVMBinding/Editor/Drawer/BindingViewer.cs)
- [Unity/Assets/UVMBinding/Editor/Drawer/EventNameDrawer.cs](Unity/Assets/UVMBinding/Editor/Drawer/EventNameDrawer.cs)
- [Unity/Assets/UVMBinding/Editor/Drawer/PropertyPathDrawer.cs](Unity/Assets/UVMBinding/Editor/Drawer/PropertyPathDrawer.cs)

### 参照

- [Unity/Assets/UVMBinding/Runtime/AssemblyInfo.cs](Unity/Assets/UVMBinding/Runtime/AssemblyInfo.cs) — `InternalsVisibleTo` の現状
- [Unity/Assets/UVMBinding/Editor/Drawer/ElementViewer.cs](Unity/Assets/UVMBinding/Editor/Drawer/ElementViewer.cs) — 既存の表示名ルール（1.6）
- [Unity/Assets/UVMBinding.Tests/Tests/BindingTests.cs](Unity/Assets/UVMBinding.Tests/Tests/BindingTests.cs)

---

## Verification

1. `IBinderInfo` から Path・型・現在値が取得できるユニットテスト
2. `SourcePaths` が `CombineBoolProvider` と `PropertyProvider<...>` の双方から取得できること
3. Editor の文字列引き置換後、既存の Drawer が同じ動作をすること
4. 構造検査が既知の違反（存在しないパス、購読されないイベント）を検出すること
5. **現行のテスト全通過。** 特に `BindingTests` / `ReflectionTests` が公開 API 追加で壊れていないこと
6. 3.2 導入後、更新スキップが起きても表示が正しく追従すること（複数 View が同一 ViewModel を参照するケースを含む）

---

## Decisions

- 読み取り専用 API を新設する。`InternalsVisibleTo` の拡張では解決しない（ユーザー定義 Binder が対象外になるため）。
- 現在値の読み取りはボックス化を許容するが、明示的インターフェース実装として隔離し、更新経路には載せない。
- 意味的役割は仮想プロパティではなくアトリビュートで与える。Runtime のフットプリントをゼロに保つため。
- 走査結果の型は Runtime に置かず、`UVMBinding.Semantics` に置く。
- ViewModel 型が宣言されていない View は検査対象外とし、**検査できなかったことを報告する**。黙って通さない。
- 消費者が確定していない API（`[Consequence]` 等）は Phase 4 として保留する。
- 構造検査（Step 5）を公開 API の作り込み途中に置き、基盤の妥当性を早期に検証する。

---

## Risks

- **公開 API の後方互換負債。** 一度公開すると削除しにくい。Phase 4 の保留はこのため。
- **`SemanticRole` の語彙固定。** ARIA と Unity `AccessibilityRole` の突き合わせが未実施。1.4 着手前に調査する。
- **走査の入口が既存挙動に依存。** `View.Prepare()` の「子 View の要素を親から除外する」挙動に乗るため、そこを変更すると走査結果も変わる。
