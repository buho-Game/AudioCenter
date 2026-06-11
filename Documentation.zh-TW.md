# AudioCenter — 官方文件

建立在原生 `AudioSource` 之上的獨立 Unity 音訊系統：池化播放、音效庫、音量匯流排、
分層音樂混音器、音訊序列器、環境音與編輯器工具。所有型別都在 `AudioCenter`
命名空間下，所有元件都在 **Add Component → AudioCenter** 選單中。

English version: [Documentation.md](Documentation.md).

## 快速開始

1. **建立資產**（在 Project 視窗按右鍵）：
   - **Create → AudioCenter → Audio → Clip Library** — 存放具名音效
   - **Create → AudioCenter → Audio → Game Settings** — 預設音量與音源池設定
2. **填入音效庫**：開啟主選單 **AudioCenter → Audio Library**，新增群組
   （例如 `BGM`、`SFX`、`UI`），並為每個音效登錄一個名稱。
3. **放置管理器**：在啟動場景的 GameObject 上加入 `AudioCenterAudioManager`，
   並指定 **Library** 與 **Default Settings** 資產。管理器會跨場景存活。
   （靜態 API 在找不到管理器時會自動建立一個，但自動建立的沒有指定音效庫——
   請務必在場景中手動放置。）
4. **播放音訊** — 用程式：

```csharp
using AudioCenter;

AudioCenterAudioManager.PlayBGM("BGM", "MainMenu");          // 從音效庫查找
AudioCenterAudioManager.PlaySound("SFX", "Click");           // 單發音效
AudioCenterAudioManager.FadeOutBgm(2f);                      // BGM 淡出
```

   — 或不寫程式：在任何 GameObject 上加 `AudioCenterAudioController`，在
   Inspector 設定 **Audio Action**，再從 Button 的 `OnClick` 呼叫
   `DoAction()`，或勾選 **Action On Start**。

## 核心概念

### 音軌與匯流排

每個聲音都在三條音軌之一播放 — **BGM**、**SFX**、**UI**
（`AudioCenterAudioTrack`）。音量與靜音以匯流排（`AudioCenterVolumeBus`）控制：
**Master**、**BGM**、**SFX**、**UI**。一條音軌的最終輸出是
「匯流排音量 × Master 音量」，並一併套用靜音、BGM 閃避與 BGM 淡化。

### 音效參照：Library 與 File

任何參照音效的地方（Audio Action、序列器步驟）都可選擇
`AudioCenterClipReferenceType`：

- **Library**（預設）— 以「群組名稱 + 音效名稱」在管理器的
  `AudioCenterClipLibrary` 中查找。建議使用：改名或換檔只需改音效庫，
  不用動到場景。
- **File** — 直接拖入 `AudioClip`。

### 池化播放

管理器透過內部的 `AudioSource` 池播放所有聲音。池的大小與是否可擴充在
`AudioCenterAudioSettingsSO` 資產上設定（**Pool Size** 預設 10；
**Pool Can Expand** 預設開啟）。

## 程式 API — `AudioCenterAudioManager`

所有方法皆為靜態，假設已 `using AudioCenter;`。

### BGM

```csharp
PlayBGM(string groupName, string clipName, bool loop = true);
PlayBGM(AudioClip clip, bool loop = true);
StopBGM();
PauseBGM();
ResumeBGM(bool fadeIn = true, float fadeInDuration = 1f);
FadeInBgm(float duration = 1f);      // 從靜音淡入
FadeOutBgm(float duration = 1f);     // 淡出至靜音
SetBgmDuckMultiplier(float m);       // 例如 0.3f：旁白時壓低 BGM
ClearBgmDuckMultiplier();
bool  IsBgmPlaying();
float GetBgmTime();                  // 目前播放位置（秒）
float GetBgmRemainingTime();         // 循環或未播放時為 0
```

