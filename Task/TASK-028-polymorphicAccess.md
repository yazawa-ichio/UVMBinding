# TASK-028: 多態性を許す範囲の決定

| | |
| --- | --- |
| Phase | 0.5（基盤再設計） |
| 状態 | **一部決定 / 未着手** |
| 依存 | TASK-003 |
| 規模 | 小〜中 |

## 背景

[TASK-003](TASK-003-bindingPropertyRedesign.md) で 1 path = 1 型にした結果、**型が完全一致
しない場合の扱いが未確定のまま残った**。

設計文書には「登録が `object` なら任意の型の書き込みを受け入れる」と書いたが、**実装は
`PropertyContainer.Get<T>` の完全一致だけ**になっていた。

**この不一致は「実装を設計に合わせる」ではなく「設計を撤回する」で解消する**（論点 1）。
型は暗黙に跨がせず、変えたい場合は明示的に解除する。

## 用語：ここでの「多態性」

`IsAssignable` は `Type.IsAssignableFrom` である。**CLR の代入互換のみ**を指す。

| 組み合わせ | 可否 |
| --- | --- |
| 派生 → 基底 | 可 |
| 実装型 → インターフェース | 可 |
| 値型 → `object`（ボックス化） | 可 |
| `int` → `float` | **不可** |
| `int` → `long` | **不可** |
| `T` → `T?` | **不可** |

**数値変換は多態性の範囲外。** それは Converter（[TASK-022](TASK-022-castConverterConsolidation.md)）の
責務であり、本タスクでは扱わない。

---

## 層の整理

`Get<T>` / `Set<T>` という名前が複数の層に存在するため、**どの層の話かを常に明示する。**

### 層 1: ViewModel

利用側が直接書く層。`[Bind]` の実体もここを通る。

| API | 公開範囲 | 呼び出す先 |
| --- | --- | --- |
| `IViewModel.Get<T>` / `Set<T>` | **public** | `ViewModel.GetImpl<T>` / `SetImpl<T>` |
| `ViewModel.GetImpl<T>` / `SetImpl<T>` | protected | `PropertyContainer.Get<T>` |
| `GeneralViewModel.Get<T>` / `Set<T>` | **public** | 同上（薄いラッパ） |
| `ViewModel.Property` | protected | 層 2 への入口 |

**ILPP が生成する `[Bind]` プロパティは `GetImpl` / `SetImpl` を呼ぶ。** 宣言型がそのまま T に
なるため、この経路では型が一貫している。

### 層 2: PropertyContainer

path とプロパティの対応を持つ層。

| API | 公開範囲 | 型の扱い |
| --- | --- | --- |
| `Subscribe<T>` | **public** | 完全一致のみ（`Get<T>` に依存） |
| `Unsubscribe<T>` | **public** | 完全一致のみ（同上） |
| `SubscribeAssignable<T>` | **public** | **代入可を許す** |
| `SetDirty` / `SetAllDirty` | **public** | 型に無関係 |
| `Get<T>` | internal | 完全一致のみ。不一致は `Log.Error` して `null` |
| `TryGet` | internal | 型消去。`IBindingProperty` を返す |
| `GetAll` / `GetAllImpl` | internal | 型消去 |

**public な型付き経路は `Subscribe<T>` / `Unsubscribe<T>` の 2 つだけ。** 値の読み書きは
層 1 経由でしか行えない。

### 層 3: BindingProperty

値そのものを持つ層。**インターフェースも実装もすべて `internal`。** 利用側からは触れない。

| API | 型の扱い |
| --- | --- |
| `IBindingProperty<T>.Value` | 完全一致。`get` / `set` を持つため**不変（invariant）** |
| `IBindingProperty.GetObject()` | 型消去 |
| `IBindingProperty.SetObject(object)` | 型消去。**不正な型では `InvalidCastException` を投げる** |
| `IBindingProperty.IsAssignable<T>()` | 代入可否の判定 |

### 層をまたぐ制約

