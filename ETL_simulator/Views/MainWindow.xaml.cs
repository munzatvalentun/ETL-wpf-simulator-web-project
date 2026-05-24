using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ETL_simulator.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Icon = CreateAppIcon();
        }

        private static BitmapSource CreateAppIcon()
        {
            const int S = 32;
            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                // Background — indigo/violet gradient
                var bg = new LinearGradientBrush(
                    Color.FromRgb(79, 70, 229),   // #4f46e5
                    Color.FromRgb(124, 58, 237),  // #7c3aed
                    new Point(0, 0), new Point(1, 1));
                dc.DrawRoundedRectangle(bg, null, new Rect(0, 0, S, S), 7, 7);

                // Lightning bolt ⚡
                var bolt = new StreamGeometry();
                using (var ctx = bolt.Open())
                {
                    ctx.BeginFigure(new Point(20, 3),  isFilled: true, isClosed: true);
                    ctx.LineTo(new Point(11, 16), isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(new Point(17, 16), isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(new Point(13, 29), isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(new Point(22, 15), isStroked: true, isSmoothJoin: false);
                    ctx.LineTo(new Point(16, 15), isStroked: true, isSmoothJoin: false);
                }
                bolt.Freeze();
                dc.DrawGeometry(Brushes.White, null, bolt);
            }

            var rtb = new RenderTargetBitmap(S, S, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(dv);
            rtb.Freeze();
            return rtb;
        }
    }
}
