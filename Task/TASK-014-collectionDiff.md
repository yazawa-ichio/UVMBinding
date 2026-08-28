# TASK-014: Collection の差分更新とプーリング

| | |
| --- | --- |
| Phase | 3（コア品質・並行トラック） |
| 状態 | 未着手 |
| 依存 | TASK-001 |
| 規模 | 大 |

## 目的

コレクションの部分変更で全要素が再バインドされる問題と、`Destroy` / `Instantiate` の繰り返しを解消する。

## 背景

`Collection<T>` / `ValueCollection<T>` はどの操作でも `SetDirty()` を呼ぶだけで、変更の種類も位置も伝えていない。

```csharp
public void Add(T val)
{
    var list = GetList();
    list.Add(val);
    SetDirty();       // 全体が dirty になる
}
```

受け側の `CollectionBinder.UpdateValue` は全要素を再バインドする。さらに余剰分は `GameObject.Destroy`、不足分は `Instantiate` で、**プーリングが無い**。

**1 要素の追加で全行が再バインドされ、GC が走る。** インベントリ、クエスト一覧、ランキングなど実際のゲームで必ず出てくる形なので、規模が出た時点で確実に刺さる。

## 参考

Cysharp/ObservableCollections の `ISynchronizedView` は Add / Remove / Replace / Move / Reset を粒度別に通知し、変換関数は Add 時に 1 回だけ呼ばれる。Unity 統合では transform 関数が GameObject を生成し、ビューが寿命を管理する。

## 変更内容

### 1. 変更通知

```csharp
public enum CollectionChangeKind { Add, Remove, Replace, Move, Reset }

public readonly struct CollectionChange
{
    public readonly CollectionChangeKind Kind;
    public readonly int Index;
    public readonly int OldIndex;   // Move 用
}
```

`Collection<T>` / `ValueCollection<T>` の各操作が、対応する種類の変更を通知する。`SetDirty()` は `Reset` として残す。互換性のためではなく（決定 A-9 により不要）、**外部から明示的に全体を無効化する手段として有用なため**。

### 2. 差分適用

`CollectionBinder` を差分適用に変更する。

- `Add` — 1 要素のみ生成・バインド
- `Remove` — 該当要素のみ回収
- `Replace` — 該当要素のみ再バインド（GameObject は再利用）
- `Move` — `SetSiblingIndex` のみ
- `Reset` — 現行の全再構築

### 3. プーリング

`Destroy` を廃し、プールへ返却する。プールは `CollectionBinder` ごとに持つ。

`ICollectionIndex` を実装する要素はインデックス更新が必要なため、`Move` / `Remove` 後に**影響範囲のみ**更新する。

### 4. メタデータ公開

TASK-004 の `IBinderInfo` 経由で、件数と仮想化の有無を公開できるようにしておく。Lint と将来の export が使う。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/ViewModel/Property/Collection.cs`
- `Unity/Assets/UVMBinding/Runtime/ViewModel/Property/ValueCollection.cs`
- `Unity/Assets/UVMBinding/Runtime/Binder/Binders/CollectionBinder.cs`

## 完了条件

- 1 要素の `Add` で、既存要素が再バインドされないテスト
- `Remove` した GameObject が `Destroy` されずプールに戻るテスト
- `Move` で GameObject が再生成されないテスト
- `ICollectionIndex` のインデックスが各操作後に正しいテスト
- `SetDirty()` による全再構築が従来通り動作すること
- **既存の `CollectionTestViewModel` を使うテストが全通過すること**
- 100 要素のリストで 1 要素追加した際のアロケーションを計測し、記録すること

## 備考

**将来的な発展として、可視範囲だけ実体化する `RecyclingCollectionBinder` を別コンポーネントで用意する余地を残す。** 本タスクには含めない。

TASK-001 で同じファイルの列挙子バグを修正するため、先行させる。
