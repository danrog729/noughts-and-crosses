using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace noughts_and_crosses
{
    class GameManager
    {
        public PlayerOrientedBoard board;
        public Scene3D scene;

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
                board = new PlayerOrientedBoard(Size, Dimensions, PlayerCount);
                MakeGrid();
            }
        }

        public int PlayerCount;
        public int CurrentPlayer;
        public bool GameFinished;

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

            // Create the board and scene
            board = new PlayerOrientedBoard(Size, Dimensions, PlayerCount);
            scene = new Scene3D(ref canvas);
            MakeGrid();

            if (players.Count > 0)
            {
                PlayBotMoves();
            }
        }

        private void MakeGrid()
        {
            // Add the root object for the scene
            Object3D root = new Object3D()
            {
                Scale = new Vector3D(
                    1.0f,
                    Dimensions >= 2 ? 1.0f : 1.0f / Size,
                    Dimensions >= 3 ? 1.0f : 1.0f / Size
                    )
            };
            scene.rootObject = root;

            // Add the cells for the board
            for (int cell = 0; cell < board.Length; cell++)
            {
                int[] dimIndex = board.DimensionalIndex(cell);
                Object3D obj = new Object3D()
                {
                    Position = new Vector3D(
                        -1.0f + (1.0f / Size) + (1.0f / Size) * dimIndex[0] * 2,
                        Dimensions >= 2 ? -1.0f + (1.0f / Size) + (1.0f / Size) * dimIndex[1] * 2 : 0.0f,
                        Dimensions >= 3 ? -1.0f + (1.0f / Size) + (1.0f / Size) * dimIndex[2] * 2 : 0.0f),
                    Scale = new Vector3D(
                        1.0f / Size,
                        Dimensions >= 2 ? 1.0f / Size : 1.0f,
                        Dimensions >= 3 ? 1.0f / Size : 1.0f),
                    colour = System.Drawing.Color.Gray
                };
                scene.rootObject.children.Add(obj);
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

        public void MouseClicked(int positionX, int positionY)
        {
            // Return if the scene has no root
            if (scene.rootObject == null)
            {
                return;
            }

            // Return if the current player is a bot
            if (Players[CurrentPlayer - 1] is BotPlayer)
            {
                return;
            }

            // Find which object was clicked on
            Object3D? clickedObject = scene.FindObjectAtPixel(positionX, positionY);
            if (clickedObject == null)
            {
                return;
            }

            // Find the index of the clicked cell and return if the cell there isn't empty
            int boardIndex = scene.rootObject.children.IndexOf(clickedObject);
            if (board[boardIndex] != 0)
            {
                return;
            }

            // Set the board's cell to have the correct number
            board[boardIndex] = CurrentPlayer;

            // Add the icon to the display
            clickedObject.children.Add(Players[CurrentPlayer - 1].Icon.icon3D);

            // If a win exists, add the line to indicate so
            if (board.WinExists(boardIndex, true) != 0 && board.winDirections != null)
            {
                GameFinished = true;
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
                                )
                        {
                            colour = System.Drawing.Color.Black
                        });
                    }
                }
            }
            if (board.Empties == 0)
            {
                GameFinished = true;
            }

            // Rerender and update the current player
            scene.Render();
            CurrentPlayer = CurrentPlayer % board.PlayerCount + 1;

            PlayBotMoves();
        }

        public void MouseMoved(float deltaX, float deltaY)
        {
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
            angle *= 0.2f; // sensitivity
            Quaternion rotation = Quaternion.FromAxisAngle(axis, angle);
            if (scene.rootObject != null)
            {
                scene.rootObject.Rotation = rotation * scene.rootObject.Rotation;
            }
            scene.Render();
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
                scene.Render();
                if (board.WinExists(selection, false) != 0)
                {
                    return;
                }
            }
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

    abstract class GameIcon
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

    class IconCross : GameIcon
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

            System.Windows.Shapes.Line line1 = new System.Windows.Shapes.Line();
            line1.Width = 10;
            line1.Stroke = colour;
            line1.X1 = 10;
            line1.Y1 = 10;
            line1.X2 = 20;
            line1.Y2 = 20;
            target.Children.Add(line1);

            System.Windows.Shapes.Line line2 = new System.Windows.Shapes.Line();
            line2.Width = 10;
            line2.Stroke = colour;
            line2.X1 = 10;
            line2.Y1 = 20;
            line2.X2 = 20;
            line2.Y2 = 10;
            target.Children.Add(line2);
        }
    }

    class IconNought : GameIcon
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

            System.Windows.Shapes.Ellipse ellipse = new System.Windows.Shapes.Ellipse();
            ellipse.Width = 10;
            ellipse.Stroke = colour;
            ellipse.Width = 20;
            ellipse.Height = 20;
            target.Children.Add(ellipse);
        }
    }

    class IconTetrahedron : GameIcon
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

            System.Windows.Shapes.Polygon triangle = new System.Windows.Shapes.Polygon();
            triangle.Stroke = colour;
            triangle.Width = 10;
            triangle.Points.Add(new System.Windows.Point(15, 10));
            triangle.Points.Add(new System.Windows.Point(10, 20));
            triangle.Points.Add(new System.Windows.Point(20, 20));
            target.Children.Add(triangle);
        }
    }
}
