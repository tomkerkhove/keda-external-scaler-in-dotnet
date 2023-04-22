using Generated.KEDA.Specs;
using Grpc.Core;

namespace KEDA.Templates.ExternalScaler.Grpc
{
    public class ExternalScalerGrpcService : Generated.KEDA.Specs.ExternalScaler.ExternalScalerBase
    {
        private const string MetricName = "<metric-name>";

        public override async Task<IsActiveResponse> IsActive(ScaledObjectRef request, ServerCallContext context)
        {
            // request.Scalermetadata contain<s the `metadata` defined in the ScaledObject
            if (!request.ScalerMetadata.ContainsKey("latitude") ||
                !request.ScalerMetadata.ContainsKey("longitude"))
            {
                throw new ArgumentException("longitude and latitude must be specified");
            }

            return new IsActiveResponse
            {
                // return true if there is more than 2 Earthquakes with mag > 1.0
                Result =true
            };
        }
        
        public override async Task StreamIsActive(ScaledObjectRef request,
            IServerStreamWriter<IsActiveResponse> responseStream, ServerCallContext context)
        {
            if (!request.ScalerMetadata.ContainsKey("latitude") ||
                !request.ScalerMetadata.ContainsKey("longitude"))
            {
                throw new ArgumentException("longitude and latitude must be specified");
            }
        
            while (!context.CancellationToken.IsCancellationRequested)
            {
                var response = new IsActiveResponse
                {
                    Result = true
                };
                await responseStream.WriteAsync(response);
        
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }
        
        public override Task<GetMetricSpecResponse> GetMetricSpec(ScaledObjectRef request, ServerCallContext context)
        {
            var resp = new GetMetricSpecResponse();
        
            resp.MetricSpecs.Add(new MetricSpec
            {
                MetricName = MetricName,
                TargetSize = 10
            });
        
            return Task.FromResult(resp);
        }

        public override async Task<GetMetricsResponse> GetMetrics(GetMetricsRequest request, ServerCallContext context)
        {
        
            var resp = new GetMetricsResponse();
            resp.MetricValues.Add(new MetricValue
            {
                MetricName = MetricName,
                MetricValue_ = 1
            });
        
            return resp;
        }
    }
}