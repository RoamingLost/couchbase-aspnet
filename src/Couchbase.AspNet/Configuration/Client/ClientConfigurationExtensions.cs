using System.Linq;
using Couchbase.AspNet.Authentication;

namespace Couchbase.AspNet.Configuration.Client
{
    public static class ClientConfigurationExtensions
    {
        public static ClusterOptions ToClusterOptions(this ClientConfiguration config)
        {
            var passwordAuthenticator = config.Authenticator as PasswordAuthenticator;
            return new ClusterOptions
            {
                ConnectionString = string.Join(",", config.Servers.Select(x => x.ToString().TrimEnd('/'))),
                UserName = passwordAuthenticator?.Username,
                Password = passwordAuthenticator?.Password,
                Buckets = config.BucketConfigs.Keys.ToList()
            };
        }
    }
}
