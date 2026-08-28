# TASK-024: テストアセンブリの分類是正

| | |
| --- | --- |
| Phase | 0（前提整備） |
| 状態 | **判断待ち** |
| 依存 | TASK-000 |
| 規模 | 小 |

## 目的

`UVMBinding.Tests` の EditMode / PlayMode 分類を是正し、`make test`（EditMode）でパッケージのテストが実行されるようにする。

## 背景

[TASK-000](TASK-000-unityVersion.md) で `make test` を追加した際に判明した。

**`make test`（EditMode）では、パッケージのテストが 1 件も実行されない。** 実行されたのは依存パッケージ（Addressables）のスタブテスト 1 件のみだった。

`make test-play`（PlayMode）では `UVMBinding.Tests` の全 20 件が実行され、すべて通過する。

### 原因

`UVMBinding.Tests.asmdef` が EditMode と PlayMode の設定を混在させている。

```json
"includePlatforms": [],                                              // 全プラットフォーム
"references": [ "UnityEngine.TestRunner", "UnityEditor.TestRunner" ] // Editor 用も参照
```

Unity の慣例は以下。

| 分類 | `includePlatforms` | 参照 |
| --- | --- | --- |
| EditMode | `["Editor"]` | `UnityEngine.TestRunner` + `UnityEditor.TestRunner` |
| PlayMode | `[]` | `UnityEngine.TestRunner` のみ |

現状は `UnityEditor.TestRunner` を参照しながら全プラットフォーム対象になっており、**Unity は PlayMode として分類している**。

### 中身は EditMode が適切

テストはすべて `[Test]` 属性の単体テストで、`[UnityTest]`（コルーチン）は使われていない。実行中のプレイヤーを必要とする内容ではない。

**PlayMode で回すのは無駄に遅く、Lint の EditMode テスト（[TASK-007](TASK-007-structuralLint.md) / [TASK-010](TASK-010-namingQualityLint.md)）との相性も悪い。**

## 判断が必要な点

**`BinderTests` が PlayMode 固有の挙動に依存していないか。**

`BinderTests` は `Resources` から prefab を読み込み、`Instantiate` して `View.Attach` を行う。EditMode でも `Instantiate` は `Awake` を呼ぶため動作する見込みだが、以下は要確認。

- `View.Awake()` が `ViewUpdater.Register` を行う経路
- `ViewUpdater` が購読する `Canvas.preWillRenderCanvases` が EditMode で発火するか
- `Binder` の `TryUpdate` が EditMode で回るか

**動かない場合は、当該テストのみ PlayMode に残す**という分割も選択肢になる。

## 変更内容（是正する場合）

```json
"includePlatforms": [ "Editor" ]
```

`UVMBinding.Tests.asmdef` を EditMode 用に変更する。

必要なら PlayMode 固有のテストを別アセンブリ（`UVMBinding.PlayModeTests`）に分離する。

## 対象ファイル

- `Unity/Assets/UVMBinding.Tests/Tests/UVMBinding.Tests.asmdef`

## 完了条件

- 判断結果を本ファイルに記録すること
- 是正する場合、**`make test` で 20 件すべてが実行され通過すること**
- 分割する場合、`make test` と `make test-play` の合計で 20 件が通過すること
- [CONVENTIONS.md](../CONVENTIONS.md) の「実行方法」節を実態に合わせて更新すること

## 備考

**現状でもテストは全通過しており、機能上の問題はない。** 是正の目的は実行の速さと、Lint テストを EditMode で追加する際の一貫性。

対応しない判断も妥当。その場合は CONVENTIONS に `make test-play` を正とする旨が既に記載されているため、追加作業は不要。

[TASK-007](TASK-007-structuralLint.md) / [TASK-010](TASK-010-namingQualityLint.md) の Lint を EditMode テストとして追加する際は、**この混在構成を踏襲せず新規に正しい asmdef を作る**こと。本タスクを対応しない場合でも、そちらは独立して正しく設定できる。
