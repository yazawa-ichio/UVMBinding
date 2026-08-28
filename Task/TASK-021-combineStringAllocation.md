# TASK-021: CombineStringProvider のボックス化解消

| | |
| --- | --- |
| Phase | 3（コア品質・並行トラック） |
| 状態 | 未着手 |
| 依存 | TASK-018 |
| 規模 | 小 |

## 目的

`CombineStringProvider` のボックス化と毎回のアロケーションを削減する。

## 背景

```csharp
public override void Init(IViewModelPropertyExtension propertyExtension)
{
    object[] values = new object[m_SourceKeys.Length];
    for (int i = 0; i < m_SourceKeys.Length; i++)
    {
        int index = i;
        propertyExtension.Subscribe<object>(m_SourceKeys[i], v =>   // ← 全入力をボックス化
        {
            values[index] = v;
            foreach (var value in values) { if (value == null) return; }
            string ret = string.Format(m_Format, values);            // ← 毎回アロケート
            propertyExtension.Set(m_ValueKey, ret);
        });
    }
}
```

問題が 2 つある。

**全入力を `object` で購読している。** `Subscribe<object>` のため、値型の入力（int / float / bool）は毎回ボックス化する。`SubscribeAssignable` は完全一致時にボックス化しない実装（`IValueHolder.TryGetValue<T>`）を持っているが、`object` で購読するとその恩恵を受けられない。

**`string.Format` を毎回呼んでいる。** 依存元のいずれかが変わるたびに新しい文字列を確保する。

Converter 側がボックス化回避を徹底しているのに対し、**Provider 側は配慮されていない**。

## 変更内容

### ボックス化の削減

`object` での購読をやめ、型を保った購読にする。ただし `string.Format(object[])` に渡す時点で最終的にボックス化は避けられない。

現実的な改善は 2 段階。

1. **型付き購読にして、変化した入力だけを再評価する**
2. `string.Format` の呼び出しを、実際に文字列が変わる場合に限定する（出力の重複排除）

`m_ValueKey` への `Set` は `BindingProperty<string>.Value` の setter が重複排除するため、**同じ文字列なら通知は飛ばない**。ただし `string.Format` 自体は実行される。

### 判断が必要な点

`string.Format` を避けるには書式指定の仕組み自体を変える必要がある。既存の `m_Format` を変更すること自体は問題ない（決定 A-9）が、**計測せずに書式仕様を変えるのは早すぎる最適化**にあたる。

UI の文字列更新は毎フレーム起きるものではないため、**ボックス化の解消だけで十分な可能性が高い。まず計測する。**

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/PropertyProvider/CombineStringProvider.cs`

## 完了条件

- 値型の依存元でボックス化が発生しないこと（アロケーション計測）
- 依存元が変化しても最終文字列が同じ場合、下流に通知が飛ばないこと
- **計測結果を本ファイルに記録すること**（ボックス化解消の前後、`string.Format` の実行回数）
- 書式仕様を変更した場合、その判断根拠を本ファイルに記録すること
- 既存テスト全通過

## 備考

[TASK-018](TASK-018-propertyProviderBuilder.md) で汎用 Provider が使えるようになると、同様の実装が増える可能性がある。**本タスクで確立した書き方を、汎用 Provider 側の指針としても使う。**

`CombineBoolProvider` は `Subscribe<bool>` と型付きで購読しており、この問題はない。
