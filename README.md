# CUE4Parse

C# Parser for Unreal Engine packages & assets

## Usage

#### FileProvider
<details>
<summary>DefaultFileProvider</summary>

This file provider lets you load packages locally from a given directory.
```csharp
var provider = new DefaultFileProvider(gameDirectory, SearchOption.TopDirectoryOnly, new VersionContainer(EGame.GAME_UE5_1)); // Make sure the UE version matches with the game.
provider.Initialize();
```
</details>

<details>
<summary>StreamedFileProvider</summary>

This file provider lets you load packages from their stream and gives you more control over what one you want to load.
```csharp
var provider = new StreamedFileProvider(gameName); // gameName is not useful for most cases
provider.Initialize(fileName, new []{fileStream}); // foreach file you wanna load
// the 'fileStream' array must contains both .utoc AND .ucas streams in case you're loading an IO Store Package
```
</details>

#### Mounting

The next step is mounting files you loaded, you can do so by using the `SubmitKey` method. You must provide a [GUID](https://en.wikipedia.org/wiki/Universally_unique_identifier) of one of the packages and its working aes key.
```csharp
Provider.SubmitKey(guid, aesKey);
```

#### Localization

Depending on the game, assets can be loaded using different languages (usually English by default). In order to load them in another language, use the following code. Keep in mind that languages hardcoded in CUE4Parse are a small amount of languages used by the most popular games, you might have to add your own language in the code to support the game you're loading.
```csharp
Provider.LoadLocalization(language);
```

#### Extract

<details>
<summary>All Exports</summary>

To get a json string of all exports included in the asset
```csharp
var exports = Provider.LoadObjectExports(fullPath);
var json = JsonConvert.SerializeObject(exports, Formatting.Indented);
```
</details>

<details>
<summary>One Export</summary>

To get a json string of one export included in the asset
```csharp
var export = Provider.LoadObject(fullPathWithExportName); // FortniteGame/Content/Athena/Items/Cosmetics/Backpacks/BID_718_ProgressiveJonesy.FortCosmeticCharacterPartVariant_0
var json = JsonConvert.SerializeObject(export, Formatting.Indented);
```
</details>

#### Export

An asset usually has its data split between multiple other assets. In order to grab them all, use `TrySavePackage` which will out a `IReadOnlyDictionary<string, byte[]>` for you to loop through.
```csharp
if (Provider.TrySavePackage(fullPath, out var assets))
{
    foreach (var kvp in assets)
    {
        File.WriteAllBytes(Path.Combine(directory, kvp.Key), kvp.Value);
    }
}
```
## Requirements

* Windows
    * [.Net SDK](https://dotnet.microsoft.com/download)
    * C++ Compiler
    * [CMake](https://cmake.org/)
    * [Windows SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/)
* Linux
    * [.Net SDK](https://dotnet.microsoft.com/download)
    * C++ Compiler
    * [CMake](https://cmake.org/)
* MacOS
    * [.Net SDK](https://dotnet.microsoft.com/download)
    * C++ Compiler
    * [CMake](https://cmake.org/)

## Building CUE4Parse

1. Clone repository and initialize submodules

    ```
    git clone https://github.com/FabianFG/CUE4Parse
    cd CUE4Parse
    git submodule update --init --recursive
    ```
2. Build 
    ``` 
    dotnet build
    ```

## Contributing

Contributions are always welcome in order to maintain the project!
## Authors

- [@Fabian](https://github.com/FabianFG)
- [@Asval](https://github.com/4sval)
- [@amr](https://github.com/Amrsatrio)
- [@GMatrix](https://github.com/GMatrixGames)
