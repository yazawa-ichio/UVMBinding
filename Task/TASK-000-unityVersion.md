# TASK-000: 対象 Unity バージョンの更新

| | |
| --- | --- |
| Phase | 0（前提整備） |
| 状態 | **完了**（2026-08-28） |
| 依存 | なし |
| 規模 | 小 |

## 実施結果（2026-08-28）

| 項目 | 結果 |
| --- | --- |
| `ProjectVersion.txt` | `6000.0.46f1` → **`6000.3.20f1`** |
| アップグレード時のコンパイルエラー | **0 件** |
| **既存テスト** | **20 件すべて通過**（PlayMode） |
| **ILPostProcessor** | **健全**（`BindingTests` / `BinderTests` 通過） |

### 依存パッケージの自動更新

| パッケージ | 変更 |
| --- | --- |
| **`nuget.mono-cecil`** | **1.11.4 → 1.11.6** |
| `test-framework` | 1.5.1 → 1.6.0 |
| `localization` | 1.5.2 → 1.5.8 |
| `ai.navigation` | 2.0.6 → 2.0.13 |
| `ide.visualstudio` | 2.0.23 → 2.0.26 |
| `testtools.codecoverage` | 1.2.6 → 1.3.0 |
| モジュール追加 | `adaptiveperformance` / `vectorgraphics` |

`mono-cecil` が想定と異なる値（1.11.6）に解決されたため、**決め打ちせず実値に合わせる方針が有効だった**。

### 副次的に判明した問題

**1. テストアセンブリの分類** — `make test`（EditMode）ではパッケージのテストが 1 件も実行されない。`UVMBinding.Tests.asmdef` が PlayMode として分類されているため。→ [TASK-024](TASK-024-testAssemblyClassification.md) として切り出し

**2. `SolutionSync` が削除済み内部 API に依存** — `Unity/Assets/DocTools/Editor/SolutionSync.cs` が `UnityEditor.SyncVS` と `UnityEditor.VisualStudioIntegration.SolutionSynchronizer` をリフレクションで参照している。これらは Unity から削除済みで、`-executeMethod` が例外を投げていた。

csproj はプロジェクトを開いた時点で自動生成されるため、**`rebuild-project` から `-executeMethod` を除去**して解決した。`SolutionSync.cs` 自体（`Tools/SolutionSync` メニュー）は壊れたまま残っている。→ 後述の残課題

**3. Unity 6.3 は `.slnx` を生成する** — 従来の `Unity.sln` ではなく新形式の `Unity.slnx` が生成される。`Unity/.gitignore` が `*.sln` しか除外していなかったため追加した。`rebuild-project` の削除対象にも追加。

## 目的

`package.json` の宣言を実態に合わせ、対象を Unity 6 に更新する。

## 背景

**現状、宣言している最低バージョンでコンパイルできない。**

| 項目 | 値 |
| --- | --- |
| `package.json` の `"unity"` | `2020.3`（= C# 8） |
| 現在の開発環境 | `6000.0.46f1` |
| **更新先** | **`6000.3.20f1`（Unity 6.3 LTS）** |
| target-typed `new()` の使用 | **16 箇所**（Runtime の `CollectionBinder.cs:20` を含む） |

target-typed `new()` は C# 9 の機能であり、Unity 2020.3 では通らない。実態が宣言を追い越している。

Unity 6.3 の C# バージョンは **C# 9 のまま**（.NET Standard 2.1）。CoreCLR は実験的機能であり、スクリプティングランタイムの置き換えは 6.8 予定。**使用できる言語機能は変わらない。**

`com.unity.nuget.mono-cecil` も宣言と実際でずれている。

| 場所 | バージョン |
| --- | --- |
| `package.json` の dependencies | `1.10.1` |
| プロジェクトの `manifest.json` | `1.11.4` |

新規インストール時は 1.10.1 が入る。

## 決定事項

**Unity 6.3（`6000.3.20f1`）に更新する。** 自分のゲーム用途であり、下位互換を維持する価値が薄いため（2026-08-28 決定）。

これにより以下が確定する。

- C# 9 の機能を使ってよい
- `UnityEngine.Accessibility.AccessibilityRole`（Unity 2023.2+）を参照してよい → [TASK-008](TASK-008-semanticRole.md)

### Makefile の Unity パスも実在しない

```makefile
UNITY_APP="C:/Program Files/Unity/Hub/Editor/2020.3.11f1/Editor/Unity.exe"
```

**2020.3.11f1 はインストールされていない。** 実際に存在するのは 2021.2.12f1 / 6000.0.46f1 / 6000.0.58f2 / 6000.3.2f1 / 6000.3.18f1 / 6000.3.20f1。

`UNITY_APP` を明示せずに `make rebuild-project` や `make doc-release` を実行すると失敗する。

## 変更内容

### 1. プロジェクトを 6000.3.20f1 で開いて更新する

`ProjectSettings/ProjectVersion.txt` が更新され、`Library` が再構築される。`manifest.json` のパッケージも解決し直される。

