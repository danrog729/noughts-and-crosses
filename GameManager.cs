using System.Windows.Controls;
using System.Windows.Media;

namespace noughts_and_crosses
{
    class GameManager
    {
        public PlayerOrientedBoard board;
        public Scene3D scene;

        public System.Drawing.Color backgroundColour;
        public System.Drawing.Color mainBoxColour;
        public System.Drawing.Color subBoxColour;

        private Vector3D _stretches;
        public Vector3D Stretches
        {
            get => _stretches;
            set
            {
                if (board.Size <= 1)
                {
                    _stretches = new Vector3D(1.0f, 1.0f, 1.0f);
                }
                else
                {
                    _stretches = value;
                    if (_stretches.X < 1.0f) _stretches.X = 1.0f;
                    if (_stretches.Y < 1.0f) _stretches.Y = 1.0f;
                    if (_stretches.Z < 1.0f) _stretches.Z = 1.0f;
                    if (_stretches.X > 3.5f) _stretches.X = 3.5f;
                    if (_stretches.Y > 3.5f) _stretches.Y = 3.5f;
                    if (_stretches.Z > 3.5f) _stretches.Z = 3.5f;
                    if (board.Dimensions <= 1) _stretches.Y = 1.0f;
                    if (board.Dimensions <= 2) _stretches.Z = 1.0f;
                }
                RecalculateStretches();
            }
        }

        private Vector3D originalScale;
        private Vector3D SplitDirection;

        private int _size;
        public int Size
        {
            get => _size;
            set
            {
                _size = value;
                board = new PlayerOrientedBoard(Size, Dimensions, PlayerCount);
                MakeGrid();
            }
        }

        private int _dimensions;
        public int Dimensions
        {
            get => _dimensions;
            set
            {
                _dimensions = value;
                board = new PlayerOrientedBoard(Size, _dimensions, PlayerCount);
                MakeGrid();
                if (_dimensions <= 1)
                {
                    Stretches = new Vector3D(Stretches.X, 1.0f, 1.0f);
                }
                else if (_dimensions <= 2)
                {
                    Stretches = new Vector3D(Stretches.X, Stretches.Y, 1.0f);
                }
            }
        }

        public int PlayerCount;
        public int CurrentPlayer;
        public bool GameFinished;
        public int winningPlayer;

        public readonly List<Player> Players;

        public GameManager(int size, int dimensions, List<Player> players, ref System.Windows.Controls.Image canvas)
        {
            // Set the attributes to be the arguments
            _size = size;
            _dimensions = dimensions;
            Players = players;
            PlayerCount = players.Count;
            CurrentPlayer = 1;
            GameFinished = false;
            _stretches = new Vector3D(1.0f, 1.0f, 1.0f);
            SplitDirection = new Vector3D(1.0f, 0.0f, 0.0f);

            // Create the board and scene
            board = new PlayerOrientedBoard(Size, Dimensions, PlayerCount);
            scene = new Scene3D(ref canvas);
            MakeGrid();

            winningPlayer = 0;
            if (players.Count > 0)
            {
                PlayBotMoves();
            }

            backgroundColour = System.Drawing.Color.White;
            mainBoxColour = System.Drawing.Color.Black;
            subBoxColour = System.Drawing.Color.Gray;
        }

