namespace Application.DataTransferObjects.Others.NS;

/// <summary>
/// The requested SuiteQL offset was past the end of the result set, so the
/// integration already retried on a legal page and carries the recovered rows.
/// Thrown (instead of returned) so the corrected page index travels with the
/// data: each layer catches, translates <typeparamref name="TRows"/> to its own
/// row type, and rethrows until the grid component repositions its pager.
/// Single retry only at the integration boundary — a second
/// OffsetOutOfBounds surfaces normally rather than looping.
/// Lives in the DTO project (not Integration.NS) so the Application and Web
/// layers can catch it without creating a circular project reference.
/// </summary>
public class NetSuiteOffsetCorrectedException<TRows> : Exception
{
    public TRows Rows { get; }
    public int TotalResults { get; }
    public int CorrectedSkip { get; }

    public NetSuiteOffsetCorrectedException(
        TRows rows,
        int totalResults,
        int correctedSkip,
        Exception innerException)
        : base(innerException.Message, innerException)
    {
        Rows = rows;
        TotalResults = totalResults;
        CorrectedSkip = correctedSkip;
    }
}
