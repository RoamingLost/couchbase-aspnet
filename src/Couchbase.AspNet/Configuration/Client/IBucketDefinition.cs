﻿using System;

namespace Couchbase.AspNet.Configuration.Client
{
    /// <summary>
    /// Abstracts a configuration definition which can be used to construct a <see cref="ClientConfiguration"/> as
    /// part of a <see cref="ICouchbaseClientDefinition"/>.
    /// </summary>
    public interface IBucketDefinition
    {
        /// <summary>
        /// A value indicating whether to use enhanced durability if the
        /// Couchbase server version supports it; if it's not supported the client will use
        /// Observe for Endure operations.
        /// </summary>
        /// <value>
        /// <c>true</c> to use enhanced durability; otherwise, <c>false</c>.
        /// </value>
        bool UseEnhancedDurability { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to get an <see cref="ErrorMap"/> to get additional error information
        /// for unknown errors returned from the server.
        /// </summary>
        /// <value>
        /// <c>true</c> to use kv error map; otherwise, <c>false</c>.
        /// </value>
        [Obsolete("KV Error Map are always enabled if available.")]
        bool UseKvErrorMap { get; }

        /// <summary>
        /// If true, use Secure Socket Layers (SSL) to encrypt traffic between the client and Couchbase server.
        /// </summary>
        /// <remarks>Requires the SSL certificate to be stored in the local Certificate Authority to enable SSL.</remarks>
        /// <remarks>This feature is only supported by Couchbase Cluster 3.0 and greater.</remarks>
        /// <remarks>If the parent <see cref="ICouchbaseClientDefinition"/>'s UseSSL is false, setting this to true will override that configuration and enable the Bucket to use SSL.</remarks>
        bool UseSsl { get; }

        /// <summary>
        /// The name of the Bucket.
        /// </summary>
        /// <remarks>The name can be set within the Couchbase Management Console.</remarks>
        string Name { get; }

        /// <summary>
        /// The password used to connect to the bucket.
        /// </summary>
        /// <remarks>The password can be set within the Couchbase Management Console.</remarks>
        string Password { get; }

        /// <summary>
        /// The connection pool settings, which override the settings in <see cref="ICouchbaseClientDefinition.ConnectionPool"/>.
        /// </summary>
        /// <remarks>The default settings are: MinSize=1, MaxSize=2, WaitTimout=2500ms, ShutdownTimeout=10000ms.</remarks>
        IConnectionPoolDefinition ConnectionPool { get; }

        /// <summary>
        /// The max time an observe operation will take before timing out.
        /// </summary>
        int ObserveInterval { get; }

        /// <summary>
        /// The interval between each observe attempt.
        /// </summary>
        int ObserveTimeout { get; }

        /// <summary>
        /// The operation lifespan, maximum time in milliseconds allowed for an operation to run.
        /// </summary>
        uint? OperationLifespan { get; }
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
