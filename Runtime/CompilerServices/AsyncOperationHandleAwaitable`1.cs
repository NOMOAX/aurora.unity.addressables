using System;
using System.Threading;
using System.Threading.Tasks;
using Aurora.CompilerServices;
using Aurora.Threading;
using Aurora.Unity.Addressables.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Aurora.Unity.Addressables.CompilerServices
{
    /// <summary>
    /// Provides an awaitable context that switches to the target <see cref="AsyncOperationHandle{TObject}"/> upon completion.
    /// </summary>
    public readonly struct AsyncOperationHandleAwaitable<TObject> : IAwaitable<TObject>
    {
        private readonly AsyncOperationHandle<TObject> _asyncOperationHandle;

        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperationHandleAwaitable{TObject}"/> structure.
        /// </summary>
        /// <param name="asyncOperationHandle">The asynchronous operation handle carrying a result.</param>
        public AsyncOperationHandleAwaitable(AsyncOperationHandle<TObject> asyncOperationHandle)
        {
            _asyncOperationHandle = asyncOperationHandle;
            _cancellationToken    = CancellationToken.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperationHandleAwaitable{TObject}"/> structure.
        /// </summary>
        /// <param name="asyncOperationHandle">The asynchronous operation handle carrying a result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public AsyncOperationHandleAwaitable(
            AsyncOperationHandle<TObject> asyncOperationHandle,
            CancellationToken             cancellationToken)
        {
            _asyncOperationHandle = asyncOperationHandle;
            _cancellationToken    = cancellationToken;
        }

        /// <inheritdoc />
        public IAwaiter<TObject> GetAwaiter()
        {
            return new Awaiter(_asyncOperationHandle, _cancellationToken);
        }

        private readonly struct Awaiter : IAwaiter<TObject>
        {
            private static readonly Action<Task<TObject>, object> RunAction = (_, state) => ((Action)state)();

            private readonly Task<TObject> _task;

            internal Awaiter(AsyncOperationHandle<TObject> asyncOperationHandle, CancellationToken cancellationToken)
            {
                _task = UnityAddressablesTasks.WhenAsyncOperationHandle(asyncOperationHandle, cancellationToken);
            }

            /// <inheritdoc />
            public bool IsCompleted => InternalIsCompleted;

            private bool InternalIsCompleted => _task is null || _task.IsCompleted;

            /// <inheritdoc />
            public void OnCompleted(Action continuation)
            {
                InternalOnCompleted(continuation);
            }

            /// <inheritdoc />
            public void UnsafeOnCompleted(Action continuation)
            {
                InternalOnCompleted(continuation);
            }

            /// <inheritdoc />
            public TObject GetResult()
            {
                if (_task == null)
                {
                    return default;
                }
                TaskUtility.ThrowIfFaultedOrCanceled(_task);
                return _task.Result;
            }

            private void InternalOnCompleted(Action continuation)
            {
                if (InternalIsCompleted)
                {
                    continuation();
                }
                else
                {
                    TaskUtility.ContinueWithSynchronously(_task, RunAction, continuation);
                }
            }
        }
    }
}
