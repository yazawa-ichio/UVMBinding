# コーディング規約

リポジトリ内の既存コードから抽出した規約。**新規コードは既存コードに合わせる**ことを原則とする。

「ツールで強制」と書いた項目は `Unity/.editorconfig` によって自動整形される。それ以外は慣習であり、人（およびエージェント）が守る必要がある。

---

## 0. 対象環境

| 項目 | 値 |
| --- | --- |
| 対象 Unity | **6000.3（Unity 6.3 LTS）** |
| C# バージョン | **C# 9**（.NET Standard 2.1） |
| ツール | **Unity CLI**（`unity`）+ `com.unity.pipeline` |

### Unity CLI で Editor を駆動する

`com.unity.pipeline` を導入済みのため、**稼働中の Editor を CLI から直接操作できる**。

#### プロジェクトパスを必ず明示する

**他の Unity プロジェクトが同時に開かれている場合があるため、パスの省略は禁止。**

`unity command` / `unity status` の `--project-path` は「auto-detected if not specified」が既定であり、**省略すると別プロジェクトの Editor を操作しうる**。取り返しのつかない変更を誤ったプロジェクトに加える危険がある。

```bash
# × 省略しない
unity status
unity command eval '...'
```

**同じ `--project-path` でもコマンドによって意味が違う。**

| コマンド | `--project-path` の意味 | 渡す値 |
| --- | --- | --- |
| `unity status` | **絶対パスへのサブストリング照合** | `UVMBinding`（`./Unity` もスラッシュ区切りの絶対パスも一致しない） |
| `unity command` | **実際のプロジェクトパス** | `./Unity` |
| `unity test` / `run` / `open` | 位置引数のプロジェクトパス | `./Unity` |

Makefile では前者を `UNITY_MATCH`、後者を `UNITY_PROJECT` として分けている。

```bash
unity status  --project-path UVMBinding --no-banner
unity command --project-path ./Unity eval 'new UnityEngine.GameObject("Foo");'
unity test    ./Unity --mode PlayMode
```

`unity pipeline list` だけはパス絞り込みを持たず全インスタンスを列挙するため、**対象行を目視で確認してから使う**。

Makefile のターゲットはすべてパスを明示している。手打ちする場合も同様にすること。

```bash
make editor-status   # 対象プロジェクトの接続確認
make editor-list     # 全インスタンス一覧
```

#### Editor を開く前にテストを回す

**コンパイルエラーがあると Editor は Safe Mode で起動し、Pipeline パッケージが読み込まれないため CLI から接続できなくなる。**

開く前に必ずテストを通し、コンパイルが成立していることを確認する。

```bash
make test              # コンパイル + テストの確認
make editor-open &     # 通ってから開く。ブロックするため必ずバックグラウンドで
make editor-status     # 接続確認（state が ready になるまで数十秒かかる）
```

**`unity open` は Editor プロセスに紐づいたままブロックする。** 前面で実行するとタイムアウトするため、必ずバックグラウンドに回す。

接続できない場合は `make editor-list` で Safe Mode かどうかを確認する。Safe Mode なら**ファイルの直接編集に逃げず、C# のコンパイルエラーを直して Editor を再起動する**。

#### 主なコマンド

```bash
unity status  --project-path ./Unity              # 接続確認（state が ready か）
unity command --project-path ./Unity              # Editor が公開するコマンド一覧
unity command --project-path ./Unity eval '...'   # 任意の C# を実行
```

**シーン・prefab・アセットを編集する前に必ず `unity status` を実行し、接続中の Editor があるか確認する。** 接続できる場合は `.unity` / `.prefab` / `.asset` の YAML を直接編集しない。fileID や GUID の手書きは誤りやすく、稼働中の Editor には再インポートまで反映されない。

接続できない場合のみファイルを直接編集し、**その旨を明示する**。

なお、C# のコンパイルエラーがあると Editor は Safe Mode で起動し Pipeline が読み込まれないため接続できなくなる。`unity pipeline list` で確認できる。

エディタを `6000.3.20f1` に更新し、`package.json` の `"unity"` を合わせる（[TASK-000](Task/TASK-000-unityVersion.md)）。下位互換は維持しない。

Unity 6.3 の C# バージョンは C# 9 のまま。CoreCLR は実験的機能（デスクトップのみ、production 非対応）であり、スクリプティングランタイムの置き換えは 6.8 予定。**C# 10 以降の機能は使えない。**

