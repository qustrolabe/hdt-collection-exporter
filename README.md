# Collection Exporter (HDT Plugin)

Exports your Hearthstone collection to JSON in the HSReplay format. Use the plugin button or the Plugins menu to export, open the output folder, or choose a custom folder.

## Build

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
