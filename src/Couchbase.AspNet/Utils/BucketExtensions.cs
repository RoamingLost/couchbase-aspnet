using System;
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
            try
            {
                var result = bucket.DefaultCollection().GetAsync(key, new GetOptions().Transcoder(transcoder)).GetAwaiter().GetResult();
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
            try
            {
                var bytes = SerializationUtil.Serialize(value);
                var result = bucket.DefaultCollection().InsertAsync(key, bytes, new InsertOptions().Transcoder(transcoder).Expiry(timeout)).GetAwaiter().GetResult();
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
            try
            {
                var bytes = SerializationUtil.Serialize(value);
                var result = bucket.DefaultCollection().UpsertAsync(key, bytes, new UpsertOptions().Transcoder(transcoder).Expiry(timeout)).GetAwaiter().GetResult();
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
            try
            {
                bucket.DefaultCollection().RemoveAsync(key).GetAwaiter().GetResult();
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
