namespace System.Threading;

/// <summary>
/// Use this when it isn't safe to immediately dipose of the CTS
/// after canceling the token (ie. another thread is looping and
/// may not "see" the cancellation immediately).
/// </summary>
public static class CancellationTokenFactory
{
    private static List<CancellationTokenSource> CTSList = new();

    public static CancellationTokenSource GetCancellationTokenSource()
    {
        var cts = new CancellationTokenSource();
        CTSList.Add(cts);
        return cts;
    }

    public static void DisposeAll()
    {
        foreach(var cts in CTSList)
        {
            cts.Dispose();
        }
        CTSList.Clear();
    }
}
