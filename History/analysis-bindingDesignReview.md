## Analysis: 設計レビューと他パッケージ比較

UVMBinding の設計を他のデータバインディング実装と比較し、改善候補を整理したもの。

### 評価軸

**自分のゲームを作るための道具として評価する。** 公開ライブラリとしての普及（star 数、ドキュメント整備、OpenUPM 登録など）は評価対象に含めない。他人が使えること自体は問題ないが、判断基準にはしない。

したがって重視するのは以下。

- 画面 1 枚を作るのにかかる手間
- 壊れたときに早く気付けるか
- 実際のゲームのコンテンツ量に耐えるか
- 設計上の制約が開発の邪魔をしないか

---

## 調査対象

| 方向 | 対象 |
| --- | --- |
| Unity 公式 | UI Toolkit Runtime Binding（2023.2+ / Unity 6） |
| Unity サードパーティ | Loxodon Framework、Unity-Weld、R3 / ObservableCollections (Cysharp) |
| .NET / XAML | UWP `x:Bind`、MAUI Compiled Bindings、CommunityToolkit.Mvvm、WPF 診断ツール |
| シグナル系 | SolidJS、Vue Reactivity、Angular Signals |
| ネイティブ UI | SwiftUI Observation (`@Observable`)、Jetpack Compose Snapshot、Qt 6 QProperty / QML |

---

## 現状アーキテクチャの要約

### 更新モデル

`Canvas.preWillRenderCanvases` → `ViewUpdater` → 全 `View` → 全 `Binder.TryUpdate()`。
各 Binder は `IBindingProperty.Hash` と自身が保持する `m_Hash` を比較し、差があるときだけ `UpdateValue()` を呼ぶ。

つまりポーリング（ダーティチェック）型。`BindingProperty<T>.OnChanged` という push 用の経路は存在するが、Binder は購読していない。

### 書き込みコスト

`SetImpl<T>` は「辞書引き → `EqualityComparer<T>` で等値比較 → 代入 → `Hash++`」で完結する。
購読者がいなければデリゲート呼び出しは null チェックのみ。

### ポーリングを採用している理由（設計意図）

**ViewModel を計算の一部としてそのまま使えるだけの軽さを保つため。**

`vm.Hp = hp;` を計算式の途中で何回書いてもコストが変わらないことを前提に置いている。Push にすると以下が壊れる。

- 1 フレーム内で同じ値を複数回書くと UI 更新が複数回走る
- 書き込み位置によって更新の実行順が変わる
- 更新中の再入を考慮する必要が出る

この「中間の書き込みを畳んで境界で一度だけ評価する」という性質自体は、Compose の snapshot や Qt 6 の遅延評価と同じ狙いである。UVMBinding はその境界をフレームに置いた。手法は素朴だが、得ている性質は同じ。

**したがって更新伝播方式については、ポーリングは妥協ではなく前提を守るための選択であり、設計上妥当と判断する。**

---

## 設計判断の評価

### 妥当と判断したもの（維持する）

- **ポーリングによるフレーム境界での畳み込み** — 上記の通り。
- **フラットなパス** — 両側を自分が握っている前提なら、ネスト解決コストを毎フレーム払わない方が正しい。`ParentViewBind` / `SelectViewBind` / `DataSourceProvider` による View 側での入れ子表現は Unity では自然。
- **Binder = MonoBehaviour のコンポジション** — View ごとにスクリプトを書かない。Loxodon はコードで `BindingSet` を書く方式なので、この点では設計判断として優位。画面 1 枚あたりのコード量がゼロに近いのが最大の価値。
- **ボックス化回避 / リフレクション不使用** — 型付き `BindingProperty<T>` による。ただし現代のフレームワークは概ね達成しているので、差別化要因ではなく最低ラインとして維持するもの。
- **ViewModel の Unity 非依存** — 値の層がテスト可能。
- **`versionDefines` による optional 依存の自動有効化** — TMP / uGUI / Localization の切り分けは丁寧。

### 環境制約により取り込めないもの

現代的な手法の一部は Unity にそのまま持ち込めない。取り込まないのは怠慢ではなく制約。

- **signals の自動依存追跡** — effect scope がクロージャ割り当てになり GC 圧になる。
- **コンパイル時バインド検証（`x:Bind` 等）** — マークアップ言語（XAML / JSX）が前提。Unity の「マークアップ」は prefab であり、コードではなくデータなので原理的に同じ手が使えない。
- **Compose / SwiftUI の read tracking** — 言語・コンパイラ支援が前提。

### 未着手のもの（移植可能だが手をつけていない）

- コレクションの差分更新
- パスの静的検証
- ViewModel 単位での更新スキップ
- 依存関係を宣言できる computed
- Command（実行可否・実行中）の抽象

---

## 改善候補（優先順）

### 1. Collection の差分更新 + プーリング

