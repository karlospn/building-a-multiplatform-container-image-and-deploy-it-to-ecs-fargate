using Amazon.CDK;
using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;
using HealthCheck = Amazon.CDK.AWS.ElasticLoadBalancingV2.HealthCheck;

namespace Cdk.Constructs
{
    public class TargetGroupConstruct : Construct
    {
        public TargetGroupConstruct(Construct scope,
            string id,
            Vpc vpc,
            ApplicationLoadBalancer pubAlb,
            FargateService arm64Service,
            FargateService amd64Service)
            : base(scope, id)
        {
            CreateTargetGroupForArm64App(vpc, pubAlb, arm64Service);
            CreateTargetGroupForAmd64App(vpc, pubAlb, amd64Service);
        }

        private void CreateTargetGroupForArm64App(Vpc vpc,
          ApplicationLoadBalancer alb,
          FargateService service)
        {
            var targetGroup = new ApplicationTargetGroup(this,
                "tg-arm64-app",
                new ApplicationTargetGroupProps
                {
                    TargetGroupName = "tg-arm64-app",
                    Vpc = vpc,
                    TargetType = TargetType.IP,
                    ProtocolVersion = ApplicationProtocolVersion.HTTP1,
                    Protocol = ApplicationProtocol.HTTP,
                    HealthCheck = new HealthCheck
                    {
                        Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP,
                        HealthyThresholdCount = 3,
                        Path = "/health",
                        Port = "8080",
                        Interval = Duration.Millis(10000),
                        Timeout = Duration.Millis(8000),
                        UnhealthyThresholdCount = 10,
                        HealthyHttpCodes = "200"
                    },
                    Port = 8080,
                    Targets = new IApplicationLoadBalancerTarget[] { service }
                });

            alb.Listeners[0].AddTargetGroups(
                "app-8080-listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { targetGroup }
                });
        }

        private void CreateTargetGroupForAmd64App(Vpc vpc,
            ApplicationLoadBalancer alb,
            FargateService service)
        {
            var targetGroup = new ApplicationTargetGroup(this,
                "tg-amd64-app",
                new ApplicationTargetGroupProps
                {
                    TargetGroupName = "tg-amd64-app",
                    Vpc = vpc,
                    TargetType = TargetType.IP,
                    ProtocolVersion = ApplicationProtocolVersion.HTTP1,
                    Protocol = ApplicationProtocol.HTTP,
                    HealthCheck = new HealthCheck
                    {
                        Protocol = Amazon.CDK.AWS.ElasticLoadBalancingV2.Protocol.HTTP,
                        HealthyThresholdCount = 3,
                        Path = "/health",
                        Port = "8080",
                        Interval = Duration.Millis(10000),
                        Timeout = Duration.Millis(8000),
                        UnhealthyThresholdCount = 10,
                        HealthyHttpCodes = "200"
                    },
                    Port = 8081,
                    Targets = new IApplicationLoadBalancerTarget[] { service }
                });

            alb.Listeners[1].AddTargetGroups(
                "app-8081-listener",
                new AddApplicationTargetGroupsProps
                {
                    TargetGroups = new IApplicationTargetGroup[] { targetGroup }
                });
        }
    }
}