using System.Drawing;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace noughts_and_crosses
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        System.Windows.Point lastMousePos;
        bool mouseDownLast = false;
        bool leftMousePressedLast = false;

        GameManager gameManager;
        List<Player> players = new List<Player>();
        bool gameStarted = false;

        string oldSizeText = "3";
        string oldDimensionText = "2";

        private bool settingSizeSlider;
        private bool settingDimensionSlider;

        List<(int, int, int)> invalidColours;

        public MainWindow()
        {
            invalidColours = new List<(int, int, int)>();
            invalidColours.Add((0, 0, 0));
            invalidColours.Add((0, 0, 0));
            InitializeComponent();

            foreach (Theme theme in App.MainApp.themes)
            {
                ThemePresetDropdown.Items.Add(new ComboBoxItem() { Content = theme.Name });
            }
            ThemePresetDropdown.SelectedIndex = 0;

            int size = 3;
            int dimensions = 2;

            gameManager = new GameManager(size, dimensions, players, ref Viewport);
            UpdateRenderColours();
            gameManager.Render();

            gameManager.gameEndEvent += GameEnded;
            gameManager.nextPlayerEvent += NextPlayer;

            settingSizeSlider = false;
            settingDimensionSlider = false;
        }



        public void ViewportSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if ((int)Viewport.ActualWidth != 0 && (int)Viewport.ActualHeight != 0)
            {
                gameManager.ViewSizeChanged((int)Viewport.ActualWidth, (int)Viewport.ActualHeight, ref Viewport);
            }
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
                if (gameManager.MouseClicked((int)position.X, (int)position.Y) && !gameManager.GameFinished)
                {
                    App.MainApp.moveSound.Play();
                }
                leftMousePressedLast = false;
            }
        }

        public void ViewportMouseDown(object sender, MouseEventArgs e)
        {
            if (gameStarted && e.LeftButton.Equals(MouseButtonState.Pressed))
            {
                leftMousePressedLast = true;
                System.Windows.Point position = e.GetPosition(Viewport);
                gameManager.MouseDown((int)position.X, (int)position.Y);
            }
        }

        public void ViewportMouseWheel(object sender, MouseWheelEventArgs e)
        {
            gameManager.GridStretch(e.Delta * 0.0005f);
            gameManager.Render();
        }



        public void SizeTextChanged(object sender, EventArgs e)
        {
            if (gameManager == null)
            {
                return;
            }
            settingSizeSlider = true;
            if (SizeInput.Text == "")
            {
                SizeSlider.Value = 1;
                gameManager.Size = 1;
                oldSizeText = SizeInput.Text;
                gameManager.ResetColours();
                gameManager.Render();
            }
            else if (Int32.TryParse(SizeInput.Text, out int value) && value >= 1)
            {
                SizeSlider.Value = value;
                gameManager.Size = value;
                oldSizeText = value.ToString();
                gameManager.ResetColours();
                gameManager.Render();
            }
            else
            {
                SizeInput.Text = oldSizeText;
            }
            settingSizeSlider = false;
        }

        public void SizeSliderMoved(object sender, EventArgs e)
        {
            if (gameManager == null || settingSizeSlider)
            {
                return;
            }
            int value = (int)SizeSlider.Value;
            gameManager.Size = value;
            SizeInput.Text = value.ToString();
            oldSizeText = SizeInput.Text;
            gameManager.ResetColours();
            gameManager.Render();
        }

        public void SizeEnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SizeTextChanged(sender, e);
            }
        }

        public void DimensionTextChanged(object sender, EventArgs e)
        {
            if (gameManager == null)
            {
                return;
            }
            settingDimensionSlider = true;
            if (DimensionInput.Text == "")
            {
                DimensionSlider.Value = 1;
                gameManager.Dimensions = 1;
                oldDimensionText = DimensionInput.Text;
                gameManager.ResetColours();
                gameManager.Render();
            }
            else if (Int32.TryParse(DimensionInput.Text, out int value))
            {
                if (value > 3)
                {
                    DimensionSlider.Value = 3;
                    gameManager.Dimensions = 3;
                    DimensionInput.Text = "3";
                    oldDimensionText = "3";
                }
                else if (value <= 0)
                {
                    DimensionSlider.Value = 1;
                    gameManager.Dimensions = 1;
                    DimensionInput.Text = "1";
                    oldDimensionText = "1";
                }
                else
                {
                    gameManager.Dimensions = value;
                    DimensionSlider.Value = value;
                    oldDimensionText = DimensionInput.Text;
                }
                gameManager.ResetColours();
                gameManager.Render();
            }
            else
            {
                DimensionInput.Text = oldDimensionText;
            }
            settingDimensionSlider = false;
        }

        public void DimensionSliderMoved(object sender, EventArgs e)
        {
            if (gameManager == null || settingDimensionSlider)
            {
                return;
            }
            int value = (int)DimensionSlider.Value;
            gameManager.Dimensions = value;
            DimensionInput.Text = value.ToString();
            oldDimensionText = DimensionInput.Text;
            gameManager.ResetColours();
            gameManager.Render();
        }

        public void DimensionEnterPressed(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DimensionTextChanged(sender, e);
            }
        }



        private void EasyBotAdded(object sender, EventArgs e)
        {
            EasyBotPlayer player = new EasyBotPlayer(
                RandomIcon(),
                "EasyBot " + (players.Count + 1));

            players.Add(player);
            PlayerCard card = new PlayerCard()
            {
                PlayerName = player.Name,
                Colour = player.Icon.icon3D.colour,
                icon = player.Icon,
                IsBot = true,
                container = PlayerCardScrollViewer
            };
            player.Icon.RenderCanvas(ref card.Icon);
            card.DeletePlayer += PlayerRemoved;
            card.MovePlayerUp += PlayerMovedUp;
            card.MovePlayerDown += PlayerMovedDown;
            card.ChangeColour += PlayerColourChanged;
            card.ChangeIcon += PlayerIconChanged;
            PlayerCardContainer.Children.Add(card);
        }

        private void MediumBotAdded(object sender, EventArgs e)
        {
            MediumBotPlayer player = new MediumBotPlayer(
                RandomIcon(),
                "MediumBot " + (players.Count + 1));

            players.Add(player);
            PlayerCard card = new PlayerCard()
            {
                PlayerName = player.Name,
                Colour = player.Icon.icon3D.colour,
                icon = player.Icon,
                IsBot = true,
                container = PlayerCardScrollViewer
            };
            player.Icon.RenderCanvas(ref card.Icon);
            card.DeletePlayer += PlayerRemoved;
            card.MovePlayerUp += PlayerMovedUp;
            card.MovePlayerDown += PlayerMovedDown;
            card.ChangeColour += PlayerColourChanged;
            card.ChangeIcon += PlayerIconChanged;
            PlayerCardContainer.Children.Add(card);
        }

        private void HardBotAdded(object sender, EventArgs e)
        {
            HardBotPlayer player = new HardBotPlayer(
                RandomIcon(),
                "HardBot " + (players.Count + 1));

            players.Add(player);
            PlayerCard card = new PlayerCard()
            {
                PlayerName = player.Name,
                Colour = player.Icon.icon3D.colour,
                icon = player.Icon,
                IsBot = true,
                container = PlayerCardScrollViewer
            };
            player.Icon.RenderCanvas(ref card.Icon);
            card.DeletePlayer += PlayerRemoved;
            card.MovePlayerUp += PlayerMovedUp;
            card.MovePlayerDown += PlayerMovedDown;
            card.ChangeColour += PlayerColourChanged;
            card.ChangeIcon += PlayerIconChanged;
            PlayerCardContainer.Children.Add(card);
        }

        private void PlayerAdded(object sender, EventArgs e)
        {
            Player player = new Player(
                RandomIcon(),
                "Player " + (players.Count + 1));

            players.Add(player);
            PlayerCard card = new PlayerCard()
            {
                PlayerName = player.Name,
                Colour = player.Icon.icon3D.colour,
                icon = player.Icon,
                container = PlayerCardScrollViewer
            };
            player.Icon.RenderCanvas(ref card.Icon);
            card.DeletePlayer += PlayerRemoved;
            card.MovePlayerUp += PlayerMovedUp;
            card.MovePlayerDown += PlayerMovedDown;
            card.ChangeColour += PlayerColourChanged;
            card.ChangeIcon += PlayerIconChanged;
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

        private void PlayerColourChanged(object? sender, EventArgs e)
        {
            if (sender == null) return;
            PlayerCard card = (PlayerCard)sender;
            int index = PlayerCardContainer.Children.IndexOf(card);

            System.Drawing.Color candidate = card.Colour;
            int red = candidate.R;
            int green = candidate.G;
            int blue = candidate.B;
            bool validPlacement = true;
            foreach ((int, int, int) colour in invalidColours)
            {
                if (Math.Sqrt(
                    (colour.Item1 - red) * (colour.Item1 - red) +
                    (colour.Item2 - green) * (colour.Item2 - green) +
                    (colour.Item3 - blue) * (colour.Item3 - blue)) < 50)
                {
                    validPlacement = false;
                }
            }
            if (validPlacement)
            {
                players[index].Icon.icon3D.colour = card.Colour;
                players[index].Icon.RenderCanvas(ref card.Icon);
                card.OldColour = card.Colour;
                gameManager.Render();
                invalidColours.Remove((card.OldColour.R, card.OldColour.G, card.OldColour.B));
                invalidColours.Add((red, green, blue));
            }
            else
            {
                card.Colour = card.OldColour;
            }
        }

        private void PlayerIconChanged(object? sender, EventArgs e)
        {
            if (sender == null) return;
            PlayerCard card = (PlayerCard)sender;
            int index = PlayerCardContainer.Children.IndexOf(card);
            gameManager.scene.ReplaceObject(players[index].Icon.icon3D, card.icon.icon3D);
            players[index].Icon = card.icon;
            players[index].Icon.RenderCanvas(ref card.Icon);
            gameManager.Render();
        }

        private void PlayerMovedUp(object? sender, EventArgs e)
        {
            if (sender == null)
            {
                return;
            }
            int index = PlayerCardContainer.Children.IndexOf((PlayerCard)sender);
            PlayerCardContainer.Children.RemoveAt(index);
            PlayerCardContainer.Children.Insert(Int32.Max(index - 1, 0), (PlayerCard)sender);
            Player player = players[index];
            players.RemoveAt(index);
            players.Insert(Int32.Max(index - 1, 0), player);
        }

        private void PlayerMovedDown(object? sender, EventArgs e)
        {
            if (sender == null)
            {
                return;
            }
            int index = PlayerCardContainer.Children.IndexOf((PlayerCard)sender);
            PlayerCardContainer.Children.RemoveAt(index);
            PlayerCardContainer.Children.Insert(Int32.Min(index + 1, PlayerCardContainer.Children.Count), (PlayerCard)sender);
            Player player = players[index];
            players.RemoveAt(index);
            players.Insert(Int32.Min(index + 1, players.Count), player);
        }

        private GameIcon RandomIcon()
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

        private System.Drawing.Color RandomColour()
        {
            // colour of any other icon is invalid
            for (int playerIndex = 0; playerIndex < PlayerCardContainer.Children.Count; playerIndex++)
            {
                PlayerCard card = (PlayerCard)PlayerCardContainer.Children[playerIndex];
                System.Drawing.Color colour = card.icon.icon3D.colour;
                invalidColours.Add((colour.R, colour.G, colour.B));
            }

            Random random = new Random();
            bool validPlacement = false;
            int red = 0;
            int green = 0;
            int blue = 0;
            int attempts = 0;
            while (!validPlacement && attempts < 10)
            {
                red = random.Next(0, 256);
                green = random.Next(0, 256);
                blue = random.Next(0, 256);
                validPlacement = true;
                foreach ((int, int, int) colour in invalidColours)
                {
                    if (Math.Sqrt(
                        (colour.Item1 - red) * (colour.Item1 - red) +
                        (colour.Item2 - green) * (colour.Item2 - green) +
                        (colour.Item3 - blue) * (colour.Item3 - blue)) < 50)
                    {
                        validPlacement = false;
                    }
                }
                attempts++;
            }
            invalidColours.Add((red, green, blue));
            return System.Drawing.Color.FromArgb(255, red, green, blue);
        }



        private void GameStarted(object sender, EventArgs e)
        {
            if (gameStarted)
            {
                // game cancelled
                gameManager.GameCancelled = true;
                gameManager.EndGame();
                return;
            }
            if (players.Count <= 0)
            {
                return;
            }
            // Start the game

            System.Drawing.Color backgroundColour = gameManager.backgroundColour;
            System.Drawing.Color mainBoxColour = gameManager.mainBoxColour;
            System.Drawing.Color subBoxColour = gameManager.subBoxColour;
            Quaternion rotation = gameManager.scene.rootObject == null ? new Quaternion(1.0f, 0.0f, 0.0f, 0.0f) : gameManager.scene.rootObject.Rotation;
            Vector3D stretches = gameManager.Stretches;

            gameManager = new GameManager(Int32.Parse(SizeInput.Text), Int32.Parse(DimensionInput.Text), players, ref Viewport);
            gameManager.gameEndEvent += GameEnded;
            gameManager.nextPlayerEvent += NextPlayer;
            if (gameManager.Players.Count > 0)
            {
                gameManager.PlayBotMoves();
            }

            gameManager.ViewSizeChanged((int)Viewport.ActualWidth, (int)Viewport.ActualHeight, ref Viewport);
            gameManager.backgroundColour = backgroundColour;
            gameManager.mainBoxColour = mainBoxColour;
            gameManager.subBoxColour = subBoxColour;
            if (gameManager.scene.rootObject != null) gameManager.scene.rootObject.Rotation = rotation;
            gameManager.Stretches = stretches;

            gameManager.ResetColours();
            gameManager.Render();

            // Set the current player
            ((PlayerCard)PlayerCardContainer.Children[gameManager.CurrentPlayer - 1]).IsCurrentPlayer = true;
            int previousPlayer = gameManager.CurrentPlayer - 1;
            if (previousPlayer == 0) previousPlayer = gameManager.PlayerCount;
            ((PlayerCard)PlayerCardContainer.Children[previousPlayer - 1]).IsCurrentPlayer = false;

            // Disable controls
            gameStarted = true;
            SizeInput.IsEnabled = false;
            DimensionInput.IsEnabled = false;
            foreach (PlayerCard card in PlayerCardContainer.Children)
            {
                card.UsernameTextbox.IsEnabled = false;
                card.DeleteButton.IsEnabled = false;
                card.MoveUpButton.IsEnabled = false;
                card.MoveDownButton.IsEnabled = false;
            }
            EasyBotButton.IsEnabled = false;
            MediumBotButton.IsEnabled = false;
            HardBotButton.IsEnabled = false;
            PlayerButton.IsEnabled = false;
            SizeSlider.IsEnabled = false;
            DimensionSlider.IsEnabled = false;
            StartButton.Content = "Stop Game";
        }

        private void GameEnded(object? sender, EventArgs e)
        {
            // display end message if not cancelled
            if (!gameManager.GameCancelled)
            {
                if (gameManager.winningPlayer != 0)
                {
                    App.MainApp.winSound.Play();
                    MessageBox.Show(((PlayerCard)(PlayerCardContainer.Children[gameManager.winningPlayer - 1])).PlayerName + " won!");
                }
                else
                {
                    MessageBox.Show("Draw!");
                }
            }

            // Enable controls
            gameStarted = false;
            SizeInput.IsEnabled = true;
            DimensionInput.IsEnabled = true;
            foreach (PlayerCard card in PlayerCardContainer.Children)
            {
                card.UsernameTextbox.IsEnabled = true;
                card.DeleteButton.IsEnabled = true;
                card.MoveUpButton.IsEnabled = true;
                card.MoveDownButton.IsEnabled = true;
            }
            EasyBotButton.IsEnabled = true;
            MediumBotButton.IsEnabled = true;
            HardBotButton.IsEnabled = true;
            PlayerButton.IsEnabled = true;
            SizeSlider.IsEnabled = true;
            DimensionSlider.IsEnabled = true;
            StartButton.Content = "Start Game";

            // Add wins, losses, draws to all playercards if not cancelled
            if (!gameManager.GameCancelled)
            {
                int winningPlayer = gameManager.winningPlayer;
                for (int playerIndex = 0; playerIndex < players.Count; playerIndex++)
                {
                    PlayerCard card = (PlayerCard)PlayerCardContainer.Children[playerIndex];
                    if (playerIndex == winningPlayer - 1)
                    {
                        // this player won
                        card.AddWin();
                    }
                    else if (winningPlayer == 0)
                    {
                        // draw
                        card.AddDraw();
                    }
                    else
                    {
                        // loss
                        card.AddLoss();
                    }
                }
            }

            // update all the player cards to be not the current player
            foreach (PlayerCard card in PlayerCardContainer.Children)
            {
                card.IsCurrentPlayer = false;
            }
        }

        private void NextPlayer(object? sender, EventArgs e)
        {
            ((PlayerCard)PlayerCardContainer.Children[gameManager.CurrentPlayer - 1]).IsCurrentPlayer = true;
            int previousPlayer = gameManager.CurrentPlayer - 1;
            if (previousPlayer == 0) previousPlayer = gameManager.PlayerCount;
            ((PlayerCard)PlayerCardContainer.Children[previousPlayer - 1]).IsCurrentPlayer = false;
        }



        private void ToggleSplitDirection(object sender, RoutedEventArgs e)
        {
            GridSplitButtonHorizontal.IsEnabled = !GridSplitButtonHorizontal.IsEnabled;
            GridSplitButtonVertical.IsEnabled = !GridSplitButtonVertical.IsEnabled;
            gameManager.SwitchSplitDirection();
        }

        private void ToggleSounds(object sender, RoutedEventArgs e)
        {
            SoundsEnableButton.IsEnabled = !SoundsEnableButton.IsEnabled;
            SoundsDisableButton.IsEnabled = !SoundsDisableButton.IsEnabled;
            App.MainApp.soundsOn = !App.MainApp.soundsOn;
        }

        private void ToggleDebug(object sender, RoutedEventArgs e)
        {
            DebugEnableButton.IsEnabled = !DebugEnableButton.IsEnabled;
            DebugDisableButton.IsEnabled = !DebugDisableButton.IsEnabled;
            App.MainApp.Debug = !App.MainApp.Debug;
        }



        private void ThemeChanged(object sender, RoutedEventArgs e)
        {
            App.MainApp.CurrentTheme = App.MainApp.themes[ThemePresetDropdown.SelectedIndex];
            if (gameManager != null)
            {
                UpdateRenderColours();
                gameManager.Render();
            }

            // update all the player cards to be the correct colour
            // update all the player card bot icons to be the correct colour
            foreach (PlayerCard card in PlayerCardContainer.Children)
            {
                card.IsCurrentPlayer = card.IsCurrentPlayer;
                card.IsBot = card.IsBot;
            }
        }

        private void UpdateRenderColours()
        {
            // background colour
            System.Windows.Media.Color backColour;
            object background = App.MainApp.FindResource("Background");
            if (background is System.Windows.Media.SolidColorBrush)
            {
                System.Windows.Media.SolidColorBrush backgroundBrush = (System.Windows.Media.SolidColorBrush)background;
                backColour = backgroundBrush.Color;
            }
            else if (background is System.Windows.Media.GradientBrush)
            {
                System.Windows.Media.GradientBrush backgroundBrush = (System.Windows.Media.GradientBrush)background;
                backColour = backgroundBrush.GradientStops[0].Color;
            }
            gameManager.backgroundColour = System.Drawing.Color.FromArgb(backColour.A, backColour.R, backColour.G, backColour.B);
            invalidColours[0] = (backColour.R, backColour.G, backColour.B);

            // foreground colour (for icon selection)
            System.Windows.Media.Color foreColour;
            object foreground = App.MainApp.FindResource("Foreground");
            if (foreground is System.Windows.Media.SolidColorBrush)
            {
                System.Windows.Media.SolidColorBrush foregroundBrush = (System.Windows.Media.SolidColorBrush)foreground;
                foreColour = foregroundBrush.Color;
            }
            else if (foreground is System.Windows.Media.GradientBrush)
            {
                System.Windows.Media.GradientBrush foregroundBrush = (System.Windows.Media.GradientBrush)foreground;
                foreColour = foregroundBrush.GradientStops[0].Color;
            }
            invalidColours[1] = (foreColour.R, foreColour.G, foreColour.B);

            // main box colour
            System.Windows.Media.Color mainColour;
            System.Windows.Media.SolidColorBrush importantText = (System.Windows.Media.SolidColorBrush)App.MainApp.FindResource("ImportantText");
            mainColour = importantText.Color;
            if (gameManager.scene.rootObject == null)
            {
                return;
            }
            gameManager.mainBoxColour = System.Drawing.Color.FromArgb(mainColour.A, mainColour.R, mainColour.G, mainColour.B);

            // other box colour
            System.Windows.Media.Color otherColour;
            System.Windows.Media.SolidColorBrush standardText = (System.Windows.Media.SolidColorBrush)App.MainApp.FindResource("StandardText");
            otherColour = standardText.Color;
            if (gameManager.scene.rootObject == null)
            {
                return;
            }
            gameManager.subBoxColour = System.Drawing.Color.FromArgb(otherColour.A, otherColour.R, otherColour.G, otherColour.B);

            gameManager.ResetColours();
        }
    }
}