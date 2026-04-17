using UnityEditor;

public static class WebGLBuilder
{
    public static void PerformWebGLBuild()
    {
        var buildPath = "Build/WebGL";
        BuildPipeline.BuildPlayer(
            EditorBuildSettings.scenes,
            buildPath,
            BuildTarget.WebGL,
            BuildOptions.None
        );
    }
}