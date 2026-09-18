# Aurora Unity Addressables

![license](https://img.shields.io/github/license/NOMOAX/aurora.unity.addressables)
![version](https://img.shields.io/badge/version-1.0.4-blue)
![lowest Unity version](https://img.shields.io/badge/Unity-2021.2%2B-blue)

Task-based asynchronous pattern (TAP) wrapper for Unity Addressables' `AsyncOperationHandle` async model.

English | [中文](README.zh.md)

## Dependencies

- [Aurora](https://github.com/NOMOAX/aurora.git)
- Addressables (`com.unity.addressables`)

## Installation

1. Open Unity package manager.
2. Click the `+` button in the upper-left corner, then select `Add package from git URL...`.
3. Input `https://github.com/NOMOAX/aurora.unity.addressables.git` and then click the `Add` button.

## Awaitables

Unity Addressables reports the progress of an asynchronous operation through `AsyncOperationHandle` / `AsyncOperationHandle<TObject>`, which can only be waited on with an event callback. `AsyncOperationHandleAwaitable` and `AsyncOperationHandleAwaitable<TObject>` are the awaitable structs that wrap them, so a handle can be awaited directly inside an `async` method.

```csharp
// Wait for a resource load; TObject is the type of the result carried by the handle
var assetHandle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Tree");
var tree = await new AsyncOperationHandleAwaitable<GameObject>(assetHandle);

// Wait for a scene load
var sceneHandle = Addressables.LoadSceneAsync("Level2");
var sceneInstance = await new AsyncOperationHandleAwaitable<SceneInstance>(sceneHandle);

// Wait for a handle that carries no result
var downloadHandle = Addressables.DownloadDependenciesAsync("my group");
await new AsyncOperationHandleAwaitable(downloadHandle);

// Both of them can be cancelled
await new AsyncOperationHandleAwaitable<GameObject>(assetHandle, cancellationToken);
```

`AsyncOperationHandleAwaitable` awaits an `AsyncOperationHandle` and produces no value; `AsyncOperationHandleAwaitable<TObject>` awaits an `AsyncOperationHandle<TObject>` and produces its `Result`. Both of them also implement `IAwaitable` / `IAwaitable<TObject>` of the Aurora package, so they can be used wherever a uniform awaitable context is needed.

### Failures and Cancellation

A handle that is not valid throws `ArgumentException` when it is awaited, instead of entering the wait.

A handle that has already completed completes the `await` immediately: a succeeded handle produces the result at once, and a failed handle throws its `OperationException` at once. A handle that is still in progress completes the `await` when it completes; when it fails, the underlying `OperationException` propagates, so the `try` / `catch` around the `await` handles Addressables failures like any other exception.

Passing a `CancellationToken` only ends the **wait**: once the token is cancelled, the awaiting method is resumed with an `OperationCanceledException`, but the Addressables operation itself is not stopped, and it still has to be waited for (or released) in some other way. Awaiting also never releases the handle, whichever way it ends — releasing is always the responsibility of the caller, through `Addressables.Release`.

```csharp
var handle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Tree");
try
{
    var tree = await new AsyncOperationHandleAwaitable<GameObject>(handle, cancellationToken);
    // uses tree
}
catch (OperationCanceledException)
{
    // The wait was cancelled; handle is still in progress and still has to be released
}
catch (Exception)
{
    // The operation failed; see the InnerException of handle.OperationException for the concrete cause
}
finally
{
    Addressables.Release(handle);
}
```

## Tasks

### UnityAddressablesTasks

`UnityAddressablesTasks` provides the `Task`-returning forms of the waits above, suitable for cases where the wait has to be stored, composed, or awaited somewhere else. The awaiters of this package are built on top of it: awaiting a handle is awaiting the task that `WhenAsyncOperationHandle` returns for that handle.

```csharp
// Wait for a resource load
var loadTask = UnityAddressablesTasks.WhenAsyncOperationHandle(handle);
var prefab = await loadTask;

// Wait for a handle that carries no result
await UnityAddressablesTasks.WhenAsyncOperationHandle(downloadHandle);

// The returned task can also be stored and composed with other tasks
await Task.WhenAll(loadTask, otherTask);

// Both of them can be cancelled, in which case the task is put into the cancelled state
await UnityAddressablesTasks.WhenAsyncOperationHandle(handle, cancellationToken);
```

For every form there is an overload that accepts a `CancellationToken`. The overloads with and without a result behave exactly like the corresponding awaitables do, including the way failures and cancellation are reported.

## Editor Utilities

### UnityAddressablesEditorUtility

`UnityAddressablesEditorUtility` (in the editor-only assembly `Aurora.UnityEditor.Addressables`) exposes the play mode configuration of Addressables, for editor tools that need to know which play mode the project is currently in.

```csharp
// The play mode index of the project (ProjectConfigData.ActivePlayModeIndex)
var playModeIndex = UnityAddressablesEditorUtility.PlayModeIndex;

// The name of the play mode
var playModeName = UnityAddressablesEditorUtility.PlayModeName;
```

`PlayModeName` produces the same text as the "Play Mode Script" dropdown of the Addressables settings window: it is `"null"` when no data builder is configured, `Use Existing Build (<platform>)` for `BuildScriptPackedPlayMode` (where `<platform>` is the subfolder name of the current platform), and the `Name` of the data builder otherwise.

Reading it requires Addressables to be set up in the project, that is, `AddressableAssetSettingsDefaultObject.Settings` must not be `null`.
