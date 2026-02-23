using Microsoft.Win32;
using ORS_ER.components;
using ORS_ER.connections;
using SkiaSharp;
using System.IO;

namespace ORS_ER;

internal static class CanvasExport
{
	internal static void SaveAsPng(Dictionary<string, Component> paintItems, Dictionary<string, Connection> connections)
	{
		if (paintItems.Count == 0)
			return;

		var dialog = new SaveFileDialog
		{
			Title = "Save canvas as PNG",
			Filter = "PNG image (*.png)|*.png",
			DefaultExt = ".png",
			AddExtension = true,
			OverwritePrompt = true
		};

		if (dialog.ShowDialog() != true)
			return;

		var bounds = ComputeWorldBounds(paintItems, connections);
		if (bounds.Width <= 0 || bounds.Height <= 0)
			return;

		const float pad = 16f;
		var padded = SKRect.Create(bounds.Left - pad, bounds.Top - pad, bounds.Width + pad * 2, bounds.Height + pad * 2);

		var width = (int)Math.Ceiling(padded.Width);
		var height = (int)Math.Ceiling(padded.Height);
		if (width <= 0 || height <= 0)
			return;

		using var bitmap = new SKBitmap(width, height, isOpaque: true);
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(SKColors.WhiteSmoke);
		canvas.Translate(-padded.Left, -padded.Top);

		var paints = ComponentPaints.Create(ComponentPaintScheme.Input);
		foreach (var connection in connections.Values)
		{
			if (connection.ToId == "")
				continue;

			if (!paintItems.TryGetValue(connection.FromComponentId, out var fromComponent))
				continue;
			if (!paintItems.TryGetValue(connection.ToComponentId, out var toComponent))
				continue;

			var fromNode = fromComponent.Outputs[connection.FromId].Node;
			var toNode = toComponent.Inputs[connection.ToId].Node;
			canvas.DrawLine(fromNode, toNode, connection.IsSelected ? paints.SelectedLineStroke : paints.LineStroke);
		}

		foreach (var item in paintItems.Values)
			item.Paint(canvas);

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Png, 100);
		using var fs = File.Open(dialog.FileName, FileMode.Create, FileAccess.Write);
		data.SaveTo(fs);
	}

	private static SKRect ComputeWorldBounds(Dictionary<string, Component> paintItems, Dictionary<string, Connection> connections)
	{
		var hasAny = false;
		var bounds = SKRect.Empty;

		foreach (var c in paintItems.Values)
		{
			var r = GetComponentBounds(c);
			if (r.Width <= 0 || r.Height <= 0)
				continue;

			if (!hasAny)
			{
				bounds = r;
				hasAny = true;
			}
			else
			{
				bounds = SKRect.Union(bounds, r);
			}
		}

		foreach (var conn in connections.Values)
		{
			if (conn.ToId == "")
				continue;

			if (!paintItems.TryGetValue(conn.FromComponentId, out var fromComponent))
				continue;
			if (!paintItems.TryGetValue(conn.ToComponentId, out var toComponent))
				continue;

			var fromNode = fromComponent.Outputs[conn.FromId].Node;
			var toNode = toComponent.Inputs[conn.ToId].Node;
			var lr = SKRect.Create(
				Math.Min(fromNode.X, toNode.X),
				Math.Min(fromNode.Y, toNode.Y),
				Math.Abs(fromNode.X - toNode.X),
				Math.Abs(fromNode.Y - toNode.Y));

			if (lr.Width <= 0 || lr.Height <= 0)
				continue;

			if (!hasAny)
			{
				bounds = lr;
				hasAny = true;
			}
			else
			{
				bounds = SKRect.Union(bounds, lr);
			}
		}

		return hasAny ? bounds : SKRect.Empty;
	}

	private static SKRect GetComponentBounds(Component c)
	{
		var minX = float.PositiveInfinity;
		var minY = float.PositiveInfinity;
		var maxX = float.NegativeInfinity;
		var maxY = float.NegativeInfinity;

		foreach (var io in c.Inputs.Values)
			Include(io.Node);
		foreach (var io in c.Outputs.Values)
			Include(io.Node);

		void Include(SKPoint p)
		{
			minX = Math.Min(minX, p.X);
			minY = Math.Min(minY, p.Y);
			maxX = Math.Max(maxX, p.X);
			maxY = Math.Max(maxY, p.Y);
		}

		if (float.IsInfinity(minX) || float.IsInfinity(minY) || float.IsInfinity(maxX) || float.IsInfinity(maxY))
			return SKRect.Empty;

		const float fudge = 90f;
		return SKRect.Create(minX - fudge, minY - fudge, (maxX - minX) + fudge * 2, (maxY - minY) + fudge * 2);
	}
}
