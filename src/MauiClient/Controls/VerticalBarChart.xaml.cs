using Ax.Fw.Extensions;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using System.Collections.Immutable;

namespace CloakTunnel.MauiClient.Controls;

public record VerticalBarChartEntry(
  string Title,
  float Value,
  string? ValueText,
  SKColor BarColor,
  SKColor TitleColor);

public record VerticalBarChartOptions(
  SKColor BackgroundColor,
  float BarTitleFontSize,
  string? ChartTitle,
  Func<VerticalBarChartEntry, float> SortFunc)
{
  public static VerticalBarChartOptions Default { get; } = new(SKColors.White, 40, null, _ => _.Value);
}

public partial class VerticalBarChart : ContentView
{
  private VerticalBarChartOptions p_options = VerticalBarChartOptions.Default;
  private ImmutableList<VerticalBarChartEntry> p_entries = ImmutableList<VerticalBarChartEntry>.Empty;

  public VerticalBarChart()
  {
    InitializeComponent();
  }

  public void SetEntries(IEnumerable<VerticalBarChartEntry> _entries)
  {
    p_entries = _entries.ToImmutableList();
    p_canvasView.InvalidateSurface();
  }

  public void SetOptions(VerticalBarChartOptions _options)
  {
    p_options = _options;
    p_canvasView.InvalidateSurface();
  }

  private void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs e)
  {
    var surface = e.Surface;
    var canvas = surface.Canvas;

    canvas.Clear(p_options.BackgroundColor);

    using var textPaint = new SKPaint { Color = SKColors.Black };
    using var textFont = new SKFont() { Size = p_options.BarTitleFontSize };

    const float margin = 20;
    var chartTitleHeight = 0f;

    if (p_options.ChartTitle != null)
    {
      textFont.MeasureText(p_options.ChartTitle, out var textBounds, textPaint);
      chartTitleHeight = margin + textBounds.Height;

      canvas.DrawText(
          p_options.ChartTitle,
          e.Info.Width / 2,
          margin + textBounds.Height / 2,
          SKTextAlign.Center,
          textFont,
          textPaint);
    }

    var entries = p_entries;

    if (entries.IsEmpty)
      return;

    var titleSizeLut = new Dictionary<VerticalBarChartEntry, SKRect>();
    foreach (var entry in entries)
    {
      textFont.MeasureText(entry.Title, out var textBounds, textPaint);
      titleSizeLut[entry] = textBounds;
    }

    var width = e.Info.Width - 2 * margin;
    var barWidth = width / entries.Count;
    var titleIsVertical = titleSizeLut
      .Any(_ => _.Value.Width > barWidth);

    var maxTitleHeight = titleIsVertical
      ? 0f
      : titleSizeLut.Max(_ => _.Value.Height);

    var height = e.Info.Height - 2 * margin - chartTitleHeight - maxTitleHeight;
    var maxValue = entries.Max(_ => _.Value);

    using var barPaint = new SKPaint
    {
      Style = SKPaintStyle.StrokeAndFill,
      Color = SKColors.Black,
      StrokeWidth = 5,
      IsAntialias = true,
    };

    var counter = 0;
    foreach (var entry in entries.OrderBy(_ => p_options.SortFunc(_)))
    {
      var barHeight = (entry.Value / maxValue) * height;
      var x = margin + counter++ * barWidth;
      var y = e.Info.Height - margin - barHeight - maxTitleHeight;

      barPaint.Color = entry.BarColor;
      canvas.DrawRect(x, y, barWidth - margin, barHeight, barPaint);

      var textSize = titleSizeLut[entry];
      textPaint.Color = entry.TitleColor;
      if (!titleIsVertical)
      {
        canvas.DrawText(
          entry.Title,
          x + (barWidth - margin) / 2 - textSize.Width / 2,
          e.Info.Height - margin / 4,
          SKTextAlign.Left,
          textFont,
          textPaint);
      }
      else
      {
        var text = entry.Title;
        while (text.Length > 2 && textSize.Width > height - 2 * margin)
        {
          text = text[..^1];
          textFont.MeasureText(text, out textSize, textPaint);
        }

        canvas.Save();

        canvas.Translate(
          x + (barWidth - margin) / 2 + textSize.Height / 2,
          y + barHeight - margin);

        canvas.RotateDegrees(-90);
        canvas.DrawText(
          text,
          0,
          0,
          SKTextAlign.Left,
          textFont,
          textPaint);

        canvas.Restore();
      }
    }
  }



}