﻿using noughts_and_crosses;
using System.CodeDom;
using System.Drawing;
using System.Reflection.Metadata;

namespace noughts_and_crosses
{
    class Scene3D
    {
        public Camera3D Camera;
        public List<Object3D> Objects;
        public bool Wireframe;

        private ImageRenderer image;
        private Matrix4D OrthoToImageMatrix;

        public Scene3D(ref System.Windows.Controls.Image canvas)
        {
            image = new ImageRenderer(750, 750);
            canvas.Source = image.bitmap;

            Camera = new Camera3D(60.0f, 0.1f, 10.0f, 1.0f, new Vector3D(0.0f, 0.0f, 3.0f), new Quaternion(1.0f, 0.0f, 0.0f, 0.0f));
            Objects = new List<Object3D>();

            Objects.Add(new Object3D() 
            { 
                Scale = new Vector3D(0.5f, 0.5f, 0.5f) 
            });

            Objects.Add(new Object3D()
            {
                Position = new Vector3D(-0.5f, -0.1f, 0.5f),
                Scale = new Vector3D(0.25f, 0.25f, 0.25f)
            });

            Objects.Add(new Object3D()
            {
                Position = new Vector3D(-0.5f, 0.5f, 0.5f),
                Rotation = new Quaternion(0.42f, 0.54f, 0.67f, -0.28f),
                Scale = new Vector3D(0.125f, 0.25f, 0.125f)
            });

            OrthoToImageMatrix = Matrix4D.ScaleMatrix(new Vector3D(image.width / 2, -image.height / 2, 1.0f)) * Matrix4D.TranslationMatrix(new Vector3D(1.0f, -1.0f, 0.0f));
            Wireframe = true;
            image.showDepthMap = false;

            Render();
        }

        public void NewSize(int width, int height, ref System.Windows.Controls.Image controlImage)
        {
            image = new ImageRenderer(width, height);
            controlImage.Source = image.bitmap;
            Render();
        }

        public void Render()
        {
            image.LockBuffer();
            image.Fill(Color.Black);

            Matrix4D WorldToImageMatrix = OrthoToImageMatrix * Camera.OrthographicMatrix * Camera.PerspectiveMatrix * Matrix4D.ScaleMatrix(new Vector3D(1.0f, 1.0f, -1.0f)) * Camera.CameraSpaceMatrix;

            foreach (Object3D obj in Objects)
            {
                Matrix4D LocalToImageMatrix = WorldToImageMatrix * obj.LocalToWorldMatrix;
                if (Wireframe)
                {
                    for (int edgeIndex = 0; edgeIndex < obj.Edges.Length; edgeIndex++)
                    {
                        (int, int) edge = obj.Edges[edgeIndex];
                        Vector4D vertex1 = obj.Vertices[edge.Item1];
                        Vector4D vertex2 = obj.Vertices[edge.Item2];
                        vertex1 = LocalToImageMatrix * vertex1;
                        vertex2 = LocalToImageMatrix * vertex2;
                        image.DrawLine(
                            new Vector3D(vertex1.X / vertex1.W, vertex1.Y / vertex1.W, vertex1.Z / vertex1.W), 
                            new Vector3D(vertex2.X / vertex2.W, vertex2.Y / vertex2.W, vertex2.Z / vertex2.W), 
                            obj.colour);
                    }
                }
                else
                {
                    for (int faceIndex = 0; faceIndex < obj.Faces.Length; faceIndex++)
                    {
                        (int, int, int) face = obj.Faces[faceIndex];
                        Vector4D vertex1 = obj.Vertices[face.Item1];
                        Vector4D vertex2 = obj.Vertices[face.Item2];
                        Vector4D vertex3 = obj.Vertices[face.Item3];
                        vertex1 = LocalToImageMatrix * vertex1;
                        vertex2 = LocalToImageMatrix * vertex2;
                        vertex3 = LocalToImageMatrix * vertex3;
                        image.DrawTriangle(
                            new Vector3D(vertex1.X / vertex1.W, vertex1.Y / vertex1.W, vertex1.Z / vertex1.W),
                            new Vector3D(vertex2.X / vertex2.W, vertex2.Y / vertex2.W, vertex2.Z / vertex2.W),
                            new Vector3D(vertex3.X / vertex3.W, vertex3.Y / vertex3.W, vertex3.Z / vertex3.W),
                            obj.colour);
                    }
                }
            }
            image.UnlockBuffer();
        }

