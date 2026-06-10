# AudioCenter

A self-contained Unity audio system built on plain `AudioSource`. It provides pooled
SFX playback, a clip library, an interactive music mixer, ambient audio layers, an
audio sequencer, and supporting editor tooling. No third-party dependencies.

- **Unity:** 2022.3+
- **Package name:** `com.audiocenter.audio`

## Installation

### Via Package Manager (git URL)

1. Open **Window → Package Manager**.
2. Click **+ → Add package from git URL…**
3. Enter:

```
https://github.com/buho-Game/AudioCenter.git
```

Pin a version by appending a tag, e.g. `#1.0.0`:

```
https://github.com/buho-Game/AudioCenter.git#1.0.0
```

### Via manifest.json

Add the dependency directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.audiocenter.audio": "https://github.com/buho-Game/AudioCenter.git#1.0.0"
  }
}
```

## Documentation

See the bundled guides:

- `文件.md` — system documentation
- `設計師教學.md` — designer tutorial
- `功能對照表.md` — feature reference table

## License

See [LICENSE.md](LICENSE.md).
