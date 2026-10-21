# PingPong 🏓

A 3D table tennis game in Unity: you vs. the computer in a sports hall with bleachers, a cheering crowd, barriers and a scoreboard. Everything is built from primitives and procedural textures, so no external assets are needed.

## How to run

1. Clone the repository (e.g. with GitHub Desktop) to a folder such as `Documents\GitHub\PingPong`.
2. In **Unity Hub → Add → Add project from disk**, pick the `PingPong` folder.
   The project was made with Unity 6000.6.7f1 (Unity 2021.3 or newer should also work).
3. When the project opens, click **PingPong → Build Scene** in the top menu.
4. Press **Play ▶**.

The scene is saved to `Assets/PingPong/Scenes/PingPong.unity`, so you only need to build it once.

## Controls

| Key | Action |
|---|---|
| Mouse | move the paddle (left/right, up/down) |
| Left click / Space | serve |
| 1 / 2 / 3 | difficulty: easy / medium / hard |
| R | restart |

The paddle's hit area is bigger than the paddle itself. You can change it on **Player Paddle → Hit Size** in the Inspector.

**Tip:** where you catch the ball on the paddle and how you swing decide where it goes. Catching it near the edge or swinging sideways sends it to the side; swinging upwards sends it deeper.

## Rules (simplified)

- First to 11 points with a 2-point lead wins.
- Serve changes every 2 points, and every point at 10 : 10.
- The ball must land on the opponent's side. You lose the point if it goes into the net or out, or if you don't return it before a second bounce.
- Simplification: the serve goes straight to the opponent's side, and you can only hit the ball after it bounced on your side.

## Structure

| File | What it does |
|---|---|
| `Scripts/PingPongBall.cs` | custom ball physics (gravity, bounces, net, paddle hits) and sounds |
| `Scripts/PingPongPaddle.cs` | player paddle (mouse) and computer paddle (predicts the ball) |
| `Scripts/PingPongGame.cs` | rules, scoring, serving, difficulty, on-screen UI |
| `Scripts/PingPongScoreboard.cs` | scoreboard next to the table |
| `Scripts/PingPongCrowd.cs` | crowd cheers / applause on every point, spectators jump |
| `Audio/*.wav` | crowd sounds (synthesised with `Tools/crowd_sfx.py`, no external samples) |
| `Scripts/PingPongCommon.cs` | table dimensions and input (works with both the old and new Input System) |
| `Editor/PingPongSceneBuilder.cs` | menu command that builds the whole scene (hall, textures, lights) |

Settings like ball speed, difficulty and hitbox size can be changed in the Inspector on the **Game**, **Ball** and **Player Paddle** objects.
