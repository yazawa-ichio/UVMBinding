# TASK-011: 走査の単一入口と Semantics アセンブリの新設

| | |
| --- | --- |
| Phase | 1（メタデータ公開層） |
| 状態 | 未着手 |
| 依存 | TASK-004, TASK-005, TASK-006, TASK-008, TASK-009 |
| 規模 | 大 |

## 目的

View 階層を走査して構造付きの中間表現を返す単一の入口を作り、それを収める optional assembly を新設する。

## 背景

現状 `View.GetElements()` は**平坦な列**を返す。

```csharp
public void GetElements(List<IViewElement> elements)
```

子 View の要素は `View.Prepare()` の段階で親から除外されるため重複は無いが、**階層情報が失われる**。構造検査にも将来の HTML 出力にも階層が要る。

また TASK-007 / TASK-010 の Lint は個別に走査を実装しており、走査ロジックが分散している。

## 変更内容

### 1. アセンブリの新設

`UVMBinding.Semantics` を新設する。TMP / Localization と同じく optional assembly として切る。

理由は 2 つ。

- この機能は本質的にアロケーション込み・リフレクション込みであり、「ボックス化しない・リフレクションしない」という **Runtime コアの性格を汚したくない**
- 使わないプロジェクトに載せる理由が無い

Runtime 側は TASK-004〜006, 008, 009 の読み取り API を提供するのみに留め、**走査結果の型は Runtime に置かない**。

### 2. 中間表現

```csharp
namespace UVMBinding.Semantics
{
    public class ViewNode
    {
        public string Id;
        public string Label;
        public bool IsModal;
        public string ViewModelTypeName;
        public List<ElementNode> Elements;
        public List<ViewNode> Children;
    }

    public class ElementNode
    {
        public string Id;
        public SemanticRole Role;
        public string Label;
        public string Value;
        public string BindPath;
        public string EventName;
        public string[] DependsOn;
        public ElementState State;
        public List<ElementNode> Children;
    }

    [Flags]
    public enum ElementState
    {
        None = 0, Disabled = 1, Hidden = 2, Clipped = 4,
    }
}
```

### 3. 2 つのキャプチャモード

| | 静的（EditMode） | 動的（Runtime） |
| --- | --- | --- |
| 値 | ViewModel 未接続のため無し | 実値を持つ |
| 用途 | Lint | 将来の export |
| 単位 | Prefab | 画面スナップショット |

同じ入口でモードを切り替える。

### 4. 除外規則

`SemanticRole.None` の要素は収集しない（TASK-008 で演出系を `None` としている）。

### 5. ビルド設定

動的キャプチャは `UNITY_EDITOR || DEVELOPMENT_BUILD` で囲む。製品ビルドに UI 走査経路を残す理由が無い。

## カバレッジの限界

**UVMBinding で結線されていない要素は収集されない。** 走査の起点が `View.Prepare()` / `GetElements()` であるため。UI 全体をこのパッケージで構築している前提に立つ。これは制約ではなく前提として記録する。

## 対象ファイル

- 新規 — `Unity/Assets/UVMBinding/Semantics/UVMBinding.Semantics.asmdef`
- 新規 — `Unity/Assets/UVMBinding/Semantics/ViewNode.cs`, `ElementNode.cs`, `ViewTraversal.cs`
- `Unity/Assets/UVMBinding/Editor/Lint/` — TASK-007 / TASK-010 を新しい入口に移行

## 完了条件

- 入れ子になった View から階層付きの `ViewNode` が取得できるテスト
- 子 View の要素が親に重複して現れないこと
- `SemanticRole.None` の要素が除外されること
- 静的モードで `Value` が `null`、動的モードで実値が入ること
- TASK-007 / TASK-010 の Lint が新しい入口を使う形に移行し、**検出結果が移行前と一致すること**
- `UVMBinding.Semantics` を含まない構成でも Runtime がコンパイルできること
- 既存テスト全通過

## 備考

`View.Prepare()` の「子 View の要素を親から除外する」挙動に依存する。**この挙動を変更すると走査結果も変わる**ため、Prepare 側に手を入れる際は本タスクの成果物も確認する。
