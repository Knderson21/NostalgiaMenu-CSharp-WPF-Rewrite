using System;
using System.Windows;
using System.Windows.Media;

namespace NostalgiaMenu.Controls
{
    /// <summary>
    /// A circular arc that depletes as RemainingSeconds approaches zero.
    /// Drawn entirely with DrawingContext — no external dependencies.
    /// </summary>
    public class CountdownRing : FrameworkElement
    {
        public static readonly DependencyProperty TotalSecondsProperty =
            DependencyProperty.Register("TotalSeconds", typeof(double), typeof(CountdownRing),
                new FrameworkPropertyMetadata(60.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RemainingSecondsProperty =
            DependencyProperty.Register("RemainingSeconds", typeof(double), typeof(CountdownRing),
                new FrameworkPropertyMetadata(60.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty RingColorProperty =
            DependencyProperty.Register("RingColor", typeof(Color), typeof(CountdownRing),
                new FrameworkPropertyMetadata(Colors.Gold, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TrackColorProperty =
            DependencyProperty.Register("TrackColor", typeof(Color), typeof(CountdownRing),
                new FrameworkPropertyMetadata(Color.FromRgb(0x2A, 0x2A, 0x2A),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register("StrokeThickness", typeof(double), typeof(CountdownRing),
                new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public double TotalSeconds
        {
            get { return (double)GetValue(TotalSecondsProperty); }
            set { SetValue(TotalSecondsProperty, value); }
        }

        public double RemainingSeconds
        {
            get { return (double)GetValue(RemainingSecondsProperty); }
            set { SetValue(RemainingSecondsProperty, value); }
        }

        public Color RingColor
        {
            get { return (Color)GetValue(RingColorProperty); }
            set { SetValue(RingColorProperty, value); }
        }

        public Color TrackColor
        {
            get { return (Color)GetValue(TrackColorProperty); }
            set { SetValue(TrackColorProperty, value); }
        }

        public double StrokeThickness
        {
            get { return (double)GetValue(StrokeThicknessProperty); }
            set { SetValue(StrokeThicknessProperty, value); }
        }

        protected override void OnRender(DrawingContext dc)
        {
            double w  = ActualWidth;
            double h  = ActualHeight;
            double cx = w / 2.0;
            double cy = h / 2.0;
            double sw = StrokeThickness;
            double r  = Math.Min(w, h) / 2.0 - sw / 2.0;

            if (r <= 0) return;

            var trackPen = new Pen(new SolidColorBrush(TrackColor), sw)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap   = PenLineCap.Round
            };
            var ringPen = new Pen(new SolidColorBrush(RingColor), sw)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap   = PenLineCap.Round
            };

            // Full circle track
            dc.DrawEllipse(null, trackPen, new Point(cx, cy), r, r);

            double fraction = TotalSeconds > 0
                ? Math.Max(0.0, Math.Min(1.0, RemainingSeconds / TotalSeconds))
                : 0.0;

            if (fraction <= 0.0) return;

            if (fraction >= 1.0)
            {
                dc.DrawEllipse(null, ringPen, new Point(cx, cy), r, r);
                return;
            }

            // Arc starts at top (−90°) and sweeps clockwise
            double startAngle = -Math.PI / 2.0;
            double sweepAngle = fraction * 2.0 * Math.PI;
            double endAngle   = startAngle + sweepAngle;

            var arcStart = new Point(cx + r * Math.Cos(startAngle), cy + r * Math.Sin(startAngle));
            var arcEnd   = new Point(cx + r * Math.Cos(endAngle),   cy + r * Math.Sin(endAngle));
            bool isLarge = sweepAngle > Math.PI;

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(arcStart, false, false);
                ctx.ArcTo(arcEnd, new Size(r, r), 0.0, isLarge, SweepDirection.Clockwise, true, false);
            }
            geo.Freeze();

            dc.DrawGeometry(null, ringPen, geo);
        }
    }
}
