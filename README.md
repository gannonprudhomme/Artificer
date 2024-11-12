# Artificer

This is a Risk of Rain 2 clone I created as my first game dev project, with the sole goal of learning various aspects of game development I had never done before, including: shaders, visual effects/particle systems,
3D modeling and animation, and texturing (Photoshop + Substance Designer).

It contains:
- 3 enemies: Lesser Wisp, Stone Golem & Lemurian
- 4 spells for the playable character, the Artificer.
- ~2 levels, Titanic Plains & Distant Roost
- 7 items: Ukulele, AtG Missile, Hopoo Feather, Energy Drink, Backup Magazine, Gasoline, Tri-Tip Dagger
- Interatables spawning: chests, barrels, multi-shop terminal
- Difficulty scaling over time

Of technical note is my sparse voxel octree (SVO) implementation, which I use in order to efficiently represent 3D space for pathfinding in 3D for flying entities. (lesser wisp & AtG Missile's projectile)
It can generate a SVO from a mesh with 2 million vertices in ~1.5 seconds, resulting in ~500k nodes, all of which is parallelized using Unity Jobs + the Burst compiler.
This can then be saved to disk & loaded at runtime.

You can read more details about it [here](Assets/Scripts/3D%20Navigation/Octree.md)

# Videos

https://github.com/user-attachments/assets/7331325e-1077-46d8-9993-1a29f2e76ee3

https://github.com/user-attachments/assets/ba9ce341-5179-43a8-8d1a-e412c6123e2a
