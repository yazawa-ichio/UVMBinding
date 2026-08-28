# TASK-001: Collection / ValueCollection の不具合修正

| | |
| --- | --- |
| Phase | 0（バグ修正） |
| 状態 | 未着手 |
| 依存 | なし |
| 規模 | 小 |

## 目的

`Collection<T>` / `ValueCollection<T>` の 3 種の不具合を修正する。列挙子の NRE、`CopyTo` の副作用、そして `OnChanged` が発火しない型不一致。

## 背景

`as` キャストの誤用により、列挙子取得が `null` を返す。

```csharp
// ValueCollection.cs:140
IEnumerator<T> IEnumerable<T>.GetEnumerator()
{
    return GetList() as IEnumerator<T>;   // List<T> は IEnumerator<T> ではない → null
}
```

`ValueCollection<T>` は public な `GetEnumerator()` を持たないため、`foreach` は明示的インターフェース実装を経由する。結果として `null` の列挙子が返り、実行時に落ちる。

`Collection<T>` の非ジェネリック側も同じ誤りを持つ。ただしジェネリック側は `GetList().Cast<T>().GetEnumerator()` で正しく実装されているため、`foreach` は動作する。壊れているのは `IEnumerable`（非ジェネリック）として扱った場合のみ。

加えて `Collection<T>.CopyTo` が読み取り操作であるにもかかわらず `SetDirty()` を呼んでいる。不要な再バインドが発生する。

### `Collection<T>.OnChanged` が発火しない

購読と書き込みで型がずれている。

```csharp
m_VM.Property.Subscribe<List<T>>(Path, OnNotifyChanged);   // List<T> を購読
m_VM.Set<List<IViewModel>>(Path, m_List);                  // List<IViewModel> に書き込み
```

現状は `BindingProperty.Next` のチェーンに 2 エントリができ、**購読側は一度も呼ばれない**。`CollectionBinder` は `IsAssignable` 経由で動くため表面化していない。

[TASK-003](TASK-003-bindingPropertyRedesign.md)（1 path = 1 値への再設計）では**チェーンが無くなるため、先に型を揃えておく必要がある**。

## 変更内容

1. `ValueCollection<T>` の `IEnumerable<T>.GetEnumerator()` を `GetList().GetEnumerator()` に修正する
2. `ValueCollection<T>` の `IEnumerable.GetEnumerator()` を同様に修正する
3. `Collection<T>` の `IEnumerable.GetEnumerator()` を修正する
4. `Collection<T>.CopyTo` から `SetDirty()` の呼び出しを削除する
5. `Collection<T>` の購読と書き込みの型を `List<IViewModel>` に揃える。`OnChanged` の公開型（`Action<List<T>>`）を維持するなら、通知時に変換する

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/ViewModel/Property/ValueCollection.cs`
- `Unity/Assets/UVMBinding/Runtime/ViewModel/Property/Collection.cs`

## 完了条件

- `ValueCollection<T>` を `foreach` で列挙できるテストを追加し、通ること
- `Collection<T>` を `IEnumerable` として列挙できるテストを追加し、通ること
- `CopyTo` 実行後に再バインドが発生しないことを確認するテストを追加し、通ること
- **`Collection<T>.OnChanged` が要素の追加・削除で発火するテスト**
- **同一 path に 1 つの型しか登録されないこと**（`GetAll()` の件数で確認）
- 既存テスト全通過

## 備考

TASK-014（Collection 差分更新）で同じファイルを大きく変更するため、先行して修正しておく。

型不一致の修正（変更内容 5）は [TASK-003](TASK-003-bindingPropertyRedesign.md) の前提。チェーンが無くなると、揃っていない型は動作しなくなる。
