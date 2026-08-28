## Plan: View HTML Export Foundation

> **Status: 草案。着手しない。**
>
> 本計画は目標の記述であり、実装計画としては未確定。
> 先行して本体側の情報収集基盤を整備する（[plan-metadataFoundation.prompt.md](plan-metadataFoundation.prompt.md)）。
> 基盤が固まり、Lint が実際に動いた時点で本計画を再評価する。
>
> 最大の未検証事項は **Step 1（命名の実測）** であり、ここの結果次第で計画全体の前提が変わる。

View の構造・状態・操作方法を、エージェントが読める形で外部化する基盤を作る。

## 設計原則

**エージェントが圧倒的に学習している標準規格に、極めて近い形へ落とし込む。**

これが唯一の設計原則であり、迷った場合は常にこれを優先する。独自形式・独自語彙・独自属性は、標準規格で表現できない場合にのみ導入する。

理由は、消費側のエージェントを特定しないため。トークン効率のような一時的な最適化ではなく、語彙の普遍性という恒久的な性質に賭ける。特定のモデルに合わせた形式は、そのモデルが変われば価値を失う。

採用する標準規格は以下。

- **HTML** — 構造と表示内容
- **ARIA**（role / state / property） — 操作方法と状態
- **HTML フォームセマンティクス**（`name` / `value` / `for`） — データバインドの対応関係
- **JavaScript** — 宣言済みのバインド関係
- **C#** — ViewModel のロジック（ソースをそのまま埋め込む）

## Goal

View ひとつを、標準規格だけで記述された 1 ファイルとして出力できる状態にする。

そのファイルを読んだエージェントが、追加の説明なしに以下を判断できることを到達点とする。

- 画面に何が表示されているか
- 何が操作でき、何が今は操作できないか
- ある操作をすると何が起きるか

---

## 情報の 4 系統と出所

この設計の要は、**すべての情報が二重実装を生まない出所を持つこと**である。手で書き起こす情報を作らない。

| 系統 | 内容 | 出所 | 表現 |
| --- | --- | --- | --- |
| 1. 構造・状態 | 階層、role、表示テキスト、現在値、操作可否 | Component 型・実行時の値 | HTML + ARIA |
| 2. 宣言済みバインド | 入力と表示の対応、派生値の依存 | `Binder.Path` の一致、`PropertyProvider` | HTML の `name` / `for`、生成 JS |
| 3. ViewModel ロジック | イベントが状態をどう変えるか | **C# ソースそのもの** | `<script type="text/x-csharp">` |
| 4. 不可逆性・コスト | 課金・削除など取り返しのつかない操作 | **新規の注釈**（唯一、追加記述が必要） | `data-consequence` |

系統 1〜3 は追加の記述コストがゼロである。系統 4 のみ注釈が必要だが、対象は不可逆または有料の操作に限られるため、ゲーム 1 本で数十個の規模に収まる。

### 系統 2 と 3 の境界

**この境界を越えて JS を生成してはならない。**

| | 例 | 判定 |
| --- | --- | --- |
| 生成してよい | `input` ⇄ `span` の同期（同じ `Path` の Binder が複数ある） | バインディンググラフから導出できる |
| 生成してよい | `CanPurchase = All(HasStock, HasFunds)` | `CombineBoolProvider` が宣言済みデータとして保持している |
| **生成してはならない** | `OnIncrement` → `Count++` | ViewModel の C# 内にあり、導出できない |

判定基準は「その関係が UVMBinding の保持するデータから導けるか」の一点。導けない関係を JS として書き起こすと二重実装になり、実装とズレたときにエージェントを確信を持って誤らせる。導けない関係は系統 3（C# ソース埋め込み）で伝える。

### 宣言的に書ける範囲を広げる意味

系統 2 の範囲が広いほど、生成できる JS が増え、エージェントが静的に理解できる範囲が広がる。
`analysis-bindingDesignReview.md` の改善候補 5（`[DependsOn]` による computed）は、記述の簡便さだけでなく、**この export の解像度を直接上げる機能**として位置づけられる。

### Converter の扱い

**Converter による変換は原則 export できない。** path を持たず、依存関係の宣言も無いため。

