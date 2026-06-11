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
- **Editor tooling** — library window, dynamic action drawer, custom sequencer inspector

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
- **編輯器工具** — 音效庫視窗、動態 Action 欄位、序列器自訂 Inspector

## 文件

- [Documentation.zh-TW.md](Documentation.zh-TW.md) — 官方文件（繁體中文）
- [Documentation.md](Documentation.md) — official reference (English)
