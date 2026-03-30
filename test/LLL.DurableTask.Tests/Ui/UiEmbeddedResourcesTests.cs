using AwesomeAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace LLL.DurableTask.Tests.Ui;

public class UiEmbeddedResourcesTests
{
    [Fact]
    public void Ui_assembly_has_embedded_file_manifest()
    {
        var assembly = typeof(DurableTaskUiApplicationBuilderExtensions).Assembly;

        var act = () => new ManifestEmbeddedFileProvider(assembly, "app/build");

        act.Should().NotThrow(
            "the NuGet package must include the embedded files manifest for all target frameworks");
    }

    [Fact]
    public void Ui_embedded_files_include_index_html()
    {
        var assembly = typeof(DurableTaskUiApplicationBuilderExtensions).Assembly;
        var fileProvider = new ManifestEmbeddedFileProvider(assembly, "app/build");

        var fileInfo = fileProvider.GetFileInfo("index.html");

        fileInfo.Exists.Should().BeTrue("index.html must be embedded in the Ui assembly");
    }
}