ただし**組み込み Converter は型名から変換内容が確定する**（`IntToFloat` / `GreaterThan` / `BoolInverse` など約 130 型）。型 → 変換内容のマッピングテーブルを持てば系統 2 として出力できる。ユーザー定義 Converter は対象外とする。

エージェントに読ませたい派生は、`PropertyProvider` か `[DependsOn]` で書くことを推奨する（[CONVENTIONS.md](../CONVENTIONS.md) 8.5）。

---

## Structural Model

### Intermediate Representation

View を走査した結果を、出力形式に依存しない C# のデータ構造として保持する。
HTML 出力・lint・非 LLM 消費者（自動 QA、ビヘイビアツリー）が同じ IR を参照する。

```csharp
class ViewNode {
    string Id;                    // View の識別子
    string Label;                 // 表示名
    bool IsModal;
    List<ElementNode> Elements;
    List<ViewNode> Children;
    string ViewModelTypeName;     // 系統 3 の埋め込み対象
}

class ElementNode {
    string Id;                    // 安定した識別子（HTML の id）
    string Role;                  // ARIA role
    string Label;                 // 表示テキスト / 画像 alt
    string Value;                 // 現在値（動的キャプチャ時のみ）
    string BindPath;              // Binder.Path      → HTML の name
    string EventName;             // ViewEventBase    → HTML の name
    string[] DependsOn;           // 派生元の要素 Id  → HTML の for
    ElementState State;           // Disabled / Hidden / Clipped
    ConsequenceKind Consequence;  // 系統 4
    List<ElementNode> Children;
}
```

### 消費者の種類

- **LLM** → HTML + 生成 JS + 埋め込み C#
- **プログラム** → IR を直接読む
- **検証機構** → 永続化したスナップショット（後述の提案。未確定）

当初は前 2 者のみとし「独自テキスト形式の置き場所は存在しない」と結論していたが、
**回帰差分には永続化された比較可能な成果物が要る**ため、第 3 の消費者がありうる。
実行中のプログラムは IR を直接読めばよいが、**ベースラインとして保存し後で差分を取る**
用途には直列化された形式が必要になる。

### 出力対象と非対象

**出力する。** 操作可能な要素、表示内容を持つ要素、構造を成す要素。

**出力しない。** 演出系 Binder（`GraphicColorBinder` / `PositionBinder` / `RotationBinder` / `ScaleBinder` / `AnimatorBoolBinder` など）。操作対象でも表示内容でもないため、ノイズにしかならない。

### カバレッジの限界

**UVMBinding で結線されていない要素は出力されない。** 走査の起点が `View.Prepare()` / `GetElements()` であるため。UI 全体をこのパッケージで構築している前提に立つ。これは制約ではなく前提として明記する。

---

## HTML への写像

### 原則

標準の `id` / `name` / `value` / `for` / `disabled` / `hidden` と ARIA 属性で表現し、**独自の `data-*` 属性は原則として導入しない**。

例外は 2 つのみ。

- `data-view-name` — View の境界を示す（標準に相当物がない）
- `data-consequence` — 系統 4（標準に相当物がない）

### 対応表

| UVMBinding | HTML | 備考 |
| --- | --- | --- |
| `View` | `<section>` / `role="dialog"` + `aria-modal` | モーダルかどうかは操作可否に直結する |
| `Binder.Path` | `name` 属性 | `name` は「この値が送信されるキー」であり意味が一致する |
| `ViewEventBase.m_EventName` | `<button name="...">` | 同上 |
| `TextBinder` / `TextMeshProBinder` | `<p>` / `<output>` | 派生値なら `<output>` |
| `InputFieldBinder` | `<input name="..." value="...">` | |
| `ToggleBinder` | `<input type="checkbox">` / `role="switch"` | |
| `SliderBinder` | `<input type="range" min max value>` | |
| `DropdownSelectBinder` | `<select>` + `<option>` | |
| `ImageSpriteBinder` | `<img alt="...">` | alt に sprite 名 |
| `CollectionBinder` | `<ul>` + `<li>` | 件数と仮想化の有無を持たせる |
| `InteractableBinder` | `disabled` / `aria-disabled` | |
| `ActiveBinder` / `CanvasGroup*` | `hidden` / `aria-hidden` | |
| 派生値（`CombineProvider`） | `<output for="...">` | `for` は仕様上「計算に寄与した要素の id リスト」 |

