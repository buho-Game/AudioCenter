# AudioCenter Inspector 功能說明

> **這份文件專門整理：AudioCenter 在 Unity Inspector 與編輯器選單裡提供的所有功能**，
> 包含每個元件的欄位、自訂繪製器（PropertyDrawer）的動態行為、自訂 Inspector（Custom Editor）、
> 右鍵選單，以及獨立的編輯器視窗。重點放在「**有哪些**」與「**怎麼用**」。
>
> - 想要 API／架構 → 看 [`文件.md`](文件.md)
> - 完全不會程式的設計師教學 → 看 [`設計師教學.md`](設計師教學.md)
> - 規劃 vs. 實作核對 → 看 [`功能對照表.md`](功能對照表.md)

---

## 目錄

1. [Inspector 功能總覽](#1-inspector-功能總覽)
2. [掛在物件上的元件（Component）](#2-掛在物件上的元件component)
   - [AudioCenterAudioManager](#21-audiocenteraudiomanager)
   - [AudioCenterAudioController](#22-audiocenteraudiocontroller)
   - [AudioCenterAmbientAudio](#23-audiocenterambientaudio)
   - [AudioCenterAudioSettings](#24-audiocenteraudiosettings)
   - [AudioCenterMusicMixer](#25-audiocentermusicmixer)
   - [AudioCenterMusicMixerDemo（右鍵測試選單）](#26-audiocentermusicmixerdemo右鍵測試選單)
   - [AudioCenterAudioSequencer（自訂 Inspector）](#27-audiocenteraudiosequencer自訂-inspector)
3. [AudioCenterAudioAction 動態繪製器](#3-audiocenteraudioaction-動態繪製器)
4. [資產（ScriptableObject）](#4-資產scriptableobject)
   - [AudioCenterAudioSettingsSO](#41-audiocenteraudiosettingsso)
   - [AudioCenterClipLibrary](#42-audiocentercliplibrary)
5. [編輯器視窗：AudioCenter ▸ Audio Library](#5-編輯器視窗audiocenter--audio-library)
6. [音效庫選單（Group／Clip 下拉）共用元件](#6-音效庫選單groupclip-下拉共用元件)

---

## 1. Inspector 功能總覽

AudioCenter 在編輯器端提供四類介面功能：

| 類型 | 內容 | 來源檔案 |
|------|------|----------|
| **標準元件欄位** | 透過 `[SerializeField]` / `public` 欄位呈現在 Inspector 的設定 | `Runtime/**` 各元件 |
| **動態繪製器（PropertyDrawer）** | `AudioCenterAudioAction` 會依選擇的「類型／動作」動態展開不同欄位 | `Editor/AudioCenterAudioActionDrawer.cs` |
| **自訂 Inspector（Custom Editor）** | 序列器的可拖曳清單、步驟下拉新增、播放高亮、Play／Stop 測試鈕 | `Editor/AudioCenterAudioSequencerEditor.cs` |
| **獨立編輯器視窗** | 選單列 `AudioCenter ▸ Audio Library`，集中編輯音效庫 | `Editor/AudioCenterAudioEditorWindow.cs` |

> 所有元件在 **Add Component** 選單中都歸在 **`audioCenter/`** 分類底下（例如 `audioCenter/audioCenterAudioController`）。

---

## 2. 掛在物件上的元件（Component）

### 2.1 AudioCenterAudioManager

> Add Component：`audioCenter/audioCenterAudioManager`
> 場景單例，整個遊戲只要一個（會自動跨場景保留）。

| 欄位 | 類型 | 說明 |
|------|------|------|
| **Library** | AudioCenterClipLibrary | 此遊戲使用的音效庫資產 |
| **Default Settings** | AudioCenterAudioSettingsSO | 預設音量與音源池設定 |

兩欄分別在 **Library** 與 **Settings** 兩個標題（Header）下，把對應資產拖進去即可。

---

### 2.2 AudioCenterAudioController

> Add Component：`audioCenter/audioCenterAudioController`
> 最常用：由 Inspector 設定「要放什麼聲音」，再決定觸發時機。可同時掛在多個物件上。

| 欄位 | 類型 | 說明 |
|------|------|------|
| **Action On Start** | bool | 勾選後在 `Start()` 自動執行一次動作（適合進場 BGM／環境音） |
| **Audio Action** | AudioCenterAudioAction | 音效行為設定，**此欄位由動態繪製器繪製**，內容會隨選擇改變，詳見[第 3 節](#3-audiocenteraudioaction-動態繪製器) |

**觸發方式**

- 勾 **Action On Start** → 進場自動播。
- 不勾 → 用 UnityEvent（按鈕 `On Click`、觸發器、動畫事件…）呼叫元件的 **`DoAction()`**。

---

### 2.3 AudioCenterAmbientAudio

> Add Component：`audioCenter/audioCenterAmbientAudio`
> 在隨機時間間隔內，從清單裡隨機挑一個聲音播放（鳥叫、風聲、滴水…）。

| 欄位 | 類型 | 說明 |
|------|------|------|
| **Audio Pool** | List\<AudioCenterAudioAction\> | 候選聲音清單；每一筆都是完整的 Audio Action（同樣由動態繪製器繪製） |
| **Repeat Interval** | Vector2 | 每次播放的間隔隨機範圍（秒），預設 `(3, 8)`，即 X=最小、Y=最大 |

**控制方法（由 UnityEvent 或程式呼叫）**

| 方法 | 作用 |
|------|------|
| `DoAction()` | 開始循環隨機播放 |
| `StopAction()` | 停止循環 |
| `PlayRandom()` | 手動觸發一次隨機播放 |

---

### 2.4 AudioCenterAudioSettings

> Add Component：`audioCenter/audioCenterAudioSettings`
> 掛在設定面板上，自動讀取目前音量並同步到滑桿；玩家拖動時即時生效。

| 欄位（Header：Sliders） | 類型 | 說明 |
|------|------|------|
| **Music Volume Slider** | UI Slider | 控制 BGM 音量 |
| **Sfx Volume Slider** | UI Slider | 控制 SFX 音量 |
| **Ui Volume Slider** | UI Slider | 控制 UI 音效音量 |

把對應的 UI `Slider` 拖進三個欄位即可，**不需要自己接 `On Value Changed`**。也可由 UnityEvent／程式呼叫 `UpdateMusicVolume(float)`、`UpdateSfxVolume(float)`、`UpdateUIVolume(float)`。

---

### 2.5 AudioCenterMusicMixer

> Add Component：`audioCenter/audioCenterMusicMixer`
> 分層音樂混音器：底層常駐 + 可切換層，所有層對齊同一拍點、無縫切換。

| 欄位 | 標題（Header） | 類型 | 說明 |
|------|------|------|------|
| **Main Bgm** | Main bed | AudioClip | 永遠墊在最底層持續播放的基底音樂 |
| **Bed Volume** | Main bed | float `[0,1]` | 有層次啟用時的基底音量 |
| **Bed Only Volume** | Main bed | float `[0,1]` | 「主頁狀態」（只有基底、無任何層）時的基底音量 |
| **Layers** | Switchable second-music layers | List\<MusicLayer\> | 可切換的第二音樂層清單 |
| **Start Layer** | Switchable second-music layers | int | 開始時讓哪一層發聲（`-1` = 只放基底） |
| **Play On Start** | Behaviour | bool | 進場自動開始 |
| **Crossfade Duration** | Behaviour | float `Min 0` | 切換層次的淡入淡出秒數（0 = 硬切） |
| **Schedule Lead Time** | Behaviour | float `Min 0.02` | 起跑前的預備時間，確保各層一起開始（預設 0.1） |

**Layers 內每一筆 `MusicLayer`：**

| 子欄位 | 類型 | 說明 |
|------|------|------|
| **Name** | string | 自取名字（如 `Combat`、`Boss`），可用 `SwitchTo(string)` 指定 |
| **Clip** | AudioClip | 該層音檔 |
| **Volume** | float `[0,1]` | 該層淡入後的目標音量 |

> ⚠️ 所有層的音檔最好是**同一首曲子拆出的分軌（stems）**，長度與 BPM 一致，否則會在循環邊界逐漸跑拍。

**切換方法（UnityEvent／程式）：** `Play()`、`Stop()`、`SwitchTo(int)`、`SwitchTo(string)`、`Next()`、`Previous()`、`MuteLayers()`、`SwitchToMainPage()`。

---

### 2.6 AudioCenterMusicMixerDemo（右鍵測試選單）

> Add Component：`audioCenter/audioCenterMusicMixerDemo`
> 與 `AudioCenterMusicMixer` 掛在同一物件上，**進入 Play 模式後**，對元件標題**按右鍵**即可從選單測試切換。

| 欄位 | 類型 | 說明 |
|------|------|------|
| **Verbose** | bool | 勾選後每次操作會在 Console 印出狀態（前綴 `[MusicMixer]`） |

**右鍵選單項目（`[ContextMenu]`）：**

| 選單項目 | 作用 |
|----------|------|
| ▶ Play (bed + start layer) | 開始播放（基底 + 起始層） |
| ■ Stop | 停止 |
| Switch ▸ Layer 0 / 1 / 2 | 直接切到第 0／1／2 層 |
| Switch ▸ Next layer | 切到下一層 |
| Switch ▸ Previous layer | 切到上一層 |
| Switch ▸ Main page (bed only, custom volume) | 切到主頁狀態（只留基底） |
| Mute layers (bed only) | 淡出所有層，只留基底 |

> 💡 這是純測試用元件，正式版本可移除，改由遊戲事件呼叫 `AudioCenterMusicMixer` 上的方法。

---

### 2.7 AudioCenterAudioSequencer（自訂 Inspector）

> Add Component：`audioCenter/audioCenterAudioSequencer`
> 由 `AudioCenterAudioSequencerEditor` 提供**完全自訂的 Inspector**，把一連串聲音動作排成「時間表」。

**頂部開關**

| 欄位 | 說明 |
|------|------|
| **Play On Start** | 進場自動開始跑序列 |
| **Loop** | 跑完整張表後從頭再跑 |

**Audio Steps 清單（ReorderableList）**

| 功能 | 怎麼用 |
|------|--------|
| **新增步驟** | 點清單右下角 **`+`**，會跳出**分類下拉選單**（見下），選一種步驟類型即可加入；新步驟預設展開 |
| **刪除步驟** | 選取後點 **`−`** |
| **拖曳排序** | 拖每列**左側的把手欄**（特意與步驟的展開箭頭分開，避免誤觸） |
| **展開／收合** | 點步驟列的**粗體 Foldout 標題**；標題會顯示步驟類型名稱，若填了 `Label` 會接著顯示 `類型 — 你的標籤` |
| **子步驟縮排** | 「不等待」的步驟會自動**縮排**並用左側細線標示，視覺上歸到前一個「等待型」步驟底下，一眼看出哪些會同時觸發 |

**`+` 下拉選單可加入的步驟（依分類）**

| 分類 | 步驟（StepName） |
|------|------|
| 🎵 BGM | `BGM/Play`、`BGM/Stop`、`BGM/Pause`、`BGM/Resume`、`BGM/Fade In`、`BGM/Fade Out` |
| 🔊 Sound | `Sound/Play`、`Sound/Stop`、`Sound/Stop All` |
| 🎚️ Volume | `Volume/Set`、`Volume/Mute`、`Volume/Fade` |
| ⏱️ Flow | `Flow/Wait`、`Flow/Wait For BGM`、`Flow/BGM Duck`、`Flow/BGM Unduck`、`Flow/Event` |

**每個步驟的共通欄位**

| 欄位 | 說明 |
|------|------|
| **Label** | 給步驟取個名字，顯示在清單標題（選填） |
| **Active** | 取消勾選＝序列器跳過這步（除錯好用） |
| **Initial Delay** | 執行這步**之前**先等幾秒 |
| **Wait For Completion** | 勾選＝序列器等這步做完才往下；不勾＝觸發後立刻往下 |

> `Flow/Wait`、`Flow/Wait For BGM` 屬於「自走（SelfPaced）」步驟，本來就會擋住流程，不必再設 Wait For Completion。
> `Flow/Wait For BGM` 另有 **Timeout** 欄位（安全上限秒數，0 = 不設上限）；`Flow/Event` 有一個 **UnityEvent** 欄位可在該時間點觸發外部系統。

**Play 模式專屬功能**

| 功能 | 說明 |
|------|------|
| **執行高亮** | 正在執行的步驟會以綠色底色 + 左側亮綠條標示；「觸發即走」的步驟會短暫發光（約 0.6 秒）後淡出 |
| **Play／Stop 按鈕** | Inspector 底部（僅 Play 模式顯示）可直接試聽整段演出，對應 `PlaySequence()` / `StopSequence()` |

> Inspector 在 Play 模式會持續重繪，讓高亮動畫平順播放與淡出。

---

## 3. AudioCenterAudioAction 動態繪製器

> 來源：`Editor/AudioCenterAudioActionDrawer.cs`（`[CustomPropertyDrawer(typeof(AudioCenterAudioAction))]`）

凡是 Inspector 上出現 `AudioCenterAudioAction` 的地方（`AudioCenterAudioController` 的 Audio Action、`AudioCenterAmbientAudio` 的 Audio Pool），都由這個繪製器呈現。它最大的特色是**欄位會依選擇動態增減**，只顯示當下有意義的設定。整塊欄位有深灰底色框起來，方便辨識。

**第一層：Type（音源類型）**

| Type | 接著顯示 |
|------|----------|
| **BGM** | `Action`（BGM 動作） |
| **Sound** | `Action`（音效動作）＋ `Track`（音軌：SFX／UI） |

**BGM → Action 的動態欄位**

| Action | 展開的欄位 |
|--------|-----------|
| **Play** | `Reference`（File／Library）→ 對應的剪輯欄位 → `Loop` |
| **Resume** | `Fade In`（勾選後再顯示 `Fade Duration`） |
| **FadeIn** / **FadeOut** | `Fade Duration` |
| **Stop** / **Pause** | 無額外欄位 |

**Sound → Action 的動態欄位**

| Action | 展開的欄位 |
|--------|-----------|
| **Play** / **AttachOnBGM** | `Reference` → 剪輯欄位 → `Loop` → `Play Mode` → `Random Pitch`（勾選後再顯示 `Pitch Range`） |
| **Stop** | `Clip`（指定要停止的 AudioClip） |
| **StopAll** | 無額外欄位 |

**Reference（剪輯來源）的差異**

| Reference | 顯示方式 |
|-----------|----------|
| **File** | 一個 `Clip` 欄位，直接拖入 AudioClip（適合臨時測試） |
| **Library** | **Group** 與 **Clip 兩個下拉選單**，自動列出專案中第一個音效庫的群組與剪輯（推薦） |

> 若專案中找不到任何 `AudioCenterClipLibrary` 資產，Library 模式會顯示警告框「No AudioCenterClipLibrary found in project.」。

**Play Mode（AudioCenterPlaySoundMode）**

| 值 | 說明 |
|----|------|
| **ReplayIfExisted** | 已在播放則重新播放（預設） |
| **IgnoreIfExisted** | 已在播放則忽略此次呼叫 |
| **AllowMultiple** | 允許多個實例同時播放 |

---

## 4. 資產（ScriptableObject）

### 4.1 AudioCenterAudioSettingsSO

> 建立方式：Project 視窗右鍵 → **Create → AudioCenter → Audio → Game Settings**
> 每款遊戲建立一份，指派到 `AudioCenterAudioManager` 的 Default Settings。

| 欄位 | 標題（Header） | 預設值 | 說明 |
|------|------|--------|------|
| **Default Master Volume** | Default Volumes | 1.0 | 預設總音量 |
| **Default Music Volume** | Default Volumes | 0.8 | 預設 BGM 音量 |
| **Default Sfx Volume** | Default Volumes | 1.0 | 預設 SFX 音量 |
| **Default UI Volume** | Default Volumes | 1.0 | 預設 UI 音效音量 |
| **Start Muted** | Initial State | false | 啟動時是否全部靜音 |
| **Pool Size** | Sound Pool | 10 | 內建音源池初始大小 |
| **Pool Can Expand** | Sound Pool | true | 音源池是否可動態擴展 |

> 四個音量欄位皆為 `[Range(0,1)]` 滑桿。

---

### 4.2 AudioCenterClipLibrary

> 建立方式：Project 視窗右鍵 → **Create → AudioCenter → Audio → Clip Library**

單一 `.asset` 內嵌所有群組與剪輯。雖然可直接在預設 Inspector 編輯，但**建議改用專屬編輯器視窗**（見下節）來管理，操作較直覺。

資料結構：

```
AudioCenterClipLibrary
 ├─ 群組 BGM
 │   ├─ MainMenu  → MainPage.mp3
 │   └─ ...
 └─ 群組 SFX
     ├─ Click_Normal → Click_Normal.wav
     └─ ...
```

---

## 5. 編輯器視窗：AudioCenter ▸ Audio Library

> 開啟方式：選單列 **AudioCenter → Audio Library**（`Editor/AudioCenterAudioEditorWindow.cs`）

開啟時會自動載入專案中第一個 `AudioCenterClipLibrary`。視窗採**左右分割**版面（中間分隔線可拖曳調整寬度）。

**頂部工具列**

| 元素 | 說明 |
|------|------|
| **Library 欄位** | 切換目前編輯的音效庫資產（物件選擇器） |
| **+ Group 按鈕** | 新增一個群組（預設名 `NewGroup`，自動選取） |

**左側：Groups 群組清單**

- 列出所有群組，每個是一顆按鈕；目前選取的群組以**淺藍底色**標示。
- 點任一群組即切換到右側編輯該群組。

**右側：群組編輯區**

| 元素 | 說明 |
|------|------|
| **Group Name** | 重新命名目前群組 |
| **Delete Group** | 刪除目前群組（紅色按鈕） |
| **Clips 清單** | 可拖曳排序的剪輯清單，每列有 **Clip Name**（左）＋ **Clip**（右，拖入 AudioClip）兩格 |
| **`+`（清單底部）** | 新增剪輯列（預設名 `NewClip`，Clip 空白） |
| **`−`（清單底部）** | 刪除選取的剪輯列 |

> 若尚未有任何音效庫，視窗會提示「No AudioCenterClipLibrary selected. Create one via Create → AudioCenter → Audio → Clip Library.」。

---

## 6. 音效庫選單（Group／Clip 下拉）共用元件

> 來源：`Editor/AudioCenterClipLibrarySelector.cs`

這是一個被多處共用的編輯器繪製器：`AudioCenterAudioAction` 動態繪製器與序列器的剪輯步驟（`BGM/Play`、`Sound/Play` 等）在 **Library 模式**下，都用它畫出 **Group** 與 **Clip** 兩個連動下拉選單。

- 自動快取並沿用專案中第一個 `AudioCenterClipLibrary` 作為來源。
- 選了 Group 後，Clip 下拉只會列出該群組底下的剪輯。
- 找不到音效庫時，顯示警告框提醒你先建立一個。

> 因此只要音效庫內容更新，所有用到 Library 模式的下拉選單會一致同步，不需要逐處手動維護。

---

*AudioCenter Inspector 功能說明 — 涵蓋所有元件欄位、動態繪製器、自訂 Inspector、右鍵選單與編輯器視窗。*