播放新 BGM 會自動停止前一首。淡化與閃避是疊在音樂音量之上的乘數，
不會覆寫玩家的音量設定。

### SFX / UI 音效

```csharp
AudioSource PlaySound(string groupName, string clipName,
    bool loop = false,
    AudioCenterPlaySoundMode playMode = AudioCenterPlaySoundMode.ReplayIfExisted,
    bool rndPitch = false, Vector2 rndRange = default);

AudioSource PlaySound(string groupName, string clipName, AudioCenterAudioTrack track,
    bool loop = false, AudioCenterPlaySoundMode playMode = ..., bool rndPitch = false, Vector2 rndRange = default);

AudioSource PlaySound(AudioClip clip, ...);                              // 相同的選填參數
AudioSource PlaySound(AudioClip clip, AudioCenterAudioTrack track, ...); // 相同的選填參數

StopSound(AudioClip clip);
StopAllSound();
```

- `AudioCenterPlaySoundMode` — 同一個音效已在播放時的處理方式：
  `ReplayIfExisted`（重新播放）、`IgnoreIfExisted`（保留原本的）、
  `AllowMultiple`（允許重疊）。
- `rndPitch` / `rndRange` — 每次播放隨機音高，公式為 `1f + Random.Range(x, y)`，
  例如 `new Vector2(-0.3f, 0.3f)` 可讓重複音效更自然。
- 預設音軌為 `SFX`；UI 音效請傳 `AudioCenterAudioTrack.UI`。

### 音量、靜音、淡化

```csharp
float GetBusVolume(AudioCenterVolumeBus bus);
SetBusVolume(AudioCenterVolumeBus bus, float volume);        // 立即生效；會取消該匯流排進行中的淡化
SetBusMute(AudioCenterVolumeBus bus, bool mute);
FadeVolume(AudioCenterVolumeBus bus, float target, float duration);   // 使用 unscaled time
FadeMasterVolume(float target, float duration);
FadeTrackVolume(AudioCenterAudioTrack track, float target, float duration);
CancelAllFades();                                            // 停止所有淡化，保留目前音量
float GetTrackOutputVolume(AudioCenterAudioTrack track);     // 實例方法；含靜音/閃避/淡化的有效輸出

// 設定介面常用的簡便方法（自動 0..1 範圍限制）：
UpdateMasterVolume(float v);  UpdateMusicVolume(float v);
UpdateSfxVolume(float v);     UpdateUIVolume(float v);

// 唯讀屬性（實例）：MasterVolume、MusicVolume、SfxVolume、UIVolume
```

### 設定的存讀

```csharp
AudioCenterAudioData data = AudioCenterAudioManager.GetAudioSettings();
// 把 data 序列化進存檔；讀檔後：
AudioCenterAudioManager.SetAudioSettings(data);
```

`AudioCenterAudioData` 包含 `masterVolume`、`musicVolume`、`sfxVolume`、
`uiVolume`、`isMuteBgm`、`isMuteSound`、`isMuteMaster`。

### Action

```csharp
AudioCenterAudioManager.DoAction(AudioCenterAudioAction action);
```

`AudioCenterAudioAction` 是 `AudioCenterAudioController` 與
`AudioCenterAmbientAudio` 使用的可序列化描述：來源類型（BGM/Sound）、動作
（BGM：播放/停止/暫停/續播/淡入/淡出；Sound：播放/停止/全部停止/AttachOnBGM）、
音效參照、音軌、循環、播放模式與隨機音高。**AttachOnBGM** 會從 BGM 目前的
播放位置開始播放音效，讓對齊現有曲目製作的分軌保持節拍同步。

## 元件

### AudioCenterAudioController — 單一物件的觸發器

掛在任何 GameObject 上，在 Inspector 設定一個 **Audio Action**。

| 欄位 | 說明 |
|---|---|
| Action On Start | `Start()` 時自動執行 |
| Audio Action | 序列化的動作（動態欄位，見「編輯器工具」） |

