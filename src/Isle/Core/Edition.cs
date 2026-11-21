namespace Isle.Core;

/// <summary>
/// Which product this build is. The developer edition (<c>dotnet build -p:Edition=Dev</c>) enables the developer
/// modules by default; the standard edition still lists them in Settings, switched off.
/// </summary>
public static class Edition
{
#if DEV_EDITION
    public static readonly bool IsDev = true;
#else
    public static readonly bool IsDev = false;
#endif
}
