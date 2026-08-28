# UVMBinding 改修インデックス

本体（UVMBinding Runtime / Editor）の改修タスク一覧と進捗。

バインディンググラフを**本体の外から読み取れる公開 API** として整備し、その上に検査機能（Lint）を載せることを目的とする。
将来の HTML export も同じ基盤に乗せるが、**現時点では草案であり着手しない**。

---

## 進捗表

凡例 — `未着手` / `進行中` / `完了` / `判断待ち` / `保留`

### Phase 0 — 前提整備・バグ修正

**TASK-000 は全タスクの前提。** 環境が確定しない状態で他を進めると、後から環境差の問題が混入する。依存欄には主要な依存先のみ記載している。

| # | タスク | 状態 | 依存 | 規模 |
| --- | --- | --- | --- | --- |
| 000 | [対象 Unity バージョンの更新](Task/TASK-000-unityVersion.md) | **完了** | — | 小 |
| 024 | [テストアセンブリの EditMode / PlayMode 分離](Task/TASK-024-testAssemblyClassification.md) | **完了** | 000 | 小 |
| 001 | [Collection / ValueCollection の不具合修正](Task/TASK-001-collectionEnumerator.md) | **完了** | — | 小 |
| 002 | [PropertyContainer のデバッグ出力除去](Task/TASK-002-removeDebugLog.md) | **完了** | — | 小 |
| 016 | [書き込みパスの ValueHolder ガード](Task/TASK-016-writePathAllocation.md) | **完了** | — | 小 |
| 017 | [Converter のバグ修正](Task/TASK-017-converterBugs.md) | **完了** | — | 小 |
| 018 | [汎用 PropertyProvider の修正](Task/TASK-018-propertyProviderBuilder.md) | **完了** | — | 小 |

### Phase 0.5 — 基盤再設計

| # | タスク | 状態 | 依存 | 規模 |
| --- | --- | --- | --- | --- |
| 025 | [IL2CPP / ストリッピング耐性の検証](Task/TASK-025-strippingVerification.md) | 未着手 | 024 | 大 |
| 023 | [ショーケースシーンの構築](Task/TASK-023-showcase.md) | 未着手 | 000 | 大 |
| 022 | [Cast Converter の集約](Task/TASK-022-castConverterConsolidation.md) | **完了** | 000,017 | 中 |
| 003 | [BindingProperty の再設計（1 path = 1 値）](Task/TASK-003-bindingPropertyRedesign.md) | 未着手 | 001,002,016,**023**（022 は先行推奨） | **大** |

### Phase 1 — メタデータ公開層

| # | タスク | 状態 | 依存 | 規模 |
| --- | --- | --- | --- | --- |
| 004 | [IBinderInfo の追加](Task/TASK-004-binderInfo.md) | 未着手 | — | 中 |
| 005 | [ViewModel 型宣言の公開と Editor 文字列引きの置換](Task/TASK-005-reservationViewModelAccessor.md) | 未着手 | — | 中 |
| 006 | [ExtensionPropertyInfo への依存元パス追加](Task/TASK-006-extensionPropertySourcePaths.md) | 未着手 | — | 小 |
| 008 | [SemanticRole アトリビュート](Task/TASK-008-semanticRole.md) | 未着手 | 000,007 | 中 |
| 009 | [表示テキストの統一アクセサ](Task/TASK-009-displayTextAccessor.md) | 未着手 | 008 | 小 |
| 011 | [走査の単一入口と Semantics アセンブリの新設](Task/TASK-011-traversalEntryPoint.md) | 未着手 | 004,005,006,008,009 | 大 |

### Phase 2 — 検査（Lint）

| # | タスク | 状態 | 依存 | 規模 |
| --- | --- | --- | --- | --- |
| 007 | [構造検査](Task/TASK-007-structuralLint.md) | 未着手 | 004,005,006 | 大 |
| 010 | [命名品質検査](Task/TASK-010-namingQualityLint.md) | 未着手 | 007,008,009 | 中 |
| 012 | [実行時ビューアの拡張](Task/TASK-012-runtimeBindingViewer.md) | 未着手 | 011 | 中 |