- target-typed `new()` を使ってよい（既に 16 箇所で使用済み）
- `UnityEngine.Accessibility` を参照してよい
- **破壊的変更を許容する。** 公開 API を後から変更・削除してよい
- **prefab / scene / asset の互換性も維持しない。** メジャーバージョンアップ作業中のため、既存アセットが壊れることを理由に変更を躊躇しない

ただし**「壊れてよい」ことと「壊れたまま気付かない」ことは別**。既存テストの通過と AppTests の動作は維持する。仕様変更でテストが不正になる場合はテストを直し、無検証で消さない。

---

## 1. ファイル

| 項目 | 規約 | 強制 |
| --- | --- | --- |
| 文字コード | UTF-8 **BOM 付き** | ツール |
| 改行 | LF | ツール |
| インデント | **タブ**（幅 4） | ツール |
| 末尾改行 | **入れない**（`insert_final_newline = false`） | ツール |

整形は `Unity/format.sh`（`dotnet format`）で行う。

```bash
cd Unity && ./format.sh
```

### 既知の逸脱

自動生成ファイル 3 件が BOM 無しで出力されている。ジェネレータ側の不整合であり、手で直しても再生成で戻る。

- `Runtime/Converter/Converters/Generated/BoolSwitch.Generated.cs`
- `Runtime/Converter/Converters/Generated/Comparison.Generated.cs`
- `Runtime/Log/Log.Generated.cs`

新規に生成処理を書く場合は BOM 付きで出力すること。

---

## 2. 命名

### フィールド

| 種別 | 接頭辞 | 例 |
| --- | --- | --- |
| インスタンスフィールド | `m_` + PascalCase | `m_Path`, `m_ViewModel`, `m_BindingCount` |
| 静的フィールド | `s_` + PascalCase | `s_Views`, `s_EqualityComparer`, `s_RemoveAll` |

実測 — `m_` 1049 箇所 / `s_` 94 箇所。**`_` 単独の接頭辞は使わない**（`Editor/CodeGen/` 配下に数例あるが、外部由来コードであり参考にしない）。

例外は予約語との衝突回避のみ。`_event` は `event` キーワードを避けるための命名で、この用途に限る。

### その他

- 型・メソッド・プロパティ・イベント — PascalCase
- インターフェース — `I` 接頭辞
- ローカル変数・引数 — camelCase

### Binder / Event の型名

役割が名前から読めるようにする。**この命名がそのまま Lint とツールの入力になる**（[TASK-010](Task/TASK-010-namingQualityLint.md)）。

| 種別 | 形式 | 例 |
| --- | --- | --- |
| Binder | `<対象><プロパティ>Binder` | `ImageSpriteBinder`, `CanvasGroupAlphaBinder`, `AnimatorBoolBinder` |
| 対象が自明な場合 | `<プロパティ>Binder` | `ActiveBinder`, `TextBinder` |
| Event | `<対象><操作>Event` / `Event<対象>` | `DropdownSelectEvent`, `EventButton` |
| Converter | `<変換内容>` | `BoolInverse`, `ObjectToString`, `StringToLength` |

---

## 3. 型とメンバー

### アクセス修飾子

**private は省略する。** 実測で `private` 明示は 44 箇所に対し、省略が大半。

```csharp
[SerializeField]
string m_Format;              // ○ 既存コードの主流

[SerializeField]
private string m_Format;      // × 混在させない
```

`public` / `protected` / `internal` は明示する。

### SerializeField

属性は**フィールドの直前の行**に置く。複数属性は 1 行にカンマ区切りでまとめる。

```csharp
[SerializeField]
string m_Format;

[SerializeField, PropertyPath]
string m_Path = null;

[SerializeReference]
IConverter m_Converter;
```

### 式形式メンバー

プロパティ・アクセサ・インデクサ・ラムダは式形式を使う。**メソッド・コンストラクタは通常のブロック形式**とする（`.editorconfig` で指定済み）。

```csharp
public string Path => m_Path;
protected virtual bool CanUse(IView view) => true;      // 単純な既定実装は可
```

### 明示的インターフェース実装

**公開したくない API は明示的インターフェース実装にする。** 内部用インターフェースの実装や、誤用を防ぎたいメンバーに使う。

