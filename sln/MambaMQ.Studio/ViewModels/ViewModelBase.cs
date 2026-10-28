namespace MambaMQ.Studio.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    public string VersionText { get; } = $"v{GetApplicationVersion()}";

    private static string GetApplicationVersion()
    {
        string? version = typeof(ViewModelBase).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return string.IsNullOrWhiteSpace(version)
            ? typeof(ViewModelBase).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"
            : version.Split('+', 2)[0];
    }
}