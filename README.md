# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Singleton

> Quick overview: Generic MonoBehaviour singletons for Unity, including a runtime auto-creating singleton, a persistent (DontDestroyOnLoad) variant, a regulator that keeps the newest initialized instance, and an editor/runtime "always available" global singleton.

Singleton components are provided to ensure at most one instance of a given MonoBehaviour exists at runtime. If no instance is present, one can be auto-created and hidden; a persistent variant survives scene loads and destroys duplicates, while a regulator variant keeps the newest instance and removes others. Accessors are exposed for safe checks and retrieval.

![screenshot](Documentation/Screenshot.png)

## Features
- Generic singleton base: `Singleton<T>`
  - `Instance` lazily returns the current instance, searching loaded objects (`FindAnyObjectByType<T>()`) or auto-creating a hidden object when none exists
  - `HasInstance`, `TryGetInstance()`, and `Current` helpers for safe checks without side effects
  - Initializes in `Awake()` (play mode) and clears in `OnDestroy()`
  - Includes a guard against unsafe Unity contexts (during import/serialization or off the main thread): in those cases `Instance` returns `null` instead of calling `Find*` / creating objects
- Persistent singleton: `PersistentSingleton<T>`
  - Detaches to root, calls `DontDestroyOnLoad`, and destroys any duplicate instances detected later
- Regulator singleton: `RegulatorSingleton<T>`
  - Detaches to root, calls `DontDestroyOnLoad`, and when the newest instance initializes it destroys all other `RegulatorSingleton<T>` instances
  - Deterministic “newest wins” behavior (does not rely on `Time.time` ordering)
- Global singleton: `GlobalSingleton<T>`
  - Editor- and runtime-persistent singleton intended to exist “always”
  - Uses `HideFlags.HideAndDontSave` and persists across Play Mode transitions without being saved into scenes

## Requirements
- Unity 6000.0+
- MonoBehaviour-derived components (generic parameter `T : Component`)
- Intended for main-thread use

## Usage
1) Simple runtime singleton
```csharp
using UnityEngine;
using UnityEssentials;

public class GameServices : Singleton<GameServices>
{
    public int Score { get; private set; }

    public void AddScore(int amount) => Score += amount;
}

// From any script at runtime
GameServices.Instance.AddScore(10);
```

2) Persistent across scenes
```csharp
using UnityEngine;
using UnityEssentials;

public class AudioManager : PersistentSingleton<AudioManager>
{
    protected override void Awake()
    {
        base.Awake(); // ensures persistence and duplicate destruction
        // Initialize audio here
    }
}
```

3) Regulated (newest wins)
```csharp
using UnityEngine;
using UnityEssentials;

public class SessionController : RegulatorSingleton<SessionController>
{
    // The most recently initialized instance will survive.
    // When it initializes, all other regulators of this type are destroyed.
}
```

4) Safe access without auto-creation
```csharp
// Avoid creating a new instance inadvertently
if (GameServices.HasInstance)
{
    var svc = GameServices.TryGetInstance();
    // use svc
}
```

5) Global singleton (Edit Mode + Play Mode)
```csharp
using UnityEngine;
using UnityEssentials;

public class GlobalServices : GlobalSingleton<GlobalServices>
{
    public void DoSomething() { }
}

// Works in Edit Mode and Play Mode.
GlobalServices.Instance?.DoSomething();
```

## How It Works
- `Singleton<T>`
  - Stores a static `s_instance` and assigns it during `Awake()` (play mode)
  - `Instance` returns `s_instance`, or finds one with `FindAnyObjectByType`, or auto-creates a hidden GameObject (HideAndDontSave) and adds `T`
  - In unsafe contexts (import/serialization/off-thread), it returns `null` instead of calling Unity object APIs
  - Clears `s_instance` in `OnDestroy()` if it points to the current object
- `PersistentSingleton<T>`
  - On first initialization: detaches from parent, marks the GameObject as `DontDestroyOnLoad`, sets `s_instance`
  - If another instance appears later, it is destroyed
- `RegulatorSingleton<T>`
  - Detaches from parent, marks the GameObject as `DontDestroyOnLoad`
  - When a new regulator instance initializes, it destroys all other regulator instances and becomes the singleton
- `GlobalSingleton<T>`
  - Uses `HideFlags.HideAndDontSave` so it isn’t saved into scenes and is hidden from the hierarchy
  - Persists across scene loads in runtime, and persists across Play Mode transitions without being saved

## Notes and Limitations
- Not thread-safe: access and initialization should occur on the main thread
- Auto-creation side effects: `Instance` can create a hidden GameObject when none exists; use `HasInstance`/`TryGetInstance` to avoid implicit creation
- Unity object API guard: `Instance` may return `null` during editor import/serialization or off the main thread
- Play-mode initialization: base `Awake()` sets up the singleton only when `Application.isPlaying` is true
- Persistent duplicates: ensure a persistent singleton is not pre-placed in multiple scenes; duplicates are destroyed at runtime
- Hidden objects: auto-created objects use `HideFlags.HideAndDontSave` and are not part of saved scenes

## Files in This Package
- `Runtime/Singleton.cs` – `Singleton<T>` implementation
- `Runtime/PersistentSingleton.cs` – `PersistentSingleton<T>` implementation
- `Runtime/RegulatorSingleton.cs` – `RegulatorSingleton<T>` implementation
- `Runtime/GlobalSingleton.cs` – `GlobalSingleton<T>` implementation
- `Runtime/GlobalSingletonBootstrap.cs` – Unity main-thread / editor-guard utilities
- `Runtime/UnityEssentials.Singleton.asmdef` – Runtime assembly definition

## Tags
unity, singleton, pattern, monoBehaviour, lifecycle, dontdestroyonload, manager, services, runtime
