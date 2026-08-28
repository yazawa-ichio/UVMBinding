# TASK-013: ViewModel 単位の version ゲート

| | |
| --- | --- |
| Phase | 3（コア品質・並行トラック） |
| 状態 | 未着手 |
| 依存 | **TASK-003** |
| 規模 | 中 |

## 目的

変更のない ViewModel にバインドされた View の毎フレーム走査を省略する。あわせて `Binding` 周辺の走査コストを削減する。

## 背景

`Canvas.preWillRenderCanvases` → 全 `View` → **全 `Binder.TryUpdate()`** が毎フレーム走る。

```csharp
// Binder.cs:19
bool IBinder.IsActive => this != null;   // 破棄チェックのみ
```

`IsActive` が Unity の破棄チェックしか行わないため、**非アクティブな UI ツリーの Binder も走査対象になる**。総 Binder 数に比例した固定コストが常に乗る。

## 設計の前提

**ポーリング方式そのものは維持する。** 書き込み（`SetImpl`）が「辞書引き → 等値比較 → `Hash++`」で完結する軽さは、ViewModel を計算の一部として使うための設計意図であり、push 型に移行するとこれが壊れる（History/analysis-bindingDesignReview.md 参照）。

本タスクは**畳み込みの性質を維持したまま**、走査コストのみを削減する。

## 変更内容

### 1. version カウンタ

`PropertyContainer` に version カウンタを持たせ、`Hash++` するところで一緒にインクリメントする。
`Binding` は最後に見た version を保持し、一致していれば `TryBinderUpdate()` を丸ごと飛ばす。

書き込み側の増分は int のインクリメント 1 つ。読み側は N 個の走査が 1 回の比較になる。

### 2. bool ではなく version にすること

`ViewModel.m_BindingCount` の通り、**1 つの ViewModel に複数の View がバインドできる**。

bool フラグにすると最初の View がフラグを消し、2 つ目が更新を取りこぼす。`Binding` 側が「自分が最後に見た値」を持つ version 方式であれば N:1 で正しく動く。

### 3. `TryRebind()` は別枠

パス変更は View 側の事象であり、ViewModel の version では捕まらない。`IsRebind` は `m_ChangePath` で短絡するとはいえ O(N) 走査が残る。

`Binder.ChangePath()` で静的カウンタを 1 つ回し、`Binding` がそれを見て変化がなければ `TryRebind` ごと飛ばす。パス変更は稀なため、**どこかで 1 回起きた日に全 Binding が 1 回スキャンする**程度の粗さで問題ない。

### 4. 第二の PropertyContainer

`ViewModelPropertyExtension` が独自の `PropertyContainer` を持つ。`Binding` には ViewModel 側と Extension 側の両方からプロパティが流れるため、**version は両方を見る必要がある**。

### 5. activeInHierarchy

`View.IsActive` に `activeInHierarchy` を加える。

```csharp
// View.cs:27（現状）
public bool IsActive => m_IsActive && this != null && gameObject != null;
```

非表示 UI が走査から丸ごと外れる。Binder は `m_Hash` を保持しており、再表示時にハッシュ不一致で変更を拾い直すため**取りこぼしは起きない**。

### 6. BindToBinder の Dictionary 化

```csharp
// Binding.cs:186
if (binder.Path == prop.Path && binder.IsActive)
```

新しいプロパティごとに全 Binder を線形走査し、文字列比較している。`Bind()` 時は O(プロパティ数 × Binder 数)。

path → Binder リストの `Dictionary` を `Binding` が保持する形に変更する。毎フレームのコストではないため緊急度は低いが、**本タスクで同じファイルを触るため同時に行う**。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/ViewModel/Property/PropertyContainer.cs`
- `Unity/Assets/UVMBinding/Runtime/View/Binding.cs`
- `Unity/Assets/UVMBinding/Runtime/View/View.cs`
- `Unity/Assets/UVMBinding/Runtime/Binder/Binder.cs`
- `Unity/Assets/UVMBinding/Runtime/PropertyProvider/ViewModelPropertyExtension.cs`

## 完了条件

- 値を変更しないフレームで `TryBinderUpdate()` が呼ばれないテスト
- **同一 ViewModel に 2 つの View をバインドし、両方が更新を受け取るテスト**（bool 実装だと落ちる）
- `ViewModelPropertyExtension` 経由の派生値変更が反映されるテスト
- 非アクティブな View を再アクティブ化した際に、非アクティブ中の変更が反映されるテスト
- `ChangePath()` 後に再バインドが行われるテスト
- `BindToBinder` の Dictionary 化前後で結線結果が一致すること
- 既存テスト全通過
- **走査コストの削減を実測して記録すること**

## 備考

**[TASK-003](TASK-003-bindingPropertyRedesign.md)（BindingProperty 再設計）の完了後に着手する。** 同じ `PropertyContainer` に手を入れるため、逆順だと version カウンタを再設計後に入れ直すことになる。

再設計が済んでいれば Phase 3 の中では最も実装が軽く、効果も測りやすい。