**問題。** `Collection<T>` / `ValueCollection<T>` はどの操作でも `SetDirty()` を呼ぶだけで、`CollectionBinder.UpdateValue` が全要素を再バインドする。さらに余剰分は `GameObject.Destroy`、不足分は `Instantiate` でプーリングがない。1 要素の追加で全行が再バインドされる。

インベントリ、クエスト一覧、ランキングなど実際のゲームで必ず出てくる形なので、規模が出た時点で確実に刺さる。

**参考。** Cysharp/ObservableCollections の `ISynchronizedView` は Add / Remove / Replace / Move / Reset を粒度別に通知し、変換関数は Add 時に 1 回だけ呼ばれる。Unity 統合では transform 関数が GameObject を生成し、ビューが寿命を管理する。React / Solid の keyed reconciliation も同じ発想。

**対応。**

- `CollectionChangedEventArgs<T>`（Add / Remove / Replace / Move / Reset）を `Collection<T>` に持たせる
- `CollectionBinder` を差分適用に変更する
- 破棄を `Destroy` ではなくプール返却に変更する
- 発展として、可視範囲だけ実体化する `RecyclingCollectionBinder` を別コンポーネントで用意する

### 2. EditMode でのパス検証

**問題。** ViewModel のプロパティ名を変更すると prefab 側が黙って壊れる。実行時に `Log.Warning "Not Found BindingProperty"` が出るだけで、LogLevel 次第では見えない。

自分のゲームで使う以上、**プロパティ名を変えた数週間後に「なぜかラベルが空」という形で自分に返ってくる**。公開ライブラリなら「ドキュメントに書けばいい」で済むが、社内用途ではこれが最大の弱点になる。

**参考。** UWP `x:Bind` / MAUI Compiled Bindings はこれをコンパイルエラーにする。WPF は `PresentationTraceSources.TraceLevel` と Visual Studio の XAML Binding Failures ウィンドウ。Unity 公式は `BindingResult` で成否を返す。

**対応。**

- 全 Prefab / Scene を走査し、`m_ReservationViewModel` の型に存在しない path を列挙する EditMode テスト。CI で落とせる形にする
- 既存の `BindingViewer` を拡張し、実行時に「未解決バインド一覧 / 型不一致一覧」を出せるようにする。静的検査とデータ源を共有できる

実装コストが小さく効果が永続するため、優先度は高い。

### 3. ViewModel 単位の version ゲート

**問題。** `Binder.IsActive` は `this != null`（破棄チェック）のみなので、非アクティブな UI ツリーの Binder も毎フレーム走査対象になる。総 Binder 数に比例した固定コストが常に乗る。

**方針。** 書き込みコストを犠牲にせずに解決できる。

- `PropertyContainer` に version カウンタを持たせ、`Hash++` するところで一緒にインクリメントする
- `Binding` は最後に見た version を保持し、一致していれば `TryBinderUpdate()` を丸ごと飛ばす

書き込み側の増分は int のインクリメント 1 つ。読み側は N 個の走査が 1 回の比較になる。フレーム境界での畳み込みの性質はそのまま維持される。

Unity 公式の `IDataSourceViewHashProvider`（ソース側のハッシュでバインディング一式をスキップする仕組み）とほぼ同じ発想。

**実装上の注意。**

- **bool フラグではなく version にすること。** `ViewModel.m_BindingCount` の通り 1 つの VM に複数 View がバインドできるため、bool では最初の View がフラグを消して 2 つ目が更新を取りこぼす。Binding 側が「自分が最後に見た値」を持つ version 方式なら N:1 で正しく動く
- **`TryRebind()` は別枠。** パス変更は View 側の事象なので VM の version では捕まらない。`ChangePath()` で静的カウンタを 1 つ回し、`Binding` がそれを見て変化がなければ `TryRebind` ごと飛ばす。パス変更は稀なので、どこかで 1 回起きた日に全 Binding が 1 回スキャンする程度の粗さで問題ない
- **`ViewModelPropertyExtension` が第二の `PropertyContainer` を持つ。** Binding には VM 側と Extension 側の両方からプロパティが流れるため、version は両方見る必要がある

**併せて。** `View.IsActive` に `activeInHierarchy` を足すと非表示 UI が走査から丸ごと外れる。Binder は `m_Hash` を保持しており再表示時に不一致で拾い直すため、取りこぼしは起きない。

### 4. 設計時プレビュー（Design-time data）

**問題。** Play しないと表示を確認できない。文字数やレイアウト崩れの確認に毎回再生が必要。

**参考。** XAML の `d:DataContext` / `d:DesignInstance`、Avalonia previewer、SwiftUI `#Preview`。

**対応。** `View` に design-time source を持たせ、EditMode で `ReflectionViewModel` 経由でバインドしてシーンビューに反映する。**`ReflectionViewModel` が既にあるため実装コストは低い。** UI の反復速度に直接効く。

### 5. Command / computed

どちらも「あると楽だが無くても回る」枠。