**層 3 の `IBindingProperty<T>` が不変であることが、すべての制約の source になっている。**

`Value` に setter があるため、`BindingProperty<Base>` を `IBindingProperty<Derived>` としても
`IBindingProperty<object>` としても返せない。したがって：

- **層 2 の `Get<T>` は多態にできない。** ハンドルを返すため
- **層 2 の `Subscribe<T>` も多態にできない。** `Get<T>` に依存しているため
- **層 1 の `GetImpl` / `SetImpl` は多態にできる。** 値しか扱わないため

**多態性を入れられるのは層 1 と、層 2 に値だけを扱う API を足した場合に限られる。**

---

## 論点

### 論点 1（層 1）：`ViewModel.Set<T>` で代入可を許すか → **決定: A-1（完全一致のみ）**

登録が `Base`、書き込みが `Derived` の場合の `IViewModel.Set<Derived>(path, val)`。

| 案 | 挙動 |
| --- | --- |
| **A-1** | 完全一致のみ。`Derived` の書き込みは `Log.Error` して無視 |
| A-2 | 代入可なら許す |
| A-3 | 登録が `object` のときだけ許す |

**A-1 を採る（2026-08-29 決定）。** 暗黙に型を跨がせず、型を変えたい場合は
**明示的に解除する**（論点 6）。

これにより **TASK-003 の設計文書に書いた「`T == object` なら常に受け入れる」は撤回する。**
実装しないのではなく、**仕様として持たない。**

### 論点 2（層 1）：`ViewModel.Get<T>` で代入可を許すか → **取り下げ**

登録が `Derived`、読み取りが `Base` の場合の `IViewModel.Get<Base>(path)` を想定していた。

**論点として立てるべきではなかった。** 書き込み側との対称性から挙げただけで、
**具体的な利用例を挙げられない。**

- `[Bind]` は宣言型がそのまま T になるため、型が食い違わない
- `IViewModel.Get<T>` を直接呼ぶ形は実プロジェクトに存在しない（[TASK-003](TASK-003-bindingPropertyRedesign.md) の調査）
- 型を跨いで読みたい場合は層 2 の `SubscribeAssignable<T>` が既にある

**論点 1 に揃えて完全一致のみとする。**

### 論点 3（層 2）：値だけを扱う API を足すか → **不要**

想定していた配置は以下。

| 項目 | 内容 |
| --- | --- |
| 置き場所 | **`PropertyContainer`**（層 2）。`Get<T>` と同じクラス |
| 公開範囲 | `internal`。`Get<T>` と揃える |
| シグネチャ | `bool TryGetValue<T>(string path, out T value)` / `bool TrySetValue<T>(string path, T value)` |
| 呼び出し元 | **`ViewModel.GetImpl<T>` / `SetImpl<T>`**（層 1） |
| 目的 | 層 1 で多態を許す際、判定を `ViewModel` と `GeneralViewModel` に二重に書かずに済ませる |

**論点 1・2 が完全一致のみに決まったため不要。** 層 1 は現状どおり `Get<T>` を呼べばよい。

### 論点 4（層 3）：`SetObject` が例外を投げる → **取り下げ**

`BindingProperty<TValue>.SetObject(object val)` は検査なしのキャスト（`Value = (TValue)val;`）で、
型が合わなければ `InvalidCastException` が飛ぶ。

これを「[TASK-003](TASK-003-bindingPropertyRedesign.md) の『出荷後に落とさない』方針と矛盾する」
として論点に挙げたが、**この理由は成り立たない。**

#### 挙げた理由が誤っていた

**1. 失敗の種類が違う。**

| 箇所 | 失敗の内容 | 誘発するもの |
| --- | --- | --- |
| 層 2 `Get<T>` | **登録の型と要求の型が違う** | prefab の結線など**データ** |
| 層 3 `SetObject` | **書き込む値の実体が型に合わない** | **コード** |

TASK-003 で「落とさない」と決めたのは前者。**データ起因の食い違いで出荷後に落とさないため**で
あり、コード側の誤りにまで広げる決定ではなかった。**失敗の種類を確認せずに方針を横展開した。**

