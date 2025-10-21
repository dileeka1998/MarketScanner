using MarketScanner.Models;

namespace MarketScanner.Services;

/// <summary>
/// Interface for providing instrument metadata.
/// </summary>
public interface IInstrumentMetadataProvider
{
    /// <summary>
    /// Gets metadata for a symbol.
    /// </summary>
    Task<InstrumentMetadata?> GetMetadataAsync(string symbol, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets metadata for multiple symbols.
    /// </summary>
    Task<IReadOnlyDictionary<string, InstrumentMetadata>> GetMetadataAsync(
        IEnumerable<string> symbols, 
        CancellationToken cancellationToken = default);
}
