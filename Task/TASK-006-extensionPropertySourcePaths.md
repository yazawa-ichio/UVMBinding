# TASK-006: ExtensionPropertyInfo への依存元パス追加

| | |
| --- | --- |
| Phase | 1（メタデータ公開層） |
| 状態 | 未着手 |
| 依存 | なし |
| 規模 | 小 |

## 目的

`PropertyProvider` が宣言する派生値について、その依存元パスを外部から取得できるようにする。

## 背景

`ViewModelPropertyExtension` に登録された `PropertyProvider` は「派生値 = f(依存元 1, 依存元 2, ...)」を**データとして宣言している**。しかし外部に公開されるのは派生値のパスと型のみで、依存元が落ちている。

```csharp
public readonly struct ExtensionPropertyInfo
{
    public readonly string Path;
    public readonly Type Type;
    // 依存元が無い
}
```

依存元は既に保持されている。

- `PropertyProvider<T1..T5, TResult>` — `m_SourceKey1` … `m_SourceKey5`（private SerializeField）
- `CombineBoolProvider` / `CombineStringProvider` — `m_SourceKeys[]`（private SerializeField）

**返すだけで済む。** 新しい情報を作る必要はない。

## この情報の用途

- **Lint（TASK-007）** — 依存元パスが ViewModel に存在するかの検査。現状は派生値のパスしか検査できない
- **export（保留）** — `<output for="...">` の `for` 属性。HTML 標準で「計算に寄与した要素の id リスト」を表す
- **TASK-015（`[DependsOn]`）** — ViewModel 側で宣言された依存も同じ経路で公開する

## 変更内容

```csharp
public readonly struct ExtensionPropertyInfo
{
    public readonly string Path;
    public readonly Type Type;
    public readonly string[] SourcePaths;   // 追加
}
```

既存コンストラクタは `SourcePaths` を空配列にするオーバーロードとして残す。互換性のためではなく（決定 A-2 / A-9 により不要）、**依存元を持たない Provider の記述を簡潔に保つため**。

各 Provider の `GetInfos()` を更新する。

- `PropertyProvider<T1, TResult>` … `PropertyProvider<T1..T5, TResult>` — 5 クラス
- `CombineBoolProvider`
- `CombineStringProvider`

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/PropertyProvider/ExtensionPropertyInfo.cs`
- `Unity/Assets/UVMBinding/Runtime/PropertyProvider/IPropertyProvider.cs` — ジェネリック基底 5 クラス
- `Unity/Assets/UVMBinding/Runtime/PropertyProvider/CombineBoolProvider.cs`
- `Unity/Assets/UVMBinding/Runtime/PropertyProvider/CombineStringProvider.cs`

## 完了条件

- `CombineBoolProvider` から依存元パスが取得できるテスト
- `PropertyProvider<T1, T2, TResult>` から依存元パスが 2 件取得できるテスト
- 依存元を持たない Provider が空配列を返すこと（`null` を返さない）
- 既存テスト全通過

## 備考

`IPropertyProvider` インターフェース自体は `internal` のままでよい。`PropertyProvider` 抽象クラスが public で `GetInfos()` も public のため、外部から到達できる。
