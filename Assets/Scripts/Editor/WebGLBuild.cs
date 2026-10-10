using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Сборка WebGL для публикации на iDosGames: меню Bloody Steak/Build WebGL или
/// Unity.exe -batchmode -quit -projectPath . -executeMethod WebGLBuild.Build.
/// Сцены берутся из Build Settings, результат — Builds/WebGL (index.html в корне).
/// Чтобы билд не зависел от заголовков Content-Encoding на хостинге, на время сборки
/// включается Gzip + Decompression Fallback; настройки проекта затем восстанавливаются.
/// </summary>
public static class WebGLBuild
{
    private const string OutputPath = "Builds/WebGL";

    [MenuItem("Bloody Steak/Build WebGL")]
    public static void Build()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();

        var prevCompression = PlayerSettings.WebGL.compressionFormat;
        var prevFallback = PlayerSettings.WebGL.decompressionFallback;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        BuildReport report;
        try
        {
            report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
        }
        finally
        {
            PlayerSettings.WebGL.compressionFormat = prevCompression;
            PlayerSettings.WebGL.decompressionFallback = prevFallback;
        }

        var summary = report.summary;
        Debug.Log($"[WebGLBuild] {summary.result}: {summary.totalSize / (1024 * 1024)} MB, " +
                  $"{summary.totalErrors} ошибок, сцены: {string.Join(", ", scenes)}");

        if (Application.isBatchMode)
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
