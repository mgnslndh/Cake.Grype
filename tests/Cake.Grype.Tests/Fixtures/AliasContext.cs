using Cake.Core;
using Cake.Core.Diagnostics;
using Cake.Core.IO;
using Cake.Core.Tooling;
using Cake.Testing;
using NSubstitute;

namespace Cake.Grype.Tests.Fixtures;

/// <summary>
/// A substitute <see cref="ICakeContext"/> backed by Cake's fakes, for calling aliases directly.
/// </summary>
internal sealed class AliasContext
{
    public AliasContext()
    {
        Environment = FakeEnvironment.CreateUnixEnvironment();
        FileSystem = new FakeFileSystem(Environment);
        FileSystem.CreateFile("/Working/tools/grype");
        ProcessRunner = new RecordingProcessRunner();

        var globber = new Globber(FileSystem, Environment);
        var tools = new ToolLocator(
            Environment,
            new ToolRepository(Environment),
            new ToolResolutionStrategy(FileSystem, Environment, globber, new FakeConfiguration(), new NullLog()));

        Context = Substitute.For<ICakeContext>();
        Context.FileSystem.Returns(FileSystem);
        Context.Environment.Returns(Environment);
        Context.ProcessRunner.Returns(ProcessRunner);
        Context.Tools.Returns(tools);
    }

    public ICakeContext Context { get; }

    public FakeEnvironment Environment { get; }

    public FakeFileSystem FileSystem { get; }

    public RecordingProcessRunner ProcessRunner { get; }
}

internal sealed class RecordingProcessRunner : IProcessRunner
{
    public FakeProcess Process { get; } = new FakeProcess();

    public List<string> Arguments { get; } = new List<string>();

    public IProcess Start(FilePath filePath, ProcessSettings settings)
    {
        Arguments.Add(settings.Arguments.Render());
        return Process;
    }
}
