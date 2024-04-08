# Building a .NET 8 multi-platform container image and deploy it to AWS ECS Fargate using AWS CDK

This repository contains a practical example about how to build a .NET 8 multi-platform container image and how to deploy it to AWS ECS Fargate.

# **What is a multi-platform image?**

Docker images can support multiple platforms, which means that a single image may contain variants for different architectures, and sometimes for different operating systems.

When you run an image with multi-platform support, Docker automatically selects the image that matches your OS and architecture.

In order to build multi-platform container images, we need to make use of the ``docker buildx`` command. ``Buildx`` is a Docker CLI plugin that extends the docker build command with the full support of the features provided by Moby BuildKit builder toolkit.

By default, a build executed with ``buildx`` will build an image for the architecture that matches the host machine. This way, you get an image that runs on the same machine you are working on. In order to build it for a different architecture, you need to the ``--platform`` flag, e.g. ``--platform=linux/arm64``.

```shell
docker buildx build --platform=linux/arm64 -t my-api .
```
You can also specify multiple platforms in a single command and it will create multiple container images, one per platform.

```shell
docker buildx build --platform=linux/arm64,linux/amd64 -t my-api .
```

When you want to create a multi-platform image you don't have to specifically use the ``buildx`` syntax. ``Buildx`` is a drop-in replacement for the legacy build client used in earlier versions of Docker Engine and Docker Desktop.   

In newer versions of Docker Desktop and Docker Engine, **you're using buildx by default when you invoke the docker build command**, so every time you're using the ``docker build`` command, in fact you're using the ``buildx`` command.


# **.NET 8 multi-platform image**

.NET 8 images support multiple platforms, which means that a single image may contain variants for different architectures.    
When you run an image with multi-platform support, Docker automatically selects the image that matches your OS and architecture.

The following code snippet demonstrates how the .NET 8 SDK and runtime images contain a multi-platform tag image.

```bash
$ docker manifest inspect mcr.microsoft.com/dotnet/sdk:8.0 | grep architecture
            "architecture": "amd64",
            "architecture": "arm",
            "architecture": "arm64",
```

```bash
$ docker manifest inspect mcr.microsoft.com/dotnet/runtime:8.0 | grep architecture
            "architecture": "amd64",
            "architecture": "arm",
            "architecture": "arm64",
```

```bash
$ docker manifest inspect mcr.microsoft.com/dotnet/runtime-deps:8.0 | grep architecture
            "architecture": "amd64",
            "architecture": "arm",
            "architecture": "arm64",
```

When running any of these images on an  ``AMD64`` processor, the ``AMD64`` variant will be pulled and run, and exactly the same happens with ``ARM64``.    

# **Building a .NET 8 multi-platform image**

You can build .NET multi-platform images using 2 different strategies:
- Using emulation. The emulation software used is named [QEMU](https://www.qemu.org/). 
- Using a stage in your Dockerfile to ``cross-compile`` to different architectures.

The preferred option for .NET is Cross-Compilation. Using cross-compilation means leveraging the capabilities of a compiler to build for multiple platforms, without the need for emulation. The idea behind it is to use a multi-stage build and in the build stage compile your code for the target architecture, and in the run stage configure the runtime to be exported to the final image.

The ``/src`` folder contains a .NET 8 "Hello World" API with a Dockerfile that is prepared to work with Cross-Compilation. 

The main differences with a simple .NET Dockerfile are the following ones:
- It uses the ``BUILDPLATFORM`` argument to pin the builder to use the host native architecture as the build platform. This is only to prevent emulation.
- It uses the ``TARGETARCH`` argument to generate the application binaries for the given target architecture.

If you want to test it,  you can run the following comands:

- To create an ARM64 image: ``docker buildx build --platform=linux/arm64 -t my-api -f Dockerfile.Multiplatform .``
- To create an ARM64 image and a AMD64 image at the same time:  ``docker buildx build --platform=linux/arm64,linux/amd64 -t my-api -f Dockerfile.Multiplatform .``

By default, you can only build for a single platform at a time. If you want to build for multiple platforms at once, you can [turn on](https://docs.docker.com/desktop/containerd/) the containerd snapshotter storage.

# **Multi-platform images on ECR**

AWS ECR is compatible with multiplatform images.  This support is achieved through the use of an image specification component known as a manifest list, or image index.

A manifest list (or image index) allows for the nested inclusion of other image manifests, where each included image is specified by architecture, operating system and other platform attributes. This makes it possible to refer to an image repository that includes platform-specific images by a more abstract name.

A GitHub Action is located in the ``.github`` folder, which selects the .NET API and generates a multi-platform image. If you take a closer look at the Action, you'll see that it runs the following steps:
- If the ECR repository does not exist, it creates one.
- The action generates both an ARM64 compatible image and an AMD64 image.
- It then pushes both images into the ECR.
- Using the docker manifest create command, it creates a new manifest for this image. The manifest uses the default latest tag.
- Finally, it pushes the manifest to the repository.

<add-img>

# **Multi-platform images on AWS ECS Fargate**

AWS ECS Fargate supports multi-platform images,  which we will utilize to test the multi-platform image that we previously pushed into the ECR.

In the ``/cdk`` folder, you'll find a stack that creates the following resources.

- A Virtual Private Cloud (VPC).
- An Application Load Balancer (ALB).
- An Elastic Container Service (ECS) Cluster.
- An ECS Task Definition and an ECS Service for the .NET 8 ARM64-compatible API.
- An ECS Task Definition and an ECS Service for the .NET 8 AMD64-compatible API.

The following code snippet shows how the ECS Task Definition for the .NET 8 AMD64-compatible image gets created.

```csharp
var repository = Repository.FromRepositoryName(this, "ecr-arm64-app", "multi-platform-imgs");

var task = new FargateTaskDefinition(this,
    $"task-definition-ecs-amd64",
    new FargateTaskDefinitionProps
    {
        Cpu = 512,
        MemoryLimitMiB = 1024,
        RuntimePlatform = new RuntimePlatform
        {
            CpuArchitecture = CpuArchitecture.X86_64,
            OperatingSystemFamily = OperatingSystemFamily.LINUX
        }
    });

task.AddContainer($"app-amd64",
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
```

And this other code snippet shows how the ECS Task Definition for the .NET 8 ARM64-compatible image gets created.

```csharp
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
```

As you can see, we're utilizing the manifest index tag in both Task Definitions. Fargate is capable of selecting one image over another based on the architecture of the task.

After deploying the CDK stack, if we test both Fargate services, we will find that they are both functioning correctly.

<add-img>