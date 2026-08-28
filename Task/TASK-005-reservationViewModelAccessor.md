# TASK-005: ViewModel 型宣言の公開と Editor 文字列引きの置換

| | |
| --- | --- |
| Phase | 1（メタデータ公開層） |
| 状態 | 未着手 |
| 依存 | なし |
| 規模 | 中 |

## 目的

`View` が宣言する ViewModel 型を外部から取得できるようにし、Editor 側の `SerializedObject` 文字列引きを解消する。

## 背景

`View.m_ReservationViewModel` は「この View が想定する ViewModel 型」を保持しており、**Lint のパス検査に必須**。しかし現状は取り出せない。

```csharp
// View.cs:12-15
[SerializeField]
#pragma warning disable CS0414
string m_ReservationViewModel = default;
#pragma warning restore CS0414
```

`CS0414`（値が代入されているが未使用）が抑止されている通り、**実行時にはどこからも読まれていないデッドデータ**である。

読んでいるのは Editor の 4 箇所のみで、いずれもフィールド名の文字列リテラルを `SerializedObject.FindProperty` に渡している。

- `Editor/Drawer/BinderDrawer.cs:63`
- `Editor/Drawer/BindingViewer.cs:152`, `:169`
- `Editor/Drawer/EventNameDrawer.cs:26`
- `Editor/Drawer/PropertyPathDrawer.cs:48`

**フィールド名を変更すると 4 箇所が黙って壊れる。** コンパイルは通り、実行時に Path ピッカーが動かなくなるだけになる。

## 変更内容

### 1. アクセサの公開

```csharp
public string ReservationViewModelName => m_ReservationViewModel;
```

`#pragma warning disable CS0414` を削除できる。

型そのものを返す `Type` プロパティは**追加しない**。型解決は `TypeCache` を使う Editor 側の責務であり、Runtime に型検索を持ち込まないため。

### 2. Editor 側の置換

4 箇所の `SerializedObject.FindProperty("m_ReservationViewModel").stringValue` を、`View` へキャストしてアクセサを読む形に置き換える。

`BindingViewer.cs:152` は編集も行っているため `SerializedProperty` が必要になる可能性がある。その場合は読み取り側のみ置換し、**フィールド名リテラルを 1 箇所の定数にまとめる**。

## 型宣言を持たない View の扱い

`ViewModelSourceProvider` / `DataSourceProvider` / `ViewBinder` 経由でバインドされる View は、この宣言を持たない場合がある。

Lint（TASK-007）はこれらを**検査対象外**として扱うが、**検査できなかった View を一覧で報告する**。黙って通すと「検査に通った」と「検査されなかった」が区別できなくなる。

**実プロジェクトの実測では、View の約 14% が型宣言を持たない。** 無視できる割合ではないため、報告は必須。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/View/View.cs`
- `Unity/Assets/UVMBinding/Editor/Drawer/BinderDrawer.cs`
- `Unity/Assets/UVMBinding/Editor/Drawer/BindingViewer.cs`
- `Unity/Assets/UVMBinding/Editor/Drawer/EventNameDrawer.cs`
- `Unity/Assets/UVMBinding/Editor/Drawer/PropertyPathDrawer.cs`

## 完了条件

- `View.ReservationViewModelName` が取得できるテスト
- `#pragma warning disable CS0414` が削除されていること
- Runtime アセンブリに `"m_ReservationViewModel"` の文字列リテラルが存在しないこと
- Editor 側の文字列リテラルが 0 箇所、または定数 1 箇所に集約されていること
- **Path ピッカー・Event 名ピッカー・BindingViewer が置換前と同じ動作をすること**（手動確認）
- 既存テスト全通過

## 備考

Editor の 4 箇所は挙動を変えずに置換する。Drawer の動作は自動テストが無いため、手動確認が必要。
