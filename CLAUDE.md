# ManyProjects — Unity Multi-Game Replica Project

## Project Structure

Each game lives under `Assets/Replicas/<GameName>/` with its own scene, scripts, resources, and assembly definition. Shared code goes in `Assets/Replicas/Common/`. Each game is a standalone mobile puzzle game replica.

Namespace convention: `ReplicaProjects.<GameName>` (e.g. `ReplicaProjects.Arrows`, `ReplicaProjects.MagicSort`). Shared code uses `ReplicaProjects.Common`.

## Hard Rules

- **Never modify `.unity`, `.prefab`, `.asset`, or `.meta` files.** Only touch C# scripts.
- **Logic must be pure C#.** Game logic classes (e.g. `BoardLogic`, `MagicSortLogic`) must not inherit MonoBehaviour and must not use Unity APIs beyond math types (`Vector2Int`, `Mathf`). This keeps them testable.
- **MonoBehaviour is for presentation only.** Rendering, animation, input detection, and Unity lifecycle belong in presentation classes.
- **Turkish comments are acceptable.** The codebase uses Turkish for inline comments and doc comments.

## Architecture Patterns

### Orchestrator Pattern
Each game uses orchestrators that own the data arrays and coordinate between logic and presentation:
- Logic classes are stateless algorithms that receive input structs and return result structs (DTO pattern)
- Orchestrators hold mutable game state and call logic methods
- `GameManager` (MonoBehaviour) wires orchestrators to presentation and handles Unity lifecycle

### Layering (bottom to top)
1. **DTO structs** — plain data carriers for inputs/outputs (`BoardCornerEvaluateInput`, `TapResult`, etc.)
2. **Logic** — pure functions, unit-testable, no side effects (`BoardLogic`, `MagicSortLogic`)
3. **Orchestrator** — owns game state arrays, calls logic, fires events (`BoardOrchestrator`, `MagicSortOrchestrator`)
4. **Presentation** — MonoBehaviours that visualize state (`BoardPresentation`, `PresentationCore`)
5. **GameManager** — top-level MonoBehaviour, creates and wires everything in `Awake()`

### Communication
- Use C# events (`event System.Action`) for upward communication (child to parent)
- Direct method calls for downward communication (parent to child)
- Subscribe in `Initialize()`, unsubscribe in `DeInitialize()`

### Asset Loading
Each game has a static `<GameName>ReplicaAssetDatabase` class that loads prefabs and assets via `Resources.Load<T>()`. All loadable assets live under each game's `Resources/` folder.

### Theme System (when applicable)
Themes implement an interface (e.g. `IMagicSortTheme`) and are swappable at runtime. The theme handles its own input detection and visual representation; the core presentation layer is theme-agnostic.

## Testing
- Tests go in `<GameName>/Tests/Editor/` using NUnit
- Test only logic classes — they are pure C# and don't need Unity runtime
- Assembly definitions: `ReplicaProjects.<GameName>.Tests`

## Design Skills

### Separation of Concerns
Before writing anything, ask: "Is this one responsibility or two?" If a class handles both decision-making and visual feedback, split it. The MagicSort Presentation layer is the reference — input detection belongs to the theme, visual orchestration belongs to core, and they meet at an interface.

### Testability-First Design
When adding game logic, design it as pure C# that takes an input struct and returns a result struct. If a NUnit test can't be written for it without Unity running, the design is wrong. Push Unity dependencies upward into presentation/orchestrator layers.

### Interface-Driven Extensibility
When something is swappable (themes, input methods, visual styles), define an interface. Don't bake concrete types into the core. `IMagicSortTheme` is the model — the core never knows which theme is active, it only talks through the contract.

### Layer Placement Discipline
Every new piece of code must have a clear answer to "which layer does this belong to?" (DTO / Logic / Orchestrator / Presentation / GameManager). If it touches both logic and visuals, it needs to be split across layers, not jammed into one class.

### Data-Oriented Thinking
Prefer flat arrays and structs over deep object graphs. Board state as `int[]` rather than `Cell[]` with methods. Keep data dumb, logic separate. Struct-of-arrays over array-of-structs when the data is iterated per-field.

### Reusability Radar
Before writing game-specific code, check: could this live in `Common/`? `EndScreenPresentation` already does this. If two games need similar functionality, extract it to `Assets/Replicas/Common/`.

## Conventions
- `Initialize()` / `DeInitialize()` instead of relying on Unity's `OnEnable`/`OnDisable` for setup/teardown
- Fields: `_camelCase` with underscore prefix for private fields
- Flat data arrays (struct-of-arrays) preferred over object arrays for game board state
- `Application.targetFrameRate = 60` set in each game's `GameManager.Awake()`
- `new()` for plain C# classes, `Instantiate()` only for prefabs
