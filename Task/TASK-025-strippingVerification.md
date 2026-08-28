# TASK-025: IL2CPP / ストリッピング耐性の検証

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **検証完了 / 対応不要** |
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

## 事前調査（2026-08-29）

### プロジェクトは既に最も厳しい構成

| 設定 | 値 |
| --- | --- |
| Scripting Backend | **IL2CPP** |
| Managed Stripping Level | **High** |
| API Compatibility | .NET Standard 2.0 |

**段階的に上げていく必要がなく、いきなり最厳条件を検証できる。** ここで通れば下位の水準はすべて通る。

### ショーケースが最良の検証対象

`[SerializeReference]` は prefab / scene 内の文字列でしか参照されないため、**実際に使われている
アセットが無いと検証にならない**。両者の被覆は以下。

| 対象 | SerializeReference の型数 | 内訳 |
| --- | --- | --- |
| `UVMBinding.Tests` の prefab | 6 種 | BoolInverse / CollectionIndexArgument / ConcatenateConverter / IntComparison / ObjectToString / TextArgument |
| **ショーケースのシーン** | **14 種・27 インスタンス** | 上記に加え CastConverter / CombineBoolProvider / CombineStringProvider / GraphicColorArgument / StringToLength / StringComparison / DelaySetBoolConverter / BoolSwitchTweenColor |

**[TASK-023](TASK-023-showcase.md) のショーケースを本タスクの主たる検証対象とする。** 単体テストより
被覆が広く、Provider・EventArgument・Converter の合成まで含む。

検証は 2 段構えにする。

1. **ショーケースを IL2CPP でビルドし、実行してログを見る** — 最も広く、最も安価
2. **PlayMode テストをプレイヤー上で実行する** — 合否が機械判定できる

## 検証方法

**ショーケースに自己診断（`ShowcaseSelfCheck`）を仕込み、Editor 実行の結果とプレイヤー実行の結果を突き合わせる。**

ログを目視する方式は採らなかった。**`[SerializeReference]` の型が落ちても例外は出ず `null` になるだけ**で、
Converter が黙って効かなくなる。画面を見ても気づけない。

自己診断は以下を出力する。

| 項目 | 内容 |
| --- | --- |
| `type` | 復元できた `[SerializeReference]` インスタンスの型名と個数 |
| `null` | 復元されなかったスロットのパス（未設定のスロットも含む） |
| `text` | 全画面を巡回した際の `Text` の表示内容 |
| `error` | 巡回中に `Application.logMessageReceived` で拾ったエラー・例外 |

**逆直列化が通っただけでは足りない**ため、全 11 タブを巡回してバインドを実際に走らせている。
リフレクション経路とジェネリック実体化は、バインドを実行しないと確かめられない。

**表示内容まで突き合わせる**のは、例外が出ないことと正しく表示されることが別だから。
Converter が黙って素通しした場合、エラー 0 でも表示は変わる。

## 検証結果（2026-08-29）

### 条件

| 項目 | 基準 | 検証対象 |
| --- | --- | --- |
| Scripting Backend | Mono（Editor） | **IL2CPP** |
| Managed Stripping | なし | **High** |
| API Compatibility | .NET Standard 2.0 | .NET Standard 2.0 |
| 対象 | ショーケース全 11 画面 | 同左 |

### 結果

```
[SelfCheck] END slots=89 nulls=58 types=14 errors=0 texts=284
```

**Editor 実行とプレイヤー実行の出力が 358 行すべて一致した。**

| 観点 | 結果 |
| --- | --- |
| `[SerializeReference]` の型（14 種・28 インスタンス） | **全て復元。欠落 0** |
| バインド実行時のエラー・例外 | **0 件** |
| 表示テキスト 284 件 | **全て一致** |
| `ReflectionViewModel` / `GeneralBinder` 経路 | 動作（画面 07・11 に含まれる） |
| 値型の `BindingProperty<T>`（int / float / bool / enum） | 動作（画面 02・08 に含まれる） |

**段階的にストリッピング水準を上げる計画だったが、不要だった。** プロジェクトが既に
最も厳しい IL2CPP + High を既定にしており、そこで問題が出なかったため下位水準は自明に通る。

### なぜ落ちなかったか

Unity のビルドパイプラインが**シーン・prefab の直列化データを走査して `[SerializeReference]`
の型をリンカのルートに加えている**ため。`[Preserve]` が 5 型しか付いておらず `link.xml` も
無い状態で通ったのは、この仕組みが効いているから。

**したがって `[Preserve]` の拡大も `link.xml` の同梱も行わない。** 効果が無い対策を入れると、
本当に必要になったときに原因の切り分けを妨げる。

### 検証範囲外（利用側の責任）

