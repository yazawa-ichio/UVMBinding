# TASK-008: SemanticRole アトリビュート

| | |
| --- | --- |
| Phase | 1（メタデータ公開層） |
| 状態 | 未着手 |
| 依存 | TASK-000, TASK-007（基盤の妥当性検証後に着手） |
| 規模 | 中 |

## 目的

各 Binder / Event が「UI として何の役割か」を宣言できるようにする。

## 背景

現状、要素の意味的役割を表す概念が存在しない。Lint のラベル欠落検査（TASK-010）にも、将来の export にも必要になる。

`TextBinder` は表示、`SliderBinder` は値入力、`GraphicColorBinder` は演出、と役割が異なるが、**型から機械的に判定する手段が無い**。

## 設計方針

**仮想プロパティではなくアトリビュートで与える。**

仮想プロパティ（`protected virtual SemanticRole Role => ...`）にすると、`Binder<T>` に役割という概念が入り、Runtime コアが持つ責務が増える。アトリビュートなら以下が成立する。

- ランタイムのフットプリントがゼロ
- 型ごとにキャッシュしてリフレクションで読む（検査・出力時のみ、更新経路には現れない）
- ユーザー定義 Binder / サードパーティ Binder も同じ手段で宣言できる

## 語彙

**ARIA role を基準とする。** 独自語彙を作らない。

対象を Unity 6 に更新したため（[TASK-000](TASK-000-unityVersion.md)）、`UnityEngine.Accessibility.AccessibilityRole`（Unity 2023.2+）を**参照してよい**。`com.unity.modules.accessibility` は既に `manifest.json` に含まれている。

この enum と突き合わせ、以下の三段構えとする。

1. ARIA と Unity の双方にあるもの — そのまま採用
2. ARIA にのみあるもの — ARIA に合わせる
3. どちらにも無いもの — `SemanticRole.None`

**着手前に ARIA role 一覧と `AccessibilityRole` enum の突き合わせを行う。** これが未実施のまま enum を切ると、後で語彙を変更することになる。

## 変更内容

```csharp
namespace UVMBinding
{
    public enum SemanticRole
    {
        None = 0,
        Text, Heading, Image, Button, Link,
        TextInput, Checkbox, Switch, Slider, ComboBox,
        List, ListItem, Group, Dialog,
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class SemanticRoleAttribute : Attribute
    {
        public SemanticRole Role { get; }
        public SemanticRoleAttribute(SemanticRole role) => Role = role;
    }
}
```

### 組み込み Binder への付与

| 分類 | 対象 | Role |
| --- | --- | --- |
| 表示 | `TextBinder`, `TextMeshProBinder` | `Text` |
| 表示 | `ImageSpriteBinder`, `RawImageTextureBinder` | `Image` |
| 入力 | `InputFieldBinder`, `TMP_InputFieldBinder` | `TextInput` |
| 入力 | `ToggleBinder` | `Switch` |
| 入力 | `SliderBinder`, `ScrollbarBinder` | `Slider` |
| 入力 | `DropdownSelectBinder`, `TMP_DropdownSelectBinder` | `ComboBox` |
| 構造 | `CollectionBinder` | `List` |
| 操作 | `EventButton` | `Button` |
| **演出** | `GraphicColorBinder`, `Position3DBinder`, `RotationBinder`, `ScaleBinder`, `AnimatorBoolBinder` 他 | **`None`** |

**演出系は `None` とし、収集対象から除外する。** 操作対象でも表示内容でもないため、検査にも出力にもノイズにしかならない。

## 対象ファイル

- 新規 — `Unity/Assets/UVMBinding/Runtime/Attribute/SemanticRoleAttribute.cs`
- `Unity/Assets/UVMBinding/uGUI/Binders/` 配下
- `Unity/Assets/UVMBinding/TMP/Binders/` 配下
- `Unity/Assets/UVMBinding/Runtime/Binder/Binders/` 配下
- `Unity/Assets/UVMBinding/uGUI/Event/EventButton.cs`

## 完了条件

- ARIA role と `AccessibilityRole` の突き合わせ結果を本ファイルに記録すること
- アトリビュートから役割を取得するヘルパ（型ごとにキャッシュ）のテスト
- 組み込み Binder すべてに付与されていること（未付与は `None` 扱い）
- 演出系が `None` になっていること
- **Runtime の更新経路にリフレクションが混入していないこと**（コードレビューで確認）
- 既存テスト全通過

## 備考

依存を TASK-007 としているのは実装上の依存ではなく順序の指定。TASK-007 で公開 API の実用性が確認できてから、残りの API を作り込む。
