# TASK-022: Cast Converter の集約（設定と実装の分離）

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **完了**（2026-08-28） |
| 依存 | TASK-000, TASK-017 |
| 規模 | 中 |

## 実施結果（2026-08-28）

**案 B（静的レジストリ）を採用。** 検証で案 A も成立することが判明したが、AOT / ストリッピング安全性で劣るため採らなかった（後述）。

| 項目 | 結果 |
| --- | --- |
| シリアライズ型 | 90 → **1**（`CastConverter`） |
| Inspector の候補数 | 130 → **41** |
| 生成物 | 90 クラス → **90 行の登録コード** |
| テスト | **34 件すべて通過**（Cast 関連 6 件を追加） |

### 実装の要点

`IConverter.TryConvert` に `outputType` を追加し、`Binder<T>` から `typeof(T)` を渡す。設定クラスは自分の出力型を知らないため。

`ConcatenateConverter` は**最終段のみ Binder の型に合わせ、中間段は次段の入力型に従う**。

実装本体の `CastImpl<TIn,TOut>` は `ConverterBase` を継承するため、双方向・ハッシュゲート・`OnUpdate` の仕組みをそのまま使える。バインドごとに状態を持つので、レジストリはファクトリを返して都度生成する。

`CastBase.cs` と旧 `Cast.Generated.cs`（90 クラス）を削除した。

## 目的

数値 Cast の 90 型を 1 型に集約する。Converter の**シリアライズされる設定**と**実行時の変換実装**を分離し、実装はバインド時に入出力型から解決する。

## 背景

### 現状

`CastGenerateProcess` が 10 種の数値型（byte / sbyte / short / ushort / int / uint / long / ulong / float / double）の直積 90 型を生成している。

```csharp
public class ByteToUShort : CastBase<byte, ushort>
{
    public override ushort Convert(byte input) => (ushort)input;
}
```

### 調査結果（2026-08-28）

**リポジトリ内の prefab / scene（prefab 7 + scene 1）で Cast は 1 件も使われていない。**

| 使用されている Converter | 件数 |
| --- | --- |
| `ObjectToString` | 3 |
| `IntComparison` | 3 |
| `BoolInverse` | 2 |
| `ConcatenateConverter` | 1 |
| `BoolSwitchTweenColor` / `BoolSwitchTweenVector3` | 各 1 |
| **Cast 系 90 型** | **0** |

**参照が 0 のため、90 型を削除しても既存 prefab は壊れない。** 移行スクリプトも `[MovedFrom]` も不要。

加えて **prefab の互換性は維持しない方針**（決定 A-9、メジャーバージョンアップ作業中）のため、仮に使用箇所があっても削除して差し支えない。設計を制約なく選べる。

なお `IsAssignable` は数値の暗黙変換を通さない（`typeof(float).IsAssignableFrom(typeof(int))` は false）ため、Cast が 0 件ということは**このリポジトリに数値型の不一致バインドが存在しない**ことを意味する。

### 集約できるのは Cast のみ

| ファミリ | シリアライズ状態 | 集約 |
| --- | --- | --- |
| **Cast**（90 型） | `CastBase<TIn,TOut>` は**空** | **可能** |
| Comparison（10 型） | `[SerializeField] T m_Value` + 演算子 enum | 不可 |
| BoolSwitch（10 型） | `[SerializeField] T m_On, m_Off` | 不可 |

Comparison と BoolSwitch は**型付きのシリアライズ値**を持つため、非ジェネリックな設定クラスに畳めない。

**130 型 → 41 型**になる。

## 検証結果（2026-08-28）

### SerializeReference はクローズドジェネリックを保持できる

Unity 6000.3.20f1 で検証。`[SerializeReference]` フィールドに `Generic<int, float>` を代入して `.asset` に保存し、再読み込みまで確認した。

```yaml
type: {class: 'Spike/Generic`2[[System.Int32, mscorlib],[System.Single, mscorlib]]',
  ns: UVMBinding.Tools, asm: Assembly-CSharp-Editor}
