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
        Settings? settings;

        System.Windows.Point lastMousePos;
        bool mouseDownLast = false;
        bool leftMousePressedLast = false;

        GameManager gameManager;
        List<Player> players = new List<Player>();
        bool gameStarted = false;

        string oldSizeText = "3";
        string oldDimensionText = "2";

        public MainWindow()
        {
            InitializeComponent();

            int size = 3;
            int dimensions = 2;

            gameManager = new GameManager(size, dimensions, players, ref Viewport);
            gameManager.Render();
        }

        public void ViewportSizeChanged(object sender, SizeChangedEventArgs e)
        {
            gameManager.ViewSizeChanged((int)Viewport.ActualWidth, (int)Viewport.ActualHeight, ref Viewport);
        }

        public void ViewportMouseMove(object sender, MouseEventArgs e)
        {
            if (e.RightButton.Equals(MouseButtonState.Pressed))
            {
                System.Windows.Point currentPos = e.GetPosition(Viewport);
                if (mouseDownLast)
                {
                    float deltaX = (float)(currentPos.X - lastMousePos.X);
                    float deltaY = (float)(currentPos.Y - lastMousePos.Y);

                    gameManager.MouseMoved(deltaX, deltaY);
                }
                mouseDownLast = true;
                lastMousePos = currentPos;
            }
            else
            {
                mouseDownLast = false;
            }
        }

        public void ViewportMouseUp(object sender, MouseEventArgs e)
        {
            if (gameStarted && leftMousePressedLast)
            {
                // handle the placement
                System.Windows.Point position = e.GetPosition(Viewport);
                gameManager.MouseClicked((int)position.X, (int)position.Y);
                leftMousePressedLast = false;

                // if wins, end the game
                if (gameManager.GameFinished)
                {
                    GameEnded();
                }
            }
        }

        public void ViewportMouseDown(object sender, MouseEventArgs e)
        {
            if (gameStarted && e.LeftButton.Equals(MouseButtonState.Pressed))
            {
                leftMousePressedLast = true;
            }
        }

        public void ViewportMouseWheel(object sender, MouseWheelEventArgs e)
        {
            gameManager.Stretches = new Vector3D(gameManager.Stretches.X + e.Delta * 0.0001f, gameManager.Stretches.Y + 0.0f, gameManager.Stretches.Z + 0.0f);
            gameManager.Render();
        }

        public void OpenSettings(object sender, EventArgs e)
        {
            if (settings == null)
            {
                settings = new Settings();
            }
            settings.Show();
        }

        public void SizeTextChanged(object sender, EventArgs e)
        {
            if (gameManager == null)
            {
                return;
            }
            if (SizeInput.Text == "")
            {
                gameManager.Size = 1;
                oldSizeText = SizeInput.Text;
                gameManager.Render();
            }
            if (Int32.TryParse(SizeInput.Text, out int value))
            {
                gameManager.Size = value;
                oldSizeText = SizeInput.Text;
                gameManager.Render();
            }
            else
            {
                SizeInput.Text = oldSizeText;
            }
        }

        public void DimensionTextChanged(object sender, EventArgs e)
        {
            if (gameManager == null)
            {
                return;
            }
            if (DimensionInput.Text == "")
            {
                gameManager.Dimensions = 1;
                oldDimensionText = DimensionInput.Text;
                gameManager.Render();
            }
            if (Int32.TryParse(DimensionInput.Text, out int value))
            {
                if (value > 3)
                {
                    gameManager.Dimensions = 3;
                    DimensionInput.Text = "3";
                    oldDimensionText = "3";
                }
                else
                {
                    gameManager.Dimensions = value;
                    oldDimensionText = DimensionInput.Text;
                }
                gameManager.Render();
            }
            else
            {
                DimensionInput.Text = oldDimensionText;
            }
        }

        private void EasyBotAdded(object sender, EventArgs e)
        {
            EasyBotPlayer player = new EasyBotPlayer(
                RandomIcon(),
                "EasyBot");

            players.Add(player);
            PlayerCard card = new PlayerCard()
            {
                PlayerName = player.Name
            };
            player.Icon.RenderCanvas(ref card.Icon);
            card.DeletePlayer += PlayerRemoved;
            PlayerCardContainer.Children.Add(card);
        }

        private void MediumBotAdded(object sender, EventArgs e)
        {
            MediumBotPlayer player = new MediumBotPlayer(
                RandomIcon(),
                "MediumBot");

            players.Add(player);
            PlayerCard card = new PlayerCard()
            {
                PlayerName = player.Name
            };
            player.Icon.RenderCanvas(ref card.Icon);
            card.DeletePlayer += PlayerRemoved;
            PlayerCardContainer.Children.Add(card);
        }

        private void HardBotAdded(object sender, EventArgs e)
        {
            HardBotPlayer player = new HardBotPlayer(
                RandomIcon(),
                "HardBot");

            players.Add(player);
            PlayerCard card = new PlayerCard()
            {
                PlayerName = player.Name
            };
            player.Icon.RenderCanvas(ref card.Icon);
            card.DeletePlayer += PlayerRemoved;
            PlayerCardContainer.Children.Add(card);
        }

        private void PlayerAdded(object sender, EventArgs e)
        {
            Player player = new Player(
                RandomIcon(),
                "Player");

            players.Add(player);
            PlayerCard card = new PlayerCard()
            {
                PlayerName = player.Name
            };
            player.Icon.RenderCanvas(ref card.Icon);
            card.DeletePlayer += PlayerRemoved;
            PlayerCardContainer.Children.Add(card);
        }

        private void PlayerRemoved(object? sender, EventArgs e)
        {
            if (sender == null)
            {
                return;
            }
            int index = PlayerCardContainer.Children.IndexOf((PlayerCard)sender);
            PlayerCardContainer.Children.RemoveAt(index);
            players.RemoveAt(index);
        }

        private void GameStarted(object sender, EventArgs e)
        {
            if (gameStarted)
            {
                GameEnded();
                return;
            }
            if (players.Count <= 0)
            {
                return;
            }
            // Start the game
            gameManager = new GameManager(Int32.Parse(SizeInput.Text), Int32.Parse(DimensionInput.Text), players, ref Viewport);
            gameManager.ViewSizeChanged((int)Viewport.ActualWidth, (int)Viewport.ActualHeight, ref Viewport);
            gameManager.Render();

            // Disable controls
            gameStarted = true;
            SizeInput.IsEnabled = false;
            DimensionInput.IsEnabled = false;
            foreach (PlayerCard card in PlayerCardContainer.Children)
            {
                card.UsernameTextbox.IsEnabled = false;
                card.DeleteButton.IsEnabled = false;
            }
            EasyBotButton.IsEnabled = false;
            MediumBotButton.IsEnabled = false;
            HardBotButton.IsEnabled = false;
            PlayerButton.IsEnabled = false;
            StartButton.Content = "Stop Game";

            if (gameManager.GameFinished)
            {
                GameEnded();
            }
        }

        private void GameEnded()
        {
            // Enable controls
            gameStarted = false;
            SizeInput.IsEnabled = true;
            DimensionInput.IsEnabled = true;
            foreach (PlayerCard card in PlayerCardContainer.Children)
            {
                card.UsernameTextbox.IsEnabled = true;
                card.DeleteButton.IsEnabled = true;
            }
            EasyBotButton.IsEnabled = true;
            MediumBotButton.IsEnabled = true;
            HardBotButton.IsEnabled = true;
            PlayerButton.IsEnabled = true;
            StartButton.Content = "Start Game";
        }

        private static GameIcon RandomIcon()
        {
            Random random = new Random();
            int selection = random.Next(3);
            switch (selection)
            {
                case 0: return new IconCross(RandomColour());
                case 1: return new IconNought(RandomColour());
                case 2: return new IconTetrahedron(RandomColour());
            }
            return new IconCross(RandomColour());
        }

        private static System.Drawing.Color RandomColour()
        {
            System.Drawing.Color[] colours = 
            {
                System.Drawing.Color.Red,
                System.Drawing.Color.Green,
                System.Drawing.Color.Blue,
                System.Drawing.Color.Cyan,
                System.Drawing.Color.Magenta
            };
            Random random = new Random();
            return colours[random.Next(colours.Length)];
        }
    }
}