`AccessibilityRole`（Unity 6 の Accessibility モジュール）の enum と突き合わせ、ARIA と Unity の双方に存在しない役割は `role="none"` + label に落とす。

### 遷移の表現は行わない

`<a href="...">` による画面遷移の表現は**採用しない**。UVMBinding のイベントは名前付き dispatch であり、それが遷移かどうかを判定する情報がソース側に存在しないため。必要になった場合は `[Event]` 側に意味の注釈を足す設計判断として別途扱う。

### 出力例

```html
<section data-view-name="PurchaseDialog" role="dialog" aria-modal="true" aria-label="商品を購入">
  <h2>商品を購入</h2>
  <img src="item_potion.png" alt="ポーション">

  <div role="group" aria-label="数量">
    <button id="dec" name="OnDecrement" type="button" disabled>−</button>
    <input  id="count" name="Count" type="number" value="1" min="1" max="99" readonly>
    <button id="inc" name="OnIncrement" type="button">＋</button>
  </div>

  <input  id="unitPrice" name="UnitPrice" value="100" hidden>
  <output id="total" for="count unitPrice" name="TotalPrice">¥100</output>

  <button id="purchase" name="OnPurchase" type="button" data-consequence="purchase">購入する</button>
  <button id="cancel"   name="OnCancel"   type="button">キャンセル</button>
</section>
```

`dec` の `disabled` は「1 未満にはできない」という規則を、規則を説明せずに伝えている。
エージェントが必要とするのは規則ではなく、その瞬間に何が可能かである。

### 生成 JavaScript

系統 2 の関係のみを出力する。

```html
<script>
// Binder.Path の一致から導出（SearchView の例）
searchText.addEventListener('input', e => { preview.textContent = e.target.value; });

// CombineProvider の宣言から導出
const CanPurchase = () => HasStock && HasFunds;
</script>
```

### ViewModel ソースの埋め込み

系統 3。`<script>` に未知の `type` を与えると実行されないため、標準の挙動のまま文書内に保持できる。

```html
<script type="text/x-csharp" data-source="PurchaseViewModel.cs">
public class PurchaseViewModel : ViewModel
{
    [Bind]  public int Count { get; set; } = 1;
    [Bind]  public int UnitPrice { get; set; }
    [Bind]  public string TotalPrice => $"¥{Count * UnitPrice}";
    [Event] public Action OnIncrement { get; set; }
    [Event] public Action OnPurchase { get; set; }
}
</script>
```

**埋め込みはソースの複製ではなく転記であるため、ドリフトが原理的に発生しない。**
手書きの JS でロジックを再現する案に対する優位はここにある。

**スコープ規約。** 際限なく膨らむため、境界を固定する。

- 対象は View に宣言された ViewModel 型のみ
- その型の `[Bind]` / `[Event]` メンバとその実装まで
- 呼び出し先のサービス層には**辿らない**

**ビルド設定。** ゲームロジックのソースを含むため、EditMode および開発ビルドに限定する。製品ビルドから確実に外す。

---

## キャプチャの 2 モード

IR は共通、収集方法のみ異なる。

| | 静的（EditMode） | 動的（Runtime） |
| --- | --- | --- |
| 値 | ViewModel 未接続のため無し | 実値を持つ |
| 用途 | **意味的な lint** | エージェントによる操作 |
| 単位 | Prefab | 画面スナップショット |

---

## 提案：検証用スナップショット出力（未確定）

> **本節は提案であり、タスクとして確定していない。** 実施するかは基盤（IR）が動いてから判断する。

### 動機

[TASK-023](../Task/TASK-023-showcase.md) のショーケース構築で、UI の不具合を検知する手段が
「コンソールのエラー」と「キャプチャ画像の目視」しか無く、**正しさの基準を人が書き下ろす必要がある**
ことが問題になった。チェックリストを毎回手で書くのは現実的でない。

一方、本計画で **View の構造解析（IR）が手に入る**。であれば、同じ走査結果を
**検証向けに別途出力してもよいのではないか**、という提案。

### 何を出すか

エージェント向けの HTML とは別に、**座標・値・状態**を含むスナップショットを出す。

