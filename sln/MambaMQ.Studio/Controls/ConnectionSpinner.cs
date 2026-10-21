namespace MambaMQ.Studio.Controls;

public sealed class ConnectionSpinner : Control
{
    private static readonly double[] SegmentOpacities = [1, 0.78, 0.58, 0.42, 0.3, 0.24, 0.2, 0.18];
    private static readonly Point[] SegmentPoints =
    [
        new(-4.5, -35),
        new(4.5, -35),
        new(2.75, -25),
        new(-2.75, -25)
    ];

    private int _step;

    public int Step
    {
        get => _step;
        set
        {
            _step = (value % SegmentOpacities.Length + SegmentOpacities.Length) % SegmentOpacities.Length;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        double scale = Math.Min(Bounds.Width, Bounds.Height) / 80;
        double centerX = Bounds.Width / 2;
        double centerY = Bounds.Height / 2;

        for (int segmentIndex = 0; segmentIndex < SegmentOpacities.Length; segmentIndex++)
        {
            double angle = segmentIndex * Math.PI / 4;
            double cosine = Math.Cos(angle);
            double sine = Math.Sin(angle);
            
            Point[] points = SegmentPoints
                .Select(point => new Point(
                    centerX + (point.X * cosine - point.Y * sine) * scale,
                    centerY + (point.X * sine + point.Y * cosine) * scale))
                .ToArray();

            StreamGeometry geometry = new();

            using (StreamGeometryContext geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(points[0], isFilled: true);
                geometryContext.LineTo(points[1]);
                geometryContext.LineTo(points[2]);
                geometryContext.LineTo(points[3]);
                geometryContext.EndFigure(isClosed: true);
            }

            int fadeIndex = (Step - segmentIndex + SegmentOpacities.Length) % SegmentOpacities.Length;
            
            SolidColorBrush brush = new(Color.Parse("#F4F7FB"))
            {
                Opacity = SegmentOpacities[fadeIndex]
            };

            context.DrawGeometry(brush, null, geometry);
        }
    }
}
