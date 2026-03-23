namespace Jamaa.Desktop.Services.Interactions;

public record DialogResponse<TResult>(bool Confirmed, TResult Result);