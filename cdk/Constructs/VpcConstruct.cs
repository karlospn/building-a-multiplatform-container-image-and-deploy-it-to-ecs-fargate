using Amazon.CDK.AWS.EC2;
using Constructs;

namespace Cdk.Constructs
{
    public class VpcConstruct : Construct
    {
        public Vpc Vpc { get; set; }
        public VpcConstruct(Construct scope, string id)
            : base(scope, id)
        {
            Vpc = new Vpc(this,
                "vpc-signalr",
                new VpcProps
                {
                    IpAddresses = IpAddresses.Cidr("10.55.0.0/16"),
                    MaxAzs = 2,
                    NatGateways = 1,
                    VpcName = "vpc-multiplatform-images-demo",
                    SubnetConfiguration = new ISubnetConfiguration[]
                    {
                        new SubnetConfiguration
                        {
                            Name = "subnet-public",
                            CidrMask = 24,
                            SubnetType = SubnetType.PUBLIC,
                        },
                        new SubnetConfiguration
                        {
                            Name = "subnet-private",
                            CidrMask = 24,
                            SubnetType = SubnetType.PRIVATE_WITH_EGRESS,
                        }
                    }
                });
        }
    }
}