```csharp
bool IBinder.IsActive => this != null;
void IViewModel.OnBind() { ... }
T IViewModel.Get<T>(string path) => GetImpl<T>(path);
```

派生クラスの API 表面を汚さない目的と、**ボックス化するメンバーが更新経路から呼ばれるのを防ぐ**目的の両方で使われている。

### 1 ファイル複数型

**関連する型は 1 ファイルにまとめてよい。** `VectorConverter.cs` に 8 型、`LerpConverter.cs` に 6 型など。ファイル名は代表となる型に合わせる。

**ただし `MonoBehaviour` は例外。1 ファイル 1 クラスとし、ファイル名をクラス名に一致させる。**

まとめると**シーンや prefab への参照が Missing Script になる**。実際にショーケース構築で 10 個の `MonoBehaviour` を 1 ファイルにまとめたところ、7 個の参照が壊れた（一部だけ壊れるため気付きにくい）。

`ScriptableObject` も同じ制約を受ける。

### var

**使ってよい。** 実測 193 箇所。`.editorconfig` は明示型を推奨しているが severity が `silent` であり、実態は var が主流。

---

## 4. 名前空間とフォルダ

**名前空間はフォルダ構成に従わない。役割で決める。**

| 名前空間 | 用途 |
| --- | --- |
| `UVMBinding` | 利用者が直接触る型（`View`, `ViewModel`, `Property<T>`, 属性） |
| `UVMBinding.Core` | 内部機構（`Binding`, `PropertyContainer`, `IViewModel`, `IBinder`） |
| `UVMBinding.Binders` | Binder 実装 |
| `UVMBinding.Events` | Event 実装 |
| `UVMBinding.Converters` | Converter 実装 |
| `UVMBinding.Arguments` | EventArgument 実装 |
| `UVMBinding.PropertyProvider` | 派生プロパティ機構 |
| `UVMBinding.Logger` | ログ |
| `UVMBinding.Setters` | ComponentSetter |
| `UVMBinding.Drawer` | Editor 拡張 |
| `UVMBinding.CodeGen` | ILPostProcessor |

`uGUI/Binders/` に `UVMBinding.Binders` と `UVMBinding.Events` が同居しているように、**フォルダと名前空間は一致しなくてよい**。役割が同じなら同じ名前空間に置く。

### using

- **名前空間の外**に置く（`csharp_using_directive_placement = outside_namespace`）
- アルファベット順。**System を先頭に固定しない**（`dotnet_sort_system_directives_first = false`）

---

## 5. 制御構文

### ブレース

**Allman**（開き波かっこは新しい行）。ツールで強制。

### ガード節

**早期 return / continue の単文では波かっこを省略してよい。** 実測 97 箇所。

```csharp
if (view == null) return;
if (item == null) continue;
if (m_Disposed) return;
```

それ以外の分岐は波かっこを付ける。

---

## 6. コメント

**書かない方が既定。** Runtime 全体でコメントは 8 行のみ。

- **XML ドキュメントコメント（`///`）は 0 件。** 既存コードに合わせるなら書かない
- `#region` は 0 件。使わない
- 書く場合は**日本語**で、「なぜそうしているか」を短く

```csharp
//イベントであれば登録
(elm as IViewEvent)?.Bind(this);

//データがすでにある
return ret;

// 破棄済みを削除
```

型名・メソッド名で伝わることを説明しない。**設計意図や非自明な理由だけを書く。**

---

## 7. ログ

**`UnityEngine.Debug.Log` を使わない。** `UVMBinding.Logger.Log` を使う。`LogLevel` による制御が効く。

```csharp
Log.Trace(this, "{0} Update Property Path:{1} Value:{2}", this, m_Path, m_Property);
Log.Debug("{0} SetDirty Path:{1}", this, path);
Log.Warning(this, "Not Found BindingProperty {0}", m_Path);
```

| 規約 | 内容 |
| --- | --- |
| 書式 | **複合書式指定（`{0}`）。文字列連結・補間を使わない** |
| 第 1 引数 | Unity のコンテキストオブジェクトを渡せる場合は渡す（`Log.Trace(this, ...)`） |
| 用途 | `Trace` = 毎フレーム級の詳細 / `Debug` = 結線・購読 / `Warning` = 解決失敗 |

文字列連結を禁じる理由は、**ログレベルで無効化されていてもアロケーションが発生するため**。

`Debug.Assert` は契約違反の検出に使ってよい（実測 5 箇所）。