**2. 到達しない。** 双方向で書き戻す組み込み Binder の T は以下で、**すべて sealed か値型**。

| Binder | T |
| --- | --- |
| `InputFieldBinder` | `string` |
| `SliderBinder` / `ScrollbarBinder` | `float` |
| `ToggleBinder` / `EventSystemSelectBinder` / `EventSystemPointerStayBinder` | `bool` |
| `DropdownSelectBinder` | `int` |
| `PointerMovePositionBinder` | `Vector2` |
| `EnumPopupBinderBase` | enum |

`Binder.Bind` が `SetObject` へ落ちる分岐に入るのは `prop.IsAssignable<T>()` が真かつ
`prop is IBindingProperty<T>` が偽のときだが、**T が sealed か値型なら代入可＝完全一致**に
なるため、この状態は起こらない。

**先に挙げた `Dog` / `Animal` の例は、利用側が非 sealed な型で双方向 Binder を自作した場合に
限られる。** そしてその状況で誤った型を書くのは**コードの誤り**であり、例外で即座に分かる方が良い。

#### 結論

**現状維持（C-1）。** 何も変えない。

### 論点 5（層 2）：新しい API の公開範囲

論点 3 で API を足す場合、`public` にするか `internal` に留めるか。

`public` にすると「型を跨ぐ読み書き」が層 2 の正規の手段になり、静的検査
（[TASK-007](TASK-007-structuralLint.md)）で不一致を潰す方針と多少ぶつかる。

`internal` に留めれば、利用側から見える多態な経路は層 1 の `Get<T>` / `Set<T>` と
層 2 の `SubscribeAssignable<T>` だけになる。

### 論点 6（層 1）：型を変えて登録し直す手段 → **決定: `GeneralViewModel.UnSet`**

1 path = 1 型のため、一度確定した型は変えられない。型が静的に保証されない
`GeneralViewModel` では詰まる。

**`GeneralViewModel` にのみ `UnSet` を用意する（2026-08-29 決定）。** 既存の型の登録を解除し、
別の型で登録し直せるようにする。

```csharp
vm.Set("P1", 10);     // int で確定
vm.UnSet("P1");       // 解除
vm.Set("P1", "Text"); // string で登録し直す
```

`[Bind]` / `Property<T>` は宣言で型が決まるため、解除する理由が無い。**`ViewModel` 本体や
`PropertyContainer` の public API には出さない。**

#### 解除で影響を受けるもの（実装を読んで確認した範囲）

path 1 つに紐づく状態は以下。

| 状態 | 保持している場所 | 解除時の扱い |
| --- | --- | --- |
| プロパティ実体 | `PropertyContainer.m_Properties[path]` | 辞書から外す |
| 型付き購読（`OnChanged`） | **プロパティ実体**が持つ | 実体ごと外れる |
| 型消去チャネルへの接続 | **プロパティ実体**の `m_OnChangedValue` | **論点 6-b** |
| 型消去購読の登録 | `PropertyContainer.m_SubscribePaths`（path の集合） | **論点 6-c** |
| `SubscribeAssignable` の購読本体 | **`PropertyContainer.OnChangedValue`**。実体を参照しない | 影響なし |
| `Subscribe<T>` のハンドル | **プロパティ実体を直接参照** | **論点 6-d** |
| `Property<T>` | **プロパティ実体を直接参照** | **論点 6-d** |
| バインド済みの Binder | **プロパティ実体を直接参照** | 再登録時に貼り直される（下記） |

#### Binder は貼り直される

`Binding.Bind` は `Property.OnNewProperty += BindToBinder` を張っており、`BindToBinder` は
path が一致する Binder に対して `binder.Bind(prop)` を呼ぶ。`NewProperty<T>` は生成時に
`OnNewProperty` を発火する。

したがって **`UnSet` → `Set<別の型>` の順なら Binder は新しいプロパティへ貼り直される。**
`UnSet` だけして放置した場合は、Binder が古い実体を持ったまま最後の値を表示し続ける。

