using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Runtime.Assets;
using Runtime.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace Runtime.UI;

public sealed class DynamicUiActionModuleLoader
{
    private readonly RunnerUriResolver _uriResolver;

    public DynamicUiActionModuleLoader(RunnerUriResolver uriResolver)
    {
        _uriResolver = uriResolver;
    }

    public async Task<IReadOnlyList<IUiActionModule>> TryLoadCompanionModulesAsync(string uiSmlUri, CancellationToken cancellationToken = default)
    {
        var companionCsUri = TryBuildCompanionCsUri(uiSmlUri);
        if (string.IsNullOrWhiteSpace(companionCsUri))
        {
            return [];
        }

        string source;
        try
        {
            source = await _uriResolver.LoadTextAsync(companionCsUri, cancellationToken: cancellationToken);
        }
        catch (FileNotFoundException)
        {
            RunnerLogger.Info("UI", $"No companion action module found for '{uiSmlUri}' (expected '{companionCsUri}').");
            return [];
        }
        catch (HttpRequestException)
        {
            RunnerLogger.Info("UI", $"Companion action module '{companionCsUri}' is not available.");
            return [];
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("UI", $"Failed to load companion action module source '{companionCsUri}'.", ex);
            return [];
        }

        var modules = CompileAndInstantiateModules(source, companionCsUri);
        if (modules.Count > 0)
        {
            RunnerLogger.Info("UI", $"Loaded {modules.Count} dynamic action module(s) from '{companionCsUri}'.");
        }

        return modules;
    }

    private static List<IUiActionModule> CompileAndInstantiateModules(string source, string sourceUri)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions, path: sourceUri);

        var references = CollectMetadataReferences();
        if (references.Count == 0)
        {
            RunnerLogger.Warn("UI", "Could not resolve metadata references for dynamic C# compilation.");
            return [];
        }

        var assemblyName = $"NoCodeRunner.Dynamic.{Guid.NewGuid():N}";
        var compilation = CSharpCompilation.Create(
            assemblyName,
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream);
        if (!emitResult.Success)
        {
            foreach (var diagnostic in emitResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error))
            {
                RunnerLogger.Warn("UI", $"Dynamic module compile error: {diagnostic}");
            }

            return [];
        }

        peStream.Position = 0;

        Assembly assembly;
        try
        {
            var hostAssembly = typeof(IUiActionModule).Assembly;
            var hostContext = AssemblyLoadContext.GetLoadContext(hostAssembly) ?? AssemblyLoadContext.Default;

            Assembly? ResolveKnownAssembly(AssemblyLoadContext _context, AssemblyName assemblyName)
            {
                if (AssemblyName.ReferenceMatchesDefinition(assemblyName, hostAssembly.GetName()))
                {
                    return hostAssembly;
                }

                return AppDomain.CurrentDomain
                    .GetAssemblies()
                    .FirstOrDefault(a => string.Equals(a.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
            }

            hostContext.Resolving += ResolveKnownAssembly;
            try
            {
                assembly = hostContext.LoadFromStream(peStream);
            }
            finally
            {
                hostContext.Resolving -= ResolveKnownAssembly;
            }
        }
        catch (Exception ex)
        {
            RunnerLogger.Warn("UI", "Failed to load compiled dynamic action module assembly.", ex);
            return [];
        }

        var modules = new List<IUiActionModule>();
        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            foreach (var loaderException in ex.LoaderExceptions)
            {
                if (loaderException is not null)
                {
                    RunnerLogger.Warn("UI", $"Dynamic module type load error: {loaderException.Message}");
                }
            }

            types = ex.Types.Where(t => t is not null).Cast<Type>().ToArray();
        }

        foreach (var type in types)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            if (!typeof(IUiActionModule).IsAssignableFrom(type))
            {
                continue;
            }

            try
            {
                if (Activator.CreateInstance(type) is IUiActionModule module)
                {
                    modules.Add(module);
                }
            }
            catch (Exception ex)
            {
                RunnerLogger.Warn("UI", $"Could not instantiate dynamic action module '{type.FullName}'.", ex);
            }
        }

        return modules;
    }

    private static List<MetadataReference> CollectMetadataReferences()
    {
        var references = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        static void AddReference(List<MetadataReference> refs, HashSet<string> known, string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return;
            }

            if (!known.Add(path))
            {
                return;
            }

            refs.Add(MetadataReference.CreateFromFile(path));
        }

        // Ensure platform/reference assemblies are available even if some runtime assemblies have empty Location.
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string tpa && !string.IsNullOrWhiteSpace(tpa))
        {
            foreach (var path in tpa.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                AddReference(references, seen, path);
            }
        }

        // Ensure app/runtime assemblies that expose the scripting API are always referenced.
        AddAssemblyReference(references, seen, typeof(IUiActionModule).Assembly);
        AddAssemblyReference(references, seen, typeof(RunnerLogger).Assembly);
        AddAssemblyReference(references, seen, typeof(Godot.Node).Assembly);

        // Include all locally deployed runtime assemblies (important in Godot/editor runtime contexts).
        var baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(baseDir) && Directory.Exists(baseDir))
        {
            foreach (var dll in Directory.EnumerateFiles(baseDir, "*.dll", SearchOption.TopDirectoryOnly))
            {
                AddReference(references, seen, dll);
            }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            AddAssemblyReference(references, seen, assembly);
        }

        return references;

        static void AddAssemblyReference(List<MetadataReference> refs, HashSet<string> known, Assembly assembly)
        {
            AddReference(refs, known, assembly.Location);

            var assemblyName = assembly.GetName().Name;
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return;
            }

            var candidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll"),
                Path.Combine(Directory.GetCurrentDirectory(), $"{assemblyName}.dll")
            };

            foreach (var candidate in candidates)
            {
                AddReference(refs, known, candidate);
            }

        }
    }

    private static string? TryBuildCompanionCsUri(string uiSmlUri)
    {
        if (string.IsNullOrWhiteSpace(uiSmlUri))
        {
            return null;
        }

        if (Uri.TryCreate(uiSmlUri, UriKind.Absolute, out var absoluteUri))
        {
            if (!absoluteUri.AbsolutePath.EndsWith(".sml", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var builder = new UriBuilder(absoluteUri)
            {
                Path = absoluteUri.AbsolutePath[..^4] + ".cs"
            };

            return builder.Uri.ToString();
        }

        return uiSmlUri.EndsWith(".sml", StringComparison.OrdinalIgnoreCase)
            ? uiSmlUri[..^4] + ".cs"
            : null;
    }
}
