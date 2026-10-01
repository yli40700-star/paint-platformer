using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

/// <summary>Builds the GitHub Pages version into the docs folder.</summary>
public static class WebGLBuild
{
    public static void BuildForGitHubPages()
    {
        string oldProductName = PlayerSettings.productName;
        WebGLCompressionFormat oldCompression = PlayerSettings.WebGL.compressionFormat;
        bool oldFallback = PlayerSettings.WebGL.decompressionFallback;

        try
        {
            // GitHub Pages works best for this project when compression is disabled.
            PlayerSettings.productName = "Paint Platformer";
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                locationPathName = "docs",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception($"WebGL build failed: {report.summary.result}");

            Console.WriteLine($"WebGL build finished: {report.summary.totalSize} bytes");
        }
        finally
        {
            // Put the editor settings back so running the build does not create noisy diffs.
            PlayerSettings.productName = oldProductName;
            PlayerSettings.WebGL.compressionFormat = oldCompression;
            PlayerSettings.WebGL.decompressionFallback = oldFallback;
        }
    }
}