#### 外から見た理想の状態（草案・未決）

内部構造から積み上げると判断がぶれるため、**先に外から観測される状態を決める。**

行は **外から触れる手段**（public なもののみ）。層 2 の `Get<T>` は `internal` なので対象外。
列は `UnSet` 後の状態。

| # | 触る手段 | 層 | A: `UnSet` 直後 | B: `UnSet` → `Set<同じ型>` | C: `UnSet` → `Set<別の型>` |
| --- | --- | --- | --- | --- | --- |
| 1 | `GeneralViewModel.Set<T>` / `Get<T>` | 1 | 未登録と同じ | 通常どおり | 通常どおり |
| 2 | `IViewModel.Set<T>` / `Get<T>` | 1 | 同上（実装は 1 と同じ） | 同上 | 同上 |
| 3 | `PropertyContainer.SubscribeAssignable<T>` | 2 | 通知が来ない | **届く** | **代入可なら届く** |
| 4 | `PropertyContainer.Subscribe<T>` のハンドル | 2 | ★1 | ★2 | ★3 |
| 5 | `Property<T>` のインスタンス | 1 | ★1 | ★2 | ★3 |
| 6 | 結線済みの Binder | View | ★4 | 貼り直されて更新 | 型が合えば貼り直し、合わなければ未結線 |

##### 行 1・2 の「未登録と同じ」の意味

**`Get<T>` は読み取りだが、未登録の path に対しては T で新規登録する。**
`ViewModel.GetImpl<T>` → `PropertyContainer.Get<T>` が「無ければ作る」実装のため。

```csharp
vm.Set("P1", 10);      // int で確定
vm.UnSet("P1");        // 解除
var x = vm.Get<string>("P1");  // ← 読んだだけで string として再登録される。x は null
```

**読み取りが型を確定させる**点は `UnSet` に限らず現在の仕様だが、`UnSet` と組み合わせると
意図せず型が決まる事故が起きやすい。**これを許容するかも決める必要がある**（後述の 4 点目）。

**基本の考え方：`UnSet` した path は「未登録に戻る」。** 以後の挙動は、その path を一度も
使っていない場合と同じにする。これが一番説明が短い。

##### ★1〜★3：型付きハンドルの扱い

`Subscribe<T>` と `Property<T>` は**プロパティ実体を直接参照する**ため、素直に作ると
解除後も孤立した実体を読み書きし続ける。**例外も警告も出ず、動いているように見える。**

| 案 | ★1（解除直後） | ★2（同じ型で再登録） | ★3（別の型で再登録） |
| --- | --- | --- | --- |
| **案 1: 無効化する** | 読みは既定値、書きは no-op。`Log.Error` | 同左（**繋ぎ直さない**。取り直しが必要） | 同左 |
| **案 2: 繋ぎ直す** | 保留。次の登録を待つ | **新しい実体に繋がる** | 型が違うので無効化 |
| 案 3: 何もしない | 孤立した実体を読み書き（**現状**） | 同左 | 同左 |

**案 3 は採らない。** 黙って壊れる。

**案 1 と案 2 の違いは「ハンドルが path を追うか、実体を追うか」。**
案 2 は `Binder` と同じ考え方（path に対して貼り直す）で一貫するが、
`SubscribeHandle<T>` / `Property<T>` が実体参照をやめる設計変更になる。

##### ★4：解除直後の Binder

| 案 | 挙動 |
| --- | --- |
| **案 a: 未結線に戻す** | `Binder.Unbind` を呼ぶ。表示は Binder の実装しだい |
| 案 b: 放置 | 最後の値を表示し続ける（**現状**） |

**案 a が「未登録に戻る」とは整合する**が、`UnSet` → `Set` を連続で呼ぶ通常の使い方では
1 フレーム未満の話であり、実害は小さい。

##### 決めること