---

## 8. パフォーマンス規約

**このパッケージの中核的な設計制約。** 逸脱すると存在意義が損なわれる。

### ボックス化しない

値は型付き（`BindingProperty<T>`）で保持する。`object` を経由する経路を更新パスに作らない。

やむを得ずボックス化する API は**明示的インターフェース実装で隔離**し、検査・出力ツール専用であることをコメントで明示する。

### 更新経路にリフレクションを入れない

`TryUpdate()` から呼ばれる経路にリフレクションを置かない。メタデータ取得のためのリフレクションは**型ごとにキャッシュ**し、EditMode / 開発ビルドに限定する。

### デリゲートをキャッシュする

イベント購読やコールバックで**毎回新しいデリゲートを生成しない**。

```csharp
// PropertyContainer.cs — 遅延生成してキャッシュする
Action<string, IValueHolder> m_InvokeChangedValueCache;
Action<string, IValueHolder> InvokeChangedValueCache
{
    get
    {
        if (m_InvokeChangedValueCache == null)
        {
            m_InvokeChangedValueCache = InvokeChangedValue;
        }
        return m_InvokeChangedValueCache;
    }
}
```

述語は静的フィールドに置く。

```csharp
// Binding.cs
static Predicate<IBinder> s_RemoveAll = (x) => !x.IsActive;
```

`EqualityComparer<T>.Default` も静的に保持する。

```csharp
static EqualityComparer<TValue> s_EqualityComparer = EqualityComparer<TValue>.Default;
```

### リストは ListPool を使う

一時的なリストは `new` せず `ListPool<T>` から借りる。**`using` で必ず返す。**

```csharp
using (ListPool<IViewElement>.Use(out var elements))
using (ListPool<IView>.Use(out var viewList))
{
    GetComponentsInChildren(true, elements);
    // ...
}
```

`GetComponentsInChildren` は**リストを受け取るオーバーロードを使う**（配列を返す版は毎回アロケートする）。

### Unity オブジェクトの null 判定

破棄済み判定には Unity の `==` オーバーロードを使う。

```csharp
public bool IsActive => m_IsActive && this != null && gameObject != null;
```

### IL2CPP 対策

リフレクションから参照される型には `[UnityEngine.Scripting.Preserve]` を付ける（`ViewModel`, `IViewModel`, `PropertyContainer` 等に付与済み）。

---

## 8.5. 値の変換・派生をどこに書くか

派生値を作る手段が 3 つある。層が違うため使い分ける。WPF の「ViewModel プロパティ / MultiBinding / IValueConverter」と同じ構造。

| | Converter | PropertyProvider | `[DependsOn]` |
| --- | --- | --- | --- |
| 層 | Binder（表示形式） | View（画面の都合） | ViewModel（ゲームのルール） |
| 入力数 | 1 | N | N |
| 双方向 | **○** | × | × |
| 状態・時間 | **○**（`OnUpdate`） | × | × |
| 結果の共有 | × その Binder 専用 | **○** path として公開 | **○** |
| 依存の宣言 | × | **○** | **○** |
| Lint / export での可視性 | 低い | **高い** | **高い** |

### 選択基準

| 条件 | 使うもの |
| --- | --- |
| 双方向が必要（InputField / Slider / Toggle の入力を戻す） | **Converter 一択** |
| 時間・状態を持つ（Lerp / DelaySet / Tween） | **Converter 一択** |
| 複数の Binder が同じ結果を使う | **PropertyProvider** |
| 入力が 2 つ以上 | **PropertyProvider** |
| ゲームのルールとして意味がある派生 | **ViewModel（`[DependsOn]`）** |
| 1 つの Binder でしか使わない純粋変換 | **Converter**（path を増やさずに済む） |

**Converter は置き換え可能ではない。** 双方向と時間を持つ変換は Converter にしか書けず、Provider の同期購読モデルでは表現できない。

### export での可視性

Converter による変換は path を持たず依存関係の宣言も無いため、**export に出せない**。ただし組み込み Converter は型名から変換内容が確定するため（`IntToFloat` / `GreaterThan` / `BoolInverse`）、マッピングテーブルを持てば出力できる。ユーザー定義 Converter は不可。

**エージェントに読ませたい派生は Provider か `[DependsOn]` で書く。**

---

## 9. アセンブリ構成

