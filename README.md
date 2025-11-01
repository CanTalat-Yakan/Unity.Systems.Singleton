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

> Quick overview: Generic MonoBehaviour singletons for Unity, including a runtime auto-creating singleton, a persistent (DontDestroyOnLoad) variant, and a regulator that keeps only the most recently initialized instance.

Singleton components are provided to ensure at most one instance of a given MonoBehaviour exists at runtime. If no instance is present, one can be auto-created and hidden; a persistent variant survives scene loads and destroys duplicates, while a regulator variant keeps the newest instance and removes older ones after scene changes. Accessors are exposed for safe checks and retrieval.

![screenshot](Documentation/Screenshot.png)

## Features
- Generic singleton base: `Singleton<T>`
  - `Instance` lazily returns the current instance, searching the scene (`FindAnyObjectByType<T>()`) or auto-creating a hidden object when none exists
  - `HasInstance`, `TryGetInstance()`, and `Current` helpers for safe checks without side effects
  - Initializes in `Awake()` (play mode) and clears in `OnDestroy()`
- Persistent singleton: `PersistentSingleton<T>`
  - Detaches, calls `DontDestroyOnLoad`, and destroys any duplicate instances detected later
- Regulator singleton: `RegulatorSingleton<T>`
  - Calls `DontDestroyOnLoad`, timestamps initialization, and destroys older instances so that the newest one persists
  - Assigns the global instance if not already set
- Lightweight and focused
  - No external dependencies; small, readable pattern tuned for Unity’s lifecycle

## Requirements
- Unity 6000.0+
- MonoBehaviour-derived components (generic parameter `T : Component`)
- Intended for main-thread use during play mode

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
    // The most recently initialized instance will survive scene changes
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

## How It Works
- `Singleton<T>`
  - Stores a static `s_instance` and assigns it during `Awake()` (play mode)
  - `Instance` returns `s_instance`, or finds one in the scene, or auto-creates a hidden GameObject (HideAndDontSave) and adds `T`
  - Clears `s_instance` in `OnDestroy()` if it points to the current object
- `PersistentSingleton<T>`
  - On first initialization: detaches from parent, marks the GameObject as `DontDestroyOnLoad`, sets `s_instance`
  - If another instance appears later, it is destroyed
- `RegulatorSingleton<T>`
  - Records an initialization time, marks `DontDestroyOnLoad`, and destroys any older instances of the same type
  - Sets `s_instance` if not already assigned

## Notes and Limitations
- Not thread-safe: access and initialization should occur on the main thread
- Auto-creation side effects: `Instance` can create a hidden GameObject when none exists; use `HasInstance`/`TryGetInstance` to avoid implicit creation
- Play-mode initialization: base `Awake()` sets up the singleton only when `Application.isPlaying` is true
- Persistent duplicates: ensure a persistent singleton is not pre-placed in multiple scenes; duplicates are destroyed at runtime
- Regulator timing: the “newest wins” policy uses `Time.time`; creation within the same frame is typically resolved by destroy order
- Hidden objects: auto-created objects use `HideFlags.HideAndDontSave` and are not part of saved scenes

## Files in This Package
- `Runtime/Singleton.cs` – Implementations of `Singleton<T>`, `PersistentSingleton<T>`, and `RegulatorSingleton<T>`
- `Runtime/UnityEssentials.Singleton.asmdef` – Runtime assembly definition

## Tags
unity, singleton, pattern, monoBehaviour, lifecycle, dontdestroyonload, manager, services, runtime