方法：`DoAction()` 執行設定好的動作；`DoAction(AudioCenterAudioAction)` 執行任意
動作。兩者都相容 UnityEvent — 把 Button 的 `OnClick` 接到 `DoAction()`
就能做按鈕音效。

### AudioCenterAmbientAudio — 隨機環境音

以隨機間隔從池中隨機播放（鳥鳴、風聲、水滴等）。

| 欄位 | 說明 |
|---|---|
| Audio Pool | 候選的 `AudioCenterAudioAction` 清單 |
| Repeat Interval | 兩次播放間的最小/最大秒數（預設 3–8） |

方法：`DoAction()` 開始循環、`StopAction()` 停止、`PlayRandom()` 立刻隨機播放
一次。此元件不會自動開始。

### AudioCenterAudioSettings — 音量滑桿綁定

掛在設定面板上，指定 **Music / Sfx / Ui Volume Slider**。`Start()` 時會把滑桿
同步成目前設定值，並自動訂閱 `onValueChanged` — 不需手動接事件。
`UpdateMusicVolume/UpdateSfxVolume/UpdateUIVolume(float)` 也是 public，
可供 UnityEvent 使用。

### AudioCenterMusicMixer — 分層（垂直）音樂

一條 **Main Bgm** 基底（bed）持續循環，可切換的 **Layers** 疊在上面。所有音源
排程到同一個 DSP 起始時間並循環，因此維持取樣精度的相位鎖定 — 切換圖層只
交叉淡化音量，時間軸永不重啟。混音器自行播放音源（不經過池），但會以管理器
的 BGM 輸出縮放音量，所以仍遵守玩家的音量設定。

| 欄位 | 說明 |
|---|---|
| Main Bgm | 永遠播放的基底音樂 |
| Bed Volume | 有圖層啟用時的基底音量 |
| Bed Only Volume | 「主頁」狀態（無圖層）時的基底音量 |
| Layers | `{ name, clip, volume }` 分軌清單 |
| Start Layer | `Play()` 時啟用的圖層（−1 = 只有基底） |
| Play On Start | 自動開始（預設開啟） |
| Crossfade Duration | 切換時的交叉淡化秒數（0 = 直接切） |
| Schedule Lead Time | DSP 排程的提前量（預設 0.1 秒） |

```csharp
Play();  Stop();
SwitchTo(int index);            // -1 = 只有基底
SwitchTo(int index, bool instant);
SwitchTo(string layerName);
Next();  Previous();
MuteLayers();                   // 淡出所有圖層，保留基底
SwitchToMainPage();             // 只有基底，使用 Bed Only Volume
// 屬性：IsPlaying、ActiveLayer、LayerCount
```

> **限制：** 所有音檔必須是同一段循環的分軌 — 與基底長度、速度完全相同。
> 長度不同的音檔會在循環邊界逐漸漂移。

`AudioCenterMusicMixerDemo`（需與混音器同物件）提供右鍵測試選單 — Play 模式下
在元件標題列按右鍵即可播放/停止與切換圖層，Console 會輸出 `[MusicMixer]`
開頭的狀態記錄（以 **Verbose** 開關）。

### AudioCenterAudioSequencer — 步驟式音訊編排

排好一份步驟清單，呼叫 `PlaySequence()` 整段播放 — 不需要寫程式。

| 欄位 | 說明 |
|---|---|
| Play On Start | `Start()` 時自動播放 |
| Loop | 播完後從頭再來 |
| Steps | 步驟清單（自訂 Inspector，見「編輯器工具」） |

步驟類型（**+** 下拉選單分組）：

- **BGM** — Play、Stop、Pause、Resume、Fade In、Fade Out
- **Sound** — Play、Stop、Stop All
- **Volume** — Set、Mute、Fade（指定匯流排）
- **Flow** — Wait、Wait For BGM（等目前非循環 BGM 播完，可設逾時）、
  BGM Duck、BGM Unduck、Event（觸發 `UnityEvent`）

