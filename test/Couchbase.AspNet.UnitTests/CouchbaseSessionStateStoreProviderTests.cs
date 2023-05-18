
using System;
using System.Web;
using Couchbase.AspNet.IO;
using Couchbase.AspNet.Session;
using Couchbase.AspNet.Utils;
using Couchbase.Core;
using Couchbase.Core.IO.Transcoders;
using Couchbase.KeyValue;
using Moq;
using Xunit;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Core.Exceptions;
using System.Threading.Tasks;

namespace Couchbase.AspNet.UnitTests
{
    public class CouchbaseSessionStateStoreProviderTests
    {
        [Fact]
        public void CreateUnintilizedItem_ThrownOnError_Is_True_And_Success_Does_Not_Throw()
        {
            var result = new Mock<IMutationResult>();
            result.Setup(x => x.Cas).Returns(1);

            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.InsertAsync("testId", It.IsAny<byte[]>(), It.IsAny<InsertOptions>()))
                .Returns(Task.FromResult(result.Object));

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object).Verifiable();

            var provider = new CouchbaseSessionStateProvider {Bucket = bucket.Object, ThrowOnError = true};
            provider.CreateUninitializedItem(HttpContext.Current, "testId", 20);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateUnintilizedItem_ThrownOnError_Is_True_And_Fail_Throws_CouchbaseSessionStateException(bool thrownOnError)
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.InsertAsync("testId", It.IsAny<SessionStateItem>(), It.IsAny<InsertOptions>())).Throws<DocumentExistsException>();

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object).Verifiable();

            var provider = new CouchbaseSessionStateProvider { Bucket = bucket.Object, ThrowOnError = thrownOnError };

            if (thrownOnError)
            {
                Assert.Throws<CouchbaseSessionStateException>(() =>
                    provider.CreateUninitializedItem(HttpContext.Current, "testId", 20));
            }
        }
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
