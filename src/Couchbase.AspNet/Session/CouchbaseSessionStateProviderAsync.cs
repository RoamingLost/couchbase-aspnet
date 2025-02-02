﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.SessionState;
using Common.Logging;
using Couchbase.AspNet.IO;
using Couchbase.AspNet.Utils;
using Couchbase.Core.IO.Transcoders;
using Microsoft.AspNet.SessionState;

namespace Couchbase.AspNet.Session
{
    public class CouchbaseSessionStateProviderAsync : SessionStateStoreProviderAsyncBase, ICouchbaseWebProvider
    {
        private readonly object _syncObj = new object();
        public IBucket Bucket { get; set; }
        public string ApplicationName { get; set; }
        public bool ThrowOnError { get; set; }
        public string Prefix { get; set; }
        public string BucketName { get; set; }
        internal ILog Log => LogManager.GetLogger<CouchbaseSessionStateProvider>();
        private SessionStateSection Config { get; set; }

        private readonly ITypeTranscoder _transcoder = new LegacyTranscoder();

        public override void Initialize(string name, NameValueCollection config)
        {
            base.Initialize(name, config);
            lock (_syncObj)
            {
                var appName = System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath;
                var webConfig = WebConfigurationManager.OpenWebConfiguration(appName);
                Config = (SessionStateSection)webConfig.GetSection("system.web/sessionState");

                var bootStapper = new BootStrapper();
                bootStapper.Bootstrap(name, config, this);
            }
        }

        public override SessionStateStoreData CreateNewStoreData(HttpContextBase context, int timeout)
        {
            Log.Trace("CreateNewStoreData called.");
            return new SessionStateStoreData(new SessionStateItemCollection(),
                SessionStateUtility.GetSessionStaticObjects(context.ApplicationInstance.Context),
                timeout);
        }

        public override async Task CreateUninitializedItemAsync(HttpContextBase context, string id, int timeout, CancellationToken cancellationToken)
        {
            var sessionId = this.PrefixIdentifier(id);
            Log.TraceFormat("CreateUninitializedItem called for item {0}.", sessionId);
            try
            {
                var sessionTimeout = TimeSpan.FromMinutes(timeout);
                var expires = DateTime.UtcNow.AddMinutes(timeout);
                var result = await Bucket.InsertAsync(this.PrefixIdentifier(sessionId), new SessionStateItem
                {
                    ApplicationName = ApplicationName,
                    Expires = expires,
                    SessionId = sessionId,
                    Actions = SessionStateActions.InitializeItem,
                    Timeout = sessionTimeout
                }, sessionTimeout, _transcoder).ConfigureAwait(false);

                if (result.Success) return;
                LogAndOrThrow(result, sessionId);
            }
            catch (Exception e)
            {
                LogAndOrThrow(e, sessionId);
            }
        }

        public override void Dispose()
        {
            //not required
        }

        public override Task EndRequestAsync(HttpContextBase context)
        {
           return Task.CompletedTask;
        }

        public override Task<GetItemResult> GetItemAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            return GetItemFromStoreAsync(context, id, cancellationToken, false);
        }

