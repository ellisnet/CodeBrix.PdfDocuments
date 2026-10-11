using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;

namespace CodeBrix.PdfDocCreate.Html2Pdf.Fonts;

/// <summary>
/// The Android application's assets, reached through <c>Android.App.Application.Context.Assets</c>
/// (an <c>Android.Content.Res.AssetManager</c>) by reflection, so this net10.0 library needs no
/// reference to Mono.Android. The <see cref="DynamicDependencyAttribute"/>s keep the four members
/// used here in a trimmed application.
/// </summary>
internal sealed class AndroidFontAssets : IFontAssetSource
{
    private readonly object _assetManager;
    private readonly MethodInfo _list;
    private readonly MethodInfo _open;

    private AndroidFontAssets(object assetManager, MethodInfo list, MethodInfo open)
    {
        _assetManager = assetManager;
        _list = list;
        _open = open;
    }

    /// <summary>
    /// The application's assets on Android; null on every other platform, and when the
    /// Android members cannot be reached.
    /// </summary>
    [DynamicDependency("get_Context", "Android.App.Application", "Mono.Android")]
    [DynamicDependency("get_Assets", "Android.Content.Context", "Mono.Android")]
    [DynamicDependency("List(System.String)", "Android.Content.Res.AssetManager", "Mono.Android")]
    [DynamicDependency("Open(System.String)", "Android.Content.Res.AssetManager", "Mono.Android")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "The members are kept by the DynamicDependency attributes.")]
    [UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The members are kept by the DynamicDependency attributes.")]
    public static AndroidFontAssets TryCreate()
    {
        if (!OperatingSystem.IsAndroid()) { return null; }

        try
        {
            var applicationType = Type.GetType("Android.App.Application, Mono.Android");
            var contextType = Type.GetType("Android.Content.Context, Mono.Android");
            var assetManagerType = Type.GetType("Android.Content.Res.AssetManager, Mono.Android");
            if (applicationType == null || contextType == null || assetManagerType == null) { return null; }

            var context = applicationType
                .GetProperty("Context", BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null);
            var assetManager = context == null
                ? null
                : contextType.GetProperty("Assets", BindingFlags.Public | BindingFlags.Instance)?.GetValue(context);
            var list = assetManagerType.GetMethod("List", BindingFlags.Public | BindingFlags.Instance, new[] { typeof(string) });
            var open = assetManagerType.GetMethod("Open", BindingFlags.Public | BindingFlags.Instance, new[] { typeof(string) });

            return assetManager != null && list != null && open != null
                ? new AndroidFontAssets(assetManager, list, open)
                : null;
        }
        catch (Exception ex) when (ex is TargetInvocationException or MemberAccessException or InvalidOperationException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public string[] List(string folder)
    {
        try
        {
            return _list.Invoke(_assetManager, new object[] { folder ?? "" }) as string[] ?? Array.Empty<string>();
        }
        catch (TargetInvocationException)
        {
            return Array.Empty<string>();
        }
    }

    /// <inheritdoc />
    public Stream Open(string path)
    {
        try
        {
            return (Stream)_open.Invoke(_assetManager, new object[] { path });
        }
        catch (TargetInvocationException ex)
        {
            throw new FileNotFoundException($"Application asset '{path}' could not be opened.", path, ex.InnerException);
        }
    }
}
