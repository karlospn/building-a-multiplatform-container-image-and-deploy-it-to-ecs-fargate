using Amazon.CDK.AWS.EC2;
using Amazon.CDK.AWS.ECR;
using Amazon.CDK.AWS.ECS;
using Amazon.CDK.AWS.ElasticLoadBalancingV2;
using Constructs;
using Protocol = Amazon.CDK.AWS.EC2.Protocol;

namespace Cdk.Constructs
{
    public class EcsServiceArm64AppConstruct : Construct
    {
        public FargateService FargateService { get; }

        public EcsServiceArm64AppConstruct(Construct scope,
            string id,
            Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer pubAlb)
            : base(scope, id)
        {
            var task = CreateTaskDefinition();
            FargateService = CreateEcsService(vpc, cluster, pubAlb, task);
        }

        private FargateTaskDefinition CreateTaskDefinition()
        {
            var repository = Repository.FromRepositoryName(this, "ecr-arm64-app", "multi-platform-imgs");

            var task = new FargateTaskDefinition(this,
                "task-definition-ecs-arm64",
                new FargateTaskDefinitionProps
                {
                    Cpu = 512,
                    MemoryLimitMiB = 1024,
                    RuntimePlatform = new RuntimePlatform
                    {
                        CpuArchitecture = CpuArchitecture.ARM64,
                        OperatingSystemFamily = OperatingSystemFamily.LINUX
                    }
                });

            task.AddContainer("app-arm64",
                new ContainerDefinitionOptions
                {
                    Cpu = 512,
                    MemoryLimitMiB = 1024,
                    Image = ContainerImage.FromEcrRepository(repository, "latest"),
                    Logging = LogDriver.AwsLogs(new AwsLogDriverProps
                    {
                        StreamPrefix = "ecs"
                    }),
                    PortMappings = new IPortMapping[]
                    {
                        new PortMapping
                        {
                            ContainerPort = 8080
                        }
                    }
                });

            return task;
        }

        private FargateService CreateEcsService(Vpc vpc,
            Cluster cluster,
            ApplicationLoadBalancer pubAlb,
            FargateTaskDefinition task)
        {

            var sg = new SecurityGroup(this,
                "sg-alb-to-ecs-service-arm64",
                new SecurityGroupProps
                {
                    SecurityGroupName = "sg-alb-to-ecs-arm64-service",
                    Description = "Allow traffic from ALB to app",
                    AllowAllOutbound = true,
                    Vpc = vpc
                });

            sg.Connections.AllowFrom(pubAlb.Connections, new Port(new PortProps
            {
                FromPort = 8080,
                ToPort = 8080,
                Protocol = Protocol.TCP,
                StringRepresentation = "Allow connection from the ALB to the Fargate Service."
            }));

            var service = new FargateService(this,
                "ecs-service-arm64",
                new FargateServiceProps
                {
                    TaskDefinition = task,
                    Cluster = cluster,
                    DesiredCount = 1,
                    AssignPublicIp = true,
                    VpcSubnets = new SubnetSelection
                    {
                        Subnets = vpc.PrivateSubnets
                    },
                    SecurityGroups = new ISecurityGroup[] { sg },
                    ServiceName = "ecs-service-arm64",
                });

            return service;
        }
    }
}