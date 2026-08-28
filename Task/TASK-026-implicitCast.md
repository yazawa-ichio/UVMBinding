# TASK-026: 数値変換の自動挿入

| | |
| --- | --- |
| Phase | 3（コア品質・並行トラック） |
| 状態 | 未着手 |
| 依存 | TASK-022（`CastRegistry`）、TASK-003（`Binder.Bind` を触るため） |
| 規模 | 中 |

## 目的

型が合わないだけで結線できない箇所に、**副作用のない数値変換を自動で挿入する**。副作用のある変換は自動にせず、Inspector から明示的に解決できるようにする。

## 背景

`Binder<T>` と バインド先の型が違うと、数値同士でも結線できない。`typeof(float).IsAssignableFrom(typeof(int))` は false のため、`int` のパスを `Binder<float>` に繋ぐには Converter が要る。

[TASK-023](TASK-023-showcase.md) のショーケース構築で実際に踏んだ。`ImageFillAmountBinder`（`Binder<float>`）を `int` のプロパティに繋ぐために `CastConverter` を明示的に付ける必要があった。

**利用側では数値の型が UI と ViewModel で食い違うことが頻繁にある**（HP は int、ゲージは float など）。そのたびに Converter を選ぶのは手間が大きい。

### TASK-022 の制約との関係

`CastConverter` は出力型をバインド時に `Binder<T>` から受け取るため、`ConcatenateConverter` の中間段では型が決まらず使えない（[TASK-022](TASK-022-castConverterConsolidation.md)）。

自動挿入を入れると**そもそも Converter を書かずに済むケースが大半**になり、この制約に当たる頻度が大きく下がる。制約自体は残るが、実用上の影響は小さくなる。

## 設計

### 適用する境界は 3 箇所

いずれも両端の型が静的に分かる。

| 境界 | 入力側 | 出力側 |
| --- | --- | --- |
| 入口 | `IBindingProperty.GetBindType()` | 先頭 Converter の `GetInputType()` |
| 段間 | 段 i の `GetOutputType()` | 段 i+1 の `GetInputType()` |
| 出口 | 最終段の `GetOutputType()`（Converter が無ければ入力型） | `Binder<T>` の `T` |

**`ConcatenateConverter` の中だけに実装しない。** それでは段間しか救えない。`Binder<T>.Bind` に置けば 3 境界すべてに効き、Converter を使わない結線でも機能する。

### 副作用の有無で扱いを分ける

**副作用がない変換は自動。副作用がある変換は自動にしない。**

| 区分 | 例 | 扱い |
| --- | --- | --- |
| **値が完全に保たれる** | `byte → int` / `int → long` / `float → double` / `short → double` | **自動挿入** |
| **精度が落ちうる**（C# では暗黙） | `int → float` / `long → double` / `uint → float` | **要判断**（後述） |
| **値域を超える・切り捨てる** | `double → int` / `long → int` / `float → int` | **自動にしない** |

### 「精度が落ちうる」区分の扱いは実装時に決める

`int → float` は C# が暗黙変換として許すが、絶対値が 2^24 を超えると精度が落ちる。`long → double` も同様（2^53 超）。

**UI の値でこの範囲に達することは稀**であり、自動に含めないと `int → float` という最頻出のケースが救えなくなる。一方「副作用がない場合のみ自動」という原則には厳密には反する。

判断は実装時に行い、**結果を本ファイルに記録する**。第一候補は「C# の暗黙変換に合わせて自動に含め、該当ペアを表として文書化する」。利用者にとって予測可能で、説明も「C# と同じ」で済むため。

### 副作用のある変換は Inspector で解決する

自動挿入しない代わりに、**Binder の Inspector に状態を出す**。

```
Path: Score
⚠ double → int は値が変わる可能性があります
   [ CastConverter を追加 ]
```

ボタンを押すと**明示的な `CastConverter` が Converter 欄に入る**。これにより以下が成立する。

- 変換の選択が**利用者の明示的な操作**として残る
- 結果は通常の Converter なので、**Inspector 上で見える**
- 宣言された Converter なので、**export の系統 2 に載る**（自動挿入は宣言が無いため出力に現れない）

Inspector に表示されるだけでも足りる。凝った UI は要らない。

### 変換できない組み合わせ

数値以外（`string → int` など）は従来どおり結線失敗とし、ログに両端の型を出す。[TASK-007](TASK-007-structuralLint.md) の Lint 項目「変換手段が無い型不一致」で静的にも検出する。

## 実装上の注意

**AOT は問題ない。** 挿入されるのは `CastRegistry` に登録済みの実装であり、`Register<TIn, TOut>` の静的参照が保たれる（[TASK-022](TASK-022-castConverterConsolidation.md)）。決定 A-13 の観点でも安全side。

**export での可視性は下がる。** 自動挿入された変換はどこにも宣言されないため出力に現れない。ただし値が保たれる変換は意味を変えないため、影響は小さいと判断する。副作用のある変換は明示的な Converter になるので出力に載る。

**`Binder.Bind` の結線判定に手を入れる。** [TASK-003](TASK-003-bindingPropertyRedesign.md) と同じ箇所であり、**再設計の後に着手する**こと。

## 完了条件

- `int` のプロパティを `Binder<float>` に Converter 無しで結線できるテスト
- `byte → int` / `float → double` など値が保たれる変換が自動で通ること
- `double → int` など副作用のある変換が**自動では通らない**こと
- 「精度が落ちうる」区分の判断結果を本ファイルに記録すること
- Inspector に警告と `CastConverter` 追加の導線が出ること（手動確認）
- 数値以外の不一致が従来どおり失敗し、両端の型がログに出ること
- 既存テスト全通過
- [ショーケース](TASK-023-showcase.md)の画面 4 で、自動挿入と明示的 Converter の両方を示すこと

## 備考

`CastConverter`（明示）は自動挿入の後も残る。役割が分かれる。

| | 用途 |
| --- | --- |
| 自動挿入 | 副作用のない変換。設定不要、Inspector にも現れない |
| `CastConverter`（明示） | 副作用のある変換。利用者が選んだことを記録する |