        public static float DegreesToRadians(float degrees)
        {
            return (float)(Math.PI * degrees / 180);
        }

        public static float SinDegrees(float degrees)
        {
            return (float)Math.Sin(Math.PI * degrees / 180.0f);
        }

        public static float CosDegrees(float degrees)
        {
            return (float)Math.Cos(Math.PI * degrees / 180.0f);
        }

        public static float TanDegrees(float degrees)
        {
            return (float)Math.Tan(Math.PI * degrees / 180.0f);
        }

        public static float ArcsinDegrees(float length)
        {
            return 180.0f * (float)Math.Asin(length) / (float)Math.PI;
        }

        public static float ArccosDegrees(float length)
        {
            return 180.0f * (float)Math.Acos(length) / (float)Math.PI;
        }

        public static float ArctanDegrees(float length)
        {
            return 180.0f * (float)Math.Atan(length) / (float)Math.PI;
        }
    }

    class Camera3D
    {
        public float FOV;
        public float NearClip;
        public float FarClip;
        public float AspectRatio;

        private Vector3D _position;
        public Vector3D Position
        {
            get => _position;
            set
            {
                _position = value;
                CalculateMatrices();
            }
        }

        private Quaternion _rotation;
        public Quaternion Rotation
        {
            get => _rotation;
            set
            {
                _rotation = value;
                CalculateMatrices();
            }
        }

        public Matrix4D CameraSpaceMatrix { get; private set; }
        public Matrix4D PerspectiveMatrix { get; private set; }
        public Matrix4D OrthographicMatrix { get; private set; }

        public float VerticalFOV
        {
            get
            {
                return FOV / AspectRatio;
            }
        }
        
        public Camera3D(float fov, float nearClip, float farClip, float aspectRatio, Vector3D position, Quaternion rotation)
        {
            FOV = fov;
            NearClip = nearClip;
            FarClip = farClip;
            AspectRatio = aspectRatio;
            _position = position;
            _rotation = rotation;
            CalculateMatrices();
        }

        public void CalculateMatrices()
        {
            Matrix4D translationMatrix = Matrix4D.TranslationMatrix(-Position);
            CameraSpaceMatrix = Quaternion.ToRotationMatrix(Rotation) * translationMatrix;

            Matrix4D orthoTranslateMatrix = Matrix4D.TranslationMatrix(new Vector3D(0.0f, 0.0f, -NearClip));
            Matrix4D orthoScaleMatrix = Matrix4D.ScaleMatrix(new Vector3D(1 / (NearClip * Scene3D.TanDegrees(FOV / 2.0f)), 1 / (NearClip * Scene3D.TanDegrees(FOV / 2.0f) / AspectRatio), 1 / (FarClip - NearClip)));
            OrthographicMatrix = orthoScaleMatrix * orthoTranslateMatrix;

            PerspectiveMatrix = new Matrix4D(
                NearClip, 0.0f, 0.0f, 0.0f,
                0.0f, NearClip, 0.0f, 0.0f,
                0.0f, 0.0f, FarClip + NearClip, -FarClip * NearClip,
                0.0f, 0.0f, 1.0f, 0.0f
                );
        }
    }

    class Object3D
    {
        private Vector3D _position;
        public Vector3D Position
        {
            get
            {
                return _position;
            }
            set
            {
                _position = value;
                CalculateMatrices();
            }
        }

        private Quaternion _rotation;
        public Quaternion Rotation
        {
            get
            {
                return _rotation;
            }
            set
            {
                _rotation = value;
                CalculateMatrices();
            }
        }

        private Vector3D _scale;
        public Vector3D Scale
        {
            get
            {
                return _scale;
            }
            set
            {
                _scale = value;
                CalculateMatrices();
            }
        }

        public Matrix4D LocalToWorldMatrix { get; private set; }

        public Vector3D[] Vertices;
        public (int, int)[] Edges;
        public (int, int, int)[] Faces;
        public Color colour;

        public Object3D()
        {
            _position = new Vector3D(0.0f, 0.0f, 0.0f);
            _rotation = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f);
            _scale = new Vector3D(1.0f, 1.0f, 1.0f);
            CalculateMatrices();

