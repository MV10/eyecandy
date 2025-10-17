
namespace eyecandy;

/// <summary>
/// Various fields which are logged when the OpenGL error callback is invoked.
/// Typically the method-level details are only used when debugging specific issues.
/// </summary>
public static class GLErrorAppState
{
    /// <summary>
    /// Overall application state information; likely changes infrequently.
    /// </summary>
    public static string AppState { get; set; } = string.Empty;

    /// <summary>
    /// Details of the specific method being executed; should be set to Empty
    /// anywhere the method may exit (ideally in a finally block) so as to avoid
    /// accidentally reporting misleading details. Setters should use the Set/Clear
    /// functions.
    /// </summary>
    public static (string Name, string Args) MethodState { get; private set; } = (string.Empty, string.Empty);

    private static Stack<(string Name, string Args)> MethodStack { get; set; } = new();

    /// <summary>
    /// Preserves any current MethodState and sets new values.
    /// </summary>
    public static void SetMethodState(string name, string args)
    {
        if (!string.IsNullOrEmpty(MethodState.Args)) MethodStack.Push((name, args));
        MethodState = (name, args);
    }

    /// <summary>
    /// Restores any previous MethodState.
    /// </summary>
    public static void ClearMethodState()
    {
        if(MethodStack.Count > 0)
        {
            MethodState = MethodStack.Pop();
        }
        else
        {
            MethodState = (string.Empty, string.Empty);
        }
    }
}