        private void MakeGrid()
        {
            if (board.Size == 1)
            {
                _stretches = new Vector3D(1.0f, 1.0f, 1.0f);
            }
            // Add the root object for the scene
            originalScale = new Vector3D(
                    1.0f,
                    Dimensions >= 2 ? 1.0f : 1.0f / Size,
                    Dimensions >= 3 ? 1.0f : 1.0f / Size
                    );
            Object3D root = new Object3D()
            {
                Rotation = scene.rootObject == null ? new Quaternion(1.0f, 0.0f, 0.0f, 0.0f) : scene.rootObject.Rotation,
                Scale = new Vector3D(_stretches.X * originalScale.X, _stretches.Y * originalScale.Y, _stretches.Z * originalScale.Z)
            };
            scene.rootObject = root;

            // Add the cells for the board
            for (int cell = 0; cell < board.Length; cell++)
            {
                int[] dimIndex = board.DimensionalIndex(cell);
                Object3D obj = new Object3D()
                {
                    Position = DimIndexToScenePos(dimIndex),
                    Scale = new Vector3D(
                        1.0f / (board.Size * _stretches.X),
                        board.Dimensions >= 2 ? 1.0f / (board.Size * _stretches.Y) : 1.0f,
                        board.Dimensions >= 3 ? 1.0f / (board.Size * _stretches.Z) : 1.0f),
                    colour = System.Drawing.Color.Gray
                };
                scene.rootObject.children.Add(obj);
            }

            if (App.MainApp.Debug)
            {
                // Add the axes
                ObjectLine xAxis = new ObjectLine(new Vector3D(0.0f, 0.0f, 0.0f), new Vector3D(1.0f, 0.0f, 0.0f)) { colour = System.Drawing.Color.Red };
                ObjectLine yAxis = new ObjectLine(new Vector3D(0.0f, 0.0f, 0.0f), new Vector3D(0.0f, 1.0f, 0.0f)) { colour = System.Drawing.Color.Green };
                ObjectLine zAxis = new ObjectLine(new Vector3D(0.0f, 0.0f, 0.0f), new Vector3D(0.0f, 0.0f, 1.0f)) { colour = System.Drawing.Color.Blue };
                scene.rootObject.children.Add(xAxis);
                scene.rootObject.children.Add(yAxis);
                scene.rootObject.children.Add(zAxis);
            }
        }

        public void Render()
        {
            scene.Render();
        }

        public void ViewSizeChanged(int newX, int newY, ref System.Windows.Controls.Image canvas)
        {
            scene.NewSize(newX, newY, ref canvas);
        }

        public void GridStretch(float delta)
        {
            if (scene.rootObject == null)
            {
                return;
            }
            Vector3D localX = (Vector3D)(scene.rootObject.Rotation * new Vector3D(1.0f, 0.0f, 0.0f) * scene.rootObject.Rotation.Conjugate());
            Vector3D localY = (Vector3D)(scene.rootObject.Rotation * new Vector3D(0.0f, 1.0f, 0.0f) * scene.rootObject.Rotation.Conjugate());
            Vector3D localZ = (Vector3D)(scene.rootObject.Rotation * new Vector3D(0.0f, 0.0f, 1.0f) * scene.rootObject.Rotation.Conjugate());
            Vector3D smallestAngle = new Vector3D(1.0f, 0.0f, 0.0f);
            float largestDotProduct = float.Max(Vector3D.DotProduct(localX, SplitDirection), Vector3D.DotProduct(localX, -SplitDirection));
            float yDotProduct = float.Max(Vector3D.DotProduct(localY, SplitDirection), Vector3D.DotProduct(localY, -SplitDirection));
            float zDotProduct = float.Max(Vector3D.DotProduct(localZ, SplitDirection), Vector3D.DotProduct(localZ, -SplitDirection));
            if (yDotProduct > largestDotProduct)
            {
                smallestAngle = new Vector3D(0.0f, 1.0f, 0.0f);
                largestDotProduct = yDotProduct;
            }
            if (zDotProduct > largestDotProduct)
            {
                smallestAngle = new Vector3D(0.0f, 0.0f, 1.0f);
                largestDotProduct = zDotProduct;
            }
            Stretches = new Vector3D(_stretches.X + smallestAngle.X * delta, _stretches.Y + smallestAngle.Y * delta, _stretches.Z + smallestAngle.Z * delta);
        }

        public bool MouseClicked(int positionX, int positionY)
        {
            // Return if the scene has no root or current player is a bot
            if (scene.rootObject == null || Players[CurrentPlayer - 1] is BotPlayer)
            {
                return false;
            }

            // Find which object was clicked on
            Object3D? clickedObject = scene.FindObjectAtPixel(positionX, positionY);
            if (clickedObject == null)
            {
                return false;
            }

            // Find the index of the clicked cell and return if the cell there isn't empty
            int boardIndex = scene.rootObject.children.IndexOf(clickedObject);
            if (board[boardIndex] != 0)
            {
                return false;
            }

            // Set the board's cell to have the correct number
            board[boardIndex] = CurrentPlayer;

            // Add the icon to the display
            clickedObject.children.Add(Players[CurrentPlayer - 1].Icon.icon3D);

            // If a win exists, add the line to indicate so
            winningPlayer = board.WinExists(boardIndex, true);
            if (winningPlayer != 0)
            {
                GameFinished = true;
                AddWinLines();
            }
            if (board.Empties == 0)
            {
                GameFinished = true;
            }

            // Rerender and update the current player
            scene.Render();
            CurrentPlayer = CurrentPlayer % board.PlayerCount + 1;

            if (!GameFinished)
            {
                PlayBotMoves();
            }
            return true;
        }

