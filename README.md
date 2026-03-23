# 2D Platformer Game (Unity)

A personal game development project focused on implementing core gameplay systems for a 2D action platformer with Metroidvania-inspired mechanics.

The project explores player mechanics, enemy AI behaviors, combat systems and modular game architecture using Unity.

---

# Engine & Technologies

Engine: Unity 2022.3 (URP)  
Language: C#  
Physics: Rigidbody2D  
Level System: Unity Tilemap  
Architecture: MonoBehaviour + ScriptableObject systems

---

# Core Gameplay Features

## Player Mechanics

- Running and precision platforming
- Jump and wall jump mechanics
- Dash system
- Coyote time
- Jump buffering
- Variable jump height

These mechanics aim to create tight and responsive platforming controls.

---

## Combat System

The player can attack enemies using projectile-based ranged combat.

Implemented features include:

- Fireball projectile system
- Hit feedback (HitFlash)
- Damage interaction with enemies and bosses
- Planned melee combat expansion with combo chains and knockback

---

## Enemy AI

Multiple enemy behavior archetypes were designed:

- Walker enemies with patrol behavior
- Charger enemies with charge-up attacks
- Dash enemies performing quick forward attacks
- Flying enemies with player tracking behavior
- Spawner enemies that summon additional units

---

## Boss System

Boss fights include:

- Multi-phase combat
- Health bar UI integration
- Hit feedback system
- Death animation and battle resolution events

---

## Game Systems

Implemented systems include:

- Checkpoint system (player respawns with saved progress and health)
- Health system
- Environmental interaction
- Hazard systems

---

## Level Architecture

Levels are built using Unity's Tilemap system.

Layers include:

- Ground
- Walls
- Decoration
- One-way platforms
- Hazards

Hidden paths and exploration mechanics are planned as part of the Metroidvania design direction.

---

# Development Goals

This project is used to explore:

- Gameplay programming
- Modular game architecture
- Enemy AI design
- Player movement systems
- Combat mechanics

---

# Future Improvements

Planned features include:

- Melee combat combo system
- Parry mechanics
- Slide movement
- Advanced enemy AI types
- Puzzle systems
- Secret areas and hidden paths
- Dynamic music system

---

# Author

Ege Oztabak

GitHub  
https://github.com/Vatulian

Gameplay video available upon request.
