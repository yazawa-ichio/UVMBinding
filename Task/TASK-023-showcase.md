# TASK-023: ショーケースシーンの構築

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **画面完成 / 基準値で判定可能 / 文章チェックリストは最終確認用に保留** |
| 依存 | TASK-000 |
| 規模 | 大 |

## 実施結果（2026-08-29）

`UVMBinding.Showcase` を新設し、**11 区分すべてをタブ切り替えで構築**した。各画面はスクロール可能。
シーンは手で組まず `ShowcaseBuilder` から再生成する（メニュー `UVMBinding/Showcase/Build All`）。

タブ切り替えは**あえて UVMBinding を使わない素の実装**にした。バインドが壊れた際も画面遷移だけは
生き残らせ、回帰の切り分けを可能にするため。

### 構築で発見したパッケージの問題

| 問題 | 記録先 |
| --- | --- |
| `CollectionBinder` が `ScrollRect` 内で再入し例外 | [TASK-014](TASK-014-collectionDiff.md) |
| `CastConverter` が Concatenate の中間段で使えない | [TASK-022](TASK-022-castConverterConsolidation.md) |
| 型不一致のたびに Converter が要る（自動挿入の着想） | [TASK-026](TASK-026-implicitCast.md) |
| `ConcatenateConverter` / `IConverter` が internal で外部から合成できない | [TASK-004](TASK-004-binderInfo.md) |
| `PositionBinder` が `RectTransform` で非決定的になる | [TASK-027](TASK-027-transformBinderLayout.md) |
| `UVMBinding.TMP` が Unity 6 で一切コンパイルされない | 本タスク内で修正済み |

### ストリッピング検証の基盤にもなった

[TASK-025](TASK-025-strippingVerification.md) で `ShowcaseSelfCheck` を追加した。全画面を巡回し、
`[SerializeReference]` の復元状況・表示テキスト・エラーを機械可読な形で書き出す。

**これが「観察可能な事象として書いたチェックリスト」の代わりになりうる。** 手で書いた文章ではなく、
**動いている状態そのものを基準値にする**方式。`make verify-strip` で差分を判定できる。

ただし現状の基準値は**表示テキストと結線の有無しか見ていない**。色・位置・アルファなど
演出系の確認は含まれないため、チェックリストを完全に置き換えるには至っていない。

### チェックリストの代わりに基準値を使う

**「観察可能な事象として書いたチェックリスト」は、文章ではなく基準値の形で用意した。**

`ShowcaseSelfCheck` が全 11 画面を巡回し、各画面について**操作前と操作後**の状態を採取する。
採取するのは以下で、`Unity/Verify/selfcheck-baseline.txt` に基準値として置いてある。

| 項目 | 内容 |
| --- | --- |
| `active` | GameObject の活性 |
| `text` | `Text.text` |
| `col` | `Graphic.color`（8bit 16 進） |
| `spr` / `tex` | `Image.sprite` / `RawImage.texture` の名前 |
| `fill` | `Image.fillAmount` |
| `alpha` | `CanvasGroup.alpha` |
| `inter` | `Selectable.interactable` |
| `val` | Slider / Scrollbar / Toggle / Dropdown / InputField の値 |
| `pos` / `scale` | Binder を持つ対象の `anchoredPosition` / `localScale` |

操作は画面ごとに決まった手順で行う。Slider は 0.75、Scrollbar は 0.25、Toggle は反転、
Dropdown は 1、InputField は固定文字列、Button はパス順に全押し。**順序を固定しないと結果が
変わる**ため、パス順に並べ替えてから実行する。

これにより、以前は文字列しか見ていなかった範囲が広がった。

| 分類 | 以前 | 現在 |
| --- | --- | --- |
| `TextBinder` | 見える | 見える |
| 入力系（双方向） | エコー経由で間接的 | **往復が値として見える**（`0.40 → 0.75` など） |
| Position / Scale / Color / Alpha / Fill / Interactable | **見えない** | **見える** |
| Event / EventArgument | 型の解決のみ | **発火後の状態が見える** |

