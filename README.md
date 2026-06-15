# AudioCenter

A self-contained Unity audio system built on plain `AudioSource`. No AudioMixer
assets, no third-party dependencies.

- **Unity:** 2022.3+
- **Package name:** `com.audiocenter.audio`

## Features

- **Pooled playback** — BGM / SFX / UI routed through a reusable `AudioSource` pool
- **Clip library** — name-based clip lookup from a single `.asset`, edited in a dedicated window
- **Volume buses** — Master / BGM / SFX / UI volume, mute, and fades; BGM ducking
- **Music mixer** — phase-locked layered music stems with crossfade switching
- **Audio sequencer** — designer-authored step lists (play, fade, wait, events) with no scripting
- **Ambient audio** — random clips at random intervals for environmental beds
- **UI Custom Button** — UGUI button with coroutine-driven hover/click animation and one-call UI SFX, plus a bulk add/remove editor tool with scene scanning
- **Editor tooling** — library window, dynamic action drawer, custom sequencer inspector, custom button tool

## UI Custom Button

`AudioCenterCustomButton` (namespace `AudioCenter.UI`) adds bouncy hover/click scale
animation and plays a UI sound on click via `AudioCenterAudioManager.PlaySound(group, clip, AudioCenterAudioTrack.UI)`.

Animation tunables live in an `AudioCenterButtonAnimationConfig` ScriptableObject so
multiple buttons can share a preset.

> [!IMPORTANT]
> **Create your own config under `Assets/` — do not author it inside the package.**
> Assets created inside `Assets/Plugins/AudioCenter/` will be **overwritten or lost** when
> the package is updated/reinstalled. Create the preset somewhere in your project, e.g.
> `Assets/Settings/`, via **Create → AudioCenter/UI/Button Animation Config**, then assign it
> on the component (or in the tool below). A button with **no** config still works — it falls
> back to built-in default animation values.

**Bulk setup:** open **AudioCenter → Custom Button Tool**, pick a scan scope
(Target GameObject / Active Scene / All Loaded Scenes), assign your config + UI SFX
group/clip, then **Add Custom Buttons**. Add/Remove are undoable.

## Installation

Already included in this project under `Assets/Plugins/AudioCenter`.

For other projects, install via **Window → Package Manager → + → Add package from git URL…**:

```
https://github.com/buho-Game/AudioCenter.git
```

Pin a version with a tag, e.g. `https://github.com/buho-Game/AudioCenter.git#1.1.1`.

## Documentation

- [Documentation.md](Documentation.md) — official reference (English)
- [Documentation.zh-TW.md](Documentation.zh-TW.md) — 官方文件（繁體中文）
- [CHANGELOG.md](CHANGELOG.md) — version history
- [LICENSE.md](LICENSE.md) — license

---

# AudioCenter（繁體中文）

建立在原生 `AudioSource` 之上的獨立 Unity 音訊系統。不需要 AudioMixer 資產，
也沒有任何第三方相依。

## 功能

- **池化播放** — BGM / SFX / UI 透過可重用的 `AudioSource` 池播放
- **音效庫** — 以群組／名稱查找音效，單一 `.asset` 檔，附專屬編輯視窗
- **音量匯流排** — Master / BGM / SFX / UI 的音量、靜音與淡入淡出；BGM 閃避（ducking）
- **音樂混音器** — 相位鎖定的分層音樂，切換時交叉淡化
- **音訊序列器** — 設計師在 Inspector 排列步驟（播放、淡化、等待、事件），不用寫程式
- **環境音** — 隨機間隔播放隨機音效
- **UI 自訂按鈕** — 以協程驅動的滑入／點擊縮放動畫，點擊時一行呼叫播放 UI 音效；附帶可掃描場景的批次新增／移除編輯器工具
- **編輯器工具** — 音效庫視窗、動態 Action 欄位、序列器自訂 Inspector、自訂按鈕工具

## UI 自訂按鈕

`AudioCenterCustomButton`（命名空間 `AudioCenter.UI`）為 UGUI 按鈕加入彈跳式的滑入／點擊
縮放動畫，並在點擊時透過 `AudioCenterAudioManager.PlaySound(group, clip, AudioCenterAudioTrack.UI)`
播放 UI 音效。

動畫參數存放在 `AudioCenterButtonAnimationConfig` ScriptableObject，讓多顆按鈕共用同一組預設。

> [!IMPORTANT]
> **請在 `Assets/` 下建立自己的設定檔，不要建立在套件資料夾內。**
> 建立在 `Assets/Plugins/AudioCenter/` 內的資產，在套件更新／重新安裝時會被**覆蓋或遺失**。
> 請於專案中（例如 `Assets/Settings/`）透過 **Create → AudioCenter/UI/Button Animation Config**
> 建立預設，再指派到元件（或下方工具）上。若按鈕**未**指派設定檔仍可運作，會退回使用內建的
> 預設動畫數值。

**批次設定：** 開啟 **AudioCenter → Custom Button Tool**，選擇掃描範圍
（Target GameObject／Active Scene／All Loaded Scenes），指派設定檔與 UI 音效的
群組／名稱，再按 **Add Custom Buttons**。新增／移除皆支援復原（Undo）。

## 文件

- [Documentation.zh-TW.md](Documentation.zh-TW.md) — 官方文件（繁體中文）
- [Documentation.md](Documentation.md) — official reference (English)
