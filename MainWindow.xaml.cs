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

        public MainWindow()
        {
            InitializeComponent();
            scene = new Scene3D(ref Viewport);
        }

        public void NewPosition(object sender, RoutedEventArgs e)
        {
            if (scene != null)
            {
                if (Single.TryParse(positionX.Text, out float result))
                {
                    scene.Camera.Position = new Vector3D(result, scene.Camera.Position.Y, scene.Camera.Position.Z);
                }
                if (Single.TryParse(positionY.Text, out result))
                {
                    scene.Camera.Position = new Vector3D(scene.Camera.Position.X, result, scene.Camera.Position.Z);
                }
                if (Single.TryParse(positionZ.Text, out result))
                {
                    scene.Camera.Position = new Vector3D(scene.Camera.Position.X, scene.Camera.Position.Y, result);
                }
                scene.Render();
            }
        }

        public void NewDegrees(object sender, RoutedEventArgs e)
        {
            if (scene != null)
            {
                if (Single.TryParse(degreesX.Text, out float result))
                {
                    scene.Camera.Rotation = new Vector3D(Scene3D.DegreesToRadians(result), scene.Camera.Rotation.Y, scene.Camera.Rotation.Z);
                }
                if (Single.TryParse(degreesY.Text, out result))
                {
                    scene.Camera.Rotation = new Vector3D(scene.Camera.Rotation.X, Scene3D.DegreesToRadians(result), scene.Camera.Rotation.Z);
                }
                if (Single.TryParse(degreesZ.Text, out result))
                {
                    scene.Camera.Rotation = new Vector3D(scene.Camera.Rotation.X, scene.Camera.Rotation.Y, Scene3D.DegreesToRadians(result));
                }
                scene.Render();
            }
        }
    }
}