### Phase 3 — コア品質（並行トラック）

Phase 1・2 と独立に進められる。

| # | タスク | 状態 | 依存 | 規模 |
| --- | --- | --- | --- | --- |
| 013 | [ViewModel 単位の version ゲート](Task/TASK-013-versionGate.md) | 未着手 | **003** | 中 |
| 014 | [Collection の差分更新とプーリング](Task/TASK-014-collectionDiff.md) | 未着手 | 001 | 大 |
| 015 | [宣言的 computed（DependsOn）](Task/TASK-015-dependsOn.md) | 未着手 | 006 | 中 |
| 019 | [Converter / Provider の型フィルタ](Task/TASK-019-converterTypeFilter.md) | 未着手 | 017,018,022 | 中 |
| 020 | [Converter の更新コスト見直し](Task/TASK-020-converterUpdateCost.md) | 未着手 | 017 | 中 |
| 021 | [CombineStringProvider のボックス化解消](Task/TASK-021-combineStringAllocation.md) | 未着手 | 018 | 小 |

---

## 推奨実行順

```
000 024 001 002 016 017 018    Phase 0（完了）
022                            Cast 集約（完了）
023                            ショーケース構築（003 の回帰検出に必要。画面 1〜7 まで）
025                            IL2CPP / ストリッピング耐性の検証
003                            ★ 最大の作業。基盤再設計
004 → 005 → 006            Phase 1 前半（公開 API の最小セット）
007                        ★ 検証点
008 → 009                  Phase 1 後半
010                        ★ 判断点
011 → 012                  走査基盤と実行時検査

並行: 014 → 015 → 020 → 021 → 019    Phase 3
003 完了後: 013                      version ゲート
```

タスク番号は**安定した ID であり、実行順ではない**。順序は本節の記載に従う。

### ★ TASK-003（BindingProperty 再設計）

**本計画で最大の作業。** path に対する型ごとの登録（`Next` チェーン）を廃し、1 path = 1 値にする。

Phase 0 の 3 件（001 / 002 / 016）は本タスクの地ならしであり、先に完了させる。特に **001 の型不一致修正は前提**で、チェーンが無くなると型が揃っていない箇所は動作しなくなる。

[TASK-022](Task/TASK-022-castConverterConsolidation.md) は `IConverter.TryConvert` のシグネチャを変更し `Binder.Bind` にも手が入るため、**先行を推奨する**（ハード依存ではない）。

[TASK-013](Task/TASK-013-versionGate.md) は同じ `PropertyContainer` に手を入れるため、**再設計後に着手する**。

**調査により影響範囲が当初想定より縮小した。** `Binder.Bind` の結線判定はチェーン廃止の影響を直接受けず、主対象は `Get<T>` の呼び出し側（`Property<T>` / `Subscribe<T>` / `Collection<T>` / `GeneralViewModel`）である。詳細は TASK-003 の「移行棚卸しの範囲」を参照。

### ★ 検証点：TASK-007

構造検査は TASK-004〜006 だけで実装できる。**ここで公開 API が実用に足るかが判明する。**
残りの API（008, 009, 011）を作り込む前に実施し、実装しきれない項目が出た場合は API を追加する前に TASK-007 に記録する。

### ★ 判断点：TASK-010

命名品質検査の出力が、**HTML export に着手すべきかの判断材料になる**。
export 計画は「既存の命名が意味を持つこと」に賭けているため、違反率が高ければ作るべきは exporter ではなく命名規約となる。

---

## 保留（Phase 4）

**消費者が確定するまで着手しない。使う先が無い API を先に生やさない。**

| 項目 | 保留理由 |
| --- | --- |
| `[Consequence]` 注釈（不可逆・有料操作のマーキング） | export 着手が前提 |
| 設計時プレビュー（EditMode で ReflectionViewModel を接続） | TASK-011 の EditMode 走査が前提 |
| HTML / JS 出力 | [草案](History/plan-viewHtmlExportFoundation.prompt.md)。TASK-010 の結果を見て判断 |

