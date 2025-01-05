using calculator_gui;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
namespace noughts_and_crosses_old
{
    class Scene3D
    {
        private List<Object3D> _objects;
        public List<Object3D> Objects
        {
            get
            {
                return _objects;
            }
            set
            {
                _objects = value;
            }
        }

        private Camera3D _camera;
        public Camera3D Camera 
        { 
            get 
            { 
                return _camera; 
            }
            set
            {
                _camera = value;
            }
        }

        private BitmapRenderer Canvas;

        private Color edgeColour = Color.White;

        public Scene3D(ref System.Windows.Controls.Image image)
        {
            _objects = new List<Object3D>();
            _objects.Add(new Object3D());
            _camera = new Camera3D(60.0f, new Point3D(0.0f, 3.0f, 0.0f), (0.0f, 0.0f, DegreesToRadians(10.0f)), 0.5f);
            Canvas = new BitmapRenderer(1000, 1000);
            image.Source = Canvas.bitmap;

            Render();
        }

        private float DegreesToRadians(float degrees)
        {
            return (float)(Math.PI * degrees / 180.0f);
        }

        public void Render()
        {
            Canvas.Fill(Color.Transparent);
            (int,int) CanvasSpaceTransform(Point3D coords)
            {
                return
                    (
                        (int)((coords.X * Camera.Zoom + 1) / 2.0f * Canvas.width),
                        (int)((coords.Z * Camera.Zoom + 1) / 2.0f * Canvas.height)
                    );
            }

            Matrix RotationZ = new Matrix(
                (float)Math.Cos(DegreesToRadians(Camera.Rotation.Item3)), (float)-Math.Sin(DegreesToRadians(Camera.Rotation.Item3)), 0.0f, 0.0f,
                (float)Math.Sin(DegreesToRadians(Camera.Rotation.Item3)), (float) Math.Cos(DegreesToRadians(Camera.Rotation.Item3)), 0.0f, 0.0f,
                0.0f, 0.0f, 1.0f, 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
                );

            Matrix RotationY = new Matrix(
                (float)Math.Cos(DegreesToRadians(Camera.Rotation.Item2)), 0.0f, (float)Math.Sin(DegreesToRadians(Camera.Rotation.Item2)), 0.0f,
                0.0f, 1.0f, 0.0f, 0.0f,
                (float)-Math.Sin(DegreesToRadians(Camera.Rotation.Item2)), 0.0f, (float)Math.Cos(DegreesToRadians(Camera.Rotation.Item2)), 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
                );

            Matrix RotationX = new Matrix(
                1.0f, 0.0f, 0.0f, 0.0f,
                0.0f, (float)Math.Cos(DegreesToRadians(Camera.Rotation.Item1)), (float)-Math.Sin(DegreesToRadians(Camera.Rotation.Item1)), 0.0f,
                0.0f, (float)Math.Sin(DegreesToRadians(Camera.Rotation.Item1)), (float)Math.Cos(DegreesToRadians(Camera.Rotation.Item1)), 0.0f,
                0.0f, 0.0f, 0.0f, 1.0f
                );

            foreach (Object3D obj in _objects)
            {
                if (obj.Wireframe == true)
                {
                    foreach ((int, int) edge in obj.Edges)
                    {
                        (int, int) vertexOne = CanvasSpaceTransform(RotationZ.Multiply(RotationY.Multiply(RotationX.Multiply(Camera.matrix.Multiply(obj.Vertices[edge.Item1])))));
                        (int, int) vertexTwo = CanvasSpaceTransform(RotationZ.Multiply(RotationY.Multiply(RotationX.Multiply(Camera.matrix.Multiply(obj.Vertices[edge.Item2])))));
                        Canvas.DrawLine(vertexOne.Item1, vertexOne.Item2, vertexTwo.Item1, vertexTwo.Item2, ref edgeColour, 10);
                    }
                }
                else
                {
                    for (int faceIndex = 0; faceIndex < obj.Faces.Length; faceIndex++)
                    {
                        (int, int, int) face = obj.Faces[faceIndex];
                        (int, int) vertexOne = CanvasSpaceTransform(RotationZ.Multiply(RotationY.Multiply(RotationX.Multiply(Camera.matrix.Multiply(obj.Vertices[face.Item1])))));
                        (int, int) vertexTwo = CanvasSpaceTransform(RotationZ.Multiply(RotationY.Multiply(RotationX.Multiply(Camera.matrix.Multiply(obj.Vertices[face.Item2])))));
                        (int, int) vertexThree = CanvasSpaceTransform(RotationZ.Multiply(RotationY.Multiply(RotationX.Multiply(Camera.matrix.Multiply(obj.Vertices[face.Item3])))));
                        Canvas.DrawTriangle(vertexOne.Item1, vertexOne.Item2, vertexTwo.Item1, vertexTwo.Item2, vertexThree.Item1, vertexThree.Item2, ref obj.FaceColours[faceIndex]);
                    }
                }
            }
        }
    }