        public void MouseMoved(float deltaX, float deltaY)
        {
            if (deltaX == 0 && deltaY == 0)
            {
                return;
            }
            float angle = -(float)Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            Vector3D axis = new Vector3D(
                    (float)Math.Sqrt(1.0f / (1 + deltaX * deltaX / deltaY / deltaY)),
                    (float)Math.Sqrt(1.0f - 1.0f / (1 + deltaX * deltaX / deltaY / deltaY)),
                    0.0f);

            if (deltaX < 0.0f)
            {
                axis.X *= -1;
                angle *= -1;
            }
            if (deltaY < 0.0f)
            {
                axis.Y *= -1;
                angle *= -1;
            }
            if (scene.rootObject != null)
            {
                axis = (Vector3D)(Quaternion.Normalise(scene.rootObject.Rotation * axis * scene.rootObject.Rotation.Conjugate()));
            }
            angle *= 0.2f; // sensitivity
            Quaternion rotation = Quaternion.FromAxisAngle(axis, angle);
            if (scene.rootObject != null)
            {
                scene.rootObject.Rotation = rotation * scene.rootObject.Rotation;
            }
            scene.Render();
        }

        public void AddWinLines()
        {
            if (board.winDirections != null && scene.rootObject != null)
            {
                foreach (WinLine win in board.Wins)
                {
                    int rootIndex = win.rootIndex;
                    int finalIndex = win.rootIndex + (board.Size - 1) * win.indexOffset;
                    scene.rootObject.children.Add(new ObjectLine(
                        scene.rootObject.children[rootIndex].Position,
                        scene.rootObject.children[finalIndex].Position)
                    {
                        colour = mainBoxColour
                    });
                }
            }
        }

        public void PlayBotMoves()
        {
            if (scene.rootObject == null)
            {
                return;
            }
            while (Players[CurrentPlayer - 1] is BotPlayer && board.Empties > 0)
            {
                int[] selection = ((BotPlayer)(Players[CurrentPlayer - 1])).Move(board, CurrentPlayer);
                board[selection] = CurrentPlayer;
                scene.rootObject.children[board.AbsIndex(selection)].children.Add(Players[CurrentPlayer - 1].Icon.icon3D);
                CurrentPlayer = CurrentPlayer % board.PlayerCount + 1;
                winningPlayer = board.WinExists(selection, true);
                if (winningPlayer != 0 || board.Empties == 0)
                {
                    GameFinished = true;
                    AddWinLines();
                    scene.Render();
                    break;
                }
                scene.Render();
            }
        }

        private void RecalculateStretches()
        {
            if (scene.rootObject == null) return;

            scene.rootObject.Scale = new Vector3D(_stretches.X * originalScale.X, _stretches.Y * originalScale.Y, _stretches.Z * originalScale.Z);

            for (int index = 0; index < board.Length; index++)
            {
                Object3D child = scene.rootObject.children[index];
                child.Scale = new Vector3D(
                    1.0f / (board.Size * _stretches.X),
                    board.Dimensions >= 2 ? 1.0f / (board.Size * _stretches.Y) : 1.0f,
                    board.Dimensions >= 3 ? 1.0f / (board.Size * _stretches.Z) : 1.0f);
                int[] dimIndex = board.DimensionalIndex(index);
                child.Position = DimIndexToScenePos(dimIndex);
            }

            // clear and re-add the win lines and debug lines
            scene.rootObject.children.RemoveRange(board.Length, scene.rootObject.children.Count - board.Length);
            AddWinLines();

            if (App.MainApp.Debug)
            {
                // Add the axes
                ObjectLine xAxis = new ObjectLine(new Vector3D(0.0f, 0.0f, 0.0f), new Vector3D(1.0f, 0.0f, 0.0f)) { colour = System.Drawing.Color.Red };
                ObjectLine yAxis = new ObjectLine(new Vector3D(0.0f, 0.0f, 0.0f), new Vector3D(0.0f, 1.0f, 0.0f)) { colour = System.Drawing.Color.Green };
                ObjectLine zAxis = new ObjectLine(new Vector3D(0.0f, 0.0f, 0.0f), new Vector3D(0.0f, 0.0f, 1.0f)) { colour = System.Drawing.Color.Blue };
                scene.rootObject.children.Add(xAxis);
                scene.rootObject.children.Add(yAxis);
                scene.rootObject.children.Add(zAxis);
            }
        }

