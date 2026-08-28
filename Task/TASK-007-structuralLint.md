# TASK-007: 構造検査（Lint）

| | |
| --- | --- |
| Phase | 2（検査） |
| 状態 | 未着手 |
| 依存 | TASK-004, TASK-005, TASK-006 |
| 規模 | 大 |

## 目的

Prefab / Scene を走査し、バインドパスとイベント名の不整合を EditMode で検出する。

**本タスクは基盤の検証点でもある。** TASK-004〜006 で公開した API だけで実装できるかどうかが、ここで判明する。残りの公開 API（TASK-008, 009, 011）を作り込む前に実施する。

## 背景

ViewModel のプロパティ名を変更すると Prefab 側が黙って壊れる。実行時に `Log.Warning "Not Found BindingProperty"` が出るだけで、`LogLevel` 次第では出力されない。

自分のゲームで使う以上、**プロパティ名を変えた数週間後に「なぜかラベルが空」という形で返ってくる**。これが現状の最大の弱点。

## 検査項目

| 検査 | 使用する API |
| --- | --- |
| バインドパスが、宣言された ViewModel 型に存在しない | TASK-004（Path）, TASK-005（型宣言） |
| 派生値の依存元パスが存在しない | TASK-006（SourcePaths） |
| View が dispatch するイベント名を ViewModel が購読していない | `IViewEvent.Name`（既存 public） |
| ViewModel が公開しているイベントを、どの View も dispatch していない | 同上 |
| 同一 View 内でイベント名が衝突している | 同上 |
| ViewModel 型が宣言されておらず検査できない View | TASK-005 |
| **変換手段が無い型不一致** — 宣言型と Binder 型が非互換で、Converter も Provider も無い | TASK-004, TASK-005 |

「検査できない View」は違反ではなく**報告**として扱う。「検査に通った」と「検査されなかった」を区別できないと、検査の意味が薄れる。

「変換手段が無い型不一致」は [TASK-003](TASK-003-bindingPropertyRedesign.md)（1 path = 1 値）の移行棚卸しをそのまま自動化したものになる。`Binder<T>` の `T`、ViewModel の宣言型、`IConverter.GetInputType()` / `GetOutputType()` を突き合わせて判定する。**手作業の棚卸しより確実なため、可能なら Lint を先に動かす。**

## 実装方針

### 走査

`AssetDatabase` で Prefab を、`EditorSceneManager` で Scene を列挙する。各 Prefab / Scene 内の `View` を起点に、`Prepare()` / `GetElements()` で要素を取得する。

**Prefab を開かずに済む方法を優先する。** 全 Prefab を `LoadAssetAtPath` するとプロジェクト規模によっては重くなるため、実行時間を計測してから最適化を判断する。

**規模の目安（実プロジェクト実測）** — prefab 約 400 / scene 10 / View 約 140 / バインドパス約 1300 / イベント名約 400。ViewModel 型は約 80、`[Bind]` 約 640、`[Event]` 約 200。この規模で実用的な速度が出るかが判断基準になる。

### ViewModel 側のプロパティ / イベント列挙

`TypeCache.GetTypesDerivedFrom<IViewModel>()` で型を取得し、`[Bind]` / `[Event]` 属性から path を列挙する。既存の `Editor/Drawer/ViewModelList.cs` と `ViewModelData` が同じことをしているため、**共通化するか参照する**。

### 除外指定

**baseline（既存違反の一括無視）ではなく除外指定を採用する。** baseline は意図が記録されないが、除外は理由が残る。

粒度を 3 段に分ける。

| 粒度 | 手段 | 用途 |
| --- | --- | --- |
| フォルダ・検査項目 | 設定用 ScriptableObject | サードパーティ資産、検査項目ごとの有効／無効 |
| 要素・View | `[LintIgnore]` 属性 / コンポーネント | `ReflectionViewModel` 経由など、構造上検査できない箇所 |
| 移行用 | baseline ファイル | 初回導入時の一時措置。**期限付きとして恒久除外と分ける** |

**理由を必須にする。**

```csharp
[LintIgnore("ReflectionViewModel 経由でバインドするため型宣言を持たない")]
```

理由なしの除外を許すと実質 baseline と同じになる。

**除外は可視化する。** 結果に「違反 0 件、除外 12 件」と出す。TASK-005 の「検査できなかった View を報告する」と同じ原則で、**黙って消えないこと**が要件。

### 出力

EditMode テストとして実装し、あわせて手動実行用のメニュー項目を用意する。

**CI は構築しない**（2026-08-28 決定）。Unity Test Runner をクラウドで回すにはライセンス認証の設定が必要で、それ自体が一仕事になるため。まず Lint を動かして有用性を確認してから CI を検討する。

違反は「ファイルパス / GameObject パス / 検査名 / 詳細」の形で出力し、**ダブルクリックで該当オブジェクトに飛べる**ようにする。

## 対象ファイル

新規。配置先は `Unity/Assets/UVMBinding/Editor/Lint/`。

参照するもの。

- `Unity/Assets/UVMBinding/Editor/Drawer/ViewModelList.cs` — 型と path の列挙
- `Unity/Assets/UVMBinding/Runtime/View/View.cs` — 走査起点

## 完了条件

- 存在しないバインドパスを持つ Prefab を用意し、検出できること
- 購読されないイベント名を持つ Prefab を用意し、検出できること
- 依存元パスが存在しない `CombineBoolProvider` を検出できること
- ViewModel 型宣言の無い View が「検査不能」として報告されること
- 正常な Prefab で違反が 0 件であること
- **既存プロジェクト全体に対して実行し、実行時間を記録すること**
- EditMode テストとして実行でき、違反があればテストが失敗すること
- メニューから手動実行でき、結果が一覧表示されること
- **`[LintIgnore]` を付けた要素が違反から除外され、除外件数として報告されること**
- **理由を書かない `[LintIgnore]` がコンパイルエラーになること**（コンストラクタ引数を必須にする）

## 備考

**この検査を既存プロジェクトに初めて流すと、大量の違反が出る可能性が高い。** 除外指定を 1 件ずつ書くのは初回では非現実的なため、移行用の baseline を併用する。ただし baseline は期限付きの一時措置として扱い、恒久的な除外（`[LintIgnore]`）とは明確に分ける。

TASK-004〜006 の API で実装しきれない項目が出た場合、**API を追加する前に本タスクに記録する**。追加の要否をまとめて判断する。
