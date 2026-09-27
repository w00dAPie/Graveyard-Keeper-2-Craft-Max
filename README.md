# Graveyard Keeper 2 - Craft Max

A small quality-of-life mod for Graveyard Keeper 2 that adds a **MAX** button to the crafting interface.

The button automatically selects the maximum number of crafts possible with the currently available ingredients.

## Features

- Adds a MAX button to supported crafting interfaces.
- Automatically calculates the maximum immediately craftable amount for the MAX action.
- Adds controller navigation for the MAX button.
- Adds LB/RB shortcuts for changing crafting quantities by 10.
- Allows larger crafting queues even when the required

## Controller Support

Craft Max includes full controller support.

- Navigate to the MAX button using the normal crafting controls.
- Press the normal confirm button to use MAX.
- LB decreases the crafting quantity by 10.
- RB increases the crafting quantity by 10.
- Hold LB/RB for repeated changes:
  - repeat starts after 0.5 seconds
  - quantity changes every 0.2 seconds
- Craft quantities can be increased beyond the amount currently supported by available ingredients.
  The game will process available crafts normally and keep the remaining amount in the queue.

### Item Transfers

The same fast quantity controls are available when moving items between inventories or containers:

- LB: -10
- RB: +10
- Hold LB/RB for repeated changes.

The normal game controls remain available.

## Requirements

- Graveyard Keeper 2
- BepInEx 5.4.23.5

## Installation

1. Install BepInEx 5.4.23.5.
2. Extract the archive into your Graveyard Keeper 2 installation directory.

The DLL should end up here:

`BepInEx/plugins/GK2CraftMax/GK2CraftMax.dll`

## Usage

Open a crafting window and press **MAX**.

The selected craft quantity will be set to the maximum amount possible with the currently available ingredients.

With a controller, focus the crafted result and press **LB** to subtract 10 or **RB** to add 10. Each press changes the quantity once; increases respect available ingredients and remaining fuel capacity. This works in normal, fuel, and single-craft windows (including the Kiln and Compost Pile). Existing fuel queues are edited immediately, matching the vanilla quantity controls. Study Table Science decomposition has no quantity selector and keeps its existing MAX behavior.

## Building

Create a local `Directory.Build.props` based on `Directory.Build.props.example` and set `GameDir` to your Graveyard Keeper 2 installation.

Then run:

```powershell
dotnet build -c Release
