using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Core.Application.Pipelines.Caching
{
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>, ICachableRequest
    {
        private readonly CacheSettings _cacheSettings;
        private readonly IDistributedCache _cache;

        public CachingBehavior(CacheSettings cacheSettings, IDistributedCache cache, IConfiguration configuration)
        {
            _cacheSettings = configuration.GetSection("CacheSettings").Get<CacheSettings>() ?? throw new InvalidOperationException();
            _cache = cache;
        }



        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request.BypassCache)
            {
                return await next();
            }
            TResponse response;
            byte[]? cachedResponse = await _cache.GetAsync(request.CacheKey, cancellationToken);
            if (cachedResponse != null)
            {
                string cachedResponseString = Encoding.Default.GetString(cachedResponse);
                response = JsonConvert.DeserializeObject<TResponse>(cachedResponseString);
            }
            else
            {
                response = await GetResponseAndAddToCache(request, next, cancellationToken);
            }
            return response;
        }

        private async Task<TResponse> GetResponseAndAddToCache(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            TResponse response = await next();

            TimeSpan slidingExpiration = request.SlidingExpiration ?? TimeSpan.FromDays(_cacheSettings.SlidingExpiration);
            DistributedCacheEntryOptions cacheOptions = new() { SlidingExpiration = slidingExpiration };

            string serializedData = JsonConvert.SerializeObject(response);
            byte[] serializedDataBytes = Encoding.UTF8.GetBytes(serializedData);

            await _cache.SetAsync(request.CacheKey, serializedDataBytes, cacheOptions, cancellationToken);

            return response;
        }
    }
}
