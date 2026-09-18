# Aurora Unity Addressables

![许可](https://img.shields.io/github/license/NOMOAX/aurora.unity.addressables)
![版本](https://img.shields.io/badge/version-1.0.3-blue)
![最低 Unity 版本](https://img.shields.io/badge/Unity-2021.2%2B-blue)

使用基于任务的异步模式（Task-based asynchronous pattern）封装 Unity Addressables 的 `AsyncOperationHandle` 异步模型。

[English](README.md) | 中文

## 依赖

- [Aurora](https://github.com/NOMOAX/aurora.git)
- Addressables（`com.unity.addressables`）

## 安装

1. 打开 Unity package manager。
2. 点击左上角的 `+` 按钮，然后选择 `Add package from git URL...`。
3. 填入 `https://github.com/NOMOAX/aurora.unity.addressables.git` 并点击 `Add` 按钮。

## 可等待对象

Unity Addressables 通过 `AsyncOperationHandle` / `AsyncOperationHandle<TObject>` 来汇报异步操作的进度，而它们只能用事件回调的方式等待。`AsyncOperationHandleAwaitable` 与 `AsyncOperationHandleAwaitable<TObject>` 就是包装它们的可等待结构，让句柄可以在 `async` 方法里直接 `await`。

```csharp
// 等待资源加载，TObject 是句柄携带的结果的类型
var assetHandle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Tree");
var tree = await new AsyncOperationHandleAwaitable<GameObject>(assetHandle);

// 等待场景加载
var sceneHandle = Addressables.LoadSceneAsync("Level2");
var sceneInstance = await new AsyncOperationHandleAwaitable<SceneInstance>(sceneHandle);

// 等待不携带结果的句柄
var downloadHandle = Addressables.DownloadDependenciesAsync("my group");
await new AsyncOperationHandleAwaitable(downloadHandle);

// 两者都可以被取消
await new AsyncOperationHandleAwaitable<GameObject>(assetHandle, cancellationToken);
```

`AsyncOperationHandleAwaitable` 等待 `AsyncOperationHandle`，不产生值；`AsyncOperationHandleAwaitable<TObject>` 等待 `AsyncOperationHandle<TObject>`，并产生它的 `Result`。两者同时实现了 Aurora 包的 `IAwaitable` / `IAwaitable<TObject>`，因此可以用在需要统一的可等待上下文的场合。

### 失败与取消

无效的句柄在被等待时会抛出 `ArgumentException`，而不是进入等待。

已经完成的句柄会立即完成 `await`：成功的句柄立即产生结果，失败的句柄立即抛出它的 `OperationException`。还在进行中的句柄在完成时完成 `await`，失败时把底层的 `OperationException` 抛出，因此 `await` 周围的 `try` / `catch` 能像处理其它异常一样处理 Addressables 的失败。

传入 `CancellationToken` 只结束 **等待**：令牌被取消后，等待的方法会以 `OperationCanceledException` 继续，但 Addressables 的操作本身并不会停止，仍然需要用别的方式等待（或释放）它。等待也从不释放句柄，无论以哪种方式结束——释放始终是调用方的责任，通过 `Addressables.Release` 完成。

```csharp
var handle = Addressables.LoadAssetAsync<GameObject>("Prefabs/Tree");
try
{
    var tree = await new AsyncOperationHandleAwaitable<GameObject>(handle, cancellationToken);
    // 使用 tree
}
catch (OperationCanceledException)
{
    // 等待被取消了，handle 仍在进行中，仍然需要释放
}
catch (Exception)
{
    // 操作失败了，具体原因见 handle.OperationException 的 InnerException
}
finally
{
    Addressables.Release(handle);
}
```

## 任务

### UnityAddressablesTasks

`UnityAddressablesTasks` 提供上面这些等待的 `Task` 形式，适合需要把等待保存起来、组合或换一个地方等待的场景。本包的可等待对象就建立在它之上：等待一个句柄，就是等待 `WhenAsyncOperationHandle` 为该句柄返回的任务。

```csharp
// 等待资源加载
var loadTask = UnityAddressablesTasks.WhenAsyncOperationHandle(handle);
var prefab = await loadTask;

// 等待不携带结果的句柄
await UnityAddressablesTasks.WhenAsyncOperationHandle(downloadHandle);

// 返回的任务也可以保存起来，与其它任务组合
await Task.WhenAll(loadTask, otherTask);

// 两者都可以被取消，此时任务进入已取消状态
await UnityAddressablesTasks.WhenAsyncOperationHandle(handle, cancellationToken);
```

每一种形式都有一个接受 `CancellationToken` 的重载。带结果与不带结果的重载的行为与对应的可等待对象完全一致，包括失败与取消的汇报方式。

## 编辑器工具

### UnityAddressablesEditorUtility

`UnityAddressablesEditorUtility`（位于仅编辑器程序集 `Aurora.UnityEditor.Addressables`）暴露 Addressables 的播放模式配置，供需要知道项目当前处于哪种播放模式的编辑器工具使用。

```csharp
// 项目的播放模式索引（ProjectConfigData.ActivePlayModeIndex）
var playModeIndex = UnityAddressablesEditorUtility.PlayModeIndex;

// 播放模式的名称
var playModeName = UnityAddressablesEditorUtility.PlayModeName;
```

`PlayModeName` 产生与 Addressables 设置窗口的 `Play Mode Script` 下拉框相同的文本：没有配置数据构建器时是 `"null"`，`BuildScriptPackedPlayMode` 是 `Use Existing Build (<平台>)`（其中 `<平台>` 是当前平台的子文件夹名），其余情况是数据构建器的 `Name`。

读取它要求项目中已经配置好 Addressables，即 `AddressableAssetSettingsDefaultObject.Settings` 不为 `null`。
