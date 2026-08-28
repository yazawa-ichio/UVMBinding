# TASK-004: IBinderInfo の追加

| | |
| --- | --- |
| Phase | 1（メタデータ公開層） |
| 状態 | 未着手 |
| 依存 | なし |
| 規模 | 中 |

## 目的

Binder が保持する `Path`・バインド型・現在値を、Runtime アセンブリの外から読み取れるようにする。

## 背景

Lint も export も、まず「この要素はどのパスにバインドされているか」を知る必要があるが、**現状では外部から取得できない**。

- `IBinder` は `internal interface` であり、`Path` に到達できない
- `Binder<T>.Path` は public だが、`T` を知らずに `Binder<T>` へキャストできない
- `View.GetElements()` が返す `IViewElement` は `CanUse(IView)` しか持たない
- 現在値を読む `Binder<T>.Get()` は `protected`

`InternalsVisibleTo` は `UVMBinding.Editor` と `UVMBinding.Tests` のみ。

## 設計方針

**`InternalsVisibleTo` の拡張では解決しない。** ユーザーが自分のゲーム側アセンブリで定義した Binder も読み取り対象になるため、公開 API が必要。

**実プロジェクトでの実測により裏付けられている。** 利用側では独自の Binder / Event / Converter / EventArgument が合計 50 以上定義されており、いずれもパッケージ外のアセンブリにある。`InternalsVisibleTo` では到達できない。

結線用の `internal IBinder` はそのまま残し、**読み取り専用の型を別に切る**。両者を混ぜると、内部実装の変更が公開 API の破壊につながる。

## 変更内容

```csharp
namespace UVMBinding.Core
{
    /// <summary>読み取り専用のバインド情報。検査・出力ツールが使用する。</summary>
    public interface IBinderInfo
    {
        string Path { get; }
        System.Type BindType { get; }

        /// <summary>現在値を取得する。ボックス化するため更新経路では使用しない。</summary>
        object GetBoundValue();
    }
}
```

`Binder<T>` に**明示的インターフェース実装**として追加する。

```csharp
public abstract class Binder<T> : MonoBehaviour, IBinder, IBinderInfo
{
    string IBinderInfo.Path => m_Path;
    System.Type IBinderInfo.BindType => typeof(T);
    object IBinderInfo.GetBoundValue() => m_Property?.GetObject();
}
```

明示的実装とする理由は 2 つ。

- `Binder<T>` の派生クラスの API 表面を汚さない
- `GetBoundValue()` がボックス化するため、**誤って `TryUpdate()` の経路から呼ばれることを防ぐ**

`IViewEvent` は既に public で `Name` / `EventType()` を持つため、これで Binder 側と対称になる。

## 対象ファイル

- `Unity/Assets/UVMBinding/Runtime/Binder/IBinder.cs` — `IBinderInfo` の追加
- `Unity/Assets/UVMBinding/Runtime/Binder/Binder.cs` — 実装

## 完了条件

- `IViewElement` から `IBinderInfo` へキャストして `Path` が取得できるテスト
- バインド済みの Binder から `GetBoundValue()` で現在値が取得できるテスト
- 未バインド時に `GetBoundValue()` が例外を投げずに `null` を返すこと
- `TryUpdate()` の経路に `GetBoundValue()` が現れないこと（コードレビューで確認）
- 既存テスト全通過

## 備考

**破壊的変更は許容される方針**のため（2026-08-28 決定）、公開後に API を変更・削除してよい。「一度出したら消せない」前提での過度な慎重さは不要。

ただし**使う先が無い API を先に生やさない**方針は維持する。追加するメンバは本タスクの 3 つに留め、必要が生じてから増やす。理由は後方互換ではなく、未使用コードを抱えないため。

`SemanticRole`（TASK-008）や表示テキスト（TASK-009）をここに足したくなるが、それぞれのタスクで手段を決めてから判断する。