**この手順を最初に行い、以降の変更は更新後の実値に合わせる。**

### 2. package.json

```json
{
  "unity": "6000.3",
  "dependencies": {
    "com.unity.nuget.mono-cecil": "<manifest.json の実値>"
  }
}
```

`mono-cecil` のバージョンは、**エディタ更新後の `manifest.json` を見て合わせる**。更新前の値（`1.11.4`）を決め打ちしない。6000.3 で解決されるバージョンが変わる可能性がある。

### 3. Makefile の既定パス

`UNITY_APP` の既定値を `6000.3.20f1` に合わせる。

```makefile
UNITY_APP="C:/Program Files/Unity/Hub/Editor/6000.3.20f1/Editor/Unity.exe"
```

### 4. テスト実行ターゲットの追加

CI を構築しない方針（決定 A-3）のため、**ローカルからテストを回す手段を Makefile に用意する**。現状はどこにも記載がなく、全タスクが「既存テスト全通過」を完了条件にしているのに手順が暗黙知になっている。

| ターゲット | 内容 |
| --- | --- |
| `make test` | **PlayMode（主軸）** |
| `make test-edit` | EditMode |
| `make test-all` | 両方 |

`TestResults.*.xml` は `.gitignore` に追加する。

**当初は EditMode を `test` に割り当てていたが、[TASK-024](TASK-024-testAssemblyClassification.md) で PlayMode を主軸とする決定（A-12）を受けて入れ替えた。** 実機・IL2CPP・ストリッピング下での検証が PlayMode でしか行えないため。

`version` は本タスクでは据え置き（`0.0.3`）。

**本計画全体がメジャーバージョンアップ作業**（決定 A-9）であり、公開 API・シリアライズ形式ともに破壊的変更が入る。バージョン番号の更新は**全タスク完了後にまとめて行う**。番号の決定は作業者の判断とし、本計画では扱わない。

## 対象ファイル

- `Unity/Assets/UVMBinding/package.json`
- `Makefile`
- `.gitignore`（新規。`TestResults.xml` / `TestResults.PlayMode.xml` を追加）
- `Unity/.gitignore`（`*.slnx` を追加）

## 完了条件

- [x] `ProjectVersion.txt` が `6000.3.20f1` になっていること
- [x] `package.json` の `"unity"` が `6000.3` になっていること
- [x] `mono-cecil` のバージョンが更新後の `manifest.json` と一致していること（`1.11.6`）
- [x] パッケージがクリーンな状態で解決・コンパイルできること
- [x] **エディタ更新後に既存テストが全通過すること**（20 件通過）
- [x] `make test` / `make test-edit` が実行され、結果が出力されること
- [x] `UNITY_APP` を明示しない状態で `make rebuild-project` が動作すること

## 追補：Unity CLI への移行（2026-08-28）

Unity MCP パッケージが廃止予定のため、**Unity CLI（`unity`）に置き換えた**。

`com.unity.pipeline`（0.5.0-exp.1）を `unity pipeline install` で導入。これにより稼働中の Editor を CLI から駆動でき、`unity command eval` で任意の C# を実行できる。MCP の `Unity_RunCommand` に相当する。

Makefile も `Unity.exe` の直接呼び出しから CLI に置き換えた。

| ターゲット | 変更 |
| --- | --- |
| `test` / `test-edit` | `unity test --mode PlayMode/EditMode`。NUnit と JUnit の両形式を出力 |
| `rebuild-project` | `unity run` |
| `editor-status`（新規） | `unity status` + `unity pipeline list` |

CLI がエディタバージョンを `ProjectVersion.txt` から解決するため、**`UNITY_APP` の手動指定が不要になった**。バージョン更新のたびに Makefile を書き換える必要もなくなる。

## 残課題

**`SolutionSync.cs` は壊れたまま。** `Tools/SolutionSync` メニューを実行すると例外になる。`rebuild-project` からは切り離したため実害は無いが、動かないメニュー項目が残っている。

対応は 3 択。

1. 削除する（csproj 生成は Unity が自動で行うため役割を終えている）
2. 現代の API で書き直す
3. 放置する

**削除が妥当**と考えるが、`DocTools` は本計画の対象外アセンブリのため、独立した判断とする。

## 備考

**エディタのマイナーバージョン更新（6000.0 → 6000.3）と `mono-cecil` の更新は、どちらも ILPostProcessor（`Editor/CodeGen/`）に影響しうる。** 更新後に既存の ILPP テスト（`BindingTests` / `BinderTests`）が通ることを最優先で確認する。ここが壊れると `[Bind]` / `[Event]` が機能せず、以降の全タスクが進まない。

**本タスクは全タスクの前提。** バージョンが確定していない状態で他のタスクを進めると、後から環境差の問題が混入する。

[TASK-022](TASK-022-castConverterConsolidation.md) の SerializeReference ジェネリック検証は、**更新後の 6000.3.20f1 で実施する**こと。6000.0 と挙動が異なる可能性がある。
