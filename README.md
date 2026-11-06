# FightNight

A fighting game built in Unity on top of [Universal Fighting Engine 2](https://www.ufe3d.com/ufe2/) (UFE).

## Requirements

| | |
|---|---|
| Unity | **2022.3.18f1** (LTS) |
| Render pipeline | Built-in |
| Fighting engine | UFE 2 Standard v2.7.3 (Unity Asset Store) |
| Code editor | VS Code with the [Unity extension](https://marketplace.visualstudio.com/items?itemName=VisualStudioToolsForUnity.vstuc), or any Unity-supported IDE |

## Getting started

1. Clone the repository:
   ```bash
   git clone https://github.com/ErkanAren/FightNight.git
   ```
2. Open **Unity Hub**, click **Add → Add project from disk**, and select the `FightNight` folder.
3. Open it with Unity **2022.3.18f1**. Unity Hub will offer to install that version if you don't have it.
4. The first launch takes a few minutes while Unity rebuilds the `Library/` folder, which isn't stored in git.

If `Assets/UFE/` is missing from your copy, import **UFE 2 Standard** from the Unity Asset Store before opening any scenes (see [UFE license](#ufe-license)).

## Playing the demos

Open any of these scenes and press **Play**:

| Scene | Path |
|---|---|
| 2D Fighter | `Assets/UFE/Demos/_2DFighter/Demo 2D Fighter.unity` |
| 2D Fighter (Shell) | `Assets/UFE/Demos/_2DFighter/Shell_Mode/Demo 2D Fighter (Shell).unity` |
| 2D Fighter (Versus Mode Only) | `Assets/UFE/Demos/_2DFighter/Shell_Mode/Demo 2D Fighter (Versus Mode Only).unity` |
| 3D Fighter (Type 1) | `Assets/UFE/Demos/_3DFighter/Demo 3D Fighter (Type 1).unity` |
| 3D Fighter (Type 2) | `Assets/UFE/Demos/_3DFighter/Demo 3D Fighter (Type 2).unity` |
| 3D Arena | `Assets/UFE/Demos/_3DArena/Demo 3D Arena.unity` |

Each demo is driven by a global config file in its own folder (`Config_*.asset`), for example `Assets/UFE/Demos/_2DFighter/Config_2D_Fighter.asset`.

## Project structure

```
Assets/
  Scenes/        Project scenes
  UFE/
    Engine/      UFE runtime, editor tools and compiled libraries
    Demos/       Demo scenes, characters, stages, sounds and configs
Packages/        Unity package manifest
ProjectSettings/ Unity project settings
```

## Documentation

- [UFE installation guide](https://www.ufe3d.com/ufe2/docs/#/installation), also included in the project as `Assets/UFE/Installation.pdf`
- [UFE docs](https://www.ufe3d.com/ufe2/docs/)
- [UFE video tutorials](https://www.ufe3d.com/ufe2/tutorials.html)

## UFE license

UFE is a paid, third-party package from the Unity Asset Store, covered by the [Asset Store Terms of Service and EULA](https://unity.com/legal/as-terms). It isn't licensed under this repository's terms. Anyone working on this project needs their own UFE license.
