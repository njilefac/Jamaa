using Avalonia.Controls;

namespace Libota.Desktop.Infrastructure.Windows;

public interface ITopLevelProvider
{
    TopLevel? Current { get; }
}