操作によって 28 個の対象が変化し、`text` / `val` / `active` / `inter` / `pos` / `alpha` /
`fill` / `scale` / `col` の 9 項目すべてで差分が出ることを確認している。

判定は `make verify-strip` で行う。**基準値との差分がそのまま回帰の検出になる。**

### 文章のチェックリストは書かない

演出の質（トゥイーンが滑らかか、色が意図どおりか）は基準値では判定できない。
これは**最終確認時に人が見る**方針とし、機能開発中は基準値のみで判定する。

### 採取できない項目

**`RectTransform` の `rect` サイズと `localPosition` は採取しない。** Editor 実行と
プレイヤー実行でフォントの計測結果が 1px 単位で異なり、レイアウトの解決結果がずれるため。
親の大きさに依存しない `anchoredPosition` を代わりに使っている。

## 目的

`UVMBinding.AppTests` を最小構成から**基本機能を網羅したショーケース**に作り替え、**手動での動作確認の基準**として使えるようにする。

## 背景

現状の `AppTests` は最小構成で、Root / Dialog / InputDialog の 3 画面しかない。スクリプトは合計 345 行。

複数のタスクが完了条件に「AppTests のシーンが動作すること（手動確認）」を挙げているが、**何をもって「動作」とするかの基準が無い**。検証不能な受け入れ条件になっており、半年後の自分や他の作業者が判断できない。

網羅性も不足している。現在使われている Converter は 6 種類（`ObjectToString` / `IntComparison` / `BoolInverse` / `ConcatenateConverter` / `BoolSwitchTweenColor` / `BoolSwitchTweenVector3`）のみで、Binder も一部しかカバーしていない。

### なぜ再設計の前に作るか

- **TASK-003 の回帰リスクはここでしか検出できない。** 単体テストは `PropertyContainer` レベルを見るが、prefab の結線が壊れたことは実機で見るしかない
- **変更前のベースラインが手に入る。** 作り替えた後に再設計すれば、挙動の差分が分かる
- **現行 API を一通り使うことになる**ため、再設計に向けた使い勝手の問題が表面化する

## 網羅する範囲

画面（またはタブ）単位で分割する。1 画面に詰め込まない。

| # | 画面 | 対象 |
| --- | --- | --- |
| 1 | 基本バインド | `TextBinder` / `ImageSpriteBinder` / `RawImageTextureBinder` / `ActiveBinder` / `EnabledBinder` / `TargetActiveBinder` / `OnOffActiveBinder` |
| 2 | 入力（双方向） | `InputFieldBinder` / `SliderBinder` / `ToggleBinder` / `ScrollbarBinder` / `DropdownSelectBinder` / `DropdownStringDataBinder` / `InteractableBinder` |
| 3 | コレクション | `CollectionBinder` / `Collection<T>` / `ValueCollection<T>` / `CollectionIndexArgument` / 要素の追加・削除・並べ替え |
| 4 | Converter | `Cast` / `Comparison` / `BoolSwitch` / `BoolSwitchTween` / `BoolInverse` / `ObjectToString` / `StringToLength` / `StringComparison` / `Lerp` / `DelaySet` / `Concatenate` / `Vector` |
| 5 | 派生値 | `CombineBoolProvider` / `CombineStringProvider` / `ViewModelPropertyExtension` / 汎用 `PropertyProvider`（TASK-018 後） |
| 6 | View 合成 | ネストした View / `ParentViewBind` / `SelectViewBind` / `ViewBinder` / `DataSourceProvider` / `ViewModelSourceProvider` |
| 7 | ViewModel 種別 | `[Bind]` / `[Event]` / `Property<T>` / `ReflectionViewModel` / `GeneralViewModel` / `SetDirty` / `SetAllDirty` |
| 8 | 演出系 Binder | `PositionBinder` / `RotationBinder` / `ScaleBinder` / `CanvasGroupAlphaBinder` / `CanvasGroupBlocksRaycastsBinder` / `GraphicColorBinder` / `AnimatorBool/Float/IntBinder` / `ImageFillAmountBinder` |
| 9 | Event と引数 | `EventButton` / `InputFieldSubmitEvent` / `DropdownSelectEvent` / `EventSystemSelectBinder` / `EventSystemPointerStayBinder` / `PointerMovePositionBinder` / 各 `EventArgument` |
| 10 | 任意パッケージ | TMP 系 Binder / Localization 系 Binder。**`defineConstraints` で切り替わることを確認できる形にする** |
| 11 | その他 | `GeneralBinder` / `ComponentSetter`（`TransformSetter` / `RectTransformSetter`） |

