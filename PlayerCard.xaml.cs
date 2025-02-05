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
        public event EventHandler MovePlayerUp = delegate { };
        public event EventHandler MovePlayerDown = delegate { };
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

        private bool _isCurrentPlayer;
        public bool IsCurrentPlayer
        {
            get => _isCurrentPlayer;
            set
            {
                _isCurrentPlayer = value;
                if (value)
                {
                    // set the colours and everything to make this obviously the current player
                    System.Windows.Media.Brush foreground = (System.Windows.Media.Brush)App.MainApp.FindResource("FocusedForeground");
                    PlayerCardBackground.Background = foreground;
                    GameDataGrid.Background = foreground;
                    UpDownGrid.Background = foreground;
                    WinCountText.Background = foreground;
                    DrawCountText.Background = foreground;
                    LossCountText.Background = foreground;
                    System.Windows.Media.Brush text = (System.Windows.Media.Brush)App.MainApp.FindResource("FocusedText");
                    UsernameTextbox.Foreground = text;
                    WinCountText.Foreground = text;
                    LossCountText.Foreground = text;
                    DrawCountText.Foreground = text;
                }
                else
                {
                    // reset the colours back to normal
                    System.Windows.Media.Brush foreground = (System.Windows.Media.Brush)App.MainApp.FindResource("Foreground");
                    PlayerCardBackground.Background = foreground;
                    GameDataGrid.Background = foreground;
                    UpDownGrid.Background= foreground;
                    WinCountText.Background = foreground;
                    DrawCountText.Background = foreground;
                    LossCountText.Background = foreground;
                    System.Windows.Media.Brush text = (System.Windows.Media.Brush)App.MainApp.FindResource("StandardText");
                    UsernameTextbox.Foreground = (System.Windows.Media.Brush)App.MainApp.FindResource("ImportantText");
                    WinCountText.Foreground = text;
                    LossCountText.Foreground = text;
                    DrawCountText.Foreground = text;
                }
            }
        }

        private bool openingPopup;

        private bool _isBot;
        public bool IsBot
        {
            get => _isBot;
            set
            {
                _isBot = value;
                UpdateBotIcon();
            }
        }

        private int winCount;
        private int drawCount;
        private int lossCount;

        public PlayerCard()
        {
            InitializeComponent();
            _playerName = UsernameTextbox.Text;
            icon = new IconCross(_colour);
            IconPopup.PlacementTarget = IconButton;
            winCount = 0;
            drawCount = 0;
            lossCount = 0;
            _isBot = false;
        }

        private void DeleteButtonClicked(object sender, RoutedEventArgs e)
        {
            if (DeletePlayer != null)
            {
                DeletePlayer(this, e);
            }
        }

        private void UpButtonClicked(object sender, RoutedEventArgs e)
        {
            if (MovePlayerUp != null)
            {
                MovePlayerUp(this, e);
            }
        }

        private void DownButtonClicked(object sender, RoutedEventArgs e)
        {
            if (MovePlayerDown != null)
            {
                MovePlayerDown(this, e);
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
            IconPopup.Resources = App.MainApp.Resources;
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
            winCount++;
            WinCountText.Text = "Wins: " + winCount.ToString();

            ScoreCard.Children.Add(rectangle);
        }

        public void AddDraw()
        {
            System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
            rectangle.Fill = System.Windows.Media.Brushes.LightGray;
            rectangle.Width = 20;
            rectangle.Height = 20;
            rectangle.Margin = new Thickness(2.5);
            drawCount++;
            DrawCountText.Text = "Draws: " + drawCount.ToString();

            ScoreCard.Children.Add(rectangle);
        }

        public void AddLoss()
        {
            System.Windows.Shapes.Rectangle rectangle = new System.Windows.Shapes.Rectangle();
            rectangle.Fill = System.Windows.Media.Brushes.Red;
            rectangle.Width = 20;
            rectangle.Height = 20;
            rectangle.Margin = new Thickness(2.5);
            lossCount++;
            LossCountText.Text = "Losses: " + lossCount.ToString();

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

        private void UpdateBotIcon()
        {
            BotCanvas.Children.Clear();
            if (_isBot)
            {
                // add the bot icon
                System.Windows.Media.SolidColorBrush botIconColour = (SolidColorBrush)(App.MainApp.FindResource("StandardText"));

                // head
                System.Windows.Shapes.Rectangle head = new System.Windows.Shapes.Rectangle();
                head.Stroke = botIconColour;
                head.StrokeThickness = 2;
                head.Width = 20;
                head.Height = 15;
                BotCanvas.Children.Add(head);
                Canvas.SetTop(head, 10);
                Canvas.SetLeft(head, 5);

                // antenna stalk
                System.Windows.Shapes.Line antennaStalk = new System.Windows.Shapes.Line();
                antennaStalk.Stroke = botIconColour;
                antennaStalk.StrokeThickness = 2;
                antennaStalk.X1 = 15;
                antennaStalk.Y1 = 10;
                antennaStalk.X2 = 15;
                antennaStalk.Y2 = 5;
                BotCanvas.Children.Add(antennaStalk);

                // antenna top
                System.Windows.Shapes.Ellipse antennaTop = new System.Windows.Shapes.Ellipse();
                antennaTop.Stroke = botIconColour;
                antennaTop.StrokeThickness = 2;
                antennaTop.Width = 5;
                antennaTop.Height = 5;
                BotCanvas.Children.Add(antennaTop);
                Canvas.SetTop(antennaTop, 2.5);
                Canvas.SetLeft(antennaTop, 12.5);

                // left eye
                System.Windows.Shapes.Ellipse leftEye = new System.Windows.Shapes.Ellipse();
                leftEye.Stroke = botIconColour;
                leftEye.StrokeThickness = 2;
                leftEye.Width = 5;
                leftEye.Height = 5;
                BotCanvas.Children.Add(leftEye);
                Canvas.SetTop(leftEye, 15);
                Canvas.SetLeft(leftEye, 8.75);

                // right eye
                System.Windows.Shapes.Ellipse rightEye = new System.Windows.Shapes.Ellipse();
                rightEye.Stroke = botIconColour;
                rightEye.StrokeThickness = 2;
                rightEye.Width = 5;
                rightEye.Height = 5;
                BotCanvas.Children.Add(rightEye);
                Canvas.SetTop(rightEye, 15);
                Canvas.SetLeft(rightEye, 16.25);
            }
        }
    }
}