1. **★1〜★3 は案 1 か案 2 か**（案 2 は設計変更を伴う）
2. **★4 は案 a か案 b か**
3. `SubscribeAssignable` を「届く」にする前提でよいか（上表の B・C 列）
4. **`Get<T>` が未登録の path を新規登録する挙動を、`UnSet` 後も許すか**

#### 論点 6-a：戻り値

解除できたかを `bool` で返すか、`void` にするか。**争点は無い。**

#### 論点 6-b：古いプロパティの型消去チャネル接続を外すか

解除された実体は、`Subscribe<T>` のハンドルや `Property<T>` からまだ書き込まれうる。
接続を残したままだと、**孤立した実体への書き込みが `PropertyContainer.OnChangedValue` に
流れ込み、`SubscribeAssignable` の購読者に古い値が届く。**

| 案 | 挙動 |
| --- | --- |
| **B-1** | 解除時に `OnChangedValue -= InvokeChangedValueCache` して切る |
| B-2 | 何もしない |

**B-1 を推す。** 孤立した実体からの通知が混ざるのは説明がつかない。

#### 論点 6-c：`m_SubscribePaths` を残すか

この集合は「その path に型消去の購読者が居る」という記録で、`NewProperty<T>` が新しい
プロパティを作る際に接続するかどうかの判断に使う。

| 案 | 挙動 |
| --- | --- |
| **C-1** | 残す。**再登録された別の型の値も `SubscribeAssignable` に届く** |
| C-2 | 消す。再登録しても届かなくなる |

**C-1 を推す。** 購読は path に対して行うものであり、型が変わったから止まるのは直感に反する。

#### 論点 6-d：`Subscribe<T>` / `Property<T>` が孤立する

**これが唯一の実質的な論点。**

両者は**プロパティ実体を直接参照する**（`SubscribeHandle<T>.m_Property` /
`Property<T>.m_Property`）。解除しても参照は生き続け、**孤立した実体を読み書きし続ける。**

- 読み取り：解除前の最後の値を返す
- 書き込み：孤立した実体にだけ反映され、Binder には届かない
- 通知：孤立した実体の `OnChanged` は発火するので、購読者は**動いているように見える**

**黙って壊れる。** 例外も警告も出ない。

`SubscribeAssignable` は `PropertyContainer` に対して購読しているため、この問題は起きない。

| 案 | 挙動 |
| --- | --- |
| **D-1** | 何もしない。`GeneralViewModel` で型付き購読を併用しない前提を文書化する |
| **D-2** | 解除時に、その path に型付き購読が居れば `Log.Warning` を出す |
| **D-3** | ハンドルを無効化する仕組みを入れる。`SubscribeHandle<T>` / `Property<T>` に解除を通知する |

**D-3 は本タスクの規模を超える。** 実体への参照をやめて container 経由にする設計変更になる。

**D-2 を推す。** 検出は安く、黙って壊れる状態は避けられる。ただし
**「型付き購読が居るか」を知るには実体の `OnChanged` に購読者が居るかを見る必要があり、
現在その判定手段は無い。** 追加が要る。

---

## 決める順序

| 順 | 論点 | 層 | 状態 |
| --- | --- | --- | --- |
| — | 論点 1（`Set` の多態） | 1 | **決定: A-1 完全一致のみ** |
| — | 論点 2（`Get` の多態） | 1 | **取り下げ** |
| — | 論点 3（値だけの API） | 2 | **不要** |
| — | 論点 6（型の解除） | 1 | **決定: `GeneralViewModel.UnSet`**。細部は未定 |
| — | 論点 4（`SetObject` の例外） | 3 | **取り下げ。現状維持** |
| 1 | 論点 5（公開範囲） | 2 | 論点 3 が不要になったため**対象は `UnSet` のみ** |

## 完了条件

- 上記 6 論点それぞれについて、採る案と理由を本ファイルに記録すること
- 決定に沿って実装し、テストを追加すること
- [TASK-003](TASK-003-bindingPropertyRedesign.md) の設計節を、実装と一致する内容に直すこと
