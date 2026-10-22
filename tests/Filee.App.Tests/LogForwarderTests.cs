// A failure while logging never ends the app (AvaloniaLogForwarder): Avalonia logs from layout and bindings, and the
// trace sink's assembly can be gone when the app was moved or replaced while running.

using Avalonia.Logging;
using Filee.App.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace Filee.App.Tests;

public class LogForwarderTests
{
    [Fact]
    public void A_sink_that_throws_does_not_take_the_app_down()
    {
        var forwarder = new AvaloniaLogForwarder(new ThrowingSink(), NullLogger.Instance);

        var error = Record.Exception(() =>
            forwarder.Log(LogEventLevel.Warning, LogArea.Binding, null, "An error in a binding: {0}", "detail"));

        Assert.Null(error);
    }

    private sealed class ThrowingSink : ILogSink
    {
        public bool IsEnabled(LogEventLevel level, string area) => true;

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate) =>
            throw new FileNotFoundException("Could not load file or assembly 'System.Diagnostics.TraceSource'");

        public void Log(LogEventLevel level, string area, object? source, string messageTemplate, params object?[] propertyValues) =>
            throw new FileNotFoundException("Could not load file or assembly 'System.Diagnostics.TraceSource'");
    }
}