        public void SwitchSplitDirection()
        {
            if (SplitDirection.X == 1.0f)
            {
                SplitDirection.X = 0.0f;
                SplitDirection.Y = 1.0f;
            }
            else
            {
                SplitDirection.X = 1.0f;
                SplitDirection.Y = 0.0f;
            }
        }

        public void ResetColours()
        {
            // colour background
            scene.backgroundColour = backgroundColour;
            if (scene.rootObject != null)
            {
                // root object is main box, colour accordingly
                scene.rootObject.colour = mainBoxColour;
                for (int childIndex = 0; childIndex < scene.rootObject.children.Count; childIndex++)
                {
                    if (childIndex < board.Length)
                    {
                        // sub-box, colour accordingly
                        scene.rootObject.children[childIndex].colour = subBoxColour;
                    }
                    else
                    {
                        // winning line, colour accordingly
                        scene.rootObject.children[childIndex].colour = mainBoxColour;
                    }
                }
            }
        }

        private Vector3D DimIndexToScenePos(int[] dimensionalIndex)
        {
            return board.Size > 1 ? new Vector3D(
                   -1.0f + 1.0f / (board.Size * _stretches.X) + 2.0f * dimensionalIndex[0] * (board.Size * _stretches.X - 1) / (board.Size * board.Size * _stretches.X - board.Size * _stretches.X),
                   board.Dimensions >= 2 ? -1.0f + 1.0f / (board.Size * _stretches.Y) + 2.0f * dimensionalIndex[1] * (board.Size * _stretches.Y - 1) / (board.Size * board.Size * _stretches.Y - board.Size * _stretches.Y) : 0.0f,
                   board.Dimensions >= 3 ? -1.0f + 1.0f / (board.Size * _stretches.Z) + 2.0f * dimensionalIndex[2] * (board.Size * _stretches.Z - 1) / (board.Size * board.Size * _stretches.Z - board.Size * _stretches.Z) : 0.0f)
                   : new Vector3D(0.0f, 0.0f, 0.0f);
        }
    }
    class Player
    {
        public GameIcon Icon;
        public string Name;

        public Player(GameIcon icon, string name)
        {
            Icon = icon;
            Name = name;
        }
    }

    abstract class BotPlayer : Player
    {
        public BotPlayer(GameIcon icon, string name) : base(icon, name)
        {

        }

        public virtual int[] Move(PlayerOrientedBoard board, int currentPlayer)
        {
            return [0, 0, 0];
        }
    }

    class EasyBotPlayer : BotPlayer
    {
        public EasyBotPlayer(GameIcon icon, string name) : base(icon, name)
        {

        }

        public override int[] Move(PlayerOrientedBoard board, int currentPlayer)
        {
            return Bot.Easy(ref board);
        }
    }

    class MediumBotPlayer : BotPlayer
    {
        public MediumBotPlayer(GameIcon icon, string name) : base(icon, name)
        {

        }

        public override int[] Move(PlayerOrientedBoard board, int currentPlayer)
        {
            return Bot.Medium(ref board, currentPlayer);
        }
    }

    class HardBotPlayer : BotPlayer
    {
        public HardBotPlayer(GameIcon icon, string name) : base(icon, name)
        {

        }

        public override int[] Move(PlayerOrientedBoard board, int currentPlayer)
        {
            return Bot.Hard(ref board, currentPlayer);
        }
    }

    abstract public class GameIcon
    {
        public Object3D icon3D;

        public GameIcon(System.Drawing.Color colour)
        {
            icon3D = new Object3D()
            {
                Scale = new Vector3D(0.8f, 0.8f, 0.8f),
                colour = colour
            };
        }