            Vertices =
                [
                    new Vector3D(-1.0f, -1.0f, -1.0f),
                    new Vector3D(-1.0f, -1.0f,  1.0f),
                    new Vector3D(-1.0f,  1.0f, -1.0f),
                    new Vector3D(-1.0f,  1.0f,  1.0f),
                    new Vector3D( 1.0f, -1.0f, -1.0f),
                    new Vector3D( 1.0f, -1.0f,  1.0f),
                    new Vector3D( 1.0f,  1.0f, -1.0f),
                    new Vector3D( 1.0f,  1.0f,  1.0f),
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
                    (0, 2, 1), (0, 1, 4), (0, 4, 2),
                    (1, 2, 3), (1, 3, 5), (1, 5, 4),
                    (2, 6, 3), (2, 4, 6),
                    (3, 7, 5), (3, 6, 7),
                    (4, 5, 6),
                    (5, 7, 6)
                ];

            colour = Color.White;
        }

        private void CalculateMatrices()
        {
            Matrix4D translationMatrix = Matrix4D.TranslationMatrix(Position);
            Matrix4D scaleMatrix = Matrix4D.ScaleMatrix(Scale);
            LocalToWorldMatrix = translationMatrix * Quaternion.ToRotationMatrix(Rotation) * scaleMatrix;
        }
    }

    struct Vector3D
    {
        public float X, Y, Z;
        public Vector3D(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3D operator +(Vector3D vector) => vector;
        public static Vector3D operator -(Vector3D vector) => new Vector3D(-vector.X, -vector.Y, -vector.Z);

        public static Vector3D operator +(Vector3D v1, Vector3D v2) => new Vector3D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        public static Vector3D operator -(Vector3D v1, Vector3D v2) => new Vector3D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);

        public static Vector3D operator *(float scalar, Vector3D vector) => new Vector3D(scalar * vector.X, scalar * vector.Y, scalar * vector.Z);
        public static Vector3D operator *(Vector3D vector, float scalar) => scalar * vector;

        public static Vector3D operator /(float scalar, Vector3D vector) => (1 / scalar) * vector;
        public static Vector3D operator /(Vector3D vector, float scalar) => (1 / scalar) * vector;

        public static explicit operator Vector3D(Vector4D vector) => new Vector3D(vector.X, vector.Y, vector.Z);

        public static Vector3D Normalise(Vector3D vector)
        {
            float magnitude = (float)Math.Sqrt(vector.X * vector.X + vector.Y * vector.Y + vector.Z * vector.Z);
            return new Vector3D(vector.X / magnitude, vector.Y / magnitude, vector.Z / magnitude);
        }

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ")";
        }
    }

    struct Vector4D
    {
        public float X, Y, Z, W;
        public Vector4D(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Vector4D operator +(Vector4D vector) => vector;
        public static Vector4D operator -(Vector4D vector) => new Vector4D(-vector.X, -vector.Y, -vector.Z, -vector.W);

        public static Vector4D operator +(Vector4D v1, Vector4D v2) => new Vector4D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z, v1.W + v2.W);
        public static Vector4D operator -(Vector4D v1, Vector4D v2) => new Vector4D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z, v1.W - v2.W);

        public static Vector4D operator *(float scalar, Vector4D vector) => new Vector4D(scalar * vector.X, scalar * vector.Y, scalar * vector.Z, scalar * vector.W);
        public static Vector4D operator *(Vector4D vector, float scalar) => scalar * vector;

        public static Vector4D operator /(float scalar, Vector4D vector) => (1/scalar) * vector;
        public static Vector4D operator /(Vector4D vector, float scalar) => (1/scalar) * vector;

        public static implicit operator Vector4D(Vector3D vector) => new Vector4D(vector.X, vector.Y, vector.Z, 1.0f);

        public override string ToString()
        {
            return "(" + X + ", " + Y + ", " + Z + ", " + W + ")";
        }
    }

    struct Matrix4D
    {
        public float m00, m01, m02, m03;
        public float m10, m11, m12, m13;
        public float m20, m21, m22, m23;
        public float m30, m31, m32, m33;

        public Matrix4D
            (
                float m00, float m01, float m02, float m03,
                float m10, float m11, float m12, float m13,
                float m20, float m21, float m22, float m23,
                float m30, float m31, float m32, float m33
            )
        {
            this.m00 = m00; this.m01 = m01; this.m02 = m02; this.m03 = m03;
            this.m10 = m10; this.m11 = m11; this.m12 = m12; this.m13 = m13;
            this.m20 = m20; this.m21 = m21; this.m22 = m22; this.m23 = m23;
            this.m30 = m30; this.m31 = m31; this.m32 = m32; this.m33 = m33;
        }

        public Matrix4D(Vector4D i, Vector4D j, Vector4D k, Vector4D fourth)
        {
            m00 = i.X; m01 = j.X; m02 = k.X; m03 = fourth.X;
            m10 = i.Y; m11 = j.Y; m12 = k.Y; m13 = fourth.Y;
            m20 = i.Z; m21 = j.Z; m22 = k.Z; m23 = fourth.Z;
            m30 = i.W; m31 = j.W; m32 = k.W; m33 = fourth.W;
        }

        public static Matrix4D operator +(Matrix4D matrix) => matrix;
        public static Matrix4D operator -(Matrix4D matrix) => new Matrix4D(
            -matrix.m00, -matrix.m01, -matrix.m02, -matrix.m03,
            -matrix.m10, -matrix.m11, -matrix.m12, -matrix.m13,
            -matrix.m20, -matrix.m21, -matrix.m22, -matrix.m23,
            -matrix.m30, -matrix.m31, -matrix.m32, -matrix.m33);

        public static Matrix4D operator +(Matrix4D m1, Matrix4D m2) => new Matrix4D(
            m1.m00 + m2.m00, m1.m01 + m2.m01, m1.m02 + m2.m02, m1.m03 + m2.m03,
            m1.m10 + m2.m10, m1.m11 + m2.m11, m1.m12 + m2.m12, m1.m13 + m2.m13,
            m1.m20 + m2.m20, m1.m21 + m2.m21, m1.m22 + m2.m22, m1.m23 + m2.m23,
            m1.m30 + m2.m30, m1.m31 + m2.m31, m1.m32 + m2.m32, m1.m33 + m2.m33);
        public static Matrix4D operator -(Matrix4D m1, Matrix4D m2) => new Matrix4D(
            m1.m00 - m2.m00, m1.m01 - m2.m01, m1.m02 - m2.m02, m1.m03 - m2.m03,
            m1.m10 - m2.m10, m1.m11 - m2.m11, m1.m12 - m2.m12, m1.m13 - m2.m13,
            m1.m20 - m2.m20, m1.m21 - m2.m21, m1.m22 - m2.m22, m1.m23 - m2.m23,
            m1.m30 - m2.m30, m1.m31 - m2.m31, m1.m32 - m2.m32, m1.m33 - m2.m33);

        public static Matrix4D operator *(Matrix4D m1, Matrix4D m2) => new Matrix4D(
            m1.m00 * m2.m00 + m1.m01 * m2.m10 + m1.m02 * m2.m20 + m1.m03 * m2.m30,
            m1.m00 * m2.m01 + m1.m01 * m2.m11 + m1.m02 * m2.m21 + m1.m03 * m2.m31,
            m1.m00 * m2.m02 + m1.m01 * m2.m12 + m1.m02 * m2.m22 + m1.m03 * m2.m32,
            m1.m00 * m2.m03 + m1.m01 * m2.m13 + m1.m02 * m2.m23 + m1.m03 * m2.m33,

            m1.m10 * m2.m00 + m1.m11 * m2.m10 + m1.m12 * m2.m20 + m1.m13 * m2.m30,
            m1.m10 * m2.m01 + m1.m11 * m2.m11 + m1.m12 * m2.m21 + m1.m13 * m2.m31,
            m1.m10 * m2.m02 + m1.m11 * m2.m12 + m1.m12 * m2.m22 + m1.m13 * m2.m32,
            m1.m10 * m2.m03 + m1.m11 * m2.m13 + m1.m12 * m2.m23 + m1.m13 * m2.m33,

            m1.m20 * m2.m00 + m1.m21 * m2.m10 + m1.m22 * m2.m20 + m1.m23 * m2.m30,
            m1.m20 * m2.m01 + m1.m21 * m2.m11 + m1.m22 * m2.m21 + m1.m23 * m2.m31,
            m1.m20 * m2.m02 + m1.m21 * m2.m12 + m1.m22 * m2.m22 + m1.m23 * m2.m32,
            m1.m20 * m2.m03 + m1.m21 * m2.m13 + m1.m22 * m2.m23 + m1.m23 * m2.m33,

            m1.m30 * m2.m00 + m1.m31 * m2.m10 + m1.m32 * m2.m20 + m1.m33 * m2.m30,
            m1.m30 * m2.m01 + m1.m31 * m2.m11 + m1.m32 * m2.m21 + m1.m33 * m2.m31,
            m1.m30 * m2.m02 + m1.m31 * m2.m12 + m1.m32 * m2.m22 + m1.m33 * m2.m32,
            m1.m30 * m2.m03 + m1.m31 * m2.m13 + m1.m32 * m2.m23 + m1.m33 * m2.m33
            );
        public static Vector4D operator *(Matrix4D matrix, Vector4D vector) => new Vector4D(
            matrix.m00 * vector.X + matrix.m01 * vector.Y + matrix.m02 * vector.Z + matrix.m03 * vector.W,
            matrix.m10 * vector.X + matrix.m11 * vector.Y + matrix.m12 * vector.Z + matrix.m13 * vector.W,
            matrix.m20 * vector.X + matrix.m21 * vector.Y + matrix.m22 * vector.Z + matrix.m23 * vector.W,
            matrix.m30 * vector.X + matrix.m31 * vector.Y + matrix.m32 * vector.Z + matrix.m33 * vector.W
            );

        public static Matrix4D TranslationMatrix(Vector3D translation) => new Matrix4D(
            1.0f, 0.0f, 0.0f, translation.X,
            0.0f, 1.0f, 0.0f, translation.Y,
            0.0f, 0.0f, 1.0f, translation.Z,
            0.0f, 0.0f, 0.0f, 1.0f
            );

        public static Matrix4D RotationZMatrix(Vector3D rotation) => new Matrix4D(
            (float)Math.Cos(rotation.Z), -(float)Math.Sin(rotation.Z), 0.0f, 0.0f,
            (float)Math.Sin(rotation.Z), (float)Math.Cos(rotation.Z), 0.0f, 0.0f,
            0.0f, 0.0f, 1.0f, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
            );
        public static Matrix4D RotationYMatrix(Vector3D rotation) => new Matrix4D(
            (float)Math.Cos(rotation.Y), 0.0f, (float)Math.Sin(rotation.Y), 0.0f,
            0.0f, 1.0f, 0.0f, 0.0f,
            -(float)Math.Sin(rotation.Y), 0.0f, (float)Math.Cos(rotation.Y), 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
            );
        public static Matrix4D RotationXMatrix(Vector3D rotation) => new Matrix4D(
            1.0f, 0.0f, 0.0f, 0.0f,
            0.0f, (float)Math.Cos(rotation.X), -(float)Math.Sin(rotation.X), 0.0f,
            0.0f, (float)Math.Sin(rotation.X), (float)Math.Cos(rotation.X), 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
            );
        public static Matrix4D RotationMatrix(Vector3D rotation) => RotationZMatrix(rotation) * RotationYMatrix(rotation) * RotationXMatrix(rotation);

        public static Matrix4D ScaleMatrix(Vector3D scale) => new Matrix4D(
            scale.X, 0.0f, 0.0f, 0.0f,
            0.0f, scale.Y, 0.0f, 0.0f,
            0.0f, 0.0f, scale.Z, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
            );
    }

    struct Quaternion
    {
        public float r;
        public float i;
        public float j;
        public float k;

        public Quaternion(float r, float i, float j, float k)
        {
            this.r = r;
            this.i = i;
            this.j = j;
            this.k = k;
        }

        public static Quaternion operator +(Quaternion quaternion) => quaternion;
        public static Quaternion operator -(Quaternion quaternion) => new Quaternion(-quaternion.r, -quaternion.i, -quaternion.j, -quaternion.k);

        public static Quaternion operator +(Quaternion q1, Quaternion q2) => new Quaternion(q1.r + q2.r, q1.i + q2.i, q1.j + q2.j, q1.k + q2.k);
        public static Quaternion operator -(Quaternion q1, Quaternion q2) => new Quaternion(q1.r - q2.r, q1.i - q2.i, q1.j - q2.j, q1.k - q2.k);

        public static Quaternion operator *(Quaternion quaternion, float scalar) => new Quaternion(scalar * quaternion.r, scalar * quaternion.i, scalar * quaternion.j, scalar * quaternion.k);
        public static Quaternion operator *(float scalar, Quaternion quaternion) => quaternion * scalar;
        public static Matrix4D operator *(Quaternion quaternion, Matrix4D matrix) => new Matrix4D(
            (Vector4D)(quaternion * new Vector3D(matrix.m00, matrix.m10, matrix.m20)),
            (Vector4D)(quaternion * new Vector3D(matrix.m01, matrix.m11, matrix.m21)),
            (Vector4D)(quaternion * new Vector3D(matrix.m02, matrix.m12, matrix.m22)),
            new Vector4D(matrix.m03, matrix.m13, matrix.m23, matrix.m33)
            );

        public static Matrix4D operator *(Matrix4D matrix, Quaternion quaternion) => new Matrix4D(
            (Vector4D)(new Vector3D(matrix.m00, matrix.m10, matrix.m20) * quaternion),
            (Vector4D)(new Vector3D(matrix.m01, matrix.m11, matrix.m21) * quaternion),
            (Vector4D)(new Vector3D(matrix.m02, matrix.m12, matrix.m22) * quaternion),
            new Vector4D(matrix.m03, matrix.m13, matrix.m23, matrix.m33)
            );

        public static Quaternion operator *(Quaternion q1, Quaternion q2) => new Quaternion(
            q1.r * q2.r - q1.i * q2.i - q1.j * q2.j - q1.k * q2.k,
            q1.r * q2.i + q1.i * q2.r + q1.j * q2.k - q1.k * q2.j,
            q1.r * q2.j - q1.i * q2.k + q1.j * q2.r + q1.k * q2.i,
            q1.r * q2.k + q1.i * q2.j - q1.j * q2.i + q1.k * q2.r
            );

        public static Matrix4D ToRotationMatrix(Quaternion quaternion) => new Matrix4D(
            1.0f - 2.0f * quaternion.j * quaternion.j - 2 * quaternion.k * quaternion.k, 2 * quaternion.i * quaternion.j + 2 * quaternion.k * quaternion.r, 2 * quaternion.i * quaternion.k - 2 * quaternion.j * quaternion.r​, 0.0f,
            2 * quaternion.i * quaternion.j - 2 * quaternion.k * quaternion.r, 1 - 2 * quaternion.i * quaternion.i - 2 * quaternion.k * quaternion.k, 2 * quaternion.j * quaternion.k + 2 * quaternion.i * quaternion.r, 0.0f,
            2 * quaternion.i * quaternion.k + 2 * quaternion.j * quaternion.r, 2 * quaternion.j * quaternion.k - 2 * quaternion.i * quaternion.r, 1 - 2 * quaternion.i * quaternion.i - 2 * quaternion.j * quaternion.j, 0.0f,
            0.0f, 0.0f, 0.0f, 1.0f
            );

        public static Quaternion RotationZ(float theta) => new Quaternion(Scene3D.CosDegrees(theta / 2), 0.0f, 0.0f, Scene3D.SinDegrees(theta / 2));
        public static Quaternion RotationY(float theta) => new Quaternion(Scene3D.CosDegrees(theta / 2), 0.0f, Scene3D.SinDegrees(theta / 2), 0.0f);
        public static Quaternion RotationX(float theta) => new Quaternion(Scene3D.CosDegrees(theta / 2), Scene3D.SinDegrees(theta / 2), 0.0f, 0.0f);

        public static Quaternion Normalise(Quaternion q)
        {
            float scaleFactor = MathF.Sqrt(q.r * q.r + q.i * q.i + q.j * q.j + q.k * q.k);
            return new Quaternion(q.r / scaleFactor, q.i / scaleFactor, q.j / scaleFactor, q.k / scaleFactor);
        }

        public static Quaternion FromAxisAngle(Vector3D axis, float angle)
        {
            float cosine = Scene3D.CosDegrees(angle);
            float sine = Scene3D.SinDegrees(angle);
            return new Quaternion(cosine, axis.X * sine, axis.Y * sine, axis.Z * sine);
        }

        public static implicit operator Quaternion(Vector3D vector) => new Quaternion(0.0f, vector.X, vector.Y, vector.Z);
        public static explicit operator Quaternion(Vector4D vector) => new Quaternion(0.0f, vector.X, vector.Y, vector.Z);
        public static explicit operator Vector3D(Quaternion quaternion) => new Vector3D(quaternion.i, quaternion.j, quaternion.k);
        public static explicit operator Vector4D(Quaternion quaternion) => new Vector4D(quaternion.i, quaternion.j, quaternion.k, 0.0f);

        public Quaternion Conjugate() => new Quaternion(r, -i, -j, -k);

    }
}
