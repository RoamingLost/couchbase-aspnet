using System;
using System.Threading.Tasks;
using System.Web;
using Couchbase.AspNet.Caching;
using Couchbase.AspNet.IO;
using Couchbase.AspNet.Utils;
using Couchbase.Core;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Core.IO.Transcoders;
using Couchbase.KeyValue;
using Moq;
using Xunit;

namespace Couchbase.AspNet.UnitTests
{
    public class CouchbaseOutputCacheProviderTests
    {
        #region Get tests

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" ", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        public void Get_When_Key_IsNullEmptyOrSpace_DoNot_Throw_ArgumentException_If_ThrowOnError(string key,
            bool throwOnError)
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>())).Throws<MissingKeyException>();

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object);

            var provider = new CouchbaseOutputCacheProvider(bucket.Object)
            {
                ThrowOnError = throwOnError
            };
            if (throwOnError)
            {
                Assert.Throws<ArgumentException>(() => provider.Get(key));
            }
            else
            {
                var result = provider.Get(key);
                Assert.Null(result);
            }
        }

        [Theory]
        [InlineData(true)]
        public void Get_When_Key_DoesNotExist_Return_Null(bool throwOnError)
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>())).Throws(new DocumentNotFoundException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object);

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = throwOnError};
            Assert.Null(provider.Get("thekey"));
        }

        [Fact]
        public void Get_When_Operation_Causes_Exception_Throw_Exception()
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>())).Throws(new CouchbaseOutputCacheException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object);

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = true};
            Assert.Throws<CouchbaseOutputCacheException>(() => provider.Get("thekey"));
        }

        [Fact]
        public void Get_When_Operation_Causes_Exception_Throw_CouchbaseCacheException()
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>())).Throws(new OutOfMemoryException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object);

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = true};
            Assert.Throws<CouchbaseOutputCacheException>(() => provider.Get("thekey"));
        }

        #endregion

        #region Set tests

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData(" ", true)]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData(" ", false)]
        public void Set_When_Key_IsNullEmptyOrSpace_Throw_ArgumentException(string key, bool throwOneError)
        {
            var provider = new CouchbaseOutputCacheProvider(null) {ThrowOnError = throwOneError};

            if (throwOneError)
            {
                Assert.Throws<ArgumentException>(() => provider.Set(null, null, DateTime.Now));
            }
        }

        [Fact]
        public void Set_When_Operation_Causes_Exception_Throw_CouchbaseCacheException()
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>())).Throws(new Core.Exceptions.TimeoutException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object);

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = true};
            Assert.Throws<CouchbaseOutputCacheException>(() => provider.Set("thekey", new object(), DateTime.Now));
        }

        [Fact]
        public void Set_When_Operation_Fails_Throw_CouchbaseCacheException()
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.UpsertAsync<dynamic>(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<UpsertOptions>())).Throws(new OutOfMemoryException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object);

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = true};
            Assert.Throws<CouchbaseOutputCacheException>(() => provider.Set("thekey", "thevalue", DateTime.MaxValue));
        }

        #endregion

        #region Add tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_When_Operation_Fails_Throw_CouchbaseCacheException(bool throwOnError)
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>())).Throws(new DocumentNotFoundException());
            collection.Setup(x => x.InsertAsync<dynamic>(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<InsertOptions>())).Throws(new OutOfMemoryException());

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object);

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = throwOnError};

            if (throwOnError)
            {
                Assert.Throws<CouchbaseOutputCacheException>(() => provider.Add("thekey", "thevalue", DateTime.MaxValue));
            }
            else
            {
                Assert.Null(provider.Add("thekey", "thevalue", DateTime.MaxValue));
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_When_Key_DoesNotExst_Inserts_Value(bool throwOnError)
        {
            var result = new Mock<IGetResult>();
            result.Setup(x => x.ContentAs<dynamic>()).Returns("value").Verifiable();
            var mutation = new Mock<IMutationResult>();
            mutation.Setup(x => x.Cas).Returns(1);

            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>())).Throws(new DocumentNotFoundException()).Verifiable();
            collection.Setup(x => x.InsertAsync<dynamic>(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<InsertOptions>())).Returns(Task.FromResult(mutation.Object));

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object).Verifiable();

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = throwOnError};
            var val = provider.Add("key", "value", DateTime.MaxValue);
            Assert.NotNull(val);
            bucket.Verify();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Add_When_Key_Exists_Gets_Value(bool throwOnError)
        {
            var get = new Mock<IGetResult>();
            get.Setup(x => x.Cas).Returns(1);
            get.Setup(x => x.ContentAs<byte[]>()).Returns(SerializationUtil.Serialize("value"));

            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<GetOptions>())).Returns(Task.FromResult(get.Object));

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object).Verifiable();

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = throwOnError};
            var val = provider.Add("key", "value", DateTime.MaxValue);
            Assert.NotNull(val);
            bucket.Verify();
        }

        #endregion

        #region Remove tests

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Remove_When_Operation_Fails_Throw_CouchbaseCacheException(bool throwOnError)
        {
            var collection = new Mock<ICouchbaseCollection>();
            collection.Setup(x => x.RemoveAsync(It.IsAny<string>(), It.IsAny<RemoveOptions>())).Throws<DocumentNotFoundException>();

            var bucket = new Mock<IBucket>();
            bucket.Setup(x => x.DefaultCollection()).Returns(collection.Object).Verifiable();

            var provider = new CouchbaseOutputCacheProvider(bucket.Object) {ThrowOnError = throwOnError};

            if (throwOnError)
            {
                Assert.Throws<CouchbaseOutputCacheException>(() => provider.Remove("thekey"));
            }
            else
            {
                provider.Remove("thekey");
            }
        }
        #endregion
    }
}
