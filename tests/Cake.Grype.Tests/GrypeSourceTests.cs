using Cake.Core.IO;

namespace Cake.Grype.Tests;

public sealed class GrypeSourceTests
{
    private static readonly DirectoryPath Working = new DirectoryPath("/Working");

    public static TheoryData<GrypeSource, string> RelativePathSources => new TheoryData<GrypeSource, string>
    {
        { GrypeSource.Sbom("artifacts/bom.cdx.json"), "sbom:/Working/artifacts/bom.cdx.json" },
        { GrypeSource.Directory("src"), "dir:/Working/src" },
        { GrypeSource.File("bin/app.jar"), "file:/Working/bin/app.jar" },
        { GrypeSource.DockerArchive("image.tar"), "docker-archive:/Working/image.tar" },
        { GrypeSource.OciArchive("image.oci.tar"), "oci-archive:/Working/image.oci.tar" },
        { GrypeSource.OciDirectory("oci"), "oci-dir:/Working/oci" },
        { GrypeSource.Singularity("image.sif"), "singularity:/Working/image.sif" },
        { GrypeSource.PurlFile("purls.txt"), "purl:/Working/purls.txt" },
        { GrypeSource.CpeFile("cpes.txt"), "cpes:/Working/cpes.txt" },
        { GrypeSource.Zarf("package.tar.zst"), "zarf:/Working/package.tar.zst" },
    };

    public static TheoryData<GrypeSource, string> ValueSources => new TheoryData<GrypeSource, string>
    {
        { GrypeSource.Image("alpine:3.20"), "alpine:3.20" },
        { GrypeSource.Docker("myorg/api:1.2.3"), "docker:myorg/api:1.2.3" },
        { GrypeSource.Podman("myorg/api:1.2.3"), "podman:myorg/api:1.2.3" },
        { GrypeSource.Registry("myorg/api:1.2.3"), "registry:myorg/api:1.2.3" },
        { GrypeSource.Purl("pkg:apk/openssl@3.2.1?distro=alpine-3.20.3"), "pkg:apk/openssl@3.2.1?distro=alpine-3.20.3" },
        { GrypeSource.Cpe("cpe:2.3:a:openssl:openssl:3.0.14:*:*:*:*:*:*:*"), "cpe:2.3:a:openssl:openssl:3.0.14:*:*:*:*:*:*:*" },
        { GrypeSource.Parse("registry:alpine:3.20"), "registry:alpine:3.20" },
    };

    [Theory]
    [MemberData(nameof(RelativePathSources))]
    public void Should_Make_Paths_Absolute(GrypeSource source, string expected)
    {
        Assert.Equal(expected, source.ToArgument(Working));
    }

    [Theory]
    [MemberData(nameof(ValueSources))]
    public void Should_Pass_Values_Through(GrypeSource source, string expected)
    {
        Assert.Equal(expected, source.ToArgument(Working));
    }

    [Fact]
    public void Should_Keep_Absolute_Paths()
    {
        Assert.Equal("sbom:/data/bom.json", GrypeSource.Sbom("/data/bom.json").ToArgument(Working));
    }

    [Fact]
    public void Should_Resolve_Against_The_Given_Directory()
    {
        Assert.Equal("dir:/Other/src", GrypeSource.Directory("src").ToArgument(new DirectoryPath("/Other")));
    }

    [Fact]
    public void Should_Convert_Implicitly_From_String()
    {
        GrypeSource source = "sbom:./bom.json";

        Assert.Equal("sbom:./bom.json", source.ToArgument(Working));
    }

    [Fact]
    public void Should_Render_The_Unresolved_Form_In_ToString()
    {
        Assert.Equal("sbom:artifacts/bom.cdx.json", GrypeSource.Sbom("artifacts/bom.cdx.json").ToString());
        Assert.Equal("registry:alpine:3.20", GrypeSource.Registry("alpine:3.20").ToString());
    }

    [Fact]
    public void Should_Throw_If_A_Path_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Sbom(null));

        Assertions.IsArgumentNullException(result, "file");
    }

    [Fact]
    public void Should_Throw_If_A_Directory_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Directory(null));

        Assertions.IsArgumentNullException(result, "directory");
    }

    [Fact]
    public void Should_Throw_If_A_Value_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Registry(null));

        Assertions.IsArgumentNullException(result, "reference");
    }

    [Fact]
    public void Should_Throw_If_A_Value_Is_Blank()
    {
        var result = Record.Exception(() => GrypeSource.Image(" "));

        Assertions.IsArgumentException(result, "reference");
    }

    [Fact]
    public void Should_Throw_If_Parsed_Text_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Parse(null));

        Assertions.IsArgumentNullException(result, "source");
    }

    [Fact]
    public void Should_Throw_If_The_Working_Directory_Is_Null()
    {
        var result = Record.Exception(() => GrypeSource.Sbom("bom.json").ToArgument(null));

        Assertions.IsArgumentNullException(result, "workingDirectory");
    }
}