    class Object3D
    {
        public Point3D[] Vertices;
        public (int, int)[] Edges;
        public (int, int, int)[] Faces;
        public Color[] FaceColours;

        public bool Wireframe;

        public Object3D()
        {
            Vertices =
                [
                    new Point3D(-1.0f, -1.0f, -1.0f), // 0     bottom  left    back
                    new Point3D(-1.0f, -1.0f,  1.0f), // 1     top     left    back
                    new Point3D(-1.0f,  1.0f, -1.0f), // 2     bottom  left    front
                    new Point3D(-1.0f,  1.0f,  1.0f), // 3     top     left    front
                    new Point3D( 1.0f, -1.0f, -1.0f), // 4     bottom  right   back
                    new Point3D( 1.0f, -1.0f,  1.0f), // 5     top     right   back
                    new Point3D( 1.0f,  1.0f, -1.0f), // 6     bottom  right   front
                    new Point3D( 1.0f,  1.0f,  1.0f)  // 7     top     right   front
                ];

            Edges =
                [
                    (0, 1), (0, 2), (0, 4),
                    (1, 3), (1, 5),
                    (2, 3), (2, 6),
                    (3, 7),
                    (4, 5), (4, 6),
                    (5, 7),
                    (6, 7)
                ];
            Faces =
                [
                    (0, 1, 2), (0, 1, 4), (0, 2, 4),
                    (1, 2, 3), (1, 3, 5), (1, 4, 5),
                    (2, 3, 6), (2, 4, 6),
                    (3, 5, 7), (3, 6, 7),
                    (4, 5, 6),
                    (5, 6, 7)
                ];
            FaceColours =
                [
                    Color.Magenta, Color.Cyan, Color.Yellow,
                    Color.Magenta, Color.Blue, Color.Cyan,
                    Color.Red, Color.Yellow,
                    Color.Blue, Color.Red,
                    Color.Green,
                    Color.Green,
                ];
            Wireframe = true;
        }
    }

    class Camera3D
    {
        public float FOV;
        public Point3D Position;
        public (float, float, float) Rotation;
        public float Zoom;

        public Matrix matrix;

        public Camera3D(float fov, Point3D position, (float,float,float) rotation, float zoom)
        {
            FOV = fov;
            Position = position;
            Rotation = rotation;
            Zoom = zoom;

            matrix = new Matrix
                (
                    1, 0, 0, -position.X,
                    0, 1, 0, -position.Y,
                    0, 0, 1, -position.Z,
                    0, 0, 0, 1
                );
        }
    }

    struct Point3D
    {
        public float X, Y, Z;

        public Point3D(float x, float y, float z)
        {
            X = x;
            Y = y; 
            Z = z;
        }

        public static Point3D operator +(Point3D point1, Point3D point2)
        {
            return new Point3D(point1.X + point2.X, point1.Y + point2.Y, point1.Z + point2.Z);
        }

        public static Point3D operator -(Point3D point1, Point3D point2)
        {
            return new Point3D(point1.X - point2.X, point1.Y - point2.Y, point1.Z - point2.Z);
        }
    }

    class Matrix
    {
        private float m00;
        private float m01;
        private float m02;
        private float m03;
        private float m10;
        private float m11;
        private float m12;
        private float m13;
        private float m20;
        private float m21;
        private float m22;
        private float m23;
        private float m30;
        private float m31;
        private float m32;
        private float m33;

        public Matrix(
            float m00, float m01, float m02, float m03, 
            float m10, float m11, float m12, float m13,
            float m20, float m21, float m22, float m23,
            float m30, float m31, float m32, float m33)
        {
            this.m00 = m00;
            this.m01 = m01;
            this.m02 = m02;
            this.m03 = m03;
            this.m10 = m10;
            this.m11 = m11;
            this.m12 = m12;
            this.m13 = m13;
            this.m20 = m20;
            this.m21 = m21;
            this.m22 = m22;
            this.m23 = m23;
            this.m30 = m30;
            this.m31 = m31;
            this.m32 = m32;
            this.m33 = m33;
        }

        public (float, float, float, float) Multiply((float, float, float, float) vector)
        {
            return
                (
                (m00 * vector.Item1) + (m01 * vector.Item2) + (m02 * vector.Item3) + (m03 * vector.Item4),
                (m10 * vector.Item1) + (m11 * vector.Item2) + (m12 * vector.Item3) + (m13 * vector.Item4),
                (m20 * vector.Item1) + (m21 * vector.Item2) + (m22 * vector.Item3) + (m23 * vector.Item4),
                (m30 * vector.Item1) + (m31 * vector.Item2) + (m32 * vector.Item3) + (m33 * vector.Item4)
                );
        }

        public Point3D Multiply(Point3D vector)
        {
            (float, float, float, float) result = Multiply((vector.X, vector.Y, vector.Z, 1));
            return new Point3D(result.Item1, result.Item2, result.Item3);
        }
    }
}