---

## 対応しない（決定事項）

| 項目 | 理由 |
| --- | --- |
| ネストしたパス（`Player.Hp`） | 設計判断として維持。フラットが正しい |
| push 型への移行 | 設計意図として維持。書き込みの軽さを守る |
| `BindingProperty.OnChanged` の活用 | 上記の帰結。version ゲートも使わない |
| glitch / 中間状態への対策 | 上記の帰結として問題が発生しない |
| ILPP → Roslyn source generator 移行 | 優先度低。Unity バージョンを自分で選べる |
| Command / AsyncCommand 抽象 | 現状 bool + InteractableBinder で回る。必要になってから |
| `IPropertyProvider` の public 化 | 不要。`PropertyProvider` 抽象クラスが public で `GetInfos()` が届く |
| ドキュメント整備 / OpenUPM 登録 | 対象外。普及を目的としない |

---

## ドキュメント構成

| 場所 | 内容 |
| --- | --- |
| `INDEX.md` | 本ファイル。タスク一覧と進捗 |
| [`CONVENTIONS.md`](CONVENTIONS.md) | コーディング規約。**実装前に読むこと** |
| `Task/` | 正式なタスク定義。目的・背景・変更内容・完了条件 |
| `History/` | 草案・調査記録。**実装の根拠として参照するが、指示としては扱わない** |

### History の内容

| ファイル | 内容 |
| --- | --- |
| [analysis-bindingDesignReview.md](History/analysis-bindingDesignReview.md) | 他パッケージとの比較調査と改善候補の整理。各タスクの背景の出典 |
| [plan-metadataFoundation.prompt.md](History/plan-metadataFoundation.prompt.md) | 本体改修計画の草案。Task/ に分割済み |
| [plan-viewHtmlExportFoundation.prompt.md](History/plan-viewHtmlExportFoundation.prompt.md) | HTML export の草案。**着手しない** |

---

## 決定事項

着手前に確定した項目（2026-08-28）。

| # | 議題 | 決定 | 理由 |
| --- | --- | --- | --- |
| A-1 | 対象 Unity バージョン | **Unity 6.3（`6000.3.20f1`）に更新** | `package.json` は `2020.3` だが実コードが C# 9（`new()` 16 箇所）を使用しており、宣言バージョンでコンパイルできない。自分のゲーム用途で下位互換の価値が薄い。C# は 6.3 でも 9 のまま → [TASK-000](Task/TASK-000-unityVersion.md) |
| A-2 | 公開 API の後方互換 | **破壊的変更を許容** | 自分用ライブラリ。0.x のまま設計の自由度を優先する |
| A-3 | テスト実行環境 | **CI は構築しない** | Unity Test Runner のクラウド実行はライセンス認証の設定が必要で、それ自体が一仕事。まず Lint の有用性を確認してから検討する |
| A-4 | git 運用 | **`html` ブランチで続行** | 既存 3 コミットと地続きで進める |
| A-5 | path に対する型ごとの登録 | **廃止し 1 path = 1 値にする** | 購読側が `IValueHolder.TryGetValue<T>` で解決済みのため、チェーンを維持する理由が無くなった。現に `Collection<T>.OnChanged` が型不一致で発火しない不具合が隠れている → [TASK-003](Task/TASK-003-bindingPropertyRedesign.md) |
| A-6 | Lint の違反抑止 | **除外指定（理由必須）を採用** | baseline は意図が記録されない。除外は理由が残り、件数として可視化できる。baseline は移行用の一時措置に限定 |
| A-7 | Converter と Combine の関係 | **Converter は維持し、役割で使い分ける** | Combine（PropertyProvider）は Converter の代替ではない。双方向と時間を持つ変換は Converter にしか書けず、N 入力と結果の共有は Provider にしかできない。使い分け指針は [CONVENTIONS.md](CONVENTIONS.md) 8.5 に記載 |
| A-8 | 数値 Cast の 90 型 | **設定と実装を分離して 1 型に集約する** | `CastBase` はシリアライズ状態を持たないため集約できる。Comparison / BoolSwitch は型付きの `[SerializeField]` を持つため不可。**調査で prefab / scene における Cast の使用が 0 件と判明**したため、移行コストなしに削除できる → [TASK-022](Task/TASK-022-castConverterConsolidation.md) |
| A-9 | prefab / アセットの互換性 | **維持しない** | メジャーバージョンアップ作業中のため。既存 prefab が壊れることを理由に変更を躊躇しない。壊れた箇所は作り直す |
| A-10 | 手動での動作確認の基準 | **ショーケースシーンを構築し、そのチェックリストで判定する** | 現 AppTests は最小構成で、「動作すること」は検証不能な条件だった。基本機能を網羅したショーケースに作り替える → [TASK-023](Task/TASK-023-showcase.md) |
| A-11 | テスト実行手段 | **Unity CLI（`make test`）** | CI は作らない（A-3）が、実行手順が暗黙知だった。`Makefile` に `test` / `test-edit` / `test-all` を追加 → [TASK-000](Task/TASK-000-unityVersion.md) |
| A-13 | ストリッピング対策の方針 | **利用側に制限を課さない。制限が避けられない設計はロールバックする** | ストリッピング下で動かない箇所を「利用側の責任」や「使用禁止」で片付けない。解決できない場合は**その設計変更自体を戻す**。したがって各タスクは独立したコミットに分け、単独で revert できる状態を保つ → [TASK-025](Task/TASK-025-strippingVerification.md) |

