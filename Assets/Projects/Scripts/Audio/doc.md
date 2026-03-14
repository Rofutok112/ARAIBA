# AudioManager クラス仕様書

`AudioManager` は、Unity プロジェクトにおいて音声リソース（AudioClip）の登録、管理、および再生を集中制御するためのシングルトン・マネージャーです。文字列キーによる管理を採用しており、動的な音声の再生や停止を容易に行うことが可能です。

## 1. 概要
* **名前空間**: `Projects.Scripts.Audio`
* **継承**: `MonoBehaviour`
* **デザインパターン**: シングルトン (Singleton)
* **主な機能**:
    * `AudioClip` の動的な登録と解除。
    * キー指定による音声の再生（ループ、音量指定対応）。
    * キーごとの独立した `AudioSource` 管理による同時再生。
    * シーン遷移後も破棄されない自動生成機能（DontDestroyOnLoad）。

---

## 2. 公開メソッド (Static Methods)

### 音声の登録・管理
| メソッド             | 引数                             | 説明                                        |
|:-----------------|:-------------------------------|:------------------------------------------|
| **Register**     | `string key`, `AudioClip clip` | 指定したキーで音声リソースを登録します。                      |
| **Unregister**   | `string key`                   | 登録された音声と、それに関連付けられた `AudioSource` を削除します。 |
| **IsRegistered** | `string key`                   | 指定したキーが既に登録されているか確認します。                   |

### 再生・停止
| メソッド                 | 引数                                                     | 説明                                    |
|:---------------------|:-------------------------------------------------------|:--------------------------------------|
| **Play**             | `string key`, `float volume = 1f`, `bool loop = false` | 指定したキーの音声を再生します。既に再生中の場合は停止してから再開します。 |
| **PlayOneShot**      | `string key`, `float volume = 1f`                      | `Play` と同様に動作しますが、ループなしで再生します。        |
| **Stop()**           | なし                                                     | 全てのキーの音声を停止し、設定をクリアします。               |
| **Stop(string key)** | `string key`                                           | 指定したキーの音声のみを停止します。                    |

---

## 3. 内部仕様と設計の特徴

### 自動インスタンス生成
`EnsureInstance()` メソッドにより、シーン内に `AudioManager` が存在しない状態でメソッドが呼ばれた場合、実行時に自動的に `GameObject` を生成し、`DontDestroyOnLoad` を設定します。これにより、事前にヒエラルキーへ配置する手間を省き、どこからでも即座に利用可能です。

### 動的な AudioSource 生成
音声再生（`Play` / `PlayOneShot`）が要求されるたびに、そのキー専用の `AudioSource` コンポーネントが内部で生成・保持されます。これにより、BGMを流しながら複数のSEを重ねて鳴らすといった制御が容易です。

### 安全設計
* **バリデーション**: キーの空チェックや、`AudioClip` の `null` チェックを行い、不正な操作には `Debug.LogWarning` で通知します。
* **シングルトン保護**: `Awake` 時の重複チェックにより、シーン内に複数のインスタンスが存在しないよう制御されています。

---

## 4. 使用例

```csharp
using Projects.Scripts.Audio;
using UnityEngine;

public class AudioExample : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip seClip;

    void Start()
    {
        // 1. 音声の登録
        AudioManager.Register("MainBGM", bgmClip);
        AudioManager.Register("ClickSE", seClip);

        // 2. BGMをループ再生 (音量 0.5)
        AudioManager.Play("MainBGM", 0.5f, true);
    }

    public void OnClickButton()
    {
        // 3. SEを単発再生
        AudioManager.PlayOneShot("ClickSE");
    }

    void OnDestroy()
    {
        // 4. 特定の音声を停止
        AudioManager.Stop("MainBGM");
    }
}
```
## 5. 注意点
* `AudioManager` はシングルトンであるため、複数のインスタンスが存在しないように注意してください。シーン内に手動で配置する場合は、既に存在するインスタンスがないことを確認してください。
* 音声リソースの登録は、再生前に行う必要があります。登録されていないキーで再生を試みると、警告が表示されます。