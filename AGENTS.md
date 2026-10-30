# AGENTS.md — AI（ChatGPT / Codex / Claude など）向けのプロジェクト案内

このファイルは、AI がこのリポジトリを読む・編集するときに最初に読むためのものです。
人間向けの説明は [README.md](README.md) にあります。ここには **コードを触るときに知らないと壊す事情** を書いています。

## 1. これは何か

**出席ディフェンダー（Shusseki Defender）** — 授業へ向かう学生を妨害して出席させない、2D タワーディフェンス。
- エンジン：Unity **6000.2.7f2**、URP 2D、新 Input System、TextMeshPro（uGUI）
- 評価が逆転しているのが特徴：**重い処分ほど良い結果**（訓告 < 厳重注意 < 1週間停学 < 無期限停学 < 退学処分）
- ステージは 6 つ（1限目〜6限目）。**6 つとも同じ `Stage_1.unity` シーンを使い、違いは `StageData` アセットの数値だけ**

## 2. ビルド・実行・テスト

- Unity エディタで開いて再生する。コマンドラインのビルドやテストは **無い**（CI なし、テストコードなし）。
- `Assets/Scenes` のシーン（Build Settings の順）：`StartMenu` → `StageSelect` → `Stage_1`（本編） / `Records`（成績） / `Settings`（設定）
- Unity を起動せずにコンパイルだけ確かめたいとき：`Assembly-CSharp.csproj` から Compile 項目だけ差し替えた使い捨ての csproj を作り、`dotnet build` する（`Library/ScriptAssemblies/*.dll` を参照に使う）。`Library/` が無い環境では不可。
- 動作確認は人間が Unity 上で行う。**Unity で動かしていない変更は、そのことを正直に書くこと。**

## 3. フォルダ構成

```
Assets/
  Scenes/            5 つのシーン（上記）
  Scripts/
    system/          ゲーム全体の仕組み（GameManager, Spawner, StageData, UIManager など）
      Skills/        必殺技の実装（PlayerSkill の派生）
      Editor/        エディタ拡張（MovePoint の経路エディタ）
    enemy/           敵（Enemy, EnemyHP, EnemyData, EnemyAbility と Abilities/）
    Weapons/         武器（Weapon, WeaponSettings, WeaponUpgrade, Bullet, WeaponUI）
    Node/            武器を置く台座（Node）
    StartMenu/ StageSelect/ Timer/ Camera/ MusicScript/
  Prefabs/           Enemy, Node, FireWeapon, StrongWeapon, Bullet, StrongBullet, WeaponButton, HPCanvas
  ScriptableObject/
    Stages/          Stage_1〜6.asset（StageData）と StageCatalog.asset
    Enemies/         EnemyData
    Skills/          PlayerSkill のアセット
    Weapon*.asset    WeaponSettings（値段など）
  ImageALL/ Music/ Animetions/ MAP/ Video/ TowerDefenseMaterials/   素材
  TextMesh Pro/, Z2DotGothic16-Regular SDF.asset                    日本語フォント（下記）
ThirdPartyLicenses/  同梱フォントのライセンス
```

`Assets/_Recovery/` は Unity の自動復元フォルダ。**触らない・コミットしない。**
`Library/ Temp/ Logs/ obj/ UserSettings/` は生成物。

## 4. ゲームの仕組み（コードの読み方）

### 全体の流れ
1. `StageSelect` で時限を選ぶ → `StageContext` に選んだ `StageData` が入る
2. `Stage_1` が開く → `GameManager.Awake` が **`StageData` を解決する唯一の場所**（`GameManager.Instance.Stage`）。他は `Start` 以降にそれを読む。
3. カウントダウン（3・2・1・GO）が終わるまで `GameManager.IsGameActive == false`。`Spawner` もここを待つ。
4. 制限時間（`StageData.clearTime`）まで耐える → クリア。ゴールに通した敵が `maxEnemyPasses` に達する → 敗北。
5. 結果でスコアが決まり、`StageData.RankOf(score)` が処分を返す。

### 数値は `StageData`（ScriptableObject）に集める
シーンには配置だけを置き、ルールの数値は `Assets/ScriptableObject/Stages/Stage_N.asset` に置く。
主な項目：`waves`（`WaveData` の波表。`spawnTable` に `EnemyData` と重み・確定数）、`clearTime`、`maxEnemyPasses`、`initialCoin`、`rankThresholds`、`maxWeapons`（0=無制限）、
`levelUpKills` / `initialNodes` / `nodesPerLevel` / `maxNodes`（レベルと使える設置スポット数）、`skills`（使える必殺技）、`selectButtonImage`、`bgm`、`backgroundTint`。

