# Changelog

All notable changes to **Graveyard Keeper 2 - Craft Max** are documented in this file.

## [0.4.0]

### 0.4.0

#### Added
- Study Table Science decomposition MAX button.
- Decompose all available selected items with one action.
- Full controller support for Science MAX.
- Controller navigation between the selected Science item and MAX.

#### Changed
- Refactored shared MAX button creation.
- Refactored shared gamepad navigation code.
- Existing Craft and Fuel MAX behavior remains unchanged.

## [0.3.0]

### Added
- Added gamepad/controller support for the MAX button.
- MAX can now be reached using controller navigation.
- Added controller support for both normal crafting and fuel crafting interfaces.
- Pressing the controller confirm button while MAX is focused now:
- Calculates and selects the maximum craftable amount.
- Immediately starts crafting or placing the selected amount.

### Improved
- Improved controller navigation handling across different crafting UI layouts.
- MAX now integrates with the active vanilla navigation group instead of relying on the `+` button navigation group.
- Preserved vanilla controller behavior when MAX is not focused.
- Mouse and controller input continue to use the same MAX calculation logic.

### Fixed
- Fixed MAX being unreachable with a controller in some crafting interfaces.
- Fixed navigation differences between normal crafting and fuel crafting windows.
- Fixed controller confirmation on MAX triggering the vanilla Craft/Place action without applying MAX first.

---

## [0.2.0]

### Added
- Added MAX support for fuel crafting.
- Added support for fuel-based crafting interfaces such as Firewood Sheds.
- Added automatic detection of the fuel container capacity.
- Added calculation of the remaining available fuel capacity.

### Improved
- MAX now respects both:
  - Available crafting materials.
  - Remaining fuel container capacity.
- Fuel limits are calculated dynamically from the game's data instead of using hardcoded capacity values.
- MAX only selects complete crafts that fit into the remaining fuel capacity.

### Fixed
- Prevented materials from being wasted by selecting crafts that would exceed the fuel container capacity.
- Improved handling of fuel crafting quantities.

### Example
With a fuel container at `290 / 500` and a recipe producing `20` fuel per craft:

- Remaining capacity: `210`
- Maximum complete crafts: `10`
- Fuel added: `200`
- Final amount: `490 / 500`

The remaining `10` capacity is intentionally left unused because another complete craft would exceed the container capacity.

---

## [0.1.0]

### Added
- Initial release.
- Added a MAX button to the crafting interface.
- MAX automatically calculates the maximum number of crafts possible with the currently available ingredients.
- Supports recipes with multiple ingredient requirements.
- Uses the currently selected ingredient variant when calculating the maximum amount.
- Integrates with the existing Graveyard Keeper 2 crafting interface.

### Behavior
Pressing MAX selects the highest craft quantity that can be produced with the currently available materials.

The mod uses the game's normal crafting flow after the quantity has been selected.