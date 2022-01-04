using Pfim;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using ImageFormat = Pfim.ImageFormat;

namespace SEBlueprintCalc
{
    public class DGVItem<T>
    {
        public Bitmap Icon { get; set; }
        public string Name { get; set; }
        public T Count { get; set; }

        public DGVItem(string Name, T Count, string path)
        {
            this.Name = Name;
            this.Count = Count;
            this.Icon = Convert(path);
        }

        public static Bitmap Convert(string path)
        {
            try
            {
                using (var image = Pfim.Pfim.FromFile(path))
                {
                    PixelFormat format;

                    // Convert from Pfim's backend agnostic image format into GDI+'s image format
                    switch (image.Format)
                    {
                        case ImageFormat.Rgba32:
                            format = PixelFormat.Format32bppArgb;
                            break;
                        default:
                            // see the sample for more details
                            throw new NotImplementedException();
                    }

                    // Pin pfim's data array so that it doesn't get reaped by GC, unnecessary
                    // in this snippet but useful technique if the data was going to be used in
                    // control like a picture box
                    var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
                    try
                    {
                        var data = Marshal.UnsafeAddrOfPinnedArrayElement(image.Data, 0);
                        var bitmap = new Bitmap(image.Width, image.Height, image.Stride, format, data);
                        return new Bitmap(bitmap, new Size(50, 50));
                    }
                    finally
                    {
                        handle.Free();
                    }
                }
            }
            catch
            {
                return null;
            }
        }
    }

}
