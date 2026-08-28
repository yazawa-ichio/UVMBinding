# TASK-025: IL2CPP / ストリッピング耐性の検証

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | 未着手 |
| 依存 | TASK-024 |
| 規模 | 大 |

## 目的

実機ビルド（IL2CPP + マネージドコードストリッピング）でこのライブラリのどこまでが動作するかを**実測し、サポート範囲を確定する**。

## 背景

**このライブラリはストリッピングの影響を受けやすい構造を複数持つ。** どれも「静的なコード参照が存在しない、または実行時にしか型が決まらない」経路であり、リンカが到達不能と判断して削除しうる。

### リスク箇所

**1. `[SerializeReference]` の型が文字列でしか参照されない**

Converter / PropertyProvider / EventArgument は prefab 内に `{class, ns, asm}` の**文字列**として保存される。コードからの静的な参照は存在しない。

利用側では Converter だけで 400 インスタンス以上、独自定義の Binder / Event / Converter / EventArgument が 50 以上ある。**ここが落ちると UI が広範囲に壊れる。**

**2. リフレクション経路**

- `ReflectionViewModel`
- `ReflectionPropertyValueResolver` / `ReflectionEventMethodResolver`
- `GeneralBinder`（実行時に対象コンポーネントのプロパティを解決する）

`IView.Bind(object)` 拡張メソッド経由で暗黙に `ReflectionViewModel` が生成される経路もあり、**呼び出し箇所が把握しにくい**。

**3. ジェネリック実体化（AOT）**

`BindingProperty<T>` / `ValueHolder<T>` / `Assignable<TSrc,TDst>` / `EqualityComparer<T>.Default`。ILPostProcessor が生成する呼び出しは静的参照になるはずだが、**実測していない**。

**4. `[Preserve]` の付与範囲が狭い**

現状 `[UnityEngine.Scripting.Preserve]` は 5 型のみ（`ViewModel` / `IViewModel` / `PropertyContainer` 等）。上記 1〜3 をカバーしているとは考えにくい。

`link.xml` は存在しない。

## 検証方法

**PlayMode テストをプレイヤービルドで実行する**（[TASK-024](TASK-024-testAssemblyClassification.md) で PlayMode を主軸にした理由）。

Unity Test Runner の「Run tests on player」を CLI から実行し、**ストリッピングレベルを変えて結果を比較する**。

| 水準 | 設定 |
| --- | --- |
| 基準 | Mono / ストリッピングなし |
| 段階 1 | IL2CPP + `ManagedStrippingLevel.Low` |
| 段階 2 | IL2CPP + `ManagedStrippingLevel.Medium` |
| 段階 3 | IL2CPP + `ManagedStrippingLevel.High` |

**どの水準から何が壊れるか**を記録する。

## 検証項目

既存の 20 件に加え、ストリッピングで落ちやすい経路を狙ったテストを追加する。

| # | 検証内容 |
| --- | --- |
| 1 | `[SerializeReference]` で保存された Converter が復元され、変換が動作する |
| 2 | 同じく PropertyProvider / EventArgument |
| 3 | `ReflectionViewModel` によるバインドが動作する |
| 4 | `IView.Bind(object)` 経由の暗黙生成が動作する |
| 5 | `GeneralBinder` が実行時にプロパティを解決できる |
| 6 | 値型（int / float / bool / enum / 構造体）の `BindingProperty<T>` が動作する |
| 7 | Converter のジェネリック実体化（`ConverterBase<TIn,TOut>`）が動作する |
| 8 | ILPostProcessor が生成した `[Bind]` / `[Event]` が動作する |

**prefab を伴う検証が必要**なため、テスト用の prefab を `Resources` に配置する（既存の `UVMBinding.Tests/Resources/` と同じ方式）。

## 想定される対応

検証結果に応じて以下を行う。

| 症状 | 対応 |
| --- | --- |
| `[SerializeReference]` 型が落ちる | `[Preserve]` の付与範囲拡大、または `link.xml` の同梱 |
| リフレクション経路が落ちる | 同上。ドキュメントで利用側の `link.xml` 追記を要求する選択もある |
| ジェネリック実体化で `ExecutionEngineException` | 該当型の明示的な実体化を仕込む |

**「サポート範囲」を確定して記録することが本タスクの成果物。** 全部を動くようにするとは限らず、**どこまでを保証しどこからは利用側の責任か**を線引きする。

## 完了条件

- ストリッピング水準ごとの結果を表として本ファイルに記録すること
- **サポート範囲を [CONVENTIONS.md](../CONVENTIONS.md) または README に明記すること**
- 落ちた箇所への対応（`[Preserve]` / `link.xml`）を実施し、再検証で通ること
- 対応しない（利用側の責任とする）と判断した項目は、その理由を記録すること
- 検証をコマンド一発で再実行できるようにすること（Makefile ターゲット）

## 備考

**[TASK-022](TASK-022-castConverterConsolidation.md) と密接に関係する。** Cast Converter の集約で静的レジストリ方式（案 B）を採る場合、`MakeGenericType` を避ける理由がまさにこの AOT 制約。本タスクの検証基盤があれば、案 B の実装が正しいかを実測できる。

**本タスクは規模が大きい。** ビルドを伴うため実行時間も長い。ただし**実機で動かないライブラリには意味がない**ため、再設計（[TASK-003](TASK-003-bindingPropertyRedesign.md)）の前に現状のサポート範囲を把握しておく価値が高い。

再設計後に同じ検証を再実行すれば、**再設計がストリッピング耐性を悪化させていないか**も確認できる。
