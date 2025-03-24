# Hotbar scroll control mode for Vintage Story

## Project creation steps (Windows - Powershell)

1. Define `VINTAGE_STORY` environment variable: `$env:VINTAGE_STORY = "$env:APPDATA\Vintagestory"`
1. Install the vsmod nuget package: `dotnet new install VintageStory.Mod.Templates`
1. Execute the command: `dotnet new vsmod --IncludeVSCode --AddSolutionFile --AddSampleCode --AddAssetFolder --output src`
1.