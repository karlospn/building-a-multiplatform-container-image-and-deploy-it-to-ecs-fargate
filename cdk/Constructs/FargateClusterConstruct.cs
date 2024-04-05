using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECS;
using Constructs;

namespace Cdk.Constructs
{
    public class FargateClusterConstruct : Construct
    {
        public Cluster Cluster { get; }

        public FargateClusterConstruct(Construct scope,
            string id,
            IVpc vpc)
            : base(scope, id)
        {
            Cluster = new Cluster(this,
                "fargate-cluster",
                new ClusterProps
                {
                    Vpc = vpc,
                    ClusterName = "fargate-cluster-multiplatform-images-demo"
                });
        }
    }
}