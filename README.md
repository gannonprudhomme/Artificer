# Artificer

This is a Risk of Rain 2 clone I created for my first major game dev, with the sole goal of learning various aspects of game development I had never done before, including: shaders, visual effects/particle systems,
3D modeling and animation, and texturing (Photoshop + Substance Designer).

It contains:
- 3 enemies: Lesser Wisp, Stone Golem & Lemurian
- 4 spells for the playable character, the Artificer.
- ~2 levels, Titanic Plains & Distant Roost
- 7 items: Ukulele, AtG Missile, Hopoo Feather, Energy Drink, Backup Magazine, Gasoline, Tri-Tip Dagger
- Interatables spawning: chests, barrels, multi-shop terminal
- Difficulty scaling over time

Of technical note is my sparse voxel octree (SVO) implementation, which I use in order to efficiently represent 3D space for pathfinding in 3D for flying entities. (lesser wisp & AtG Missile's projectile)
It can generate a SVO from a mesh with 2 million vertices and generating 500k nodes in ~1.5 seconds, which can then be loaded from memory at runtime, the generation of which is parallelized using Unity's Jobs + Burst compiler.
You can read more details about it [here](Assets/Scripts/3D%20Navigation/Octree.md)

# Videos

![video1](Videos/Artificer-Clip-1.mp4)

![video2](Videos/Artificer-Clip-2.mp4)
