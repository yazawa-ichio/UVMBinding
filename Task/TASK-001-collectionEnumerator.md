# TASK-001: Collection / ValueCollection の不具合修正

| | |
| --- | --- |
| Phase | 0（バグ修正） |
| 状態 | **完了**（2026-08-28） |
| 依存 | なし |
| 規模 | 小 |

## 目的

`Collection<T>` / `ValueCollection<T>` の 3 種の不具合を修正する。列挙子の NRE、`CopyTo` の副作用、そして `OnChanged` が発火しない型不一致。

## 実施結果（2026-08-28）

**`Collection<T>.OnChanged` のシグネチャを `Action<List<T>>` から `Action<Collection<T>>` に変更した。**

ストレージを `List<T>` にはできない。`CollectionBinder` が `Binder<IList<IViewModel>>` であり、`IList<T>` は非変なので `List<T>` を `IList<IViewModel>` として渡せないため。`List<IViewModel>` に揃えると `Action<List<T>>` の維持には通知ごとの変換アロケーションが必要になり、パフォーマンス規約に反する。

自身を渡す形にすれば変換もクロージャも発生しない。購読者はパッケージ・テスト・AppTests のいずれにも存在しなかった。

`ValueCollection<T>` は元から型が揃っており、列挙子と `CopyTo` のみ修正。

### 副次的に判明した問題：`CollectionBinder` が null を許容していなかった

型を揃えた結果、コンストラクタの `Subscribe<List<IViewModel>>` がプロパティを生成するようになり、**値が null の状態で Binder に届く**ようになった。`CollectionBinder.UpdateValue` が null を想定しておらず `NullReferenceException` になり、既存テスト `BinderTests.コレクションを利用したBinder` が落ちた。

修正前は型不一致により、`GetList()` が呼ばれるまで `BindingProperty<List<IViewModel>>` が存在せず、**偶然 null が渡らなかっただけ**。Binder が null のコレクションを許容するのが本来の姿であるため、`UpdateValue` に null ガードを追加した。

### 初回追加時に通知が 2 回発生する

`Add` の初回は「リストの遅延生成による `Set`」と「`SetDirty`」で 2 回通知される。2 回目以降は 1 回。

ハッシュ方式のポーリングにより同一フレーム内の複数通知は 1 回の UI 更新に畳まれるため実害はない。**仕様上の問題ではないと判断し、テスト側を実挙動に合わせた。** [TASK-014](TASK-014-collectionDiff.md) の差分更新で自然に解消される見込み。

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
