using System;
using NCoreUtils.Memory;

namespace NCoreUtils.Images.Internal
{
    public class WaterMark : IFilter, IEmplaceable<WaterMark>
    {
        public WaterMark(IImageSource waterMarkSource)
        {
            WaterMarkSource = waterMarkSource;
        }

        public IImageSource WaterMarkSource { get; }

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
                    && builder.TryAppend(serializableImage)
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
