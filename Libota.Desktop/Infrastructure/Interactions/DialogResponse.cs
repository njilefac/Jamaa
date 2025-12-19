namespace Libota.Desktop.Infrastructure.Interactions;

public record DialogResponse<TResult>(bool Confirmed, TResult Result);