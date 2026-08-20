# Shared Unreal fixture tests

These tests run against the engine-specific data directories next to this folder.
`CUE4PARSE_FIXTURE_ENGINE` selects the fixture and matching `EGame` value:

- unset or `UE5_8`: `Fixtures/UE5_8` with `GAME_UE5_8`
- `UE6_0`: `Fixtures/UE6_0` with `GAME_UE6_0`

Keep semantic assertions shared. Add an engine-specific expectation only when the
cooked representation intentionally differs, and derive nondeterministic generated
package names from that fixture's manifest.