| アセンブリ | 役割 | 有効条件 |
| --- | --- | --- |
| `UVMBinding` | Runtime コア | 常時 |
| `UVMBinding.Editor` | Editor 拡張 | Editor |
| `Unity.UVMBinding.Editor.CodeGen` | ILPostProcessor | Editor |
| `UVMBinding.UI` | uGUI 対応 | `com.unity.ugui` |
| `UVMBinding.TMP` | TextMeshPro 対応 | `com.unity.textmeshpro` |
| `UVMBinding.Localization` | Localization 対応 | `com.unity.localization` |

### 任意依存の追加

外部パッケージに依存する機能は**別アセンブリに切り、`versionDefines` と `defineConstraints` で自動有効化する**。パッケージが無い環境でコンパイルエラーにならないこと。

```json
{
    "defineConstraints": [ "UVMB_TMP" ],
    "versionDefines": [
        { "name": "com.unity.textmeshpro", "expression": "", "define": "UVMB_TMP" }
    ]
}
```

### internal の公開

`Runtime/AssemblyInfo.cs` の `InternalsVisibleTo` は `UVMBinding.Editor` と `UVMBinding.Tests` のみ。

**新しいアセンブリのために `InternalsVisibleTo` を追加しない。** 外部から必要な情報は読み取り専用の公開 API として設計する（[TASK-004](Task/TASK-004-binderInfo.md) の方針）。理由は、利用者が自分のアセンブリで定義した Binder が対象外になるため。

---

## 10. テスト

### PlayMode を主軸とする

**新しいテストは原則 PlayMode に置く。** EditMode は Editor でしか動かないものに限る。

理由は、**PlayMode テストは実機・IL2CPP・ストリッピング下で実行できる**ため。このライブラリは `[SerializeReference]` の型が prefab 内の文字列でしか参照されない、リフレクション経路を持つ、ジェネリック実体化が AOT に依存する、といった理由でストリッピングの影響を受けやすい（[TASK-025](Task/TASK-025-strippingVerification.md)）。

| アセンブリ | 分類 | `includePlatforms` | 参照 | 用途 |
| --- | --- | --- | --- | --- |
| `UVMBinding.Tests` | **PlayMode（主軸）** | `[]` | `UnityEngine.TestRunner` のみ | ランタイム挙動 |
| `UVMBinding.EditorTests` | EditMode | `["Editor"]` | `UnityEngine.TestRunner` + `UnityEditor.TestRunner` | Lint、Drawer、アセット走査 |

**PlayMode アセンブリに `UnityEditor.TestRunner` を参照させない。** 参照するとプレイヤービルドに含められず、実機検証ができなくなる。

### 振り分け

| 対象 | 配置先 |
| --- | --- |
| ViewModel / Binder / Converter のランタイム挙動 | **PlayMode** |
| ILPostProcessor が生成したコードの動作 | **PlayMode** |
| ボックス化・アロケーションの実測 | **PlayMode** |
| Lint、Drawer、Prefab / Scene 走査 | EditMode |

**迷ったら PlayMode。** Editor でしか動かないものだけを EditMode に置く。

配置は `Unity/Assets/UVMBinding.Tests/Tests/`、名前空間は `UVMBinding.Tests`。

### メソッド名

**`対象_期待結果_条件` 形式（英語）を使う。** 新しいテストはこの形式に統一する。

```csharp
[Test]
public void Constructor_ThrowsArgumentNullException_WhenDataSourceIsNull()
[Test]
public void OnBind_ReadsPropertyValueFromDataSource()
[Test]
public void DirtyEventWithPath_UpdatesSpecificProperty()
```

`BindingTests.cs` には日本語のメソッド名（`Bind属性使用時のViewModel`）が残っているが、**新規では使わない**。既存テストの改名は行わなくてよい。

### 構造

1 メソッド内でフェーズを分ける場合は**波かっこブロックで区切り、日本語コメントを添える**。

```csharp
[Test]
public void Bind属性使用時のViewModel()
{
    var vm = new BindTestViewModel();
    // 初期値が正しいか？
    {
        Assert.AreEqual(0, vm.IntValue);
        Assert.AreEqual(20, vm.IntValue2);
    }
    // 値の設定
    {
        vm.IntValue2 = 10;
        Assert.AreEqual(10, change);
    }
}
```

### 実行方法

CI は構築しない（決定 A-3）。**Unity CLI（`unity test`）で実行する。**

