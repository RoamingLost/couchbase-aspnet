using System;
using System.Threading;
using System.Threading.Tasks;

namespace Couchbase.AspNet.Utils
{
    /// <summary>
    /// Task helper methods.
    /// </summary>
    /// <remarks>
    /// Provides a safer method than using <c>.GetAwaiter().GetResult()</c> alone 
    /// when needing to force async method to run synchronously.
    /// Based on Microsoft.AspNet.Identity.Core AsyncHelper.
    /// </remarks>
    /// <seealso cref="https://github.com/aspnet/AspNetIdentity/blob/main/src/Microsoft.AspNet.Identity.Core/AsyncHelper.cs">Microsoft.AspNet.Identity.Core AsyncHelper</seealso>
    internal static class AsyncHelper
    {
        private static readonly TaskFactory _taskFactory = new
            TaskFactory(CancellationToken.None,
                        TaskCreationOptions.None,
                        TaskContinuationOptions.None,
                        TaskScheduler.Default);

        /// <summary>
        /// Force an asynchronous task to run synchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the object to return.</typeparam>
        /// <param name="func">The asynchronous operation to run synchronously.</param>
        /// <returns>The object to return.</returns>
        /// <example>
        /// <code>
        /// bool success = AsyncHelper.RunSync(() => client.DoTaskAsync());
        /// </code>
        /// </example>
        public static TResult RunSync<TResult>(Func<Task<TResult>> func)
            => _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();

        /// <summary>
        /// Force an asynchronous task to run synchronously.
        /// </summary>
        /// <param name="func">The asynchronous operation to run synchronously.</param>
        /// <example>
        /// <code>
        /// AsyncHelper.RunSync(() => client.DoTaskAsync());
        /// </code>
        /// </example>
        public static void RunSync(Func<Task> func)
            => _taskFactory
                .StartNew(func)
                .Unwrap()
                .GetAwaiter()
                .GetResult();

        /// <summary>
        /// Force an asynchronous value task to run synchronously.
        /// </summary>
        /// <typeparam name="TResult">The type of the object to return.</typeparam>
        /// <param name="func">The asynchronous operation to run synchronously.</param>
        /// <returns>The object to return.</returns>
        /// <example>
        /// <code>
        /// bool success = AsyncHelper.RunSync(() => client.DoTaskAsync());
        /// </code>
        /// </example>
        public static TResult RunSync<TResult>(Func<ValueTask<TResult>> func)
            => _taskFactory
                .StartNew(() => func().AsTask())
                .Unwrap()
                .GetAwaiter()
                .GetResult();
    }
}
