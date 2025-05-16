# Unity Essentials

**Unity Essentials** is a lightweight, modular utility namespace designed to streamline development in Unity. 
It provides a collection of foundational tools, extensions, and helpers to enhance productivity and maintain clean code architecture.

## 📦 This Package

This package is part of the **Unity Essentials** ecosystem.  
It integrates seamlessly with other Unity Essentials modules and follows the same lightweight, dependency-free philosophy.

## 🌐 Namespace

All utilities are under the `UnityEssentials` namespace. This keeps your project clean, consistent, and conflict-free.

```csharp
using UnityEssentials;
```


# Singleton<T>, PersistentSingleton<T>, RegulatorSingleton<T>

Each example demonstrates one specific use-case for Singleton, PersistentSingleton, or RegulatorSingleton, enabling different initialization and lifecycle behaviors based on your Unity project's needs.

## Usage Examples

1. Basic Singleton Usage

```csharp
public class GameManager : Singleton<GameManager>
{
    public int Score;
}
```
Access GameManager.Instance.Score globally. Auto-creates the object if missing in scene.

2. Persistent Singleton for Cross-Scene Managers


```csharp
public class AudioManager : PersistentSingleton<AudioManager>
{
    public void PlaySound() { /*...*/ }
}
```
Ensures AudioManager survives scene loads and auto-destroys duplicates.

3. Regulator Singleton to Keep Newest Instance


```csharp
public class SessionLogger : RegulatorSingleton<SessionLogger>
{
    private void Start()
    {
        Debug.Log("Logger Initialized at Time: " + _initializationTime);
    }
}
```
Destroys older SessionLogger instances and keeps the most recently initialized one.

4. Using TryGetInstance() Safely


```csharp
public class UIManager : Singleton<UIManager>
{
    public void Show() { /*...*/ }
}

// Usage somewhere else
void TryShowUI()
{
    UIManager.TryGetInstance()?.Show();
}
```
Accesses the singleton only if it exists, avoiding auto-creation or null exceptions.

5. Check Existence with HasInstance

```csharp

if (GameManager.HasInstance)
    Debug.Log("GameManager exists");
```
Useful for checking initialization state before usage.

6. Override Awake in Custom Singleton


```csharp
public class NetworkManager : PersistentSingleton<NetworkManager>
{
    protected override void Awake()
    {
        base.Awake();
        InitializeNetwork();
    }

    private void InitializeNetwork() { /*...*/ }
}
```
Extends Awake for custom logic while preserving singleton behavior.

7. Manually Assigning Instance


```csharp
public class UIController : Singleton<UIController>
{
    public void RegisterAsInstance()
    {
        _instance = this;
    }
}
```
Explicitly sets the singleton without relying on Awake, useful in complex setup flows.

8. Singleton Auto-Creation With Default GameObject Name


```csharp
var settings = SettingsManager.Instance;
```
If SettingsManager doesn’t exist, it auto-creates a GameObject named SettingsManagerAutoCreated.

