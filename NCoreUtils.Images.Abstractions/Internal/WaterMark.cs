using System;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Internal
{
    public class WaterMark : IFilter, IEmplaceable<WaterMark>
    {
        public IImageSource WaterMarkSource { get; }

        public WaterMarkGravity Gravity { get; } = WaterMarkGravity.Northeast;

        public int? X { get; }

        public int? Y { get; }


        public WaterMark(IImageSource waterMarkSource)
        {
            WaterMarkSource = waterMarkSource;
        }
        public WaterMark(IImageSource waterMarkSource, WaterMarkGravity gravity) : this(waterMarkSource)
        {
            Gravity = gravity;
        }

        public WaterMark(IImageSource waterMarkSource, WaterMarkGravity gravity, int x, int y) : this(waterMarkSource, gravity)
        {
            X = x;
            Y = y;
        }

        public int Emplace(Span<char> span)
        {
            if (TryEmplace(span, out var used))
            {
                return used;
            }
            throw new ArgumentException("Insufficient buffer size.", nameof(span));
        }

        public bool TryEmplace(Span<char> span, out int used)
        {
            if (WaterMarkSource is ISerializableImageResource serializableImage)
            {
                var builder = new SpanBuilder(span);
                if (builder.TryAppend("watermark(")
                    && builder.TryAppend(serializableImage.Uri)
                    && builder.TryAppend(",")
                    && builder.TryAppend((int)Gravity)
                    && builder.TryAppend(",")
                    && builder.TryAppend(X ?? 0)
                    && builder.TryAppend(",")
                    && builder.TryAppend(Y ?? 0)
                    && builder.TryAppend(')'))
                {
                    used = builder.Length;
                    return true;
                }
            }
            else
            {
                throw new ArgumentException("Image is not serializable.", nameof(WaterMarkSource));
            }
            used = default;
            return false;
        }
    }
}
