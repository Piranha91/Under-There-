# Under-There-
Synthesis Patcher inspired by Equipable Underwear for Everyone

<h2>What it is:</h2>
Under There! is a Synthesis patcher that will distribute undergarments to NPCs in Skryim. These undergarments are imported from other mods (dependencies) and assigned to NPCs either at random or by rules. They will be worn by NPCs under clothes/armor, and will immediately be visible upon looting. 

<h2>Why would you make this?</h2>
As a VR player, I want realism in my game. I use realistic body textures (randomized via zEBD!) - I want a “Game of Thrones / Witcher” style of immersive fantasy game. What that means is that I don’t want to leave a bunch of naked corpses behind while looting. Some tasteful nudity is great; a trail of naked corpses not so much. Also, I want to be able to show off my cool VR setup without worrying about how people will react to video game nudity. This mod aims to fix these issues by giving NPCs low-value undergarments that I can leave unlooted because I’m an adventurer, not a medieval panty snatcher. 

<h2>How is this mod different from X?</h2>
Underwear in Skyrim seems to be a saturated market, and there are two strategies to implement it without having it clipping through armor and clothes:

<h3>“Just in time” underwear</h3>
In this implementation, the NPCs receive underwear via script after their armor or clothes are looted. Examples of this approach are ws’s new and great Equippable Underwear for NPCs and the older Smalls - (Just In Time Underwear). 

<h4>Advantages</h4>

- Mods can be relatively small - just armor, scripts, and the spells or effects to activate them.

- Easy to install, few or no compatibility patches required.

<h4>Disadvantages</h4>

- Underwear distribution is done via Papyrus, which kicks in after the NPCs’ clothes or armor is removed. Therefore, there is a flash of the body before it gets covered by underwear. 

<h3>“At launch” distribution</h3>

- In this implementation, the NPCs receive underwear by modifying their record in a .esp file. Because such a strategy would require compatibility patching with virtually everything, it is best-performed using a patcher that generates an esp from the user’s load order (SSEedit, zEdit, Synthesis). The previous example of this approach is Equipable Underwear for Everyone, an SSEedit patcher. 

<h4>Advantages</h4>

- NPCs get the item at launch, so they’re already wearing it when you loot them (their worn clothes or armor and also patched so as to not clip with the underwear). That means when you loot them, the underwear appears immediately, with no flash of a naked body.

- (Under There! only): additional items can be imported into the patcher and distributed to NPCs via editing the .json configuration file in a text editor (see the “Adding or Replacing Undergarments” section of the ReadMe or Manual). 

<h4>Disadvantages</h4>

- By nature of how Skyrim armors work, all other armors need to be patched to avoid clipping with the underwear if it is to be worn at the same time as their clothes. Therefore, every time your load order changes, you have to rerun the patcher to patch it accordingly. IMO it’s not that big of a burden, but it’s more cumbersome than just checking or unchecking a .esp file. 

Under There! is a Synthesis patcher heavily inspired by Equipable Underwear for Everyone (in fact, the default settings expect you to have the EUfE installed so that it can make use of its underwear assets. It functions very similarly - you run the patch and it distributes underwear (either the default set or variants) to all humanoid NPCs. The key improvements over EUfE are:

- Leveled NPCs that inherit their inventory from a template get the correct gendered items. Due to the way leveled lists are structured, the patchers can’t know in advance whether a leveled actor will be male or female. Therefore, the EUfE patcher distributes both tops and bottoms to all leveled actors, resulting in male guards, soldiers, bandits, etc having equipped (invisible) tops. While I have nothing against IRL crossdressers, this is not how I envision such characters behaving in the world of Skyrim, and it’s a bit immersion breaking in the loot inventory screen. Under There! makes use of a light script (distributed via powerofthree’s amazing Spell Perk Item Distributor) to fix gendered items, removing tops from male characters while leaving them on the females.

- Under There! does not suffer from weird NPC skipping bugs (that seem to be caused by SSEedit bugs rather than EUfE itself). EUfE will skip over some NPCs for reasons that are not readily apparent. You can test this for yourself by trying to patch Immersive Patrols SE - EUfE will for some reason skip the soldiers while Under There! will patch them correctly.

- Under There! is customizable via a json settings file (described in detail in the Configuration Section of the manual). You can import items from other plugins if you define them in the settings file and distribute them along with or instead of the default EUfE underwear. VR players, fear not the 255 plugin limit - after the patcher imports items, the source plugin(s) can be disabled so you don’t have to spend a precious esp slot just for an underwear mod.

<h2>Requirements</h2>

- Spell Perk Item Distributor: hard requirement - to correctly edit gendered NPC inventory

- Equipable Underwear for Everyone: soft requirement - the default settings expect to import EUfE underwear. You can edit the settings to import underwear from other mods instead, in which case those mods will become dependencies. **However**, after Synthesis is built, **these dependencies can be disabled** so you will not need those extra mods in your load order.

<h2>How to use</h2> 
Install the patcher via Synthesis. Click “Git Repository” in the top left corner next to the + sign. If it is not visible in the Browse patcher list, click “Input” and paste in the patcher’s git repository URL: https://github.com/Piranha91/Under-There-. Wait a few seconds for it to find the patcher, click the bar under “Project” and select UnderThere\UnderThere.csproj. Wait another few seconds and click the checkbox that should turn blue in the bottom right corner. Then return to the main menu, click the circle next to Under-There- so that it turns into a checkbox, and click the Run (arrow) button at the bottom to run the patcher. It should only take a few seconds to complete. 

<h2>Configuration</h2>
The settings for Under There! are defined in the .json file found in Synthesis\Data\Under-There-\settings.json. The settings are editable under the patcher's settings tab. Hovering your cursor over each setting will bring up a tooltip telling you what that setting does.

<h2>Acknowledgements:</h2>

Noggog (Synthesis Discord channel) for enduring an endless amount of my questions as I was learning how to write Synthesis patchers.

EZ (Skyrim Modding Hub Discord channel) for a long discussion about how slots work.

Noah Boddie (Skyrim Modding Hub Discord channel) for Papyrus help.

firehazard (r/SkyrimMods Discord channel) for refactoring and cleaning up my Papyrus script.

SirJesto (r/SkyrimMods Discord channel) for Papyrus help.

wSkeever (r/SkyrimMods Discord channel) for general consultation.

Fuzzles (Synthesis Discord channel) for general encouragement.
