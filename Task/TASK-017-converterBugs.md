# TASK-017: Converter のバグ修正

| | |
| --- | --- |
| Phase | 0（前提整備・バグ修正） |
| 状態 | 未着手 |
| 依存 | なし |
| 規模 | 小 |

## 目的

Converter の実装バグ 3 件を修正する。

## 背景

### 1. `BoolSwitchBase.TryInverseConvert` が常に false を返す

```csharp
public override bool TryInverseConvert(T value, ref bool ret)
{
    if (s_EqualityComparer.Equals(m_On))   // ← 引数が 1 つ
    {
        ret = true;
        return true;
    }
    if (s_EqualityComparer.Equals(m_Off))
    ...
}
```

`EqualityComparer<T>` は `Equals(T, T)` を持つが、**1 引数版は `object.Equals(object)` に解決される**。つまり比較器インスタンスと `m_On` を比較しており、常に false になる。引数の `value` が一度も読まれていない。

**BoolSwitch 系 10 型すべてで逆変換（双方向バインド）が機能しない。** コンパイル警告も出ないため気付けない。

### 2. `ConverterBase.SetImmediate(TOutput output)` が引数を無視している

```csharp
protected void SetImmediate(TOutput output)
{
    if (m_Input is IBindingProperty<TInput> input) { m_Output.Value = Convert(input.Value); }
    else { m_Output.Value = Convert((TInput)m_Input.GetObject()); }
    m_Output.SetDirty();
}
```

`output` を一度も読んでおらず、中身は `TryUpdate` の末尾と同一。呼び出し側（`DelaySetConverter`）は値を渡しているつもりでいる。

### 3. `DelaySetConverter` が永久に再通知する

`m_Pendding` が false に戻らない。`SetImmediate` → `Convert` が `m_LastTime` を更新するため、**入力が変わらなくても `m_Delay` 間隔で通知が出続ける**。

加えて `[SerializeField] m_DefaultValue` を実行時に書き換えている。シリアライズフィールドに実行時状態を持たせており、Editor では対象がダーティになる。

### 実プロジェクトでの影響（実測）

**3 件とも実際に使われている経路のバグ。** 机上の問題ではない。

| バグ | 影響を受ける使用箇所（実測） |
| --- | --- |
| BoolSwitch の逆変換 | BoolSwitch 系が **30 箇所以上** |
| `SetImmediate` の引数無視 | DelaySet 経由で下記に波及 |
| DelaySet の永久再通知 | DelaySet 系が数箇所。**`[SerializeField]` の実行時書き換えにより、Editor 上で prefab がダーティになりうる** |

## 変更内容

### 1. `BoolSwitchBase`

```csharp
if (s_EqualityComparer.Equals(value, m_On)) { ret = true; return true; }
if (s_EqualityComparer.Equals(value, m_Off)) { ret = false; return true; }
return false;
```

### 2. `SetImmediate`

`output` を使って設定するか、引数を削除して `SetImmediate()` にする。呼び出し側の意図（`DelaySetConverter` が確定した値を流す）を踏まえると、**引数を使う実装が正しい**。

```csharp
protected void SetImmediate(TOutput output)
{
    m_Output.Value = output;
}
```

`SetDirty()` の要否は [TASK-020](TASK-020-converterUpdateCost.md) の判断に合わせる。

### 3. `DelaySetConverter`

- 通知後に `m_Pendding = false` にする
- `m_DefaultValue`（`[SerializeField]`）を実行時に書き換えない。遅延中の値は非シリアライズのフィールドに持つ

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/Converter/Converters/BoolSwitchBase.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/ConverterBase.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/Converters/DelaySetConverter.cs`

## 完了条件

- BoolSwitch の逆変換が `m_On` / `m_Off` と一致する値で true を返し、一致しない値で false を返すテスト
- `SetImmediate` に渡した値が出力に反映されるテスト
- `DelaySetConverter` が遅延後に 1 回だけ通知し、その後入力が変わらなければ通知しないテスト
- `DelaySetConverter` 実行後に `m_DefaultValue` が変化していないこと
- 既存テスト全通過

## 備考

`ConverterBase.s_InputIsValueType` は宣言のみで未使用。あわせて削除してよい。

BoolSwitch の逆変換が今まで機能していなかったため、**修正すると双方向の挙動が変わる**。互換性は維持しない方針（決定 A-9）だが、意図しない挙動変化に気付けるよう、既存 prefab で BoolSwitch を使っている箇所（`BoolSwitchTweenColor` / `BoolSwitchTweenVector3` が Root.prefab に各 1 件）は動作確認する。
