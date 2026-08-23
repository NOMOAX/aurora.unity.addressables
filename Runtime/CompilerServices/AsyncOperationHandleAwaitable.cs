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
    /// Provides an awaitable context that switches to the target <see cref="AsyncOperationHandle"/> upon completion.
    /// </summary>
    public readonly struct AsyncOperationHandleAwaitable : IAwaitable
    {
        private readonly AsyncOperationHandle _asyncOperationHandle;

        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperationHandleAwaitable"/> structure.
        /// </summary>
        /// <param name="asyncOperationHandle">The asynchronous operation handle.</param>
        public AsyncOperationHandleAwaitable(AsyncOperationHandle asyncOperationHandle)
        {
            _asyncOperationHandle = asyncOperationHandle;
            _cancellationToken    = CancellationToken.None;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncOperationHandleAwaitable"/> structure.
        /// </summary>
        /// <param name="asyncOperationHandle">The asynchronous operation handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public AsyncOperationHandleAwaitable(
            AsyncOperationHandle asyncOperationHandle,
            CancellationToken    cancellationToken)
        {
            _asyncOperationHandle = asyncOperationHandle;
            _cancellationToken    = cancellationToken;
        }

        /// <inheritdoc />
        public IAwaiter GetAwaiter()
        {
            return new Awaiter(_asyncOperationHandle, _cancellationToken);
        }

        private readonly struct Awaiter : IAwaiter
        {
            private static readonly Action<Task, object> RunAction = (_, state) => ((Action)state)();

            private readonly Task _task;

            internal Awaiter(AsyncOperationHandle asyncOperationHandle, CancellationToken cancellationToken)
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
            public void GetResult()
            {
                if (_task != null)
                {
                    TaskUtility.ThrowIfFaultedOrCanceled(_task);
                }
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