**演出系（画面 8）は [TASK-008](TASK-008-semanticRole.md) で `SemanticRole.None` として収集対象外にする対象**でもある。ショーケースに含めておくと、除外が正しく効いているかの確認にも使える。

## 動作確認のプロトコル

**「動作する」を検証可能な条件として定義する。**

1. Play 中に Console へ **エラー・例外が出ないこと**
2. 各画面の**確認項目チェックリスト**をすべて満たすこと
3. 画面遷移とダイアログの開閉が破綻しないこと

チェックリストは画面ごとにシーン内（またはショーケース用の `README`）に記述し、**画面上に確認手順を表示する**。「Toggle を押すと色が 0.2 秒かけて赤へ遷移する」のように、**観察可能な事象として書く**。

このチェックリストが、他タスクの「AppTests のシーンが動作すること」を置き換える。

## 対象ファイル

- `Unity/Assets/UVMBinding.AppTests/` 配下（全面的に作り替え）
- 既存の `Root.prefab` / `Dialog.prefab` / `InputDialog.prefab` は流用または作り直し

## 完了条件

- ~~上記 11 区分すべてに対応する画面が存在すること~~ 済
- ~~各画面に確認項目チェックリストがあり、観察可能な事象として記述されていること~~ **基準値
  （`Unity/Verify/selfcheck-baseline.txt`）で代替。演出の質のみ最終確認時に人が見る**
- ~~Play して全画面を巡回し、Console にエラー・例外が出ないこと~~ 済（`errors=0` で自動判定）
- ~~TMP / Localization を外した構成でもコンパイル・実行できること~~ **TMP は未導入構成で
  動作を確認済み。あわせて Unity 6 で `UVMB_TMP` が立たない不具合を修正した。Localization を
  外した構成は未確認**
- ~~チェックリストを他タスクから参照できる形で配置すること~~ 済（`make verify-strip`）

## 備考

**規模が大きい。** 分割して進めてよいが、**TASK-003 着手前に画面 1〜7 は完成させる**こと。再設計の回帰検出に必要な範囲がここに集中している。

画面 8〜11 は TASK-003 と並行、または後回しでよい。

### 実使用のある機能を優先する

実プロジェクトで実際に使われている Converter は 12 種類に集中している（Comparison 系・BoolInverse・ObjectToString・Concatenate・StringComparison・BoolSwitch 系・Lerp・DelaySet）。**これらを先に網羅する。**

一方、実使用がゼロの機能（数値 Cast、`PropertyProvider` 系、`ReflectionViewModel` / `GeneralViewModel` の直接利用）もある。網羅が目的なので最終的には含めるが、**優先度は下げてよい**。

利用側では独自の Binder / Event / Converter / EventArgument が 50 以上定義されている。**ショーケースにも「ユーザー定義の拡張」の実例を 1 種ずつ含める**こと。パッケージ組み込みの型だけでは、拡張点が機能しているかを確認できない。

既存の `TestEnumPopupBinder` / `TestViewModelSourceProvider` / `GlobalStateDataSource` は、ユーザー定義の Binder / Provider の実例として**残す価値がある**。作り替えの際に削除しないこと。
