using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Cryptography.Xml;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace noughts_and_crosses
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Scene3D scene;
        System.Windows.Point lastMousePos;
        bool mouseDownLast = false;
        bool rightMousePressedLast = false;

        PlayerOrientedBoard board;
        int currentPlayer = 1;

        public MainWindow()
        {
            InitializeComponent();
            scene = new Scene3D(ref Viewport);

            int playerCount = 3;
            int size = 3;
            int dimensions = 3;
            board = new PlayerOrientedBoard(size, dimensions, playerCount);

            Object3D root = new Object3D()
            {
                Scale = new Vector3D(
                    1.0f,
                    1.0f,
                    dimensions >= 3 ? 1.0f : 1.0f / size
                    )
            };
            scene.rootObject = root;
            for (int cell = 0; cell < board.Length; cell++)
            {
                int[] dimIndex = board.DimensionalIndex(cell);
                Object3D obj = new Object3D()
                {
                    Position = new Vector3D(
                        -1.0f + (1.0f / size) + (1.0f / size) * dimIndex[0] * 2,
                        -1.0f + (1.0f / size) + (1.0f / size) * dimIndex[1] * 2,
                        dimensions >= 3 ? -1.0f + (1.0f / size) + (1.0f / size) * dimIndex[2] * 2 : 0.0f),
                    Scale = new Vector3D(
                        1.0f / size,
                        1.0f / size,
                        dimensions >= 3 ? 1.0f / size : 1.0f),
                    colour = System.Drawing.Color.Gray
                };
                scene.rootObject.children.Add(obj);
            }

            scene.Render();
        }

        public void ViewportSizeChanged(object sender, SizeChangedEventArgs e)
        {
            scene.NewSize((int)Viewport.ActualWidth, (int)Viewport.ActualHeight, ref Viewport);
        }

        public void ViewportZoom(object sender, MouseWheelEventArgs e)
        {
            float zoomDistance = 1.0f / 10.0f * (float)Scene3D.TanDegrees(scene.Camera.FOV) * -e.Delta / 120f;
            Vector3D delta = (Vector3D)(scene.Camera.Rotation * new Vector3D(0.0f, 0.0f, zoomDistance) * scene.Camera.Rotation.Conjugate());
            scene.Camera.Position = scene.Camera.Position + delta;
            scene.Render();
        }

        public void ViewportMouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton.Equals(MouseButtonState.Pressed))
            {
                System.Windows.Point currentPos = e.GetPosition(Viewport);
                float angle = -(float)Math.Sqrt((currentPos.X - lastMousePos.X) * (currentPos.X - lastMousePos.X) + (currentPos.Y - lastMousePos.Y) * (currentPos.Y - lastMousePos.Y));
                float deltaX = (float)(lastMousePos.X - currentPos.X);
                float deltaY = (float)(lastMousePos.Y - currentPos.Y);

                Vector3D axis = new Vector3D(
                    (float)Math.Sqrt(1.0f / (1 + deltaX * deltaX / deltaY / deltaY)),
                    (float)Math.Sqrt(1.0f - 1.0f / (1 + deltaX * deltaX / deltaY / deltaY)),
                    0.0f);

                if (lastMousePos.X > currentPos.X)
                {
                    axis.X *= -1;
                    angle *= -1;
                }
                if (lastMousePos.Y > currentPos.Y)
                {
                    axis.Y *= -1;
                    angle *= -1;
                }
                angle *= 0.2f; // sensitivity
                Quaternion rotation = Quaternion.FromAxisAngle(axis, angle);
                if (mouseDownLast)
                {
                    if (scene.rootObject != null)
                    {
                        scene.rootObject.Rotation = rotation * scene.rootObject.Rotation;
                    }
                }
                mouseDownLast = true;
                lastMousePos = currentPos;
                scene.Render();
            }
            else
            {
                mouseDownLast = false;
            }
        }

        public void ViewportMouseUp(object sender, MouseEventArgs e)
        {
            if (rightMousePressedLast && scene.rootObject != null)
            {
                // find which object has been clicked on
                System.Windows.Point position = e.GetPosition(Viewport);
                Object3D? clickedObject = scene.FindObjectAtPixel((int)position.X, (int)position.Y);
                if (clickedObject != null)
                {
                    int boardIndex = scene.rootObject.children.IndexOf(clickedObject);
                    if (board[boardIndex] != 0)
                    {
                        return;
                    }
                    board[boardIndex] = currentPlayer;

                    switch (currentPlayer)
                    {
                        case 1:
                            clickedObject.children.Add(new ObjectCross()
                            {
                                Scale = new Vector3D(0.8f, 0.8f, 0.8f),
                                colour = System.Drawing.Color.Red
                            }); break;
                        case 2:
                            clickedObject.children.Add(new ObjectNought()
                            {
                                Scale = new Vector3D(0.8f, 0.8f, 0.8f),
                                colour = System.Drawing.Color.Green
                            }); break;
                        case 3:
                            clickedObject.children.Add(new ObjectTetrahedron()
                            {
                                Scale = new Vector3D(0.8f, 0.8f, 0.8f),
                                colour = System.Drawing.Color.Blue
                            }); break;
                    }
  
                    if (board.WinExists(boardIndex, true) != 0 && board.winDirections != null)
                    {
                        foreach (WinDirection winDirection in board.winDirections)
                        {
                            if (winDirection.win == true)
                            {
                                int[] rootDimIndex = board.DimensionalIndex(winDirection.rootIndex);
                                int[] finalDimIndex = board.DimensionalIndex(winDirection.rootIndex + (board.Size - 1) * winDirection.indexOffset);
                                scene.rootObject.children.Add(new ObjectLine(
                                    new Vector3D(
                                        -1.0f + 1.0f / board.Size + (1.0f / board.Size) * rootDimIndex[0] * 2.0f,
                                        -1.0f + 1.0f / board.Size + (1.0f / board.Size) * rootDimIndex[1] * 2.0f,
                                        board.Dimensions >= 3 ? -1.0f + 1.0f / board.Size + (1.0f / board.Size) * rootDimIndex[2] * 2.0f : 0.0f),
                                    new Vector3D(
                                        -1.0f + 1.0f / board.Size + (1.0f / board.Size) * finalDimIndex[0] * 2.0f,
                                        -1.0f + 1.0f / board.Size + (1.0f / board.Size) * finalDimIndex[1] * 2.0f,
                                        board.Dimensions >= 3 ? -1.0f + 1.0f / board.Size + (1.0f / board.Size) * finalDimIndex[2] * 2.0f : 0.0f)
                                        ));
                            }
                        }
                    }
                    scene.Render();
                    currentPlayer = currentPlayer % board.PlayerCount + 1;
                }
                rightMousePressedLast = false;
            }
        }

        public void ViewportMouseDown(object sender, MouseEventArgs e)
        {
            if (e.LeftButton.Equals(MouseButtonState.Pressed))
            {
                rightMousePressedLast = true;
            }
        }
    }
}