```

型引数を含む文字列として記録され、復元も成功する。**技術的には案 A が成立する。**

### それでも案 B を採用する

**理由は AOT / ストリッピング安全性**（決定 A-13）。上記 YAML が示す通り、案 A では**クローズドジェネリックの実体化が名前解決でしか参照されない**。

| 案 | 名前解決が必要な型数 | ジェネリック実体化の参照 |
| --- | --- | --- |
| 現状（90 型） | **90** | 不要（非ジェネリック） |
| 案 A | 1（+ 90 の実体化） | **名前のみ。静的参照なし** |
| **案 B** | **1** | **静的コード参照（`Register<int, float>`）** |

IL2CPP は値型のジェネリック実体化を実行時に生成できない。案 A は「YAML の文字列からしか参照されない実体化」を要求するため、**リンカが削除する余地が最も大きい**。

案 B は名前解決の対象を 90 → 1 に減らし、かつ実体化を静的参照にする。**現状より厳密に安全side**であり、案 A より明確に優れる。

案 A に強制的な実体化を足して安全にする方法もあるが、それは実質的に案 B と同じものになり、YAML 側にジェネリック型文字列が残る分だけ劣る。

## 設計

### `IConverter` のシグネチャ変更

現在の `TryConvert(IBindingProperty input, ref IBindingProperty output)` は**出力型を受け取らない**。`ConverterBase<TIn,TOut>` は自分が `TOut` を知っているから成立していた。設定クラスは知らないため追加する。

```csharp
bool TryConvert(IBindingProperty input, System.Type outputType, ref IBindingProperty output);
```

`Binder<T>` から `typeof(T)` を渡す。入力型は `input.GetBindType()` から取れるので両端が揃う。破壊的変更を許容する方針のため問題ない。

### 実装の解決方法

**手順 1 の検証結果で分岐する。ただし検証結果だけで決めない。**

| 案 | 条件 | 内容 |
| --- | --- | --- |
| **A** | SerializeReference がクローズドジェネリックを保持できる | `Cast<TIn,TOut>` 1 型。ボックス化なし |
| **B** | 保持できない、または AOT 安全性で劣る | 設定 1 型 + **静的レジストリ**から実装を取得 |

### AOT / ストリッピング安全性を評価軸に含める（決定 A-13）

**案 A が通っても、案 B の方が安全な可能性がある。**

案 A はクローズドジェネリックの実体化を Unity のデシリアライザに任せることになり、**静的なコード参照が存在しない**。ストリッピング下で実体化が削除されると `ExecutionEngineException` になりうる。

案 B は生成した `Register<int, float>(...)` が**静的参照になる**ため、リンカから到達可能であり AOT 安全性が高い。設定型も 1 つの非ジェネリッククラスで済む。

**型数の少なさやボックス化の有無だけで選ばない。** [TASK-025](TASK-025-strippingVerification.md) の観点を先取りして判断する。

利用側に制限を課す解決（「High ストリッピングでは Cast を使えない」等）は採らない。それしか手がない場合は**集約自体を見送り、現状の 90 型を維持する**（決定 A-13）。

### 案 B の要点：AOT 安全性

**`MakeGenericType` + `Activator.CreateInstance` は使わない。** ジェネリック展開が静的に参照されないため IL2CPP で実行時に落ちる。

生成した静的登録コードで参照を作る。

```csharp
static class CastRegistry
{
    static CastRegistry()
    {
        Register<int, float>(x => (float)x);      // 生成コード 90 件
        Register<int, double>(x => (double)x);
        ...
    }
}
```

90 の**クラス**は消えるが、90 の**ジェネリック展開**は登録の形で残る。減るのは `ConverterBase<TIn,TOut>` の本体 90 個分。

### インスタンスはバインドごとに生成する

`ConverterBase` は `m_Output` / `m_Hash` / `m_ForceUpdate` という**バインドごとの状態**を持つため、レジストリからシングルトンを返せない。**ファクトリを返してバインド時に生成する。**

### 双方向

数値 Cast の逆変換は逆ペアなので、レジストリで `(outputType, inputType)` を引く。精度が落ちる方向（`double → int → double`）で往復値が変わるのは現状の 90 型でも同じであり、新規の問題ではない。

## 実装手順

1. **SerializeReference のジェネリック対応を検証する。** 検証用テストを `Unity/Assets/UVMBinding.Tests/Tests/` に置いて実行する（対照群として非ジェネリックのケースも含める）。**[TASK-000](TASK-000-unityVersion.md) で 6000.3.20f1 に更新した後に実施する**こと。結果を本ファイルに記録する
2. 結果に応じて案 A / 案 B を確定する
3. `IConverter.TryConvert` に `outputType` を追加する。既存 Converter（`ConverterBase` 派生、`ConcatenateConverter`）を追随させる
4. `CastConverter` を実装する
5. `Cast.Generated.cs`（90 型）と `CastBase` を削除する
6. `CastGenerateProcess` を削除するか、案 B のレジストリ生成に置き換える

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/Converter/IConverter.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/ConverterBase.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/ConcatenateConverter.cs`
- `Unity/Assets/UVMBinding/Runtime/Binder/Binder.cs`
- `Unity/Assets/UVMBinding/Runtime/Converter/Converters/CastBase.cs`（削除）
- `Unity/Assets/UVMBinding/Runtime/Converter/Converters/Generated/Cast.Generated.cs`（削除）
- `Unity/Assets/UVMBinding.Tools/Editor/GenerateTool/CastGenerateProcess.cs`

## 完了条件

- 検証結果と採用案を本ファイルに記録すること
- `int → float` / `float → int` / `double → byte` が 1 つの設定型で変換できるテスト
- 登録されていない組み合わせで、**両端の型を含むエラーログ**が出ること
- 逆変換（双方向）が動作するテスト
- 案 B の場合、**IL2CPP ビルドで実行時に落ちないこと**（AOT 検証）
- Cast 90 型が削除され、Inspector の候補が 41 型になっていること
- 既存テスト全通過
- [ショーケース](TASK-023-showcase.md)の**画面 4（Converter）**のチェックリストを満たすこと

## 備考

**実プロジェクトでも Cast の使用は 0 件**（prefab 約 400 / バインドパス約 1300 規模で確認済み、2026-08-28）。削除の前提は確定している。

参考として、使用箇所を数えたい場合は以下で確認できる。

```
grep -rhoE 'class: (Byte|SByte|Short|UShort|Int|UInt|Long|ULong|Float|Double)To(Byte|SByte|Short|UShort|Int|UInt|Long|ULong|Float|Double), ns: UVMBinding\.Converters' --include="*.prefab" --include="*.unity" --include="*.asset" .
```

`GenerateUtil.CanCast` と `CastGenerateProcess` に `UnityEngine.Debug.Log` が残っている（生成のたびに約 100 行出力される）。削除または置き換えの際にあわせて処理する。
