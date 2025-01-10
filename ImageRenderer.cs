using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace noughts_and_crosses
{
    class ImageRenderer
    {
        public WriteableBitmap bitmap;
        public int width;
        public int height;
        public bool showDepthMap;

        private Bitmap backingBitmap;
        private ushort[,] depthMap;
        private Graphics backingGraphics;

        public ImageRenderer(int newWidth, int newHeight)
        {
            width = newWidth;
            height = newHeight;
            showDepthMap = false;

            bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgr32, null);
            backingBitmap = new Bitmap(width, height, bitmap.BackBufferStride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, bitmap.BackBuffer);
            depthMap = new ushort[width, height];
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    depthMap[x, y] = ushort.MaxValue;
                }
            }
            backingGraphics = Graphics.FromImage(backingBitmap);
        }

        /// <summary>
        /// Lock the backbuffer for updates
        /// </summary>
        public void LockBuffer()
        {
            bitmap.Lock();
        }

        /// <summary>
        /// Mark the entire backbuffer as dirty, and unlock it
        /// </summary>
        public void UnlockBuffer()
        {
            bitmap.AddDirtyRect(new System.Windows.Int32Rect(0, 0, width, height));
            bitmap.Unlock();
        }

        /// <summary>
        /// Fill the entire image with a colour
        /// </summary>
        /// <param name="colour">The colour to fill with</param>
        public void Fill(System.Drawing.Color colour)
        {
            if (!showDepthMap)
            {
                // Fill the image with the given colour
                backingGraphics.Clear(colour);
            }
            else
            {
                // Fill the image with white
                backingGraphics.Clear(System.Drawing.Color.White);
            }
            // Set the depth buffer to max depth
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    depthMap[x, y] = ushort.MaxValue;
                }
            }
        }

        /// <summary>
        /// Draw a line between 2 points using a colour
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <param name="colour">The colour to draw with</param>
        public void DrawLine(Vector3D p1, Vector3D p2, System.Drawing.Color colour)
        {
            if (p1.Z < 0 || p2.Z < 0)
            {
                return;
            }
            // Draw the line
            backingGraphics.DrawLine(new System.Drawing.Pen(colour, 1), new Point((int)p1.X, (int)p1.Y), new Point((int)p2.X, (int)p2.Y));
        }

        /// <summary>
        /// Draw a triangle between 3 points using a colour. Updates the depth buffer.
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <param name="p3">Point 3</param>
        /// <param name="colour">The colour to draw with</param>
        public void DrawTriangle (Vector3D p1, Vector3D p2, Vector3D p3, System.Drawing.Color colour)
        {
            // backface culling
            int fullArea = Edge(p1, p2, p3);
            if (fullArea <= 0)
            {
                return;
            }

            int minX = Int32.Max(Int32.Min(Int32.Min((int)p1.X, (int)p2.X), (int)p3.X), 0);
            int minY = Int32.Max(Int32.Min(Int32.Min((int)p1.Y, (int)p2.Y), (int)p3.Y), 0);
            int maxX = Int32.Min(Int32.Max(Int32.Max((int)p1.X, (int)p2.X), (int)p3.X), width - 1);
            int maxY = Int32.Min(Int32.Max(Int32.Max((int)p1.Y, (int)p2.Y), (int)p3.Y), height - 1);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    int area3 = Edge(p1, p2, new Vector3D(x, y, 0));
                    if (area3 < 0)
                    {
                        continue;
                    }
                    int area2 = Edge(p1, new Vector3D(x, y, 0), p3);
                    if (area2 < 0)
                    {
                        continue;
                    }
                    int area1 = Edge(new Vector3D(x, y, 0), p2, p3);
                    if (area1 < 0)
                    {
                        continue;
                    }
                    else
                    {
                        // find depth
                        float depth = ((float)area1 / fullArea) * p1.Z + ((float)area2 / fullArea) * p2.Z + ((float)area3 / fullArea) * p3.Z;
                        if (depth < 0)
                        {
                            continue;
                        }
                        int intDepth = (int)(depth * ushort.MaxValue);
                        if (intDepth < depthMap[x, y])
                        {
                            // set this pixel
                            if (!showDepthMap)
                            {
                                backingBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(
                                255,
                                (int)(255 * ((float)area1 / fullArea)),
                                (int)(255 * ((float)area2 / fullArea)),
                                (int)(255 * ((float)area3 / fullArea))));
                            }
                            else
                            {
                                backingBitmap.SetPixel(x, y, System.Drawing.Color.FromArgb(255, intDepth / 256, intDepth / 256, intDepth / 256));
                            }
                            depthMap[x, y] = (ushort)intDepth;
                        }
                    }
                }
            }
        }

        private int Edge(Vector3D p1, Vector3D p2, Vector3D p3)
        {
            return ((int)p2.X - (int)p1.X) * ((int)p3.Y - (int)p1.Y) - ((int)p2.Y - (int)p1.Y) * ((int)p3.X - (int)p1.X);
        }
    }
}
