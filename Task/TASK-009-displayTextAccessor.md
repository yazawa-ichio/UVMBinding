# TASK-009: 表示テキストの統一アクセサ

| | |
| --- | --- |
| Phase | 1（メタデータ公開層） |
| 状態 | 未着手 |
| 依存 | TASK-008 |
| 規模 | 小 |

## 目的

要素が現在表示しているテキスト（画像の場合は識別可能な名前）を、型を問わず取得できるようにする。

## 背景

Lint のラベル欠落検査（TASK-010）に必要。「操作可能な要素にラベルが無い」を検出するには、ラベルの有無を型横断で判定する手段が要る。

現状は `Binder<TValue, TComponent>.Target` から取れるが、`Text.text` / `TMP_Text.text` / `Image.sprite.name` と型ごとに異なり、`Target` も `protected`。

## 設計方針

TASK-008 で `SemanticRole` をアトリビュートで解決したため、**同じ手段が使えるか実装時に判断する**。

| 案 | 利点 | 欠点 |
| --- | --- | --- |
| A. `IBinderInfo` に `string GetDisplayText()` を追加 | 実装が単純。Target に直接触れる | 公開 API が増える。全 Binder が実装を持つ |
| B. アトリビュートでコンポーネント型とプロパティ名を宣言し、リフレクションで読む | Runtime に API を足さない | リフレクション。宣言が煩雑 |
| C. Semantics アセンブリ側に型別の抽出テーブルを持つ | Runtime を一切変更しない | ユーザー定義 Binder に対応できない |

**A を第一候補とする。** 理由は、ユーザー定義 Binder が自分で表示テキストを返せる必要があり、B・C ではそれが成立しにくいため。ただし `IBinderInfo` の肥大化は避けたいので、TASK-004 の備考の通り**本タスクの実装時に最終判断する**。

A を採る場合、既定実装を `Binder<TValue, TComponent>` に置き、`Target` から取得を試みる形にすれば、個別 Binder への実装は最小限で済む。

## 参考

`Editor/Drawer/ElementViewer.cs` の `GetName()` が近いルールを持つ。

```csharp
// Binder なら "Path(型名)"、それ以外は "GameObject名(型名)"
return $"{m_Binder.Path}({m_Binder.GetType().Name})";
```

ただしこれは **Editor の一覧表示用の識別名**であり、UI として表示されているテキストではない。用途が異なるため流用はできないが、命名規則の参考にはなる。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/Binder/IBinder.cs`（案 A の場合）
- `Unity/Assets/UVMBinding/Runtime/Binder/Binder.cs`
- 表示系 Binder（`TextBinder`, `TextMeshProBinder`, `ImageSpriteBinder` 他）

## 完了条件

- 採用案と理由を本ファイルに記録すること
- `TextBinder` から表示中の文字列が取得できるテスト
- `ImageSpriteBinder` から sprite 名が取得できるテスト
- 表示テキストを持たない Binder が `null` または空文字を返すこと（例外を投げない）
- 既存テスト全通過

## 備考

Runtime の値取得はボックス化・アロケーションを伴う可能性があるため、TASK-004 の `GetBoundValue()` と同様に**更新経路から隔離する**。
