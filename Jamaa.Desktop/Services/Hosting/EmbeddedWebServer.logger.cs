using System;
using Microsoft.Extensions.Logging;

namespace Jamaa.Desktop.Services.Hosting;

public partial class EmbeddedWebServer
{
    [LoggerMessage(LogLevel.Information, "Embedded ASP.NET Core server started at {BaseAddress}")]
    partial void LogEmbeddedServerStarted(Uri baseAddress);
}