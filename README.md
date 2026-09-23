# Graveyard Keeper 2 - Craft Max

A small quality-of-life mod for Graveyard Keeper 2 that adds a **MAX** button to the crafting interface.

The button automatically selects the maximum number of crafts possible with the currently available ingredients.

## Features

- Adds a MAX button next to the vanilla craft quantity controls
- Automatically calculates the maximum craftable amount
- Supports recipes with multiple ingredients
- Uses the currently selected ingredient variants
- Preserves the normal Graveyard Keeper 2 crafting and queue system
- Designed to match the vanilla crafting UI

## Requirements

- Graveyard Keeper 2
- BepInEx 5.4.23.5

## Installation

1. Install BepInEx 5.4.23.5.
2. Download the latest release.
3. Extract the archive into your Graveyard Keeper 2 installation directory.

The DLL should end up here:

`BepInEx/plugins/GK2CraftMax/GK2CraftMax.dll`

## Usage

Open a crafting window and press **MAX**.

The selected craft quantity will be set to the maximum amount possible with the currently available ingredients.

## Building

Create a local `Directory.Build.props` based on `Directory.Build.props.example` and set `GameDir` to your Graveyard Keeper 2 installation.

Then run:

`dotnet build -c Release`

## License

MIT