| 項目 | 用途 |
| --- | --- |
| 矩形（位置・サイズ） | 重なり、サイズ 0、親領域からのはみ出し、画面外の検出 |
| 値 | バインド結果が期待どおりか |
| 状態 | 有効／無効、表示／非表示、クリップ |
| 並び順 | 読み順・タブ順の回帰 |

### 最大の価値は単発検査ではなく回帰差分

```
変更前にスナップショットを保存 → 変更 → 再取得 → 差分
```

**期待値を人が書く代わりに、動いている状態そのものをベースラインにできる。**
UI のスナップショットテストとして確立した手法であり、TASK-023 の
「チェックリストを書かないと正しさを定義できない」問題に直接効く。

期待値を書かずに機械判定できるものもある。重なり、サイズ 0、はみ出し、画面外、並び順の変化。

### 効果の実測（TASK-023 で踏んだ 5 件で検証）

| 事象 | 座標・値・状態で検知できたか |
| --- | --- |
| ラベルと値の重なり（`childControlWidth` の誤り） | **○** 矩形の重複 |
| コレクション行が不可視 | **○** サイズが期待と異なる + 非アクティブ |
| sprite 未設定で白いまま | **○** 値が null |
| Viewport の描画破綻（`Mask` によるシアン塗り潰し） | **×** 座標は正常。**描画の問題**でピクセルを見るしかない |
| `CollectionBinder` の再入による例外 | **×** コンソールでしか分からない |

**5 件中 3 件。** 有効だが、**キャプチャ画像とコンソールの置き換えにはならない。**
3 つを併用する前提で設計する。

### 設計上の制約

**エージェント向けの出力には混ぜない。** 本計画の軸は「標準規格に寄せて**意味**を伝える」ことであり、
演出系 Binder を出力対象から外す判断もしている。座標は意味ではなく**体裁**であるため、
同じ出力に混ぜると方針が濁る。

**別のシリアライザとして分ける。** IR を主にして出力を使い分ける構成にしてあるため、
消費者が違う（エージェント／検証機構）なら出力も分けるのが自然。既存の構造にそのまま乗る。

**座標は正規化する。** 絶対ピクセルで持つと解像度が変わるだけで全件差分になる。
親からの相対値、または参照解像度基準に正規化しないと回帰差分として使えない。

**演出系 Binder はこの出力では対象に含めてよい。** エージェント向けには除外するが、
位置・色・スケールの回帰はまさに検証したい対象であり、判断が逆になる。

### 未確定の論点

- 出力形式（YAML / JSON / 独自）。**エージェント向けではないため、標準規格に寄せる制約は適用されない**
- 差分の粒度と許容誤差（浮動小数の丸め）
- ベースラインの保管場所と更新手順
- 動的キャプチャのみか、EditMode でも取るか

---

## Lint

IR が完成した時点で実装でき、**AI 連携に投資する前に IR の妥当性を検証できる**ため、実装順では先に置く。

`analysis-bindingDesignReview.md` の改善候補 2（EditMode パス検証）と走査経路・データを共有する。

### 構造検査

- バインドパスが宣言された ViewModel 型に存在しない
- View が dispatch するイベント名を ViewModel が購読していない（逆も検査する）
- 同一 View 内でイベント名が衝突している
- 対話可能な role を持つ要素にラベル（テキスト・画像 alt）が無い
- 画面に「戻る / 閉じる」に相当する操作が存在しない

### 命名品質検査

**この設計は「既存の命名が意味を持っていること」に賭けている。** `OnClick01` / `Btn_Sub` / `Value2` のような命名では、どれだけ形式を整えてもエージェントは読めない。そして現状のシステムは悪い命名を検出しない。バインドは同じように通るためである。

人手を「注釈を書く作業」ではなく「規約違反を直す作業」に置き換える。

- `Click\d+` / `Button\d+` / `Value\d+` などの無意味名をブロックリストで検出
- 名前に `Purchase` / `Delete` / `Reset` を含むのに `[Consequence]` 注釈が無い要素を検出

### 系統 4 のゲート

未注釈のイベントは、エージェント連携側が**自動押下を拒否する**既定とする。
任意のドキュメントではなくゲートにすることで、注釈が腐らない。

---

## Implementation Steps

