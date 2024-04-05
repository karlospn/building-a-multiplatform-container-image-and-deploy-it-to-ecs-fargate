using Amazon.CDK;
using Cdk.Constructs;
using Constructs;

namespace Cdk.Stacks
{
    public class CdkStack : Stack
    {
        internal CdkStack(Construct scope, 
            string id, 
            IStackProps props = null) 
            : base(scope, id, props)
        {
            var vpc = new VpcConstruct(this,
                "vpc-construct");

            var fg = new FargateClusterConstruct(this,
                "fargate-cluster-construct",
                vpc.Vpc);

            var publicAlb = new PublicLoadBalancerConstruct(this,
                "pub-alb-construct",
                vpc.Vpc);

            var arm64Service = new EcsServiceArm64AppConstruct(this,
                "ecs-arm64-service-construct-",
                vpc.Vpc,
                fg.Cluster,
                publicAlb.Alb);

            var amd64Service = new EcsServiceAmd64AppConstruct(this,
                "ecs-amd64-service-construct-",
                vpc.Vpc,
                fg.Cluster,
                publicAlb.Alb);

            _ = new TargetGroupConstruct(this,
                "alb-tg-construct-signalr-core-demo",
                vpc.Vpc,
                publicAlb.Alb,
                amd64Service.FargateService,
                arm64Service.FargateService);
        }
    }
}
