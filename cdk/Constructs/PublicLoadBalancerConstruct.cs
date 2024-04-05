using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;

namespace Cdk.Constructs
{
    public class PublicLoadBalancerConstruct : Construct
    {
        public ApplicationLoadBalancer Alb { get; }

        public PublicLoadBalancerConstruct(Construct scope,
            string id,
            IVpc vpc)
            : base(scope, id)
        {
            var securityGroup = new SecurityGroup(this,
                "sg-pub-alb",
                new SecurityGroupProps()
                {
                    Vpc = vpc,
                    AllowAllOutbound = true,
                    Description = "Security group for the public ALB",
                    SecurityGroupName = "sg-pub-alb"
                });

            securityGroup.AddIngressRule(Peer.AnyIpv4(),
                Port.Tcp(8080),
                "Allow port 8080 ingress traffic");

            securityGroup.AddIngressRule(Peer.AnyIpv4(),
                Port.Tcp(8081),
                "Allow port 8081 ingress traffic");

            Alb = new ApplicationLoadBalancer(this,
                "alb",
                new ApplicationLoadBalancerProps
                {
                    InternetFacing = true,
                    Vpc = vpc,
                    VpcSubnets = new SubnetSelection
                    {
                        OnePerAz = true,
                        SubnetType = SubnetType.PUBLIC,
                    },
                    SecurityGroup = securityGroup,
                    LoadBalancerName = "alb-pub-multiplat-demo"
                });

            _ = Alb.AddListener("alb-http-listener-8080", new ApplicationListenerProps
            {
                Protocol = ApplicationProtocol.HTTP,
                LoadBalancer = Alb,
                DefaultAction = ListenerAction.FixedResponse(500),
                Port = 8080
            });

            _ = Alb.AddListener("alb-http-listener-8081", new ApplicationListenerProps
            {
                Protocol = ApplicationProtocol.HTTP,
                LoadBalancer = Alb,
                DefaultAction = ListenerAction.FixedResponse(500),
                Port = 8081
            });
        }
    }
}