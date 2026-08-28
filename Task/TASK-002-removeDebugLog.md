# TASK-002: PropertyContainer のデバッグ出力除去

| | |
| --- | --- |
| Phase | 0（バグ修正） |
| 状態 | **完了**（2026-08-28） |
| 依存 | なし |
| 規模 | 小 |

## 目的

`SubscribeAssignable` に残っているデバッグ用の `UnityEngine.Debug.Log` を除去する。

## 背景

`PropertyContainer.SubscribeAssignable` に 3 箇所の `UnityEngine.Debug.Log` が残っている。

- `PropertyContainer.cs:61` — `"SubscribeAssignable" + m_Properties.Count`
- `PropertyContainer.cs:65` — `"SubscribeAssignable" + path`
- `PropertyContainer.cs:80` — `"InvokeChangedValue" + path`

パッケージ全体は `UVMBinding.Logger.Log`（`Log.Trace` / `Log.Debug`）で統一されており、`LogLevel` による制御が効く。この 3 箇所だけが制御外で、購読のたびに無条件に出力される。文字列連結によるアロケーションも発生する。

## 変更内容

3 箇所を削除する。情報として残す価値があるものは `Log.Trace` に置き換える。

同メソッド冒頭に既に `Log.Debug("Property SubscribeAssignable<{0}> Path:{1}", ...)` があるため、`:61` と `:65` は重複しており削除でよい。`:80` はループ内で毎回出力されており、残すなら `Log.Trace` とする。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/ViewModel/Property/PropertyContainer.cs`

## 完了条件

- `UnityEngine.Debug.Log` の呼び出しが Runtime アセンブリから消えていること
- 既存テスト全通過

## 備考

TASK-003 で同じメソッドを変更するため、先に片付けるか同時に行う。
