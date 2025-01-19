using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace noughts_and_crosses
{
    /// <summary>
    /// Interaction logic for PlayerCard.xaml
    /// </summary>
    public partial class PlayerCard : UserControl
    {
        public event EventHandler DeletePlayer = delegate { };
        public event EventHandler UpdatePlayer = delegate { };
        public event EventHandler ChangeColour = delegate { };
        public event EventHandler ChangeIcon = delegate { };

        public GameIcon icon;

        private string _playerName;
        public string PlayerName
        {
            get => _playerName;
            set
            {
                _playerName = value;
                UsernameTextbox.Text = value;
            }
        }

        private System.Drawing.Color _colour;
        public System.Drawing.Color Colour
        {
            get => _colour;
            set => _colour = value;
        }

        private int _playerNumber;
        public int PlayerNumber
        {
            get => _playerNumber;
            set => _playerNumber = value;
        }

        private bool openingPopup;

        public PlayerCard()
        {
            InitializeComponent();
            _playerName = UsernameTextbox.Text;
            icon = new IconCross(_colour);
        }

        private void DeleteButtonClicked(object sender, RoutedEventArgs e)
        {
            if (DeletePlayer != null)
            {
                DeletePlayer(this, e);
            }
        }

        private void PlayerNameChanged(object sender, EventArgs e)
        {
            _playerName = UsernameTextbox.Text.Trim();
            if (UpdatePlayer != null)
            {
                UpdatePlayer(this, e);
            }
        }

        private void OpenPopup(object sender, RoutedEventArgs e)
        {
            openingPopup = true;
            IconPopup.IsOpen = true;
            RedSlider.Value = _colour.R;
            GreenSlider.Value = _colour.G;
            BlueSlider.Value = _colour.B;
            openingPopup = false;
        }

        private void ColourSliderMoved(object sender, RoutedEventArgs e)
        {
            if (openingPopup)
            {
                return;
            }
            _colour = System.Drawing.Color.FromArgb((int)RedSlider.Value, (int)GreenSlider.Value, (int)BlueSlider.Value);
            if (ChangeColour != null)
            {
                ChangeColour(this, e);
            }
        }

        private void CrossClicked(object sender, RoutedEventArgs e)
        {
            icon = new IconCross(_colour);
            if (ChangeIcon != null)
            {
                ChangeIcon(this, e);
            }
        }

        private void NoughtClicked(object sender, RoutedEventArgs e)
        {
            icon = new IconNought(_colour);
            if (ChangeIcon != null)
            {
                ChangeIcon(this, e);
            }
        }

        private void TetrahedronClicked(object sender, RoutedEventArgs e)
        {
            icon = new IconTetrahedron(_colour);
            if (ChangeIcon != null)
            {
                ChangeIcon(this, e);
            }
        }

        public void AddWin()
        {
            System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
            rectangle.Fill = System.Windows.Media.Brushes.Lime;
            rectangle.Width = 20;
            rectangle.Height = 20;
            rectangle.Margin = new Thickness(2.5);

            ScoreCard.Children.Add(rectangle);
        }

        public void AddDraw()
        {
            System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
            rectangle.Fill = System.Windows.Media.Brushes.LightGray;
            rectangle.Width = 20;
            rectangle.Height = 20;
            rectangle.Margin = new Thickness(2.5);

            ScoreCard.Children.Add(rectangle);
        }

        public void AddLoss()
        {
            System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
            rectangle.Fill = System.Windows.Media.Brushes.Red;
            rectangle.Width = 20;
            rectangle.Height = 20;
            rectangle.Margin = new Thickness(2.5);

            ScoreCard.Children.Add(rectangle);
        }

        public void ScoreCardScroll(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                if (e.Delta > 0)
                {
                    scrollViewer.LineLeft();
                }
                else
                {
                    scrollViewer.LineRight();
                }
            }
            e.Handled = true;
        }
    }
}