**Command。** 現状イベントは fire-and-forget dispatch のみ。「押せる / 押せない」「連打防止」「非同期実行中はスピナー」は手で bool を作って `InteractableBinder` に繋ぐ必要がある。参考は `AsyncRelayCommand` の `IsRunning` / `CanBeCanceled` / `ExecutionTask` / `AllowConcurrentExecutions=false`。`Command` / `AsyncCommand` 型と `CommandBinder`（Button.interactable と連動、実行中は自動で無効化）を 1 組用意すれば、現行のイベント機構の上に薄く載る。

**computed。** `CombineBoolProvider` / `CombineStringProvider` は `ViewModelPropertyExtension`（MonoBehaviour）側の仕組みで、ViewModel 側で派生値を書くと自前で `SetDirty` を撒く必要がある。自動依存追跡まで踏み込まなくても、CommunityToolkit の `[NotifyPropertyChangedFor]` のような宣言的な依存記述で足りる。ILPP が既に `[Bind]` を処理しているので、`[Bind, DependsOn(nameof(First), nameof(Last))]` を追加して依存元 setter に `SetDirty(派生path)` を注入する形が素直。

### 見送り / 優先度を下げたもの

| 項目 | 判断 |
| --- | --- |
| ネストしたパス（`Player.Hp`） | **見送り。** フラットな方が速く、両側を自分が握る前提なら制約にならない |
| ILPP → Roslyn source generator 移行 | **優先度低。** Unity バージョンを自分で選べるなら「ある日壊れて出荷できない」は起きない。逃げ道の用意は後回しでよい |
| パス定数の生成 / `[FormerPath]` | **優先度低。** 改善候補 2 の EditMode 検証で大半をカバーできる |
| ドキュメント整備 / OpenUPM 登録 | **対象外。** 普及を目的としないため |

### 補足：着手順

3 は 1・2 より実装が軽いため、先に入れて感触を見る順でも問題ない。

---

## 実装バグ

調査中に見つけたもの。上記の改善とは独立。

| 箇所 | 内容 |
| --- | --- |
| `Runtime/ViewModel/Property/ValueCollection.cs:140` | `GetList() as IEnumerator<T>` — `List<T>` は `IEnumerator<T>` ではないので null を返す。public な `GetEnumerator` がないため `foreach` が NullReferenceException になる |
| `Runtime/ViewModel/Property/Collection.cs:141` | 非ジェネリック `IEnumerable.GetEnumerator()` が同じ理由で null |
| `Runtime/ViewModel/Property/Collection.cs` `CopyTo` | 読み取り操作なのに `SetDirty()` を呼んでいる |
| `Runtime/View/Binding.cs:186` | 新プロパティごとに全 Binder を線形走査 + 文字列比較。`Bind()` 時は O(プロパティ数 × Binder 数)。path → Binder の Dictionary 化で解消できる |
| `Runtime/ViewModel/Property/PropertyContainer.cs:61,65,80` | `UnityEngine.Debug.Log` のデバッグ出力が残っている。他が `Log.Trace` / `Log.Debug` の独自ロガーで統一されているのと不整合で、購読のたびに毎回出力される |

---

## 参考

- [Loxodon Framework (Unity-MVVM)](https://github.com/vovgou/loxodon-framework)
- [Real-Serious-Games/Unity-Weld](https://github.com/Real-Serious-Games/Unity-Weld)
- [Cysharp/ObservableCollections](https://github.com/Cysharp/ObservableCollections)
- [Cysharp/R3](https://github.com/Cysharp/R3)
- [Unity - Manual: Define a data source for runtime binding](https://docs.unity3d.com/6000.1/Documentation/Manual/UIE-runtime-binding-define-data-source.html)
- [Unity - Manual: Data binding (UI Toolkit)](https://docs.unity3d.com/6000.5/Documentation/Manual/best-practice-guides/ui-toolkit-for-advanced-unity-developers/data-binding.html)
- [x:Bind markup extension](https://learn.microsoft.com/en-us/windows/uwp/xaml-platform/x-bind-markup-extension)
- [Compiled bindings - .NET MAUI](https://learn.microsoft.com/en-us/dotnet/maui/fundamentals/data-binding/compiled-bindings)
- [MVVM source generators - CommunityToolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/overview)
- [AsyncRelayCommand - CommunityToolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand)
- [Fine-grained reactivity - SolidJS](https://docs.solidjs.com/advanced-concepts/fine-grained-reactivity)
- [How derivedStateOf works](https://blog.zachklipp.com/how-derivedstateof-works-a-deep-d-er-ive/)
- [All about property bindings in Qt 6.2](https://www.qt.io/blog/all-about-property-bindings-in-qt-6.2)
- [XAML live previewer and design-time settings - Avalonia](https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-use-design-time-data)
- [UnityRecyclingListView](https://github.com/sinbad/UnityRecyclingListView)