### A-13 の適用

**設計の選択肢が複数ある場合、AOT / ストリッピング安全性を評価軸に含める。** 機能や型数の少なさだけで選ばない。

具体例として [TASK-022](Task/TASK-022-castConverterConsolidation.md) では、クローズドジェネリックを `[SerializeReference]` で保持する案より、**静的レジストリで実体化を静的参照にする案の方が AOT 安全**である可能性が高い。検証結果だけでなくこの観点でも判断する。

**ロールバック可能性を保つため、タスクごとにコミットを分ける。**
| A-12 | テストの主軸 | **PlayMode を主軸とし、EditMode と分離する** | PlayMode テストは実機・IL2CPP・ストリッピング下で実行できる。本ライブラリは `[SerializeReference]` の型が prefab 内の文字列でしか参照されない、リフレクション経路を持つ、ジェネリック実体化が AOT に依存する、といった理由でストリッピングの影響を受けやすく、**サポート範囲を実測する必要がある** → [TASK-024](Task/TASK-024-testAssemblyClassification.md) / [TASK-025](Task/TASK-025-strippingVerification.md) |

### A-2 / A-9 の適用範囲

| 対象 | 維持するか |
| --- | --- |
| 公開 API（A-2） | **しない。** 変更・削除してよい |
| シリアライズされた prefab / scene / asset（A-9） | **しない。** 壊れたら作り直す |
| **既存テストの通過** | **する。** 仕様変更でテストが不正になる場合はテストを直す。無検証で消さない |
| **AppTests の動作** | **する。** 実装の健全性確認に使うため |

互換性の制約が無いことと、**壊れたまま気付かないこと**は別。変更の影響はテストと AppTests で必ず確認する。

### A-2 の適用範囲

破壊的変更は許容するが、**使う先が無い API を先に生やさない**方針は維持する。理由が後方互換ではなく「未使用コードを抱えない」ことに変わるだけで、Phase 4 の保留判断は有効。

---

## 前提

- **評価軸は「自分のゲームを作るための道具」。** 公開ライブラリとしての普及は評価対象に含めない
- **カバレッジの限界。** UVMBinding で結線されていない UI 要素は、検査にも出力にも現れない。UI 全体をこのパッケージで構築している前提に立つ
- **対象は Unity 6.3（`6000.3.20f1`）。** C# 9 の機能と `UnityEngine.Accessibility` を使用してよい。C# 10 以降は使えない
