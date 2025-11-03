# UdonSharp Port

This directory contains a self-contained `LifeGameController` script that mirrors
Python gameplay logic from the original project in a form that can run inside a
VRChat world through [UdonSharp](https://github.com/MerlinVR/UdonSharp).

## Overview

* `LifeGameController.cs` recreates the major managers from the Python version
  (property, talent, age, and event systems) using Unity serialisable classes so
  the data can be authored directly in the Inspector or via ScriptableObjects.
* Conditions use explicit structs instead of the Python `eval`-based helper.
  Each condition can check property thresholds, talent ownership, or event
  history.  Complex boolean expressions from the JSON files can be broken down
  into combinations of the provided `all`/`any` arrays.
* Property effects expose dedicated fields (`chr`, `intel`, `str`, `money`,
  `spirit`, `life`, `age`, `total`, and `randomBonus`) that map to the dynamic
  dictionary in the Python version.

The C# code stays within the feature set supported by UdonSharp (no reflection,
no dynamic code generation, arrays instead of Python `set`s) while preserving
important game flow concepts: drawing talents, allocating starting stats,
progressing through yearly events, and handling chained events.

## How to use inside Unity/VRChat

1. Import the UdonSharp package into your Unity project (2019.4.x) and enable
   UdonSharp scripting.
2. Create an empty GameObject in your scene and add the `LifeGameController`
   behaviour.
3. Populate the inspector arrays:
   * **Talent Definitions** – list every talent, its metadata, property effect,
     exclusivity list, and triggering condition.
   * **Age Definitions** – list events available at each age and the talents
     that should be granted automatically at that age.
   * **Event Definitions** – fill in narrative text, property effects, optional
     chained branches, and include/exclude conditions.
4. Optionally set `autoStart` to immediately run the simulation during play
   mode, or call `BeginLife()` from another Udon behaviour.
5. Hook the `lastLog` string into a UI Text component or other display to show
   the generated life story.

## Adapting data

The JSON data supplied with the Python project relies on dynamic expressions in
`Utils.parseCondition`. When moving the content into Unity you will need to
translate each expression into one or more `ConditionDefinition` entries.  A few
examples:

| Original expression | UdonSharp configuration |
| ------------------- | ----------------------- |
| `AGE>=18` | Single condition with mode `PropertyGreaterOrEqual`, property `AGE`, threshold `18`. |
| `TLT?[1001,1002]` | Mode `HasTalent` with `idList = [1001, 1002]`. |
| `EVT?[2001]|EVT?[2002]` | Two entries inside the `any` array (each with mode `HasEvent`). |

This explicit representation keeps the runtime deterministic and friendly to the
Udon compiler.

## Limitations

* `System.Random` is used for repeatable randomness. UdonSharp supports it in
  bytecode mode, but if you target IL2CPP you may need to replace it with
  `UnityEngine.Random` plus manual seeding.
* The current implementation assumes there are at most 32 active talents and 256
  triggered events. Adjust `TalentRuntimeState.MaxTalents` or
  `EventRuntimeState.MaxEvents` if your content exceeds those limits.
* Console I/O from the Python CLI is replaced by inspector-driven interaction.
  You can expose additional methods (for example, to select talents via UI) by
  expanding `ChooseTalents` and `AllocateInitialProperties`.

By following the structure in this directory you can iteratively migrate the
Python logic into UdonSharp scripts without relying on unsupported dynamic
features.
