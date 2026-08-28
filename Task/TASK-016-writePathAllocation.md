# TASK-016: 書き込みパスの ValueHolder ガード

| | |
| --- | --- |
| Phase | 0（前提整備・バグ修正） |
| 状態 | 未着手 |
| 依存 | なし |
| 規模 | 小 |

## 目的

購読者がいない場合に `ValueHolder` のプール操作が走らないようにする。

## 背景

`BindingProperty<T>` の setter が、**`m_OnChangedValue` に購読者がいなくても** `ValueHolder` のプール操作を実行している。

```csharp
// BindingProperty.cs
set
{
    if (s_EqualityComparer.Equals(m_Value, value)) return;
    m_Value = value;
    Hash++;
    OnChanged?.Invoke(value);
    using (ValueHolder<TValue>.Use(value, out var holder))   // ← 無条件に実行される
    {
        m_OnChangedValue?.Invoke(Path, holder);              // ← null チェックは内側
    }
}
```

`ValueHolder<T>.Use` は `ThreadLocal<Queue<ValueHolder<T>>>.Value` へのアクセスとキューの Dequeue / Enqueue を行う。

**「書き込みは辞書引き → 等値比較 → `Hash++` で完結する」という設計意図に反している。** ViewModel を計算の一部として使う前提（`vm.Hp = hp;` を式の途中で何度書いてもコストが変わらない）が、この一行で崩れている。

`SetDirty()` にも同じ形がある。

## 変更内容

`using` の外側で null チェックする。

```csharp
OnChanged?.Invoke(value);
if (m_OnChangedValue != null)
{
    using (ValueHolder<TValue>.Use(value, out var holder))
    {
        m_OnChangedValue.Invoke(Path, holder);
    }
}
```

`SetDirty()` にも同じ変更を適用する。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/ViewModel/Property/BindingProperty.cs`

## 完了条件

- 購読者がいない状態で値を書き込んだ際、アロケーションと `ThreadLocal` アクセスが発生しないこと（プロファイラまたはアロケーション計測で確認）
- 購読者がいる場合の通知が従来通り動作するテスト
- `SetDirty()` でも同様であること
- 既存テスト全通過

## 備考

[TASK-003](TASK-003-bindingPropertyRedesign.md)（BindingProperty 再設計）でも同じファイルに手が入るが、**本タスクを先に行う**。再設計後も型消去の通知パスは残るため、このガードは引き続き必要になる。

再設計を待たずに単独で入れられ、効果が計測しやすい。
