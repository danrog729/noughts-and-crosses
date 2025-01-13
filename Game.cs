using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace noughts_and_crosses
{
    public class PlayerOrientedBoard
    {
        private readonly byte[] bitboards;
        public readonly int Size;
        public readonly int Dimensions;
        public readonly int Length;
        public readonly int PlayerCount;
        private readonly int Stride;
        public int Empties;

        public readonly int MaxPlayerEval;

        public readonly int[] dimensionMultipliers;
        public WinDirection[]? winDirections;

        public PlayerOrientedBoard(int size, int dimensions, int playerCount)
        {
            Size = size;
            Dimensions = dimensions;
            Length = (int)Math.Pow(size, dimensions);
            PlayerCount = playerCount;

            // allocate the memory for the bitboards
            Stride = (int)Math.Ceiling(Length / 8f);
            bitboards = new byte[Stride * (PlayerCount + 1)];
            Empties = Length;
            MaxPlayerEval = PlayerCount * PlayerCount * PlayerCount + PlayerCount;

            // set the "empty" bitboard to all ones
            for (int byteIndex = 0; byteIndex < Stride; byteIndex++)
            {
                bitboards[byteIndex] = 0b_1111_1111;
            }

            // create the dimensional multipliers
            dimensionMultipliers = new int[Dimensions];
            int multiplier = Length / Size;
            for (int dimension = 0; dimension < Dimensions; dimension++)
            {
                dimensionMultipliers[dimension] = multiplier;
                multiplier /= Size;
            }

            // find the win directions
            CalculateWinDirections();
        }

        public PlayerOrientedBoard(PlayerOrientedBoard referenceBoard)
        {
            bitboards = new byte[referenceBoard.bitboards.Length];
            referenceBoard.bitboards.CopyTo(bitboards, 0);
            Size = referenceBoard.Size;
            Dimensions = referenceBoard.Dimensions;
            Length = referenceBoard.Length;
            PlayerCount = referenceBoard.PlayerCount;
            Stride = referenceBoard.Stride;
            Empties = referenceBoard.Empties;
            dimensionMultipliers = new int[Dimensions];
            referenceBoard.dimensionMultipliers.CopyTo(dimensionMultipliers, 0);
            if (referenceBoard.winDirections != null)
            {
                winDirections = new WinDirection[referenceBoard.winDirections.Length];
                referenceBoard.winDirections.CopyTo(winDirections, 0);
            }
            MaxPlayerEval = referenceBoard.MaxPlayerEval;
        }

        /// <summary>
        /// Translates n-dimensional indices into an absolute index
        /// </summary>
        /// <param name="index">The n-dimensional indices</param>
        /// <returns>The absolute index</returns>
        public int AbsIndex(int[] index)
        {
            int absoluteIndex = 0;
            int multiplier = 1;
            for (int dimension = 0; dimension < Dimensions; dimension++)
            {
                absoluteIndex += index[index.Length - dimension - 1] * multiplier;
                multiplier *= Size;
            }
            return absoluteIndex;
        }

        /// <summary>
        /// Translates an absolute index into n-dimensional indices
        /// </summary>
        /// <param name="index">The index to translate</param>
        /// <returns>The n-dimensional indices</returns>
        public int[] DimensionalIndex(int index)
        {
            int[] indices = new int[Dimensions];
            for (int dimension = 0; dimension < Dimensions; dimension++)
            {
                int value = index % (dimensionMultipliers[dimension] * Size);
                indices[dimension] = value / dimensionMultipliers[dimension];
            }
            return indices;
        }

        public int this[int index]
        {
            get
            {
                for (int player = 0; player <= PlayerCount; player++)
                {
                    if ((bitboards[Stride * player + index / 8] << (index % 8) & 0b_1000_0000) != 0)
                    {
                        return player;
                    }
                }
                return 0;
            }
            set
            {
                if (value == 0)
                {
                    // set the cell
                    int oldPlayer = this[index];
                    int byteNumber = index / 8;
                    int byteOffset = index % 8;
                    bitboards[byteNumber] |= (byte)(0b_1000_0000 >> byteOffset);
                    bitboards[byteNumber + Stride * oldPlayer] &= (byte)~(0b_1000_0000 >> byteOffset);
                    Empties++;
                }
                else
                {
                    int byteNumber = index / 8;
                    int byteOffset = index % 8;
                    bitboards[byteNumber] &= (byte)~(0b_1000_0000 >> byteOffset);
                    bitboards[byteNumber + value * Stride] |= (byte)(0b_1000_0000 >> byteOffset);
                    Empties--;
                }
            }
        }

        public int this[int[] indices]
        {
            get
            {
                return this[AbsIndex(indices)];
            }
            set
            {
                this[AbsIndex(indices)] = value;
            }
        }

        /// <summary>
        /// Find all the possible directions that a win can exist in.
        /// </summary>
        /// <returns>An array of WinDirections holding all of the possible win directions</returns>
        public void CalculateWinDirections()
        {
            int upperLimit = (int)Math.Pow(3, Dimensions - 1) * 2;
            int disableIndex = 1;
            int maxMultiplier = 1;
            winDirections = new WinDirection[((int)Math.Pow(3, Dimensions) - 1) / 2];
            int directionIndex = 0;
            for (int stateCode = 1; stateCode < upperLimit; stateCode++, disableIndex++)
            {
                if (disableIndex > maxMultiplier)
                {
                    stateCode += maxMultiplier - 1;
                    disableIndex = 0;
                    maxMultiplier *= 3;
                    continue;
                }

                // find the root of the win condition based on the directions of each axis
                // also at the same time find the offset in index
                int rootIndex = 0;
                int indexOffset = 0;
                int axisOffset = Length / Size;
                int multiplier = (int)Math.Pow(3, Dimensions);
                bool[] variableDimensions = new bool[Dimensions];
                for (int dimension = 0; dimension < Dimensions; dimension++)
                {
                    int axisValue = 0;
                    int digit = stateCode % multiplier;
                    multiplier /= 3;
                    digit /= multiplier;
                    switch (digit)
                    {
                        case 0:
                            variableDimensions[dimension] = true; break;
                        case 1:
                            axisValue = 0;
                            indexOffset += axisOffset; break;
                        case 2:
                            axisValue = Size - 1;
                            indexOffset -= axisOffset; break;
                    }
                    rootIndex += axisValue * dimensionMultipliers[dimension];
                    axisOffset /= Size;
                }
                winDirections[directionIndex] = new WinDirection(rootIndex, indexOffset, variableDimensions);
                directionIndex++;
            }
        }

        public int WinExists(int[] index, bool findAll)
        {
            if (winDirections == null)
            {
                return 0;
            }

            int targetPlayer = this[index];
            if (targetPlayer == 0)
            {
                return 0;
            }
            bool winExists = false;
            for (int directionIndex = 0; directionIndex < winDirections.Length; directionIndex++)
            {
                WinDirection direction = winDirections[directionIndex];
                int rootIndex = direction.rootIndex;
                int indexOffset = direction.indexOffset;

                for (int axisIndex = 0; axisIndex < Dimensions; axisIndex++)
                {
                    if (direction.variableDimensions[axisIndex])
                    {
                        rootIndex += index[axisIndex] * dimensionMultipliers[axisIndex];
                    }
                }

                // check each cell in that line
                rootIndex += targetPlayer * Stride * 8;
                bool win = true;
                for (int cell = rootIndex; cell < rootIndex + Size * indexOffset; cell += indexOffset)
                {
                    if ((bitboards[cell / 8] << (cell % 8) & 0b_1000_0000) == 0)
                    {
                        win = false;
                        break;
                    }
                }
                if (win)
                {
                    winExists = true;
                    if (findAll)
                    {
                        winDirections[directionIndex].win = true;
                        winDirections[directionIndex].player = targetPlayer;
                    }
                    else
                    {
                        return targetPlayer;
                    }
                }
                else
                {
                    winDirections[directionIndex].win = false;
                }
            }
            if (winExists)
            {
                return targetPlayer;
            }
            else
            {
                return 0;
            }
        }
        public int WinExists(int index, bool findAll)
        {
            return WinExists(DimensionalIndex(index), findAll);
        }

        /// <summary>
        /// Converts the board state to a string
        /// </summary>
        /// <returns>The string</returns>
        public override string ToString()
        {
            string output = "";
            for (int index = 0; index < Length; index++)
            {
                output += (int)this[index] + " ";
                int temp = index + 1;
                while (temp % Size == 0 && temp != 0 && temp != Length)
                {
                    output += "\n";
                    temp /= (int)Size;
                }
            }
            return output;
        }
    }

    public class Win(int[] indices, int player)
    {
        public int[] Indices = indices;
        public int Player = player;

        public override string ToString()
        {
            string output = "";
            output += "(";
            for (int index = 0; index < Indices.Length; index++)
            {
                output += Indices[index].ToString();
                if (index < Indices.Length - 1)
                {
                    output += ", ";
                }
            }
            output += ")";
            return output;
        }
    }

    public struct WinDirection(int rootIndex, int indexOffset, bool[] variableDimensions)
    {
        public int rootIndex = rootIndex;
        public int indexOffset = indexOffset;
        public bool[] variableDimensions = variableDimensions;
        public int player = 0;
        public bool win = false;
    }

    public static class Bot
    {
        private static readonly Random random = new();
        private static int samplesPerNode = 10;

        private static readonly bool useImmediatePruning = true;
        private static readonly bool useShallowPruning = true;

        /// <summary>
        /// Chooses a random empty cell and suggests that
        /// </summary>
        /// <param name="board">The board</param>
        /// <returns>An array of integers specifying the coordinates of the chosen cell</returns>
        public static int[] Easy(ref PlayerOrientedBoard board)
        {
            int choice = random.Next(board.Empties);
            int empties = 0;
            int finalIndex = 0;
            for (int index = 0; index < board.Length; index++)
            {
                if (board[index] == 0)
                {
                    // empty
                    empties++;
                }
                if (empties == choice)
                {
                    finalIndex = index;
                    break;
                }
            }
            return board.DimensionalIndex(finalIndex);
        }

        /// <summary>
        /// Performs a Monte-Carlo tree search to estimate the best move in a game
        /// </summary>
        /// <param name="board"></param>
        /// <returns></returns>
        public static int[] Medium(ref PlayerOrientedBoard board, int currentPlayer)
        {
            samplesPerNode = 50 * board.Empties;

            List<int[]> possibleMoves = [];
            List<(int, int, int)> scores = [];
            for (int index = 0; index < board.Length; index++)
            {
                if (board[index] == 0)
                {
                    // empty cell
                    possibleMoves.Add(board.DimensionalIndex(index));

                    board[index] = currentPlayer;
                    scores.Add(MCSample(ref board, currentPlayer));
                    board[index] = 0;
                }
            }

            // if a game with 0 losses exists, return the game with 0 losses that has the most wins
            int maxWins = 0;
            bool noLossesExists = false;
            foreach ((int, int, int) score in scores)
            {
                if (score.Item3 == 0)
                {
                    noLossesExists = true;
                    if (score.Item1 > maxWins)
                    {
                        maxWins = score.Item1;
                    }
                }
            }
            if (noLossesExists)
            {
                int amountEncountered = 0;
                for (int index = 0; index < possibleMoves.Count; index++)
                {
                    if (scores[index].Item3 == 0 && scores[index].Item1 == maxWins)
                    {
                        // potentially return it based on a probability
                        if (random.Next(amountEncountered) > amountEncountered - 2)
                        {
                            return possibleMoves[index];
                        }
                        amountEncountered++;
                    }
                }
            }

            // find the move with the highest score ((wins * 2 + draws) / losses)
            int highestScoreIndex = 0;
            float highestScore = ((scores[0].Item1 * 2 + scores[0].Item2) / (float)scores[0].Item3);
            for (int index = 1; index < possibleMoves.Count; index++)
            {
                float score = ((scores[index].Item1 * 2 + scores[index].Item2) / (float)scores[index].Item3);
                if (score > highestScore)
                {
                    highestScore = score;
                    highestScoreIndex = index;
                }
            }
            return possibleMoves[highestScoreIndex];
        }

        public static (int, int, int) MCSample(ref PlayerOrientedBoard board, int currentPlayer)
        {
            int wins = 0;
            int draws = 0;
            int losses = 0;

            int maxPossibilities = board.Empties;
            for (int i = maxPossibilities - 1; i >= 2; i--)
            {
                maxPossibilities *= i;
                if (maxPossibilities > samplesPerNode)
                {
                    maxPossibilities = samplesPerNode;
                    break;
                }
            }
            for (int sample = 0; sample < maxPossibilities; sample++)
            {
                PlayerOrientedBoard testBoard = new(board);
                int nextPlayer = currentPlayer % board.PlayerCount + 1;
                int winningPlayer = 0;
                while (winningPlayer == 0 && testBoard.Empties > 0)
                {
                    int[] index = Bot.Easy(ref testBoard);
                    testBoard[Bot.Easy(ref testBoard)] = nextPlayer;
                    winningPlayer = testBoard.WinExists(index, false);
                    nextPlayer++;
                    if (nextPlayer > testBoard.PlayerCount)
                    {
                        nextPlayer = 1;
                    }
                }
                // if its done this in one move, then its essentially forced
                if (testBoard.Empties == board.Empties - 1 && winningPlayer != 0)
                {
                    // "nextPlayer" has won, block this possibility
                    return (0, 0, 1);
                }
                if (winningPlayer != 0)
                {
                    // win or loss
                    if (winningPlayer == currentPlayer)
                    {
                        wins++;
                    }
                    else
                    {
                        losses++;
                    }
                }
                else
                {
                    // draw
                    draws++;
                }
            }
            return (wins, draws, losses);
        }

        /// <summary>
        /// Uses the Max^n algorithm to find the best option for play. Follows this paper: https://cdn.aaai.org/AAAI/2000/AAAI00-031.pdf
        /// </summary>
        /// <param name="board">The board</param>
        /// <param name="currentPlayer">The current player</param>
        /// <returns>The dimensional coordinates of the move to play</returns>
        public static int[] Hard(ref PlayerOrientedBoard board, int currentPlayer)
        {
            // check if the board will be full
            bool oneLeft = false;
            if (board.Empties == 1)
            {
                oneLeft = true;
            }

            // Look at the game's children and maximise the evaluation for the current player
            int[] maxEval = new int[board.PlayerCount];
            int maxEvalIndex = 0;
            bool foundMove = false;

            for (int index = 0; index < board.Length; index++)
            {
                if (board[index] == 0)
                {
                    if (oneLeft)
                    {
                        maxEvalIndex = index;
                        break;
                    }

                    // empty cell, child here
                    board[index] = currentPlayer;
                    int nextPlayer = currentPlayer % (int)board.PlayerCount + 1;

                    int[] eval = MaxN(ref board, nextPlayer, index, 15 / board.Size, maxEval[currentPlayer - 1]);
                    if (!foundMove || eval[currentPlayer - 1] > maxEval[currentPlayer - 1])
                    {
                        maxEval = eval;
                        maxEvalIndex = index;
                        foundMove = true;

                        // Immediate pruning. End here if it is the maximum possible evaluation (current player immediately wins)
                        if (useImmediatePruning && maxEval[currentPlayer - 1] == board.MaxPlayerEval)
                        {
                            board[index] = 0;
                            maxEvalIndex = index;
                            break;
                        }
                    }
                    else if (eval[currentPlayer - 1] == maxEval[currentPlayer - 1] && eval[nextPlayer - 1] < maxEval[nextPlayer - 1])
                    {
                        // the next player wins, block them.
                        maxEval = eval;
                        maxEvalIndex = index;
                    }
                    board[index] = 0;
                }
            }
            return board.DimensionalIndex(maxEvalIndex);
        }

        /// <summary>
        /// Implementation of the Max^n algorithm. Follows this paper: https://cdn.aaai.org/AAAI/2000/AAAI00-031.pdf
        /// </summary>
        /// <param name="board">The board</param>
        /// <param name="currentPlayer">The current player</param>
        /// <param name="depth">The current depth. Decrements with each recursion depth</param>
        /// <param name="alpha">The current best value the previous player can get</param>
        /// <returns></returns>
        private static int[] MaxN(ref PlayerOrientedBoard board, int currentPlayer, int mostRecentPlacement, int depth, int alpha)
        {
            depth--;
            int[] maxEval = new int[board.PlayerCount];

            // If terminal state or max depth reached, return the evaluation of the board

            if (board.Empties == 0)
            {
                // draw
                for (int player = 0; player < board.PlayerCount; player++)
                {
                    maxEval[player] = 1;
                }
                return maxEval;
            }
            else if (depth == 0)
            {
                for (int player = 0; player < board.PlayerCount; player++)
                {
                    maxEval[player] = 1;
                }
                return maxEval;
            }
            else
            {
                int winningPlayer = board.WinExists(mostRecentPlacement, false);
                if (winningPlayer != 0)
                {
                    // win
                    maxEval[winningPlayer - 1] = board.MaxPlayerEval;
                    return maxEval;
                }
            }

            // Look at the game's children and maximise the evaluation for the current player
            bool foundMove = false;
            for (int index = 0; index < board.Length; index++)
            {
                if (board[index] == 0)
                {
                    // empty cell, child here
                    board[index] = currentPlayer;
                    int nextPlayer = currentPlayer % (int)board.PlayerCount + 1;

                    int[] eval = MaxN(ref board, nextPlayer, index, depth, maxEval[currentPlayer - 1]);

                    if (!foundMove || eval[currentPlayer - 1] > maxEval[currentPlayer - 1])
                    {
                        maxEval = eval;
                        foundMove = true;

                        // Immediate pruning. End here if it is the maximum possible evaluation (current player immediately wins)
                        if (useImmediatePruning && maxEval[currentPlayer - 1] == board.MaxPlayerEval)
                        {
                            board[index] = 0;
                            return maxEval;
                        }

                        // Shallow pruning. End this branch if the maximum the previous player can get in this scenario is worse than or equal to a previous option
                        if (useShallowPruning && alpha >= board.MaxPlayerEval - maxEval[currentPlayer - 1])
                        {
                            board[index] = 0;
                            return maxEval;
                        }
                    }
                    else if (eval[currentPlayer - 1] >= maxEval[currentPlayer - 1] && eval[nextPlayer - 1] < maxEval[nextPlayer - 1])
                    {
                        // the next player wins, block them.
                        maxEval = eval;
                    }

                    board[index] = 0;
                }
            }
            return maxEval;
        }
    }
}
