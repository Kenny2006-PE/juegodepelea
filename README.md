# Tanjiro vs Madara — 1v1 Arena Fighter (Unity 2022.3)

Juego de pelea 1v1 en 2.5D desarrollado en **Unity (C#)** con **Tanjiro Kamado** y **Madara Uchiha**, soporte para **1 Jugador vs CPU** (con 3 niveles de dificultad) y **2 Jugadores Local**, animaciones Humanoid de Mixamo con Animator Override Controller, sistema de Hitboxes/Hurtboxes, bloqueo con *chip damage*, derribos con invulnerabilidad y cámara dinámica.

## Características
- **Modos de juego:** `1P vs CPU` (Fácil / Normal / Difícil) y `1P vs 2P (Local)`.
- **Personajes jugables:** Tanjiro Kamado y Madara Uchiha (intercambiables entre P1, P2 y CPU).
- **Combate 2.5D:**
  - `Jab Cross`: golpe ligero (`8` daño, `LightHit` / *hitstun*).
  - `Roundhouse Kick`: ataque fuerte (`18` daño, derribo `Stunned -> Getting Up` con invulnerabilidad y empuje fuerte).
  - Combo encadenable `Jab Cross -> Roundhouse Kick`.
  - `Center Block`: bloqueo frontal que reduce el daño en un **85%** y evita el *hitstun*/derribo.
- **HUD Arcade & Menú de Selección:** Barras de vida con rastro de daño suavizado, temporizador de 99s, rondas al mejor de 3, carteles animados y pantalla de victoria con revancha.
- **Herramienta de automatización en Unity:** Menú `Tools -> Setup Fighting Game` para configurar Rigs/Avatars, Animators, Prefabs, Arena y UI automáticamente.

## Controles

| Acción | Jugador 1 | Jugador 2 (Local) |
| :--- | :--- | :--- |
| **Moverse** | `A` / `D` | `←` / `→` |
| **Bloqueo (`Center Block`)** | `S` | `↓` |
| **Golpe Ligero (`Jab Cross`)** | `F` | `J` o `Numpad 1` |
| **Patada Fuerte (`Roundhouse Kick` - Derribo)** | `G` | `K` o `Numpad 2` |

## Cómo abrir y ejecutar
1. Abrir el proyecto en **Unity 2022.3.22f1** (o compatible 2022.3 LTS).
2. Abrir la escena `Assets/Scenes/FightingArena.unity` (o regenerarla desde `Tools -> Setup Fighting Game -> Full Setup (All Steps)`).
3. Pulsar **Play**.