```bash
make test
```

| ターゲット | 実行対象 |
| --- | --- |
| `make test` | **PlayMode（主軸）** |
| `make test-edit` | EditMode |
| `make test-all` | 両方 |
| `make editor-status` | 稼働中 Editor への接続確認 |

`Unity.exe` を直接叩かず `unity test` を使う。エディタバージョンの解決（`ProjectVersion.txt` から）、タイムアウト、NUnit / JUnit 両形式のレポート出力を CLI が扱うため。

Editor 上から実行する場合は Window > General > Test Runner。

`UVMBinding.EditorTests` はまだ存在しない。EditMode テストが最初に必要になる [TASK-007](Task/TASK-007-structuralLint.md) で作成する。**中身のない空アセンブリを先に作らない。**

### 実機での検証

実機・IL2CPP・ストリッピング下での動作確認は [TASK-025](Task/TASK-025-strippingVerification.md) で扱う。**サポート範囲が確定するまで、ストリッピング環境での動作は保証されない。**

### 手動での動作確認

`UVMBinding.AppTests` のショーケースシーンを使う（[TASK-023](Task/TASK-023-showcase.md)）。各画面の確認項目チェックリストを満たすことが「動作確認完了」の定義。

**「動作すること」のような検証不能な条件を完了条件に書かない。** 観察可能な事象として記述する。

### その他

- ログを見たいテストはコンストラクタで `Logger.Log.Level = Logger.LogLevel.All;` を設定する
- パスの指定には `nameof()` を使う — `ivm.Get<int>(nameof(BindTestViewModel.IntValue2))`
- `Assert` の第 3 引数に日本語の説明を書いてよい
- テスト用 ViewModel は `Tests/ViewModel/` に置く

---

## 11. `.editorconfig` と実態の差異

`.editorconfig` は Visual Studio の既定値がベースであり、**一部は実際のコードと食い違う**。以下は実態を優先する。

| 設定 | 設定値 | 実態 |
| --- | --- | --- |
| `csharp_style_var_*` | `false`（明示型を推奨） | **var を使う**（193 箇所） |
| `csharp_prefer_braces` | `true` | **ガード節では省略**（97 箇所） |
| フィールド命名ルール | **未定義** | `m_` / `s_` 接頭辞（1143 箇所） |
| `dotnet_style_require_accessibility_modifiers` | `for_non_interface_members` | **private は省略** |

いずれも severity が `silent` であるため警告は出ない。**規約としては本ドキュメントを優先する。**

`.editorconfig` を書き換える場合は、既存コード全体への影響（`format.sh` 実行時の差分）を確認してから行う。

## 12. 実機（IL2CPP / ストリッピング）でのサポート範囲

**IL2CPP + Managed Stripping High で動作することを実測で確認している**（[TASK-025](Task/TASK-025-strippingVerification.md)）。
`[Preserve]` の拡大や `link.xml` の同梱は**行っていない。必要が無いため。**

Unity のビルドパイプラインがシーン・prefab の直列化データから `[SerializeReference]` の型を
リンカのルートに加えるため、Converter / PropertyProvider / EventArgument は削除されない。

### 保証する範囲

| 対象 | 条件 |
| --- | --- |
| `[SerializeReference]` の Converter / PropertyProvider / EventArgument | **ビルドに含まれるシーンまたは prefab から参照されていること** |
| `ReflectionViewModel` / `GeneralBinder` などのリフレクション経路 | 同上 |
| 値型の `BindingProperty<T>`（int / float / bool / enum） | 同上 |

### 保証しない範囲（利用側の責任）

| 経路 | 対応 |
| --- | --- |
| AssetBundle / Addressables で後から読む prefab | 利用側で `link.xml` を用意するか、いずれかのシーンから型を参照させる |
| どのアセットからも参照されない型 | 削除されるのが正しい。参照を作るか、不要なら消す |

### 回帰の検出

```
make verify-strip
```

ショーケースを IL2CPP でビルドして実行し、Editor 実行時の基準値
（`Unity/Verify/selfcheck-baseline.txt`）と突き合わせる。**実行前に Editor を閉じること。**

**差分が出た場合、型が落ちたかバインドが壊れている。** `[SerializeReference]` の型が落ちても
例外は出ず `null` になるだけなので、画面を見ても気づけない。**必ずこのコマンドで判定する。**