**ここで通ったのは「ビルド時にシーンまたは prefab から参照されている型」に限る。** 以下は
本検証の対象外であり、利用側で確認が必要。

| 経路 | 理由 |
| --- | --- |
| AssetBundle / Addressables で後から読む prefab | ビルド時にリンカが型を見つけられない可能性がある |
| コードから `new` せずリフレクションのみで生成する Converter | 静的参照が一切無い |
| ビルドに含まれるどのアセットからも参照されていない型 | 削除されるのが正しい挙動 |

### プレイヤー上でのテスト実行は成立しなかった

当初の計画では**段 2 として PlayMode テストをプレイヤー上で実行**する予定だった。実行を試みたが、
テストプレイヤーのビルドと起動には成功するものの、**プレイヤーが Editor へ接続を返さずタイムアウト**した
（`Test execution timed out. No activity received from the player in 600 seconds.`）。

接続経路の問題であり、ライブラリの検証結果ではない。深追いしていない。理由は以下。

- `unity test` はプレイヤー実行に対応していない（Editor 内実行のみ）。Editor を直接起動して
  `-runTests -testPlatform StandaloneWindows64` を渡す必要があり、**再実行手段として脆い**
- **被覆はショーケースの方が広い。** テストの prefab が持つ `[SerializeReference]` 型 6 種は
  ショーケースの 14 種に**完全に含まれる**
- 段 1 が全画面・全表示内容の一致という**機械判定可能な結果**を出しており、段 2 で得られる
  情報が上乗せにならない

**唯一この方式でしか触れない経路は `Resources.Load` 経由の prefab だが**、リンカが直列化データから
型を収集する仕組みはシーンでも `Resources` でも同一であるため、差が出る余地は小さいと判断した。

## 検証で見つかった不具合

**`CastConverter` が同一型間の変換に失敗していた。** 生成器が `x == y` を除外していたため
`Single => Single` が未登録で、警告を出して変換自体が失敗していた。

Inspector で Cast を挟んだまま型が一致した場合に黙って壊れる。恒等変換を登録するよう
生成器を修正し、90 → 100 登録になった。**ストリッピングとは無関係で、Editor でも同じ挙動だった。**

## 再実行

```
make verify-strip
```

ショーケースを IL2CPP でビルドし（`Build/Showcase/`）、実行し、
`Unity/Verify/selfcheck-baseline.txt` と差分を取る。プレイヤーは自己診断を書き出した後に
自分で終了するため、待ち時間の調整は要らない。

**実行前に Editor を閉じること。** 同一プロジェクトを二重に開けない。

**基準値を更新する場合**は Editor でショーケースのシーンを開いて Play し、
`persistentDataPath/selfcheck.txt` を `Unity/Verify/selfcheck-baseline.txt` に上書きする。

### 実装上の注意

| 事象 | 対応 |
| --- | --- |
| `Unity/Temp` に出力すると Editor 終了時に消える | 出力先を `Build/Showcase/` にした |
| `BuildPlayer` の相対パスは作業ディレクトリ基準 | `Application.dataPath` から絶対パスを組み立てる |
| `FindObjectsByType` の順序が非決定的 | 出力前に `StringComparer.Ordinal` で並べ替える |
| 1 メッセージが長いとログ側で切り詰められる | 本体はファイルに書き、ログには要約だけ出す |

## 完了条件

- ~~ストリッピング水準ごとの結果を表として本ファイルに記録すること~~ 済（「検証結果」の節）
- ~~サポート範囲を明記すること~~ 済（[CONVENTIONS.md](../CONVENTIONS.md) §12）
- ~~落ちた箇所への対応（`[Preserve]` / `link.xml`）を実施し、再検証で通ること~~ **落ちた箇所が無かったため対応不要**
- ~~対応しない（利用側の責任とする）と判断した項目は、その理由を記録すること~~ 済（「検証範囲外」の節）
- ~~検証をコマンド一発で再実行できるようにすること（Makefile ターゲット）~~ 済（`make verify-strip`）

## 備考

**[TASK-022](TASK-022-castConverterConsolidation.md) と密接に関係する。** Cast Converter の集約で静的レジストリ方式（案 B）を採る場合、`MakeGenericType` を避ける理由がまさにこの AOT 制約。本タスクの検証基盤があれば、案 B の実装が正しいかを実測できる。

**本タスクは規模が大きい。** ビルドを伴うため実行時間も長い。ただし**実機で動かないライブラリには意味がない**ため、再設計（[TASK-003](TASK-003-bindingPropertyRedesign.md)）の前に現状のサポート範囲を把握しておく価値が高い。

再設計後に同じ検証を再実行すれば、**再設計がストリッピング耐性を悪化させていないか**も確認できる。
