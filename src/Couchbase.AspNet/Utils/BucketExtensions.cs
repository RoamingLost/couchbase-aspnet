using System;
using System.Threading.Tasks;
using Couchbase.AspNet.IO;
using Couchbase.Core.Exceptions.KeyValue;
using Couchbase.Core.IO.Transcoders;
using Couchbase.KeyValue;

namespace Couchbase.AspNet.Utils
{
    public static class BucketExtensions
    {
        public static IOperationResult<T> Get<T>(this IBucket bucket, string key, ITypeTranscoder transcoder) where T : class
        {
            return AsyncHelper.RunSync(() => GetAsync<T>(bucket, key, transcoder));
        }

        public static async Task<IOperationResult<T>> GetAsync<T>(this IBucket bucket, string key, ITypeTranscoder transcoder) where T : class
        {
            try
            {
                var result = await bucket.DefaultCollection().GetAsync(key, new GetOptions().Transcoder(transcoder));
                var bytes = result.ContentAs<byte[]>();
                var value = SerializationUtil.Deserialize<T>(bytes);
                return new OperationResult<T>
                {
                    Id = key,
                    Success = true,
                    Status = ResponseStatus.Success,
                    Message = ResponseStatus.Success.ToString(),
                    Cas = result.Cas,
                    Value = value,
                };
            }
            catch (DocumentNotFoundException notFoundEx)
            {
                return new OperationResult<T>
                {
                    Id = key,
                    Success = false,
                    Status = ResponseStatus.KeyNotFound,
                    Message = ResponseStatus.KeyNotFound.ToString(),
                    Exception = notFoundEx
                };
            }
            catch (Exception ex)
            {
                return new OperationResult<T>
                {
                    Id = key,
                    Success = false,
                    Status = ResponseStatus.ClientFailure,
                    Message = ex.Message,
                    Exception = ex
                };
            }
        }

        public static IOperationResult<T> Insert<T>(this IBucket bucket, string key, T value, TimeSpan timeout, ITypeTranscoder transcoder) where T : class
        {
            return AsyncHelper.RunSync(() => InsertAsync<T>(bucket, key, value, timeout, transcoder));
        }

        public static async Task<IOperationResult<T>> InsertAsync<T>(this IBucket bucket, string key, T value, TimeSpan timeout, ITypeTranscoder transcoder) where T : class
        {
            try
            {
                var bytes = SerializationUtil.Serialize(value);
                var result = await bucket.DefaultCollection().InsertAsync(key, bytes, new InsertOptions().Transcoder(transcoder).Expiry(timeout));
                return new OperationResult<T>
                {
                    Id = key,
                    Success = true,
                    Status = ResponseStatus.Success,
                    Message = ResponseStatus.Success.ToString(),
                    Cas = result.Cas,
                    Value = value
                };
            }
            catch (Exception ex)
            {
                return new OperationResult<T>
                {
                    Id = key,
                    Success = false,
                    Status = ResponseStatus.UnknownError,
                    Message = ex.Message,
                    Exception = ex
                };
            }
        }

        public static IOperationResult<T> Upsert<T>(this IBucket bucket, string key, T value, TimeSpan timeout, ITypeTranscoder transcoder) where T : class
        {
            return AsyncHelper.RunSync(() => UpsertAsync<T>(bucket, key, value, timeout, transcoder));
        }

        public static async Task<IOperationResult<T>> UpsertAsync<T>(this IBucket bucket, string key, T value, TimeSpan timeout, ITypeTranscoder transcoder) where T : class
        {
            try
            {
                var bytes = SerializationUtil.Serialize(value);
                var result = await bucket.DefaultCollection().UpsertAsync(key, bytes, new UpsertOptions().Transcoder(transcoder).Expiry(timeout));
                return new OperationResult<T>
                {
                    Id = key,
                    Success = true,
                    Status = ResponseStatus.Success,
                    Message = ResponseStatus.Success.ToString(),
                    Cas = result.Cas,
                    Value = value
                };
            }
            catch (Exception ex)
            {
                return new OperationResult<T>
                {
                    Id = key,
                    Success = false,
                    Status = ResponseStatus.UnknownError,
                    Message = ex.Message,
                    Exception = ex
                };
            }
        }

        public static IOperationResult Remove(this IBucket bucket, string key)
        {
            return AsyncHelper.RunSync(() => RemoveAsync(bucket, key));
        }

        public static async Task<IOperationResult> RemoveAsync(this IBucket bucket, string key)
        {
            try
            {
                await bucket.DefaultCollection().RemoveAsync(key);
                return new OperationResult
                {
                    Id = key,
                    Success = true,
                    Status = ResponseStatus.Success,
                    Message = ResponseStatus.Success.ToString()
                };
            }
            catch (Exception ex)
            {
                return new OperationResult
                {
                    Id = key,
                    Success = false,
                    Status = ResponseStatus.UnknownError,
                    Message = ex.Message,
                    Exception = ex
                };
            }
        }

        public static IOperationResult Touch(this IBucket bucket, string key, TimeSpan timeout)
        {
            return AsyncHelper.RunSync(() => TouchAsync(bucket, key, timeout));
        }

        public static async Task<IOperationResult> TouchAsync(this IBucket bucket, string key, TimeSpan timeout)
        {
            try
            {
                await bucket.DefaultCollection().TouchAsync(key, timeout);
                return new OperationResult
                {
                    Id = key,
                    Success = true,
                    Status = ResponseStatus.Success,
                    Message = ResponseStatus.Success.ToString()
                };
            }
            catch (Exception ex)
            {
                return new OperationResult
                {
                    Id = key,
                    Success = false,
                    Status = ResponseStatus.UnknownError,
                    Message = ex.Message,
                    Exception = ex
                };
            }
        }
    }
}
