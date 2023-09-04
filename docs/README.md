# Hat Mouse Lacey
Adds a new, familiar face to Stardew Valley.

With this mod, the hat mouse from the far corner of Cindersap Forest is a real
character named Lacey. She moved to the outskirts of Pelican Town just before
you did, and is working on starting her hat business.

![The farmer character is talking to an anthropomorphic mouse named Lacey
outside her house in the forest.](promo.png)

She is single.

It's not required, but I think you'll have the best experience with her if you
start a new save, since she has some Year 1 dialogue.

I also recommend not snooping around in the mod files, since you may find
spoilers in there, and I think it's fun to discover things and be surprised.
Of course, if you like spoilers, be my guest; I can't stop you.

## Requirements
This mod has only two dependencies:

* [SMAPI](https://smapi.io) (the mod framework for Stardew Valley)
* [Content Patcher](https://github.com/Pathoschild/StardewMods/tree/develop/ContentPatcher)

## Installation
Download the desired release `.zip` and unzip it into your Mods folder. It
contains two mod folders:

* `HatMouseLacey`
* `HatMouseLacey_Core`

`HatMouseLacey` is a Content Patcher content pack, which contains all of the
images, map data, and text to be injected into the game. `HatMouseLacey_Core`
is the C# mod which makes all the code changes necessary for Lacey's special
features.

Because the Core mod loads the music I wrote for Lacey's events, that folder
contains the music files.

## Configuration
At this time, Hat Mouse Lacey supports two config settings:

* `DTF`: true/false (default true). If true, enables some suggestive dialogue
(nothing more so than Emily's sleeping bag). Set to false to keep it G-rated.
* `AlwaysAdopt`: true/false (default true). If true, Lacey will always adopt
children when married to the farmer. If false, she can become pregnant with a
male farmer.

**These config settings will be read from the HatMouseLacey_Core mod's
config.json.** The HatMouseLacey mod does not generate a config.json; the Core
mod sets Content Patcher tokens to reflect its config, which the content pack
uses instead of its own config. This way, the two mods share their config
values and won't fall out of sync.

More config settings may be added in future releases.

## Roadmap
* `1.0`: Current release.
* `1.1`: The Compatibility Update. Add support for (and address conflicts with)
other mods.
* `1.2`: The Content Update. Add extra dialogue, seasonal outfits, beach
visits, etc.
* `1.6`: Update the mod to work with Stardew Valley 1.6.

Subject to change, especially if 1.6's release date appears.

## Known Incompatibilities
In the 1.1 release, I will attempt to fix issues with using Lacey alongside
other mods. However, some mods are fundamentally incompatible with this one
and I won't be able to support those.

* [Hat Shop Restoration](https://www.nexusmods.com/stardewvalley/mods/17291)
* Any other mods which alter the hat mouse's shack

## Other Questions You May Have

### I need help with X. Do you have a help file I can check?
I don't have a help file at this time (expect one before 1.1). Until then, you
can open a ticket or find me on
[the Stardew Valley Discord server](https://discord.gg/stardewvalley).

### iS iT cOmPaTiBlE wItH sVe?
1.1 is the planned Compatibility Release. I expect Lacey will be capital-C
Compatible with it (and other Important Mods) then!

That said, I have tried to be conservative with my code changes and content
injections, so I don't think Lacey will *break* much. But you may see some
weird behavior or asset overlaps.

### What about multiplayer?
I haven't tested it yet. Hopefully it mostly works. Whatever doesn't work I'd
like to fix for the 1.1 Compatibility Release.

### kind of a weird decision to make the mouse datable
That's not a question.