        public virtual void RenderCanvas(ref Canvas target)
        {
            SolidColorBrush colour = new SolidColorBrush(System.Windows.Media.Color.FromRgb(icon3D.colour.R, icon3D.colour.G, icon3D.colour.B));
            target.Children.Clear();

            System.Windows.Shapes.Line line1 = new System.Windows.Shapes.Line();
            line1.Width = 10;
            line1.Stroke = colour;
            line1.X1 = 10;
            line1.Y1 = 10;
            line1.X2 = 20;
            line1.Y2 = 10;
            target.Children.Add(line1);

            System.Windows.Shapes.Line line2 = new System.Windows.Shapes.Line();
            line2.Width = 10;
            line2.Stroke = colour;
            line2.X1 = 10;
            line2.Y1 = 10;
            line2.X2 = 10;
            line2.Y2 = 20;
            target.Children.Add(line2);

            System.Windows.Shapes.Line line3 = new System.Windows.Shapes.Line();
            line3.Width = 10;
            line3.Stroke = colour;
            line3.X1 = 20;
            line3.Y1 = 10;
            line3.X2 = 20;
            line3.Y2 = 20;
            target.Children.Add(line3);

            System.Windows.Shapes.Line line4 = new System.Windows.Shapes.Line();
            line4.Width = 10;
            line4.Stroke = colour;
            line4.X1 = 10;
            line4.Y1 = 20;
            line4.X2 = 20;
            line4.Y2 = 20;
            target.Children.Add(line4);
        }
    }

    public class IconCross : GameIcon
    {
        public IconCross(System.Drawing.Color colour) : base(colour)
        {
            icon3D = new ObjectCross()
            {
                Scale = new Vector3D(0.8f, 0.8f, 0.8f),
                colour = colour
            };
        }

        public override void RenderCanvas(ref Canvas target)
        {
            SolidColorBrush colour = new SolidColorBrush(System.Windows.Media.Color.FromRgb(icon3D.colour.R, icon3D.colour.G, icon3D.colour.B));
            target.Children.Clear();

            System.Windows.Shapes.Line line1 = new System.Windows.Shapes.Line();
            line1.StrokeThickness = 2;
            line1.Stroke = colour;
            line1.X1 = -10;
            line1.Y1 = -10;
            line1.X2 = 10;
            line1.Y2 = 10;
            target.Children.Add(line1);

            System.Windows.Shapes.Line line2 = new System.Windows.Shapes.Line();
            line2.StrokeThickness = 2;
            line2.Stroke = colour;
            line2.X1 = -10;
            line2.Y1 = 10;
            line2.X2 = 10;
            line2.Y2 = -10;
            target.Children.Add(line2);
        }
    }

    public class IconNought : GameIcon
    {
        public IconNought(System.Drawing.Color colour) : base(colour)
        {
            icon3D = new ObjectNought()
            {
                Scale = new Vector3D(0.8f, 0.8f, 0.8f),
                colour = colour
            };
        }

        public override void RenderCanvas(ref Canvas target)
        {
            SolidColorBrush colour = new SolidColorBrush(System.Windows.Media.Color.FromRgb(icon3D.colour.R, icon3D.colour.G, icon3D.colour.B));
            target.Children.Clear();

            System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
            ellipse.Stroke = colour;
            ellipse.StrokeThickness = 2;
            ellipse.Width = 25;
            ellipse.Height = 25;
            target.Children.Add(ellipse);
            Canvas.SetLeft(ellipse, -12.5);
            Canvas.SetTop(ellipse, -12.5);
        }
    }

    public class IconTetrahedron : GameIcon
    {
        public IconTetrahedron(System.Drawing.Color colour) : base(colour)
        {
            icon3D = new ObjectTetrahedron()
            {
                Scale = new Vector3D(0.8f, 0.8f, 0.8f),
                colour = colour
            };
        }

        public override void RenderCanvas(ref Canvas target)
        {
            SolidColorBrush colour = new SolidColorBrush(System.Windows.Media.Color.FromRgb(icon3D.colour.R, icon3D.colour.G, icon3D.colour.B));
            target.Children.Clear();

            System.Windows.Shapes.Polygon triangle = new System.Windows.Shapes.Polygon();
            triangle.Stroke = colour;
            triangle.StrokeThickness = 2;
            triangle.Points.Add(new System.Windows.Point(0, -10));
            triangle.Points.Add(new System.Windows.Point(-10, 10));
            triangle.Points.Add(new System.Windows.Point(10, 10));
            target.Children.Add(triangle);
        }
    }
}
