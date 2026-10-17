# LiveSplit.Teslagrad2

A [LiveSplit](https://livesplit.org/) component autosplitter for [Teslagrad 2](https://store.steampowered.com/app/1698220/Teslagrad_2/) speedruns.

## Features

- Auto-start on new file creation
- Auto-reset on save slot deletion
- Automatic splitting based on in-game events
- Configurable split conditions via the component settings
- Position splits: split when Lumina enters a square area (center X/Y and radius)
- Optional information lines in the layout: position, current scene, game version. Ticking them in the autosplitter settings adds Text components at the bottom of the layout, which can then be moved and styled like any other component

## LiveSplit variables

The same information is available as LiveSplit variables, to format it yourself with a Text component (Use Variable):

| Variable | Example |
|---|---|
| `Teslagrad2_Position` | `-121.47, -174.31` |
| `Teslagrad2_Scene` | `Lumina Landed` |
| `Teslagrad2_Version` | `public (build 23225880)` |
| `Teslagrad2_Build` | `23225880` |

## Installation

1. Download the latest release
2. Place the component DLL in your LiveSplit `Components` folder
3. In LiveSplit, right-click > Edit Layout > Add the **Teslagrad 2** component

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## Questions

Join the [official Discord server](https://discord.gg/KUKzJKCyCA) or open an issue.

## License

[MIT](LICENSE)
