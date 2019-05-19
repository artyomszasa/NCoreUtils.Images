using System.IO;

namespace NCoreUtils.Images.Unit
{
    public static class Resources
    {
        public static byte[] Get10MbImage()
        {
            using (var buffer = new MemoryStream())
            using (var res = typeof(Resources).Assembly.GetManifestResourceStream("NCoreUtils.Images.Unit.Resources.10mb.jpg"))
            {
                res.CopyTo(buffer);
                return buffer.ToArray();
            }
        }
    }
}