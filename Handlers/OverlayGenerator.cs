using System;
using Android.Graphics;
using Android.Graphics.Drawables;
using Emgu.CV.Util;

namespace CameraX.Handlers
{
    public class OverlayGenerator : Drawable
    {
        private readonly VectorOfPoint _contourData;
        
        private readonly Paint _boundingRectPaint = new Paint
        {
            StrokeWidth = 5f,
            Color = Color.Yellow,
            Alpha = 200,
        };

        private readonly Paint _contentRectPaint = new Paint
        {
            Color = Color.Yellow,
            Alpha = 255,
        };

        private readonly Paint _contentTextPaint = new Paint
        {
            Color = Color.DarkGray,
            Alpha = 255,
            TextSize = 36f,
        };
        
        private readonly int _contentPadding = 25;
        
        public OverlayGenerator(VectorOfPoint contourData)
        {
            _contourData = contourData;
        }

        public override void Draw(Canvas canvas)
        {
            var boxData = _contourData.ToArray();
            // Calculate the bounding box coordinates
            var left = int.MaxValue;
            var top = int.MaxValue;
            var right = int.MinValue;
            var bottom = int.MinValue;

            foreach (var point in boxData)
            {
                left = Math.Min(left, point.X);
                top = Math.Min(top, point.Y);
                right = Math.Max(right, point.X);
                bottom = Math.Max(bottom, point.Y);
            }

            var boundingRect = new Rect(left, top, right, bottom);
            
            canvas.DrawRect(
                boundingRect,
                _contentRectPaint
            );
        }

        public override void SetAlpha(int alpha)
        {
            _boundingRectPaint.Alpha = alpha;
            _contentRectPaint.Alpha = alpha;
            _contentTextPaint.Alpha = alpha;
        }

        public override void SetColorFilter(ColorFilter? colorFilter)
        {
            _boundingRectPaint.SetColorFilter(colorFilter);
            _contentRectPaint.SetColorFilter(colorFilter);
            _contentTextPaint.SetColorFilter(colorFilter);
        }

        public override int Opacity => (int)Format.Translucent;
    }
}