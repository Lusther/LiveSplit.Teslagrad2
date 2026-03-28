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

## Reporting issues

If you find broken splits or pointer paths after a game update, please open an issue with:
- The game version
- Which splits are affected
- Any error messages from LiveSplit