        public override Task<GetItemResult> GetItemExclusiveAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            return GetItemFromStoreAsync(context, id, cancellationToken, true);
        }

        public async Task<GetItemResult> GetItemFromStoreAsync(HttpContextBase context, string id, CancellationToken cancellationToken, bool exclusive)
        {
            if (id == null) return null;
            var sessionId = this.PrefixIdentifier(id);

            Log.TraceFormat("GetItemFromStoreAsync called for item {0}.", sessionId);

            var get = await Bucket.GetAsync<SessionStateItem>(sessionId, _transcoder).ConfigureAwait(false);
            if (get.Status == ResponseStatus.KeyNotFound)
            {
                return null;
            }

            GetItemResult itemResult = null;
            if (get.Status == ResponseStatus.Success)
            {

                var item = get.Value;
                var lockAge = DateTime.UtcNow - item.LockDate;

                if (item.Locked)
                {
                    if (lockAge > new TimeSpan(0, 0, 31536000))
                    {
                        lockAge = TimeSpan.Zero;
                    }
                    itemResult = new GetItemResult(null, item.Locked, lockAge, item.LockId, SessionStateActions.None);
                }
                else
                {
                    var storeData = GetSessionStateStoreData(item.SessionItems, context, (int)Config.Timeout.TotalMinutes);
                    itemResult = new GetItemResult(storeData, item.Locked, lockAge, item.LockId, item.Actions);
                }

                if (exclusive)
                {
                    item.Locked = true;
                    var upsert = await Bucket.UpsertAsync(sessionId, item, item.Timeout, _transcoder).ConfigureAwait(false);
                    if (!upsert.Success)
                    {
                        LogAndOrThrow(upsert, sessionId);
                    }
                }
            }
            return itemResult;
        }

        public override void InitializeRequest(HttpContextBase context)
        {
            Log.Trace("InitializeRequest called.");
        }

        public override async Task ReleaseItemExclusiveAsync(HttpContextBase context, string id, object lockId, CancellationToken cancellationToken)
        {
            var sessionId = this.PrefixIdentifier(id);
            Log.TraceFormat("ReleaseItemExclusiveAsync called for item {0} with lockid {1}.", sessionId, lockId);
            var get = await Bucket.GetAsync<SessionStateItem>(sessionId, _transcoder).ConfigureAwait(false);
            var item = get.Value;


            if (get.Success && item != null && lockId != null && (uint) lockId == item.LockId)
            {
                item.Locked = false;
                item.LockId = 0;

                var upsert = await Bucket.UpsertAsync(sessionId, item, item.Timeout, _transcoder).ConfigureAwait(false);
                if (!upsert.Success)
                {
                    LogAndOrThrow(upsert, sessionId);
                }
            }
            else
            {
                LogAndOrThrow(get, sessionId);
            }
        }

        public override async Task RemoveItemAsync(HttpContextBase context, string id, object lockId, SessionStateStoreData item,
            CancellationToken cancellationToken)
        {
            var sessionId = this.PrefixIdentifier(id);
            Log.TraceFormat("RemoveItemAsync called for item {0} with lockid {1}.", sessionId, lockId);
            var removed = await Bucket.RemoveAsync(sessionId).ConfigureAwait(false);
            if (!removed.Success)
            {
                LogAndOrThrow(removed, sessionId);
            }
        }

        public override async Task ResetItemTimeoutAsync(HttpContextBase context, string id, CancellationToken cancellationToken)
        {
            var sessionId = this.PrefixIdentifier(id);
            Log.TraceFormat("ResetItemTimeoutAsync called for item {0}.", sessionId);
            var touched = await Bucket.TouchAsync(sessionId, Config.Timeout).ConfigureAwait(false);
            if (!touched.Success)
            {
                LogAndOrThrow(touched, sessionId);
            }
        }

        public override async Task SetAndReleaseItemExclusiveAsync(HttpContextBase context, string id, SessionStateStoreData item, object lockId,
            bool newItem, CancellationToken cancellationToken)
        {
            var sessionId = this.PrefixIdentifier(id);
            Log.TraceFormat("SetAndReleaseItemExclusiveAsync called for item {0}.", sessionId);
            if (newItem)
            {
                var insert = await Bucket.InsertAsync(sessionId, new SessionStateItem
                {
                    Actions = SessionStateActions.None,
                    LockId = (uint?) lockId ?? 0,
                    SessionItems = Serialize(item.Items)
                }, Config.Timeout, _transcoder).ConfigureAwait(false);

                if (!insert.Success)
                {
                    LogAndOrThrow(insert, sessionId);
                }
            }
            else
            {
                var upsert = await Bucket.UpsertAsync(sessionId, new SessionStateItem
                {
                    LockId = (uint?)lockId ?? 0,
                    Actions = SessionStateActions.None,
                    SessionItems = Serialize(item.Items)
                }, Config.Timeout, _transcoder).ConfigureAwait(false);
                if (!upsert.Success)
                {
                    LogAndOrThrow(upsert, sessionId);
                }
            }

            await ReleaseItemExclusiveAsync(context, id, lockId, cancellationToken).ConfigureAwait(false);
        }

        public override bool SetItemExpireCallback(SessionStateItemExpireCallback expireCallback)
        {
            return false;
        }

        internal SessionStateStoreData GetSessionStateStoreData(byte[] buffer, HttpContextBase context, int timeout)
        {
            return new SessionStateStoreData(Deserialize(buffer),
                SessionStateUtility.GetSessionStaticObjects(context.ApplicationInstance.Context),
                timeout);
        }

        #region Utility methods

        public byte[] Serialize(ISessionStateItemCollection items)
        {
            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);
                ((SessionStateItemCollection)items).Serialize(writer);
                return ms.ToArray();
            }
        }

        public ISessionStateItemCollection Deserialize(byte[] bytes)
        {
            if (bytes == null) return new SessionStateItemCollection();
            using (var ms = new MemoryStream(bytes))
            {
                var reader = new BinaryReader(ms);
                return SessionStateItemCollection.Deserialize(reader);
            }
        }

        /// <summary>
        /// Logs the reason why an operation fails and throws and exception if <see cref="ThrowOnError"/> is
        /// <c>true</c> and logging the issue as WARN.
        /// </summary>
        /// <param name="e">The e.</param>
        /// <param name="key">The key.</param>
        /// <exception cref="CouchbaseSessionStateException"></exception>
        private void LogAndOrThrow(Exception e, string key)
        {
            Log.Error($"Could not retrieve, remove or write key '{key}' - reason: {e}");
            if (ThrowOnError)
            {
                throw new CouchbaseSessionStateException($"Could not retrieve, remove or write key '{key}'", e);
            }
        }

        /// <summary>
        /// Logs the reason why an operation fails and throws and exception if <see cref="ThrowOnError"/> is
        /// <c>true</c> and logging the issue as WARN.
        /// </summary>
        /// <param name="result">The result.</param>
        /// <param name="key">The key.</param>
        /// <exception cref="InvalidOperationException"></exception>
        private void LogAndOrThrow(IOperationResult result, string key)
        {
            if (result.Exception != null)
            {
                LogAndOrThrow(result.Exception, key);
                return;
            }
            Log.Error($"Could not retrieve, remove or write key '{key}' - reason: {result.Status}");
            if (ThrowOnError)
            {
                throw new InvalidOperationException(result.Status.ToString());
            }
        }

        #endregion
    }
}

#region [ License information          ]
/* ************************************************************
 *
 *    @author Couchbase <info@couchbase.com>
 *    @copyright 2017 Couchbase, Inc.
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/
#endregion
