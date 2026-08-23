using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Aurora.Unity.Addressables.Threading.Tasks
{
    /// <summary>
    /// Provides a set of methods that return <see cref="Task"/> or <see cref="Task{TResult}"/>.
    /// </summary>
    public static class UnityAddressablesTasks
    {
        #region AsyncOperationHandle

        /// <summary>
        /// Creates a task that completes when <see cref="AsyncOperationHandle"/> completes.
        /// </summary>
        /// <param name="asyncOperationHandle">The asynchronous operation handle.</param>
        /// <returns>A task that completes when <paramref name="asyncOperationHandle"/> completes.</returns>
        /// <exception cref="ArgumentException"><paramref name="asyncOperationHandle"/> is invalid.</exception>
        public static Task WhenAsyncOperationHandle(AsyncOperationHandle asyncOperationHandle)
        {
            return InternalWhenAsyncOperationHandle(asyncOperationHandle, CancellationToken.None);
        }

        /// <summary>
        /// Creates a task that completes when <see cref="AsyncOperationHandle"/> completes.
        /// </summary>
        /// <param name="asyncOperationHandle">The asynchronous operation handle.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that completes when <paramref name="asyncOperationHandle"/> completes or <paramref name="cancellationToken"/> requests cancellation.</returns>
        /// <exception cref="ArgumentException"><paramref name="asyncOperationHandle"/> is invalid.</exception>
        public static Task WhenAsyncOperationHandle(
            AsyncOperationHandle asyncOperationHandle,
            CancellationToken    cancellationToken)
        {
            return InternalWhenAsyncOperationHandle(asyncOperationHandle, cancellationToken);
        }

        private static Task InternalWhenAsyncOperationHandle(
            AsyncOperationHandle asyncOperationHandle,
            CancellationToken    cancellationToken)
        {
            if (!asyncOperationHandle.IsValid())
            {
                throw new ArgumentException("The handle is invalid.", nameof(asyncOperationHandle));
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled(cancellationToken);
            }
            switch (asyncOperationHandle.Status)
            {
                case AsyncOperationStatus.None:
                    var asyncOperationHandlePromise = cancellationToken.CanBeCanceled switch
                    {
                        false => new AsyncOperationHandlePromise(asyncOperationHandle),
                        true => new AsyncOperationHandlePromiseWithCancellation(asyncOperationHandle, cancellationToken)
                    };
                    return asyncOperationHandlePromise.Task;
                case AsyncOperationStatus.Succeeded:
                    return Task.CompletedTask;
                case AsyncOperationStatus.Failed:
                    return Task.FromException(asyncOperationHandle.OperationException);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private class AsyncOperationHandlePromise : TaskCompletionSource<VoidResult>
        {
            internal AsyncOperationHandlePromise(AsyncOperationHandle asyncOperationHandle)
            {
                asyncOperationHandle.Completed += OnAsyncOperationComplete;
            }

            private void OnAsyncOperationComplete(AsyncOperationHandle asyncOperationHandle)
            {
                switch (asyncOperationHandle.Status)
                {
                    case AsyncOperationStatus.None:
                        if (TrySetException(new ArgumentException("The operation is still in progress.")))
                        {
                            CleanUp();
                        }
                        break;
                    case AsyncOperationStatus.Succeeded:
                        if (TrySetResult(new VoidResult()))
                        {
                            CleanUp();
                        }
                        break;
                    case AsyncOperationStatus.Failed:
                        if (TrySetException(asyncOperationHandle.OperationException))
                        {
                            CleanUp();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected virtual void CleanUp()
            {
            }
        }

        private sealed class AsyncOperationHandlePromiseWithCancellation : AsyncOperationHandlePromise
        {
            private static readonly Action<object> ActionCancel = Cancel;

            private readonly CancellationTokenRegistration _cancellationTokenRegistration;

            internal AsyncOperationHandlePromiseWithCancellation(
                AsyncOperationHandle asyncOperationHandle,
                CancellationToken    cancellationToken) : base(asyncOperationHandle)
            {
                _cancellationTokenRegistration = cancellationToken.Register(
                    ActionCancel,
                    Tuple.Create(this, cancellationToken)
                );
                if (Task.IsCompleted)
                {
                    _cancellationTokenRegistration.Dispose();
                }
            }

            private static void Cancel(object state)
            {
                var (asyncOperationHandlePromiseWithCancellation, cancellationToken) =
                    (Tuple<AsyncOperationHandlePromiseWithCancellation, CancellationToken>)state;
                if (asyncOperationHandlePromiseWithCancellation.TrySetCanceled(cancellationToken))
                {
                    asyncOperationHandlePromiseWithCancellation.CleanUp();
                }
            }

            protected override void CleanUp()
            {
                _cancellationTokenRegistration.Dispose();
                base.CleanUp();
            }
        }

        #endregion

        #region AsyncOperationHandle<TObject>

        /// <summary>
        /// Creates a task that completes when <see cref="AsyncOperationHandle{TObject}"/> completes.
        /// </summary>
        /// <param name="asyncOperationHandle">The asynchronous operation handle carrying a result.</param>
        /// <typeparam name="TObject">The type of the asynchronous operation result.</typeparam>
        /// <returns>A task that completes when <paramref name="asyncOperationHandle"/> completes.</returns>
        /// <exception cref="ArgumentException"><paramref name="asyncOperationHandle"/> is invalid.</exception>
        public static Task<TObject> WhenAsyncOperationHandle<TObject>(
            AsyncOperationHandle<TObject> asyncOperationHandle)
        {
            return InternalWhenAsyncOperationHandle(asyncOperationHandle, CancellationToken.None);
        }

        /// <summary>
        /// Creates a task that completes when <see cref="AsyncOperationHandle{TObject}"/> completes.
        /// </summary>
        /// <param name="asyncOperationHandle">The asynchronous operation handle carrying a result.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <typeparam name="TObject">The type of the asynchronous operation result.</typeparam>
        /// <returns>A task that completes when <paramref name="asyncOperationHandle"/> completes or <paramref name="cancellationToken"/> requests cancellation.</returns>
        /// <exception cref="ArgumentException"><paramref name="asyncOperationHandle"/> is invalid.</exception>
        public static Task<TObject> WhenAsyncOperationHandle<TObject>(
            AsyncOperationHandle<TObject> asyncOperationHandle,
            CancellationToken             cancellationToken)
        {
            return InternalWhenAsyncOperationHandle(asyncOperationHandle, cancellationToken);
        }

        private static Task<TObject> InternalWhenAsyncOperationHandle<TObject>(
            AsyncOperationHandle<TObject> asyncOperationHandle,
            CancellationToken             cancellationToken)
        {
            if (!asyncOperationHandle.IsValid())
            {
                throw new ArgumentException("The handle is invalid.", nameof(asyncOperationHandle));
            }
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<TObject>(cancellationToken);
            }
            switch (asyncOperationHandle.Status)
            {
                case AsyncOperationStatus.None:
                    var asyncOperationHandlePromise = cancellationToken.CanBeCanceled switch
                    {
                        false => new AsyncOperationHandlePromise<TObject>(asyncOperationHandle),
                        true => new AsyncOperationHandlePromiseWithCancellation<TObject>(
                            asyncOperationHandle,
                            cancellationToken
                        )
                    };
                    return asyncOperationHandlePromise.Task;
                case AsyncOperationStatus.Succeeded:
                    return Task.FromResult(asyncOperationHandle.Result);
                case AsyncOperationStatus.Failed:
                    return Task.FromException<TObject>(asyncOperationHandle.OperationException);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private class AsyncOperationHandlePromise<TObject> : TaskCompletionSource<TObject>
        {
            internal AsyncOperationHandlePromise(AsyncOperationHandle<TObject> asyncOperationHandle)
            {
                asyncOperationHandle.Completed += OnAsyncOperationComplete;
            }

            private void OnAsyncOperationComplete(AsyncOperationHandle<TObject> asyncOperationHandle)
            {
                switch (asyncOperationHandle.Status)
                {
                    case AsyncOperationStatus.None:
                        if (TrySetException(new ArgumentException("The operation is still in progress.")))
                        {
                            CleanUp();
                        }
                        break;
                    case AsyncOperationStatus.Succeeded:
                        if (TrySetResult(asyncOperationHandle.Result))
                        {
                            CleanUp();
                        }
                        break;
                    case AsyncOperationStatus.Failed:
                        if (TrySetException(asyncOperationHandle.OperationException))
                        {
                            CleanUp();
                        }
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            protected virtual void CleanUp()
            {
            }
        }

        private sealed class AsyncOperationHandlePromiseWithCancellation<TObject> : AsyncOperationHandlePromise<TObject>
        {
            private static readonly Action<object> ActionCancel = Cancel;

            private readonly CancellationTokenRegistration _cancellationTokenRegistration;

            internal AsyncOperationHandlePromiseWithCancellation(
                AsyncOperationHandle<TObject> asyncOperationHandle,
                CancellationToken             cancellationToken) : base(asyncOperationHandle)
            {
                _cancellationTokenRegistration = cancellationToken.Register(
                    ActionCancel,
                    Tuple.Create(this, cancellationToken)
                );
                if (Task.IsCompleted)
                {
                    _cancellationTokenRegistration.Dispose();
                }
            }

            private static void Cancel(object state)
            {
                var (asyncOperationHandlePromiseWithCancellation, cancellationToken) =
                    (Tuple<AsyncOperationHandlePromiseWithCancellation<TObject>, CancellationToken>)state;
                if (asyncOperationHandlePromiseWithCancellation.TrySetCanceled(cancellationToken))
                {
                    asyncOperationHandlePromiseWithCancellation.CleanUp();
                }
            }

            protected override void CleanUp()
            {
                _cancellationTokenRegistration.Dispose();
                base.CleanUp();
            }
        }

        #endregion
    }
}
