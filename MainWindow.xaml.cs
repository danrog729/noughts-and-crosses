using System.Diagnostics;
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
        Vector lastMouseAngles;
        bool mouseDownLast = false;

        public MainWindow()
        {
            InitializeComponent();
            scene = new Scene3D(ref Viewport);
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
            if (e.LeftButton.Equals(MouseButtonState.Pressed))
            {
                Point currentPos = e.GetPosition(Viewport);
                Vector angles = new Vector(scene.Camera.FOV * (currentPos.X / Viewport.ActualWidth), scene.Camera.VerticalFOV * (currentPos.Y / Viewport.ActualHeight));
                if (mouseDownLast)
                {
                    // find delta theta
                    Vector delta = lastMouseAngles - angles;
                    // rotate the camera by delta theta
                    Quaternion rotation = Quaternion.RotationY(-(float)delta.X) * Quaternion.RotationX(-(float)delta.Y);
                    scene.Camera.Rotation = Quaternion.Normalise(rotation * scene.Camera.Rotation);
                    scene.Render();
                }
                mouseDownLast = true;
                lastMouseAngles = angles;
            }
            else
            {
                mouseDownLast = false;
            }
        }
    }
}