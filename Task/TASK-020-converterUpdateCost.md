# TASK-020: Converter の更新コスト見直し

| | |
| --- | --- |
| Phase | 3（コア品質・並行トラック） |
| 状態 | 未着手 |
| 依存 | TASK-017 |
| 規模 | 中 |

## 目的

Converter が重複排除を無効化している問題と、状態を持たない Converter が毎フレームコストを払っている問題を解消する。

## 背景

### 1. `SetDirty()` の無条件呼び出しが重複排除を壊している

```csharp
// ConverterBase.TryUpdate
if (!m_ForceUpdate && m_Input.Hash == m_Hash) return;
m_ForceUpdate = false;
m_Hash = m_Input.Hash;
m_Output.Value = Convert(...);   // 値が同じなら通知しない（重複排除が効く）
m_Output.SetDirty();             // ← 無条件に Hash++ して通知
```

Comparison 系（`int → bool`、例えば `> 5`）で入力が 6→7 のとき、出力は true→true。`Value` setter の重複排除が効いた直後に `SetDirty()` が強制通知するため、**下流の Binder が無駄に更新される**。

`ConcatenateConverter` で N 段繋ぐと N 回の強制通知になる。**実プロジェクトでは Concatenate が 30 箇所近く使われており**、影響は段数分に増幅される。

なお Comparison 系は実プロジェクトで最も多用される Converter（全 Converter インスタンスの約 3 割）であり、**この重複排除の欠落が最も効くのがこの経路**。

**パッケージ全体の重複排除方針（`EqualityComparer<T>` による値の重複排除）と矛盾している。**

### 2. `OnUpdate()` が全 Converter で毎フレーム走る

```csharp
public void TryUpdate()
{
    if (m_OnUpdateLock) return;
    m_OnUpdateLock = true;
    try { OnUpdate(); } finally { m_OnUpdateLock = false; }   // ← ハッシュゲートの前
    if (m_Output == null || m_Input == null) return;
    if (!m_ForceUpdate && m_Input.Hash == m_Hash) return;
    ...
}
```

`OnUpdate()` はハッシュゲートの**前**にあり、`m_OnUpdateLock` の try/finally 付き。

時間を持つ Converter（`LerpConverter` / `DelaySetConverter` / `BoolSwitchTweenBase`）には必要だが、**`Cast` / `BoolInverse` / `Comparison` のような静的な変換でも毎フレーム仮想呼び出しと try/finally を払っている**。

## 変更内容

### 1. `SetDirty()` の条件化

無条件呼び出しをやめる。`m_Output.Value` の setter が値の変化を検出して通知するため、変化がなければ通知しない。

強制通知が必要なケース（時間を持つ Converter が同じ値を再送する等）は、**`OnUpdate()` 側から明示的に呼ぶ**形にする。

**判断が必要な点。** `SetDirty()` が意図的だった可能性がある。既存 prefab で「同じ値でも再通知されること」に依存している箇所がないか確認する。

### 2. `OnUpdate()` の呼び出し条件化

状態を持つ Converter だけが毎フレーム更新を要求するようにする。方式は 2 案。

| 案 | 内容 |
| --- | --- |
| A | `bool NeedsUpdate => false` を仮想プロパティで持たせ、時間を持つ Converter だけ override |
| B | `IUpdatableConverter` インターフェースを分離し、実装している場合のみ呼ぶ |

**A を第一候補とする。** B は `IConverter` が internal であることと、`ConcatenateConverter` が構成要素を集約する必要があることから、分岐が増える。

`m_OnUpdateLock` の try/finally も、`OnUpdate()` を呼ばない場合はスキップできる。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/Converter/ConverterBase.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/ConcatenateConverter.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/Converters/LerpConverter.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/Converters/DelaySetConverter.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/Converters/BoolSwitchTweenBase.cs`

## 完了条件

- Comparison 系で入力が変化しても出力値が同じ場合、下流の Binder が更新されないテスト
- 時間を持つ Converter（Lerp / DelaySet / Tween）が従来通り毎フレーム更新されるテスト
- 静的な Converter（Cast / BoolInverse）で `OnUpdate()` が呼ばれないこと
- `ConcatenateConverter` で構成要素のいずれかが更新を要求する場合、正しく伝播すること
- **`SetDirty()` 条件化の判断結果を本ファイルに記録すること**
- 既存テスト全通過
- [ショーケース](TASK-023-showcase.md)の**画面 4（Converter）と画面 8（演出系）**のチェックリストを満たすこと

## 備考

[TASK-016](TASK-016-writePathAllocation.md)（`ValueHolder` ガード）と同じ構図の問題。**「変化していないなら何もしない」という原則が、Converter 層では守られていない。**

`SetDirty()` の条件化は挙動を変えるため、慎重に確認する。特に `BoolSwitchTweenBase` は同じ値を送り続けることでアニメーションしている可能性がある。