1. **命名の実測**（実装前に行う）。既存画面を 3 枚選び、理想化せず現在の `Path` / `EventName` のまま HTML を手で起こし、実際に載せる予定のモデルで操作できるか試す。通らなければ作るべきものは exporter ではなく命名規約と lint になる。
2. IR を定義する。`ViewNode` / `ElementNode` と、静的・動的の 2 キャプチャモード。
3. View 走査の単一入口を作る。`Prepare()` / `GetElements()` を起点とする。
4. **Lint を実装する**。構造検査と命名品質検査。IR の妥当性がここで検証される。
5. HTML シリアライザを実装する。ARIA + フォームセマンティクス。
6. 生成 JS を実装する。系統 2 の境界を守る。
7. ViewModel ソース埋め込みを実装する。スコープ規約とビルド設定。
8. `[Consequence]` 注釈と、未注釈イベントの自動押下拒否。

---

## Relevant Files

- [Unity/Assets/UVMBinding/Runtime/View/View.cs](Unity/Assets/UVMBinding/Runtime/View/View.cs) — 走査の起点
- [Unity/Assets/UVMBinding/Runtime/View/Binding.cs](Unity/Assets/UVMBinding/Runtime/View/Binding.cs) — 結線情報とバインド対象
- [Unity/Assets/UVMBinding/Runtime/View/IViewElement.cs](Unity/Assets/UVMBinding/Runtime/View/IViewElement.cs) — 出力最小単位
- [Unity/Assets/UVMBinding/Runtime/Event/ViewEventBase.cs](Unity/Assets/UVMBinding/Runtime/Event/ViewEventBase.cs) — イベント名の出所
- [Unity/Assets/UVMBinding/Runtime/Binder/Binder.cs](Unity/Assets/UVMBinding/Runtime/Binder/Binder.cs) — `Path` の出所
- [Unity/Assets/UVMBinding/Runtime/PropertyProvider/CombineBoolProvider.cs](Unity/Assets/UVMBinding/Runtime/PropertyProvider/CombineBoolProvider.cs) — 系統 2 の派生宣言
- [Unity/Assets/UVMBinding/Editor/Drawer/ElementViewer.cs](Unity/Assets/UVMBinding/Editor/Drawer/ElementViewer.cs) — 既存の表示名ルール
- [Unity/Assets/UVMBinding/Editor/Drawer/BindingViewer.cs](Unity/Assets/UVMBinding/Editor/Drawer/BindingViewer.cs) — 列挙順の参照実装

---

## Verification

1. IR のユニットテスト。View から `ViewNode` / `ElementNode` への変換。
2. HTML 出力のスナップショットテスト。標準属性のみが出ること、`data-*` が 2 種類に限られること。
3. 生成 JS の境界テスト。宣言されていない関係が JS として出力されないこと。
4. Lint のテスト。既知の違反パターンを検出できること。
5. 既存の Binding / Reflection テストが壊れていないこと。
6. 実際に載せる予定のモデルで、出力ファイルから操作判断ができること。

---

## Decisions

- 標準規格への近接を唯一の設計原則とする。独自形式は採用しない。
- 独自属性は `data-view-name` と `data-consequence` の 2 種のみ。
- 生成 JS はバインディンググラフから導出できる関係に限定する。ViewModel ロジックを JS で再現しない。
- ViewModel の C# ソースは複製せず転記する。EditMode / 開発ビルド限定。
- 画面遷移の `<a href>` 表現は採用しない（判定材料がソースに無い）。
- 演出系 Binder は出力しない。
- 出力対象は UVMBinding で結線された要素のみ。
- 実装配置は `UVMBinding.Semantics` として optional assembly に切る。Runtime コアの性格を汚さないため。
- Lint を HTML 出力より先に実装する。

---

## Risks

- **命名品質への依存。** 系統 2 は既存の命名が意味を持つことに賭けている。Step 1 の実測で成否が決まる。
- **消費側の能力下限。** 一定以下の能力のエージェントでは、どの表現形式でも操作できない。その場合の解はスクリプト用 API であり、本計画とは別物になる。
- **系統 4 の注釈量。** 消費側が弱いほど、強いモデルが推論で埋めていた分を明示する必要が生じ、注釈対象が増える。