### 主なクラス
| 役割 | クラス |
|---|---|
| 進行・勝敗・スコア | `GameManager`（スコア = 撃破×50 + 所持金×5 + 耐えた秒数×10。係数は Inspector） |
| 敵の湧き | `Spawner`（`MovePoint` を `Spawn_Route` の子から自動収集。`RouteBranching` で途中で別経路へ乗り換え） |
| 敵 | `Enemy`（移動）, `EnemyHP`（HP・撃破イベント）, `EnemyData`（HP/速度/報酬/プレハブ）, `EnemyAbility`（特殊効果。状態は `EnemyContext` に置く） |
| 武器 | `Weapon`（索敵・狙い）, `WeaponSettings`（値段）, `WeaponUpgrade`（強化・売却額）, `Bullet` |
| 設置 | `Node`（台座。`unlockOrder` で解放順）, `WeaponShopManager`（購入・上限・値段上昇）, `UIManager`, `WeaponUI` |
| お金 | `CurrencyManager`（`totalCoins`） |
| レベル | `StageLevel`（撃破数でレベルアップ → 使える Node が増える）, `LevelBar`（上端中央の表示） |
| 必殺技 | `PlayerSkill`（抽象 SO）, `Skills/*`, `PlayerSkillController`, `SkillBar`（左下ボタン） |
| 雨 | `RainController`（右下「雨」ボタン。50円で 8 秒、全員の速さ ×0.5。`Enemy.WeatherSpeedScale`） |
| 時間 | `GameSpeed`（`Time.timeScale` の一元管理。**他から `Time.timeScale` を直接書かない**） |
| 保存 | `StageProgress`（クリア状況とハイスコアを PlayerPrefs へ） |
| UI 部品 | `MenuUI`（コードから uGUI を組み立てる道具箱。LevelBar・RainController・成績/設定画面が使う） |

### ゲームの現行ルール（README の一部は古いことがある）
- 1 つの Node に置ける武器は 1 つ。同じ武器は置くほど値段が上がる。売却額は払った額の半分。
- 武器は 2 種：通常（近く・速い・低ダメージ）と強力（遠く・遅い・一撃で倒すダメージ）。
- 学生を倒すとレベルが上がり、使える Node が増える（ステージごとに `StageData` で調整）。

## 5. 守ってほしい書き方・落とし穴

**ファイル形式**
- `.cs` は **UTF-8 BOM + CRLF**。コメントは日本語で、「なぜそうするか」を書くスタイル。
- `.cs` を新しく作ったら、同じフォルダに `.cs.meta`（新しい guid の `MonoImporter`）も作る。guid は既存と重ならないこと。
- `.gitattributes`：`*.asset` は **git-LFS**。アセットの競合はポインタの競合になる（中身を 3 者マージしてから `git add`）。

**Unity シーン・プレハブ（YAML）を手で編集するとき**
- `fileID` は文書ごとの識別子。削除するときは **その文書を参照している `m_Children` / `m_Father` / `m_Component` / `SceneRoots` も直す**。消した `fileID` がどこにも残っていないか確かめる。
- 数百個ある文書を一括置換しない。意味単位（GameObject 1 つとその Transform・コンポーネント一式）で扱う。
- プレハブの上書きは `PrefabInstance` の `m_Modifications`（`target / propertyPath / value / objectReference`）。
- 手で編集したら、できれば Unity で開いて保存し直すと、コード側の新フィールドがシーンに書き込まれる。

**TextMeshPro の日本語**
- 既定の `LiberationSans SDF` は **英数字だけ**。日本語を出すテキストは `Z2DotGothic16-Regular SDF`（静的アトラス）を指定する。
- 指定を忘れると **□（豆腐）** になる。`TMP Settings.asset` に Z2DotGothic16 を全体のフォールバックとして登録してあるが、各テキストにも明示するのが確実。
- 静的アトラスなので、字形が入っていない漢字は出ない。足りなければフォントアセットを作り直す必要がある。

**イベント**
- `EnemyHP.OnEnemyDead` は `Action<EnemyHP>`、`Enemy.OnReachedGoal` は `Action`（引数なし）。型が違うので **同じメソッドを両方に登録するとコンパイルエラー**（過去に起きた）。
- `static` イベントは購読と解除を `OnEnable/OnDisable`（または `Start/OnDestroy`）で対にする。

**そのほか**
- 敵はオブジェクトプール（`ObjectPooler`）。プールから出し直すたびに状態を戻す必要がある（`OnEnable` で初期化）。
- `Time.timeScale` は `GameSpeed` 経由。倍速（×1.5）を保持するため。
- 画面は `AspectRatioController` で 16:9 に黒帯合わせ。Screen Space Overlay の Canvas は `camera.rect` の影響を受けない。
- Canvas は `ScaleWithScreenSize` 1920×1080 が基本。
- 結果画面のテキストは、パネルの子ではなく Canvas 直下に `Text1〜` として並んでいる（`GameManager` の SerializeField から参照）。

## 6. 作業の進め方（このリポジトリの運用）

- 変更は **小さなブランチ 1 本 = 1 つの PR**。main への直接 push はしない。PR は squash マージ。
- コミットメッセージ・PR 本文は **日本語**。1 行目に要点、本文に「何を・なぜ・何を確かめたか」。
- 見た目や挙動が変わる変更は、数値と理由を PR に書く。確認していないことは「未確認」と書く。
- **やらないこと**：`Assets/_Recovery/` のコミット、`Library/` の編集、他人（人間の作者）が GitHub 上で編集した `README.md` の上書き、素材の著作権・ライセンス表記の勝手な変更。

## 7. ライセンスと素材

- ライセンスの正は [README.md](README.md) のクレジット表と [LICENSE](LICENSE)（MIT）。
- フォント **DotGothic16** は SIL OFL 1.1（[ThirdPartyLicenses/DotGothic16-OFL.txt](ThirdPartyLicenses/DotGothic16-OFL.txt)）。
- 一部の素材（ドット絵・学習チャンネルの素材）の表記は、作者が確認中で README に入れていない。**ライセンスや出どころを推測で書かないこと。** 分からなければ作者に聞く。
- 素材を足すときは、出どころとライセンスを README のクレジット表に足すこと。
