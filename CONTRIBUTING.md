# Contributing

Thanks for your interest in improving the LiveSplit.Teslagrad2 component!

## How to contribute

1. Fork this repository
2. Create a branch following the naming convention: `type/branch-name` (e.g. `feat/add-scroll-split`, `fix/pointer-path`)
3. Make your changes
4. Test your changes in LiveSplit (see below)
5. Open a Pull Request

## Conventions

### Branches

Use the format `type/branch-name`, where type is one of: `feat`, `fix`, `refactor`, `docs`, `chore`.

### Commits

Use the format `type: description` with no body. The description must start with a verb. Examples:
- `feat: add split for new scroll`
- `fix: update pointer path for v1.2`
- `refactor: extract helper for memory scanning`

### Pull Requests

- All changes go through a PR — direct commits to `main` are not allowed
- PRs are merged via **squash merge** only
- PR title follows the same format as commits (`type: title`) and will be used as the squash commit message
- PR description should clearly describe the changes
- Each PR should be atomic — one logical change per PR

## Testing

1. Open LiveSplit
2. Right-click > Edit Layout > Add the Teslagrad 2 component (or point it to your local build)
3. Launch Teslagrad 2 and verify your changes work correctly
4. Test both new game and existing save scenarios

## Supporting a new game build

Game builds are identified by their Steam build id and listed in `Teslagrad2Constants.Builds`. Older builds are kept so runs on previous versions keep working.

1. Install the build through Steam, then dump it with [Il2CppDumper](https://github.com/Perfare/Il2CppDumper):
   ```
   Il2CppDumper.exe "<game>\GameAssembly.dll" "<game>\Teslagrad 2_Data\il2cpp_data\Metadata\global-metadata.dat" .\dump
   ```
2. Preview the entry (also checks the field offsets used in `Teslagrad2Memory.cs` against `dump.cs`):
   ```
   .\tools\Update-Offsets.ps1 -DumpDir .\dump -GameDir "<game>"
   ```
3. Run it again with `-Apply` to add the build to `Teslagrad2Constants.cs`, then test in game.

## Reporting issues

If you find broken splits or pointer paths after a game update, please open an issue with:
- The game version (Steam build id, shown in the `Teslagrad2_Build` LiveSplit variable)
- Which splits are affected
- Any error messages from LiveSplit
