using System.Collections.Concurrent;
using Externalscaler;
using ExternalScalerSample;
using Grpc.Core;
using Newtonsoft.Json;

public class ExternalScalerService : ExternalScaler.ExternalScalerBase
{
    private static readonly HttpClient _client = new HttpClient();

    private static readonly ConcurrentDictionary<string, ConcurrentBag<IServerStreamWriter<IsActiveResponse>>> _streams =
        new();

    async Task<int> GetEarthQuakeCount(string longitude, string latitude)
    {
        var startTime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
        var endTime = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var radiusKm = 500;
        var query = $"format=geojson&starttime={startTime}&endtime={endTime}&longitude={longitude}&latitude={latitude}&maxradiuskm={radiusKm}";

        var resp = await _client.GetAsync($"https://earthquake.usgs.gov/fdsnws/event/1/query?{query}");
        resp.EnsureSuccessStatusCode();
        var payload = JsonConvert.DeserializeObject<USGSResponse>(await resp.Content.ReadAsStringAsync());
        return payload.features.Count(f => f.properties.mag > 1.0);
    }

  public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
  {
    // request.Scalermetadata contains the `metadata` defined in the ScaledObject
    if (!request.ScalerMetadata.ContainsKey("latitude") ||
      !request.ScalerMetadata.ContainsKey("longitude")) {
      throw new ArgumentException("longitude and latitude must be specified");
    }

    var longitude = request.ScalerMetadata["longitude"];
    var latitude = request.ScalerMetadata["latitude"];
    var startTime = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd");
    var endTime = DateTime.UtcNow.ToString("yyyy-MM-dd");
    var radiusKm = 500;
    var query = $"format=geojson&starttime={startTime}&endtime={endTime}&longitude={longitude}&latitude={latitude}&maxradiuskm={radiusKm}";

    var resp = await _client.GetAsync($"https://earthquake.usgs.gov/fdsnws/event/1/query?{query}");
    resp.EnsureSuccessStatusCode();
    var payload = JsonConvert.DeserializeObject<USGSResponse>(await resp.Content.ReadAsStringAsync());

    return new IsActiveResponse
    {
      // return true if there is more than 2 Earthquakes with mag > 1.0
      Result = payload.features.Count(f => f.properties.mag > 1.0) > 2
    };
  }
  
  public override async Task StreamIsActive(ScaledObjectRef request, IServerStreamWriter<IsActiveResponse> responseStream, ServerCallContext context)
  {
    if (!request.ScalerMetadata.ContainsKey("latitude") ||
        !request.ScalerMetadata.ContainsKey("longitude"))
    {
      throw new ArgumentException("longitude and latitude must be specified");
    }

    var longitude = request.ScalerMetadata["longitude"];
    var latitude = request.ScalerMetadata["latitude"];
    var key = $"{longitude}|{latitude}";

    while (!context.CancellationToken.IsCancellationRequested)
    {
      var earthquakeCount = await GetEarthQuakeCount(longitude, latitude);
      if (earthquakeCount > 2) {
        await responseStream.WriteAsync(new IsActiveResponse
        {
          Result = true
        });
      }

      await Task.Delay(TimeSpan.FromHours(1));
    }
  }
  
  public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
  {
    var resp = new GetMetricSpecResponse();

    resp.MetricSpecs.Add(new MetricSpec
    {
      MetricName = "earthquakeThreshold",
      TargetSize = 10
    });

    return Task.FromResult(resp);
  }
  
  public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
  {
    if (!request.ScaledObjectRef.ScalerMetadata.ContainsKey("latitude") ||
        !request.ScaledObjectRef.ScalerMetadata.ContainsKey("longitude"))
    {
      throw new ArgumentException("longitude and latitude must be specified");
    }

    var longitude = request.ScaledObjectRef.ScalerMetadata["longitude"];
    var latitude = request.ScaledObjectRef.ScalerMetadata["latitude"];

    var earthquakeCount = await GetEarthQuakeCount(longitude, latitude);

    var resp = new GetMetricsResponse();
    resp.MetricValues.Add(new MetricValue
    {
      MetricName = "earthquakeThreshold",
      MetricValue_ = earthquakeCount
    });

    return resp;
  }
}