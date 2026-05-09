namespace Jamaa.Application.Finances.Values;

/// <summary>
/// Application-layer currency value type.
/// Kept in this namespace for backward-compatibility with Akka.Persistence journal entries
/// that were serialized with the fully-qualified type name
/// "Jamaa.Application.Finances.Values.Currency, Jamaa.Application".
/// New domain code should reference Domain.Finances.Values.Currency directly.
/// </summary>
public record Currency(string Code, string Symbol);

