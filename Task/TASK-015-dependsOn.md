# TASK-015: 宣言的 computed（DependsOn）

| | |
| --- | --- |
| Phase | 3（コア品質・並行トラック） |
| 状態 | 未着手 |
| 依存 | TASK-006 |
| 規模 | 中 |

## 目的

ViewModel 側で派生値の依存関係を宣言できるようにし、依存元の変更時に自動で `SetDirty` が走るようにする。

## 背景

現状、派生値を扱う手段は 2 つあるがどちらも不足がある。

**View 側（`ViewModelPropertyExtension` + `CombineBoolProvider`）** — 宣言的でメタデータも取れるが、**MonoBehaviour 側の仕組み**であり、ViewModel のロジックとして書けない。

**ViewModel 側で普通に書く** — 自然だが、依存元が変わったときに**自前で `SetDirty` を撒く必要がある**。撒き忘れると表示が更新されない。

```csharp
[Bind] public string First { get; set; }
[Bind] public string Last { get; set; }
[Bind] public string FullName => $"{First} {Last}";
// First を変えても FullName は更新されない
```

## 変更内容

```csharp
[Bind, DependsOn(nameof(First), nameof(Last))]
public string FullName => $"{First} {Last}";
```

ILPP が既に `[Bind]` を処理しているため、**依存元プロパティの setter に `SetDirty("FullName")` を注入する**形で実装できる。自動依存追跡（signals / `derivedStateOf` 相当）までは行わない。クロージャ割り当てによる GC 圧を避けるため。

参考は CommunityToolkit.Mvvm の `[NotifyPropertyChangedFor]`。

### メタデータ公開

宣言された依存関係を、TASK-006 の `ExtensionPropertyInfo.SourcePaths` と**同じ経路で公開する**。Lint（依存元の存在検査）が View 側 Provider と ViewModel 側 `[DependsOn]` を区別せず扱えるようにする。

## export への影響

宣言的に書ける派生が増えるほど、将来の export で静的に理解できる範囲が広がる（History/plan-viewHtmlExportFoundation.prompt.md の系統 2）。

**記述の簡便さだけの機能ではなく、基盤の解像度を上げる機能**として位置づける。

## 対象ファイル

- 新規 — `Unity/Assets/UVMBinding/Runtime/Attribute/DependsOnAttribute.cs`
- `Unity/Assets/UVMBinding/Editor/CodeGen/ViewModelPostProcessor.cs`
- `Unity/Assets/UVMBinding/Runtime/PropertyProvider/ExtensionPropertyInfo.cs` — 公開経路の共通化

## 完了条件

- 依存元を変更すると派生値が更新されるテスト
- 依存元が 2 つ以上ある場合、いずれの変更でも更新されるテスト
- 依存元の連鎖（A → B → C）が正しく伝播するテスト
- 存在しないプロパティ名を `[DependsOn]` に指定した場合、**ILPP がエラーを出すこと**
- 宣言された依存関係がメタデータとして取得できるテスト
- 既存テスト全通過

## 備考

**ILPP の変更は影響範囲が大きい。** `[Bind]` / `[Event]` の既存処理を壊していないことを、既存の `BindTestViewModel` / `BinderTestViewModel` を使うテストで確認する。

依存の連鎖を許すと循環の可能性が出る。**循環検出を ILPP 側で行い、コンパイルエラーにする**か、実行時の再入ガードで対処するかを実装時に決める。
