# TASK-024: テストアセンブリの EditMode / PlayMode 分離

| | |
| --- | --- |
| Phase | 0（前提整備） |
| 状態 | **完了**（2026-08-28） |
| 依存 | TASK-000 |
| 規模 | 小 |

## 目的

テストアセンブリを EditMode 用と PlayMode 用に分離し、**PlayMode を主軸**とする構成を確立する。

## 決定事項

**PlayMode を主軸とする**（2026-08-28 決定）。

理由は、**PlayMode テストは実機環境・IL2CPP・ストリッピング下で実行できる**ため。このライブラリは以下の理由でストリッピングの影響を受けやすく、**どこまでサポートできるかをテストで実測する必要がある**。

- `[SerializeReference]` の Converter / Provider / EventArgument は、参照が prefab 内の**文字列（`{class, ns, asm}`）だけ**。静的なコード参照が存在しない
- `ReflectionViewModel` / `ReflectionPropertyValueResolver` / `ReflectionEventMethodResolver` がリフレクションを使う
- `BindingProperty<T>` / `ValueHolder<T>` / `Assignable<TSrc,TDst>` のジェネリック実体化が AOT で必要になる
- 現状 `[UnityEngine.Scripting.Preserve]` は 5 型にしか付いていない

詳細な検証は [TASK-025](TASK-025-strippingVerification.md) で行う。**本タスクはその土台**にあたる。

## 現状の問題

`UVMBinding.Tests.asmdef` が EditMode と PlayMode の設定を混在させている。

```json
"includePlatforms": [],                                              // 全プラットフォーム
"references": [ "UnityEngine.TestRunner", "UnityEditor.TestRunner" ] // Editor 用も参照
```

**`UnityEditor.TestRunner` を参照していると、プレイヤービルドに含められない。** 実機での実行という主軸の目的が達成できない。

また `make test`（EditMode）では 1 件も実行されなかった。

## 目標とする構成

| アセンブリ | 分類 | `includePlatforms` | 参照 | 用途 |
| --- | --- | --- | --- | --- |
| `UVMBinding.Tests` | **PlayMode（主軸）** | `[]` | `UnityEngine.TestRunner` のみ | ランタイム挙動。実機 / IL2CPP / ストリッピング検証 |
| `UVMBinding.EditorTests` | EditMode | `["Editor"]` | `UnityEngine.TestRunner` + `UnityEditor.TestRunner` | Lint、Drawer、アセット走査など Editor 専用 |

### 振り分けの基準

| 対象 | 配置先 |
| --- | --- |
| ViewModel / Binder / Converter のランタイム挙動 | **PlayMode** |
| ILPostProcessor が生成したコードの動作 | **PlayMode** |
| ボックス化・アロケーションの実測 | **PlayMode** |
| Lint（[TASK-007](TASK-007-structuralLint.md) / [TASK-010](TASK-010-namingQualityLint.md)） | EditMode |
| Drawer / Inspector の検証 | EditMode |
| Prefab / Scene の走査 | EditMode |

**迷ったら PlayMode に置く。** Editor でしか動かないものだけを EditMode に置く。

## 変更内容

### 1. `UVMBinding.Tests` を PlayMode 専用にする

`references` から `UnityEditor.TestRunner` を削除する。テストソースは `UnityEditor` 名前空間を一切使っていないため、削除しても影響しない。

### 2. `UVMBinding.EditorTests` の新設

**本タスクでは作らない。** 中身のない空アセンブリを先に作らない方針のため、EditMode テストが最初に必要になる TASK-007 で作成する。

構成のみ本ファイルに定義しておき、TASK-007 はこれに従う。

### 3. Makefile のターゲット構成

主軸を反映し、`make test` を PlayMode に割り当てる。

| ターゲット | 内容 |
| --- | --- |
| `make test` | **PlayMode（主軸）** |
| `make test-edit` | EditMode |
| `make test-all` | 両方 |

## 対象ファイル

- `Unity/Assets/UVMBinding.Tests/Tests/UVMBinding.Tests.asmdef`
- `Makefile`

## 完了条件

- `UVMBinding.Tests` が `UnityEditor.TestRunner` を参照しないこと
- **`make test` で既存 20 件が実行され、すべて通過すること**
- `make test-edit` が実行でき、結果が出力されること（現状パッケージ側は 0 件で正常）
- [CONVENTIONS.md](../CONVENTIONS.md) のテスト節が構成を反映していること

## 備考

`UVMBinding.AppTests` は `defineConstraints` を持たずテストアセンブリでもないため、本タスクの対象外。[TASK-023](TASK-023-showcase.md) でショーケースとして作り替える。
