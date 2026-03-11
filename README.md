# Collection Exporter (HDT Plugin)

Plugin to export your hearthstone collection to `.json` file.

## Install

Copy `.dll` from [releases](https://github.com/qustrolabe/hdt-collection-exporter/releases) into your [Hearthstone Deck Tracker](https://github.com/HearthSim/Hearthstone-Deck-Tracker) `plugin` directory. Launch HDT, open Settings->Tracker->Plugins and enable this plugin. 

## Usage

1. Launch game and wait in main menu
2. Launch HDT
3. In HDT menu on top go to **PLUGINS** > Collection Exporter
4. Either pick custom path to export to or default one

## Schema

Export looks like:

```
{"collection": {"7": [1,0,0,0],"8": [2,2,0,0],
```

- `"7"` stands for card's DBF ID
- `[1,0,0,0]` as far as I understand means number of cards in your collection of each quality like: `[normal, golden, diamond, signature]`

Similar schema fetched from HSReplay private API when you go to hsreplay.net/collection/mine](https://hsreplay.net/collection/mine/).

## Dev build

1. Create `Directory.Build.props.user` and set your local HDT paths:

```xml
<?xml version="1.0" encoding="utf-8"?>
<Project>
  <PropertyGroup>
    <HDT_DIR>C:\Users\User\AppData\Local\HearthstoneDeckTracker\app-1.49.15</HDT_DIR>
    <!-- Optional if HearthDb.dll is in the same folder as the exe -->
    <HDT_LIB_DIR>C:\Users\User\AppData\Local\HearthstoneDeckTracker\app-1.49.15</HDT_LIB_DIR>
  </PropertyGroup>
</Project>
```

2. Build:

```
dotnet build .\CollectionExportPlugin.csproj -c Release
```
