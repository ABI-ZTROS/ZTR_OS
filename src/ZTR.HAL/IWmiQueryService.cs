using System.Management;
using System.Runtime.Versioning;

namespace ZTR.HAL;

/// <summary>
/// Abstraction for WMI query operations to enable unit testing.
/// </summary>
public interface IWmiQueryService
{
    /// <summary>
    /// Executes a WMI query and returns a collection of property dictionaries.
    /// </summary>
    /// <param name="query">The WMI query string.</param>
    /// <returns>An enumerable of dictionaries, each representing a management object's properties.</returns>
    IEnumerable<IDictionary<string, object>> ExecuteQuery(string query);
}

/// <summary>
/// Default implementation of <see cref="IWmiQueryService"/> using System.Management.
/// </summary>
[SupportedOSPlatform("windows")]
public class WmiQueryService : IWmiQueryService
{
    /// <inheritdoc />
    public IEnumerable<IDictionary<string, object>> ExecuteQuery(string query)
    {
        using var searcher = new ManagementObjectSearcher(query);
        using var results = searcher.Get();
        foreach (ManagementObject obj in results)
        {
            var dict = new Dictionary<string, object>();
            foreach (PropertyData prop in obj.Properties)
            {
                if (prop.Value != null)
                    dict[prop.Name] = prop.Value;
            }
            yield return dict;
        }
    }
}