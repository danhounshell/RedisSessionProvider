using Moq;
using RedisSessionProvider.Config;
using StackExchange.Redis;
using System.Web;
using System.Configuration;

namespace RedisSessionProviderUnitTests.Integration
{
    using NUnit.Framework;
    using RedisSessionProvider;
    using System;


    [TestFixture]
    public class ExpirationTests
    {
        private static string REDIS_SERVER = ConfigurationManager.AppSettings["REDIS_SERVER"];
        private static int REDIS_INDEX = int.Parse(ConfigurationManager.AppSettings["REDIS_INDEX"]);
		private static string REDIS_PORT = ConfigurationManager.AppSettings["REDIS_PORT"];
	    private static string REDIS_CONFIG = string.Format("{0}:{1}", REDIS_SERVER, REDIS_PORT);
        private static TimeSpan TIMEOUT = new TimeSpan(1, 0, 0);
        private static string SESSION_ID = "SESSION_ID";

        static ConfigurationOptions _redisConfigOpts;

        private IDatabase db;

        [SetUp]
        public void OnBeforeTestExecute()
        {
            _redisConfigOpts = ConfigurationOptions.Parse(REDIS_CONFIG);
            RedisConnectionConfig.GetSERedisServerConfigDbIndex = @base => new Tuple<string, int, ConfigurationOptions>(
                "SessionConnection", REDIS_INDEX, _redisConfigOpts);
            RedisSessionConfig.SessionTimeout = TIMEOUT;

            // StackExchange Redis client
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(REDIS_CONFIG);
            db = redis.GetDatabase(REDIS_INDEX);
        }

        [Test]
        public void ExpirationSet_AsExpected()
        {

            var mockHttpContext = new Mock<HttpContextBase>();
            var mockHttpRequest = new Mock<HttpRequestBase>();
            mockHttpRequest.Setup(x => x.Cookies).Returns(new HttpCookieCollection()
                                                          {
                                                              new HttpCookie(RedisSessionConfig.SessionHttpCookieName, SESSION_ID)
                                                          });
            mockHttpContext.Setup(x => x.Request).Returns(mockHttpRequest.Object);


            using (var sessAcc = new RedisSessionAccessor(mockHttpContext.Object))
            {
                sessAcc.Session["MyKey"] = DateTime.UtcNow;
            }

            // Assert directly using Stackexchange.Redis
            var ttl = db.KeyTimeToLive(SESSION_ID);
            
            // We should not have a null here
            Assert.IsNotNull(ttl);
            Assert.IsTrue(ttl.Value <= TIMEOUT);
            Assert.IsTrue(ttl.Value.Minutes > 0);

        }

        [TearDown]
        public void OnAfterTestExecute()
        {
            // cleanup
            db.KeyDelete(SESSION_ID);
        }
    }
}
