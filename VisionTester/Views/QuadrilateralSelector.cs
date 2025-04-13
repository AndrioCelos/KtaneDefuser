using System;
using System.Globalization;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Media.Immutable;
using Avalonia.Metadata;
using KtaneDefuserConnector;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using Color = Avalonia.Media.Color;
using Point = Avalonia.Point;
using Size = Avalonia.Size;

namespace VisionTester.Views;

public class QuadrilateralSelector : Control {
	public static readonly StyledProperty<Image<Rgba32>?> SourceProperty = AvaloniaProperty.Register<QuadrilateralSelector, Image<Rgba32>?>(nameof(Source));
	public static readonly StyledProperty<Quadrilateral> QuadrilateralProperty = AvaloniaProperty.Register<QuadrilateralSelector, Quadrilateral>(nameof(Quadrilateral));
	private static readonly IPen SelectionLinePen = new ImmutablePen(Brushes.LightGray);
	private int? draggingPoint;
	private Point[] points { get; } = new Point[4];

	[Content]
	public Image<Rgba32>? Source
	{
		get => GetValue(SourceProperty);
		set => SetValue(SourceProperty, value);
	}

	public Quadrilateral Quadrilateral
	{
		get => GetValue(QuadrilateralProperty);
		set => SetValue(QuadrilateralProperty, value);
	}

	private IImage? avaloniaImage;

	private Rect imageRect;

	public event EventHandler? PointsChanged;

	public QuadrilateralSelector() => FocusableProperty.OverrideDefaultValue(typeof(QuadrilateralSelector), true);

	private Point ToDisplayPoint(Point bitmapPoint) => Source is null ? bitmapPoint : new(bitmapPoint.X * imageRect.Height / Source.Size.Height, bitmapPoint.Y * imageRect.Height / Source.Size.Height);
	private Point FromDisplayPoint(Point displayPoint) => Source is null ? displayPoint : new(displayPoint.X * Source.Size.Height / imageRect.Height, displayPoint.Y * Source.Size.Height / imageRect.Height);

	protected override Size MeasureOverride(Size availableSize) {
		if (Source is null) return new();
		var size = new Size(availableSize.Width, availableSize.Width * Source.Size.Height / Source.Size.Width);
		if (size.Height > availableSize.Height)
			size = new(availableSize.Height * Source.Size.Width / Source.Size.Height, availableSize.Height);
		return size;
	}

	protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
		if (change.Property == SourceProperty) {
			if (Source is null) {
				avaloniaImage = null;
			} else {
				using var ms = new MemoryStream();
				Source.Save(ms, new BmpEncoder());
				ms.Position = 0;
				avaloniaImage = new Bitmap(ms);
				UpdateImageRect();
				InvalidateMeasure();
			}
		} else if (change.Property == QuadrilateralProperty) {
			for (var i = 0; i < 4; i++)
				points[i] = new(Quadrilateral[i].X, Quadrilateral[i].Y);
			InvalidateVisual();
		}
		base.OnPropertyChanged(change);
	}

	protected override void OnSizeChanged(SizeChangedEventArgs e) {
		UpdateImageRect();
	}

	private void UpdateImageRect() {
		if (Source is null) return;
		var size = new Size(Bounds.Width, Bounds.Width * Source.Size.Height / Source.Size.Width);
		if (size.Height > Bounds.Height)
			size = new(Bounds.Height * Source.Size.Width / Source.Size.Height, Bounds.Height);
		imageRect = new((Bounds.Width - size.Width) / 2, (Bounds.Height - size.Height) / 2, size.Width, size.Height);
	}

	public override void Render(DrawingContext context) {
		if (Source is null || avaloniaImage is null) return;
		context.DrawImage(avaloniaImage, imageRect);
		context.DrawLine(SelectionLinePen, ToDisplayPoint(points[0]), ToDisplayPoint(points[1]));
		context.DrawLine(SelectionLinePen, ToDisplayPoint(points[1]), ToDisplayPoint(points[3]));
		context.DrawLine(SelectionLinePen, ToDisplayPoint(points[3]), ToDisplayPoint(points[2]));
		context.DrawLine(SelectionLinePen, ToDisplayPoint(points[2]), ToDisplayPoint(points[0]));
		for (int i = 0; i < 4; i++) {
			var brush = i switch { 0 => Brushes.Red, 1 => Brushes.Yellow, 2 => Brushes.Lime, _ => Brushes.RoyalBlue };
			var point = ToDisplayPoint(points[i]);
			context.DrawEllipse(brush, null, point, 5, 5);
		}
		if (draggingPoint != null) {
			var point = points[draggingPoint.Value];
			var displayPoint = ToDisplayPoint(point);
			//e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			var destRect = new Rect(displayPoint.X + 16, displayPoint.Y + 16, 144, 144);
			
			for (int y = 0; y < 9; y++) {
				for (int x = 0; x < 9; x++) {
					var x2 = (int) Math.Round(point.X + x - 4);
					var y2 = (int) Math.Round(point.Y + y - 4);
					if (x2 >= 0 && y2 >= 0 && x2 < Source.Size.Width && y2 < Source.Size.Height) {
						var pixel = Source[x2, y2];
						context.FillRectangle(new ImmutableSolidColorBrush(Color.FromArgb(byte.MaxValue, pixel.R, pixel.G, pixel.B)), new(displayPoint.X + 16 + x * 16, displayPoint.Y + 16 + y * 16, 16, 16));
					}
				}
			}
			context.DrawRectangle(null, SelectionLinePen, destRect);
			context.FillRectangle(Brushes.White, new(displayPoint.X + 16 + 72, displayPoint.Y + 16 + 72, 1, 1));
			var rectangle = new Rect(destRect.Left, destRect.Bottom, 72, 24);
			context.FillRectangle(new ImmutableSolidColorBrush(Color.FromArgb(192, 0, 0, 0)), rectangle);
			context.DrawText(new($"({(int) Math.Round(point.X)}, {(int) Math.Round(point.Y)})", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, Typeface.Default, 12, Brushes.Magenta), rectangle.Position);
		}
	}

	protected override void OnPointerPressed(PointerPressedEventArgs e) {
		var displayPoint = e.GetCurrentPoint(this).Position;
		var ds = new double[4];
		var index = 0;
		for (int i = 0; i < 4; i++) {
			var point = ToDisplayPoint(points[i]);
			ds[i] = (displayPoint.X - point.X) * (displayPoint.X - point.X) + (displayPoint.Y - point.Y) * (displayPoint.Y - point.Y);
			if (i != 0 && ds[i] < ds[index]) index = i;
		}
		if (ds[index] > 100) return;
		draggingPoint = index;
		InvalidateVisual();
	}

	protected override void OnPointerMoved(PointerEventArgs e) {
		if (draggingPoint != null && Source is not null) {
			var displayPoint = e.GetCurrentPoint(this).Position;
			var point = FromDisplayPoint(displayPoint);
			point = new(Math.Min(Math.Max(point.X, 0), Source.Width), Math.Min(Math.Max(point.Y, 0), Source.Height));
			points[draggingPoint.Value] = point;
			UpdateQuadrilateral();
		}
	}

	private void UpdateQuadrilateral() {
		Quadrilateral = new(
			new((int) Math.Round(points[0].X), (int) Math.Round(points[0].Y)),
			new((int) Math.Round(points[1].X), (int) Math.Round(points[1].Y)),
			new((int) Math.Round(points[2].X), (int) Math.Round(points[2].Y)),
			new((int) Math.Round(points[3].X), (int) Math.Round(points[3].Y))
		);
		PointsChanged?.Invoke(this, EventArgs.Empty);
		InvalidateVisual();
	}

	protected override void OnPointerReleased(PointerReleasedEventArgs e) {
		draggingPoint = null;
		InvalidateVisual();
	}

	protected override void OnKeyDown(KeyEventArgs e) {
		if (draggingPoint is not { } i) return;
		switch (e.Key) {
			case Key.W:
				points[i] += new Point(0, -1);
				UpdateQuadrilateral();
				e.Handled = true;
				break;
			case Key.S:
				points[i] += new Point(0, 1);
				UpdateQuadrilateral();
				e.Handled = true;
				break;
			case Key.A:
				points[i] += new Point(-1, 0);
				UpdateQuadrilateral();
				e.Handled = true;
				break;
			case Key.D:
				points[i] += new Point(1, 0);
				UpdateQuadrilateral();
				e.Handled = true;
				break;
		}
	}
}
