# ARAIBA

> 飲食店のお皿洗いを、ドラッグ&ドロップのパズルとして再構成した Unity ゲームプロジェクト。

[![Unity](https://img.shields.io/badge/Unity-6000.3.8f1-black?logo=unity)](#)
[![Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-ff6f00)](#)
[![Input](https://img.shields.io/badge/Input%20System-New%20Input%20System-2c7be5)](#)
[![Async](https://img.shields.io/badge/Async-UniTask-00a3a3)](#)
[![Animation](https://img.shields.io/badge/Animation-DOTween-00a3a3)](#)

## Overview

**ARAIBA** は、食器を食洗機へ効率よく並べる感覚を、グリッド型パズルに落とし込んだ作品です。  
プレイヤーはさまざまな形の食器ピースをトレイに配置し、確定後に洗浄を進めます。

単なるブロック配置ではなく、以下の要素で「皿洗いらしさ」をゲーム化しています。

- 食器ごとに異なる形状ピース
- スロットごとに時間経過で補充される在庫
- トレイの占有率を使った提出フロー
- 食洗機の作動アニメーションと SE

## Features

### 1. Dish Puzzle Loop

- ピースをドラッグしてトレイグリッドへ配置
- 失敗時は元の位置へ戻る直感的な操作
- 半透明ゴーストで配置先をプレビュー
- トレイ確定時に占有率を集計し、盤面をリセット

### 2. Refill System

- ピースはスロット単位でスタック管理
- 各形状に補充間隔を持たせ、時間経過で 1 枚ずつ補充
- 一番上のピースだけを操作可能にすることで、在庫感を演出

### 3. Audio / Animation Feedback

- クリック、配置、キャンセルの SE を分離
- 食洗機の起動音、稼働音、完了音を個別制御
- `AudioManager` によるキー管理型の音声再生

### 4. Touch-Friendly Input

- `Unity Input System` ベース
- マウスとタッチ入力の両方に対応
- `RawImage` + `RenderTexture` を経由した入力座標変換

## Play Flow

1. スロットから一番上の食器ピースをつかむ
2. グリッド上へドラッグして配置する
3. 必要なだけ並べたら UI の確定ボタンを押す
4. 食洗機を操作して洗浄演出を再生する
5. 新しい在庫で次のトレイを作る

## Tech Stack

| Category | Details |
| --- | --- |
| Engine | Unity `6000.3.8f1` |
| Rendering | Universal Render Pipeline `17.3.0` |
| Input | `com.unity.inputsystem` |
| Async | `Cysharp/UniTask` |
| UI | UGUI |
| Tween / Utility | DOTween |

## Project Structure

```text
Assets/
└─ Projects/
   ├─ Data/                  # パズル形状の ScriptableObject
   ├─ Fonts/                 # UI/演出用フォント
   ├─ Prefab/                # PuzzlePiece などのプレハブ
   ├─ Resource/AudioClip/    # SE / 洗浄音
   ├─ Scenes/                # シーン
   ├─ Scripts/
   │  ├─ Audio/              # AudioManager
   │  ├─ BackGround/         # 食洗機の背景演出
   │  ├─ Control/            # 入力制御
   │  ├─ InteractiveObjects/ # DishWasher, PuzzleZone
   │  ├─ Puzzle/             # グリッド、ピース、生成器
   │  └─ UI/                 # 確定ボタンなどの UI
   └─ Sprites/               # 食器・フレーム・背景素材
```

## Key Scripts

| Script | Role |
| --- | --- |
| `PuzzleGrid` | グリッド占有状態の管理、配置判定、ライン消去、占有率計算 |
| `PuzzlePiece` | ドラッグ操作、ゴースト表示、スナップ配置 |
| `PuzzlePieceGenerator` | スロット生成、スタック管理、時間補充、提出後の再生成 |
| `PuzzleGridView` | グリッドの見た目生成と座標変換 |
| `InputManager` | タッチ / マウス入力を `IInputHandler` に中継 |
| `DishWasher` | 洗浄タイマー、音声再生、演出開始・終了 |
| `AudioManager` | AudioClip の登録、再生、停止を一元管理 |

## Included Puzzle Shapes

現在、以下の形状データが `Assets/Projects/Data` に含まれています。

- `1x1`
- `1x5`
- `2x2`
- `3x3`
- `4x4`
- `5x5withHole`

## Getting Started

### Requirements

- Unity Hub
- Unity Editor `6000.3.8f1`

### Setup

1. このリポジトリをクローン
2. Unity Hub でプロジェクトを開く
3. Unity Editor は `6000.3.8f1` を使用
4. 必要に応じて `Assets/Projects/Scenes/Test.unity` を開く

## Notes

- パッケージには `UniTask`、URP、Input System が含まれています
- 音声制御の詳細は `Assets/Projects/Scripts/Audio/doc.md` に記載されています
- `ProjectSettings/EditorBuildSettings.asset` では `Assets/Scenes/SampleScene.unity` が参照されているため、実行時はシーン設定の確認が必要です

## Concept

**ARAIBA** は、単に食器を洗うのではなく、  
「限られたスペースにどう積むか」という現場の判断を遊びに変換することを狙ったプロジェクトです。

パズルとしての気持ちよさと、作業ゲームとしての手触りの両立を目指しています。