每個步驟都有：**Label**（顯示名稱）、**Active**（取消勾選即跳過）、
**Initial Delay**（執行前等待秒數）、**Wait For Completion**（等此步驟的時長
— 例如淡化 — 結束後才執行下一步；關閉時為射後不理）。

```csharp
PlaySequence();  StopSequence();
// 屬性：IsPlaying、CurrentStepIndex（閒置時為 -1）
float TimeSinceStep(int index);   // 此輪未執行過則為 -1
```

`StopSequence()` 停止序列，但不會還原已發出的呼叫（播放中的音效、進行中的
淡化）— 需要的話請加上明確的 Stop/Fade 步驟。

## 資產

### AudioCenterClipLibrary

**Create → AudioCenter → Audio → Clip Library。** 單一 `.asset` 內含具名群組，
每個群組存放 `{ clipName, clip }`：

```
ClipLibrary
├─ "BGM":  "MainMenu" → MainPage.mp3, "Battle" → Battle.mp3, …
├─ "SFX":  "Click" → Click.wav, …
└─ "UI":   …
```

執行期存取：`library["SFX"]` → `AudioCenterClipGroup`；`group["Click"]` →
`AudioCenterClipAsset`（`clipName`、`clip`）；另有 `GroupCount`、`Count`、
`FindIndex(name)` 與 `int` 索引子。

### AudioCenterAudioSettingsSO

**Create → AudioCenter → Audio → Game Settings。** 管理器在 `Awake` 時讀取一次：

| 欄位 | 預設 | 說明 |
|---|---|---|
| Default Master/Music/Sfx/UI Volume | 1 / 0.8 / 1 / 1 | 初始匯流排音量 |
| Start Muted | 關 | 啟動時全部靜音 |
| Pool Size | 10 | 預先配置的 `AudioSource` 數量 |
| Pool Can Expand | 開 | 用完時自動擴充 |

## 編輯器工具

- **Audio Library 視窗**（**AudioCenter → Audio Library**）— 雙欄編輯器：
  左側為群組清單，右側為所選群組的音效。工具列可切換要編輯的音效庫資產；
  **+ Group** 新增群組；群組面板可改名、刪除與管理音效列（支援多選刪除）。
- **Audio Action 欄位** — `AudioCenterAudioAction` 在 Inspector 中逐層展開：
  選 BGM 或 Sound → 選動作 → 選 File 或 Library 參照 → Library 會顯示
  Group/Clip 下拉選單（自動讀取專案中的音效庫資產）。只顯示相關欄位。
- **序列器 Inspector** — 可拖曳排序的步驟清單、依分類分組的 **+** 下拉選單、
  逐步驟展開/收合。Play 模式下執行中的步驟會以綠色高亮，底部出現
  Play/Stop 按鈕。

## 常見問題

**沒有聲音。** 依序檢查：場景中有 `AudioCenterAudioManager` 且已指定
**Library** 資產；群組/音效名稱與音效庫完全一致（區分大小寫）；沒有匯流排
被靜音且音量大於 0；使用 Library 參照時，音效庫中該欄位確實放了音檔。

**怎麼讓聲音循環？** `PlaySound`/`PlayBGM` 傳入 `loop: true`，或在動作/步驟上
勾選 **Loop**。循環音效用 `StopSound(clip)` 或 Sound/Stop 步驟停止。

**重複的音效聽起來很死板。** 開啟 **Rnd Pitch** 並設定範圍如 (−0.3, 0.3)，
讓每次播放略有變化。

**該用 Library 還是 File 參照？** 用 Library — 它是預設值，且音效指定集中在
單一資產。File 只適合不需要進音效庫的一次性音檔。

**音樂圖層越來越不同步。** 確認每個圖層音檔的長度與速度都與 Main Bgm 基底
完全相同；它們必須是同一段循環的分軌。
