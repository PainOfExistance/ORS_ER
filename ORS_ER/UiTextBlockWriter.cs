using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ORS_ER;

internal sealed class UiTextBlockWriter : TextWriter
{
    private readonly TextBlock _target;
    private readonly Dispatcher _dispatcher;
    private readonly int _maxChars;

    public UiTextBlockWriter(TextBlock target, int maxChars = 50_000)
    {
        _target = target ?? throw new ArgumentNullException(nameof(target));
        _dispatcher = target.Dispatcher;
        _maxChars = Math.Max(1_000, maxChars);
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value) => Append(value.ToString());
    public override void Write(string? value) { if (value is not null) Append(value); }
    public override void WriteLine(string? value) => Append((value ?? string.Empty) + Environment.NewLine);

    private void Append(string text)
    {
        if (_dispatcher.CheckAccess())
        {
            AppendOnUi(text);
            return;
        }

        _dispatcher.BeginInvoke(() => AppendOnUi(text), DispatcherPriority.Background);
    }

    private void AppendOnUi(string text)
    {
        _target.Text += text;

        if (_target.Text.Length > _maxChars)
            _target.Text = _target.Text[^_maxChars..];
    }
}