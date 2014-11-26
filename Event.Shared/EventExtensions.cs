﻿#if UseIteratorTasks
using IteratorTasks;
#else
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Disposables;
#endif

namespace System
{
    public static partial class EventExtensions
    {
        public static CancellationToken ToCancellationToken<TArg>(this IEvent<TArg> e)
        {
            var cts = new CancellationTokenSource();
            e.Subscribe(cts.Cancel);
            return cts.Token;
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// </summary>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, CancellationToken ct)
        {
            return FirstAsync(e, _ => true, ct);
        }
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e)
        {
            return FirstAsync(e, CancellationToken.None);
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// predicate を満たすまでは何回でもイベントを受け取る。
        /// </summary>
        /// <typeparam name="TArg">イベント引数の型。</typeparam>
        /// <param name="e">イベント発生元。</param>
        /// <param name="predicate">受け取り条件。</param>
        /// <param name="ct">キャンセル用。</param>
        /// <returns>イベントが1回起きるまで待つタスク。</returns>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, Func<TArg, bool> predicate, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TArg>();

            Handler<TArg> a = null;
            a = (sender, args) =>
            {
                if (predicate(args))
                {
                    e.Remove(a);
                    tcs.TrySetResult(args);
                }
            };

            e.Add(a);

            if (ct != CancellationToken.None)
            {
                ct.Register(() =>
                {
                    e.Remove(a);
                    tcs.TrySetCanceled();
                });
            }

            return tcs.Task;
        }

        /// <summary>
        /// イベントを1回だけ受け取る。
        /// predicate を満たすまでは何回でもイベントを受け取る。
        /// predicate が非同期処理なバージョン。
        /// </summary>
        public static Task<TArg> FirstAsync<TArg>(this IEvent<TArg> e, Func<TArg, Task<bool>> predicate, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<TArg>();

            Handler<TArg> a = null;
            a = (sender, args) =>
            {
                predicate(args).ContinueWith(t =>
                {
                    if (t.Result)
                    {
                        e.Remove(a);
                        tcs.TrySetResult(args);
                    }
                });
            };

            e.Add(a);

            if (ct != CancellationToken.None)
            {
                ct.Register(() =>
                {
                    e.Remove(a);
                    tcs.TrySetCanceled();
                });
            }

            return tcs.Task;
        }

        public static IDisposable Subscribe<T>(this IEvent<T> e, Handler<T> handler)
        {
            e.Add(handler);
            return Disposable.Create(() => e.Remove(handler));
        }
        public static IDisposable Subscribe<T>(this IEvent<T> e, Action<T> handler)
        {
            return Subscribe(e, (_1, arg) => handler(arg));
        }
        public static IDisposable Subscribe<T>(this IEvent<T> e, Action handler)
        {
            return Subscribe(e, (_1, _2) => handler());
        }

        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Handler<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }
        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Action<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }
        public static void SubscribeUntil<T>(this IEvent<T> e, CancellationToken ct, Action handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }

        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, AsyncHandler<T> handler)
        {
            e.Add(handler);
            return Disposable.Create(() => e.Remove(handler));
        }
        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, Func<T, Task> handler)
        {
            return Subscribe(e, (_1, arg) => handler(arg));
        }
        public static IDisposable Subscribe<T>(this IAsyncEvent<T> e, Func<Task> handler)
        {
            return Subscribe(e, (_1, _2) => handler());
        }

        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, AsyncHandler<T> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }
        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, Func<T, Task> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }
        public static void SubscribeUntil<T>(this IAsyncEvent<T> e, CancellationToken ct, Func<Task> handler)
        {
            var d = e.Subscribe(handler);
            ct.Register(d.Dispose);
        }
    }
}
