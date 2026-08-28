# TASK-018: 汎用 PropertyProvider の修正

| | |
| --- | --- |
| Phase | 0（前提整備・バグ修正） |
| 状態 | 未着手 |
| 依存 | なし |
| 規模 | 小 |

## 目的

`PropertyProvider<T1..T5, TResult>` が `NullReferenceException` で動作しない不具合を修正する。

## 背景

`CombineParameterQuery.Add<T1>` が `PropertyExtension` を渡し忘れている。

```csharp
// CombineParameterQuery.cs:11-14
public CombineParameterBuilder<T1> Add<T1>(string key)
{
    return new CombineParameterBuilder<T1>
    {
        SourceKey = key        // ← PropertyExtension が未設定
    };
}
```

以降の `Add<T2>` 等は `PropertyExtension = PropertyExtension` で伝播しているが、**最初の `Add<T1>` で null になっているため伝播しても null のまま**。`Handle` が `propertyExtension.Subscribe` を呼んだ時点で NRE になる。

```csharp
// PropertyProvider<T1, TResult>.Init
propertyExtension.Create().Add<T1>(m_SourceKey1).Handle((value1) => { ... });
//                         ^^^^^^^ ここで PropertyExtension が失われる
```

**5 つの汎用基底クラス（`PropertyProvider<T1,TResult>` 〜 `<T1..T5,TResult>`）がすべて動作しない。**

### 具象クラスが存在しない

実際に動いているのは `PropertyProvider` 抽象クラスを直接継承し、自前で `Subscribe` を書いている 2 つのみ。

- `CombineBoolProvider`
- `CombineStringProvider`

汎用機構（315 行の `CombineParameterQuery`）は**一度も使われていない**。具象サブクラスが 0 であることが、動作しないまま気付かれなかった理由。

## 変更内容

```csharp
public CombineParameterBuilder<T1> Add<T1>(string key)
{
    return new CombineParameterBuilder<T1>
    {
        PropertyExtension = PropertyExtension,
        SourceKey = key
    };
}
```

修正は 1 行だが、**具象クラスが 0 のため動作確認のテストが必須**。テスト用の具象 Provider を作って検証する。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/PropertyProvider/CombineParameterQuery.cs`

## 完了条件

- `PropertyProvider<T1, TResult>` を継承したテスト用具象クラスが動作するテスト
- 2 入力（`PropertyProvider<T1, T2, TResult>`）が動作するテスト
- 3 入力以上でも `PropertyExtension` が伝播していること
- 依存元のいずれかが変更された際に派生値が更新されること
- 既存テスト全通過

## 備考

修正後、[TASK-006](TASK-006-extensionPropertySourcePaths.md)（`SourcePaths` の公開）で汎用 Provider からも依存元を取得できるようにする。現状は動かないため公開しても意味がないが、修正後は Lint と export の対象になる。

汎用機構を修正せず**削除する**選択もあるが、N 入力の宣言的な派生値は Lint / export で可視になるという利点があり、修正コストが 1 行のため修正を選ぶ。
