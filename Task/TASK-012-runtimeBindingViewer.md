# TASK-012: 実行時ビューアの拡張

| | |
| --- | --- |
| Phase | 2（検査） |
| 状態 | 未着手 |
| 依存 | TASK-011 |
| 規模 | 中 |

## 目的

Play 中に未解決バインド・型不一致を一覧表示できるようにする。

## 背景

バインド失敗は `Log.Debug` / `Log.Warning` に流れるだけで、`LogLevel` 次第では無音になる。

```csharp
// Binder.cs
Log.Warning(this, "Not Found BindingProperty {0}", m_Path);
Log.Debug(this, "Not Found BindingProperty {0} Type:{1}", m_Path, prop.GetBindType());
```

静的検査（TASK-007）は Prefab 単位で検査するが、**実行時にしか判明しない失敗がある**。

- `ViewModelSourceProvider` / `DataSourceProvider` 経由でバインドされる View（型宣言が無いため静的検査の対象外）
- `ReflectionViewModel` 経由のバインド
- 実行時に `ChangePath()` でパスが変更されたケース

WPF の `PresentationTraceSources` や Visual Studio の XAML Binding Failures ウィンドウに相当するものが無い。

## 変更内容

既存の `Editor/Drawer/BindingViewer.cs` を拡張する。

- **未解決バインド一覧** — `IBinderInfo.Path` に対応するプロパティが ViewModel に存在しない要素
- **型不一致一覧** — プロパティは存在するが `BindType` が代入不能な要素
- 該当要素の選択・ハイライト
- 静的検査（TASK-007）と**同じ検査ロジックを共有する**。実装を二重に持たない

## 対象ファイル

- `Unity/Assets/UVMBinding/Editor/Drawer/BindingViewer.cs`
- `Unity/Assets/UVMBinding/Editor/Lint/` — 検査ロジックの共有

## 完了条件

- 未解決バインドを持つ View を Play 中に表示し、一覧に現れること
- 型不一致を検出できること
- 一覧から該当 GameObject を選択できること
- 検査ロジックが TASK-007 と共有されていること（同じ違反が同じ判定になる）
- 既存の BindingViewer の機能が損なわれていないこと（手動確認）

## 備考

Drawer には自動テストが無いため、動作確認は手動になる。
