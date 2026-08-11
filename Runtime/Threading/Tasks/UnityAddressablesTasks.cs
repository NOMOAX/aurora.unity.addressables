using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Aurora.Unity.Addressables.Threading.Tasks
{
    /// <summary>
    /// 提供一组返回值为 <see cref="Task"/> 或 <see cref="Task{TResult}"/> 的方法。
    /// </summary>
    public static class UnityAddressablesTasks
    {
        #region AsyncOperationHandle

        /// <summary>
        /// 创建一个任务，该任务将在 <see cref="AsyncOperationHandle"/> 完成时完成。
        /// </summary>
        /// <param name="asyncOperationHandle">异步操作句柄。</param>
        /// <returns>在 <paramref name="asyncOperationHandle"/> 完成时完成的任务。</returns>
        /// <exception cref="ArgumentException"><paramref name="asyncOperationHandle"/> 非法。</exception>
        public static Task WhenAsyncOperationHandle(AsyncOperationHandle asyncOperationHandle)
        {
            return InternalWhenAsyncOperationHandle(asyncOperationHandle, CancellationToken.None);
        }

        /// <summary>
        /// 创建一个任务，该任务将在 <see cref="AsyncOperationHandle"/> 完成时完成。
        /// </summary>
        /// <param name="asyncOperationHandle">异步操作句柄。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <returns>在 <paramref name="asyncOperationHandle"/> 完成或者 <paramref name="cancellationToken"/> 发出取消请求时完成的任务。</returns>
        /// <exception cref="ArgumentException"><paramref name="asyncOperationHandle"/> 非法。</exception>
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
        /// 创建一个任务，该任务将在 <see cref="AsyncOperationHandle{TObject}"/> 完成时完成。
        /// </summary>
        /// <param name="asyncOperationHandle">带有结果的异步操作句柄。</param>
        /// <typeparam name="TObject">异步操作的结果的类型。</typeparam>
        /// <returns>在 <paramref name="asyncOperationHandle"/> 完成时完成的任务。</returns>
        /// <exception cref="ArgumentException"><paramref name="asyncOperationHandle"/> 非法。</exception>
        public static Task<TObject> WhenAsyncOperationHandle<TObject>(
            AsyncOperationHandle<TObject> asyncOperationHandle)
        {
            return InternalWhenAsyncOperationHandle(asyncOperationHandle, CancellationToken.None);
        }

        /// <summary>
        /// 创建一个任务，该任务将在 <see cref="AsyncOperationHandle{TObject}"/> 完成时完成。
        /// </summary>
        /// <param name="asyncOperationHandle">带有结果的异步操作句柄。</param>
        /// <param name="cancellationToken">取消令牌。</param>
        /// <typeparam name="TObject">异步操作的结果的类型。</typeparam>
        /// <returns>在 <paramref name="asyncOperationHandle"/> 完成或者 <paramref name="cancellationToken"/> 发出取消请求时完成的任务。</returns>
        /// <exception cref="ArgumentException"><paramref name="asyncOperationHandle"/> 非法。</exception>
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
