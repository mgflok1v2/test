using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Drawing.Common;

namespace cli_life
{
    public class Cell
    {
        public bool IsAlive;
        public readonly List<Cell> Neighbors = new List<Cell>();
        public bool IsAliveNext;
        
        public void DetermineNextLiveState()
        {
            int liveNeighbors = Neighbors.Count(x => x.IsAlive);
            if (IsAlive)
                IsAliveNext = liveNeighbors == 2 || liveNeighbors == 3;
            else
                IsAliveNext = liveNeighbors == 3;
        }
        
        public void Advance()
        {
            IsAlive = IsAliveNext;
        }
    }
    
    public class Board
    {
        public readonly Cell[,] Cells;
        public readonly int CellSize;
        private readonly Random rand = new Random();
        
        public int Columns => Cells.GetLength(0);
        public int Rows => Cells.GetLength(1);
        public int Width => Columns * CellSize;
        public int Height => Rows * CellSize;
        
        public Board(int width, int height, int cellSize, double liveDensity = .1)
        {
            CellSize = cellSize;

            Cells = new Cell[width / cellSize, height / cellSize];
            for (int x = 0; x < Columns; x++)
                for (int y = 0; y < Rows; y++)
                    Cells[x, y] = new Cell();

            ConnectNeighbors();
            Randomize(liveDensity);
        }

        public void Randomize(double liveDensity)
        {
            foreach (var cell in Cells)
                cell.IsAlive = rand.NextDouble() < liveDensity;
        }

        public void Advance()
        {
            foreach (var cell in Cells)
                cell.DetermineNextLiveState();
            foreach (var cell in Cells)
                cell.Advance();
        }
        
        private void ConnectNeighbors()
        {
            for (int x = 0; x < Columns; x++)
            {
                for (int y = 0; y < Rows; y++)
                {
                    int xL = (x > 0) ? x - 1 : Columns - 1;
                    int xR = (x < Columns - 1) ? x + 1 : 0;
                    int yT = (y > 0) ? y - 1 : Rows - 1;
                    int yB = (y < Rows - 1) ? y + 1 : 0;

                    Cells[x, y].Neighbors.Add(Cells[xL, yT]);
                    Cells[x, y].Neighbors.Add(Cells[x, yT]);
                    Cells[x, y].Neighbors.Add(Cells[xR, yT]);
                    Cells[x, y].Neighbors.Add(Cells[xL, y]);
                    Cells[x, y].Neighbors.Add(Cells[xR, y]);
                    Cells[x, y].Neighbors.Add(Cells[xL, yB]);
                    Cells[x, y].Neighbors.Add(Cells[x, yB]);
                    Cells[x, y].Neighbors.Add(Cells[xR, yB]);
                }
            }
        }
    }
    
    class Program
    {
        static Board board;
        static private void Reset()
        {
            board = new Board(
                width: 50,
                height: 20,
                cellSize: 1,
                liveDensity: 0.5);
        }
        
        static void Render()
        {
            for (int row = 0; row < board.Rows; row++)
            {
                for (int col = 0; col < board.Columns; col++)   
                {
                    var cell = board.Cells[col, row];
                    if (cell.IsAlive)
                    {
                        Console.Write('*');
                    }
                    else
                    {
                        Console.Write(' ');
                    }
                }
                Console.Write('\n');
            }
        }
        
        static void SaveState(string fileName)
        {
            var state = new bool[board.Columns][];
            for (int x = 0; x < board.Columns; x++)
            {
                state[x] = new bool[board.Rows];
                for (int y = 0; y < board.Rows; y++)
                {
                    state[x][y] = board.Cells[x, y].IsAlive;
                }
            }
    
            string json = JsonSerializer.Serialize(state);
            File.WriteAllText(fileName, json);
        }
        
        static void LoadState(string fileName)
        {
            string json = File.ReadAllText(fileName);
            var stateList = JsonSerializer.Deserialize<List<List<bool>>>(json);
    
            int width = stateList.Count;
            int height = stateList[0].Count;
            bool[,] state = new bool[width, height];
    
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    state[x, y] = stateList[x][y];
                }
            }
    
            for (int x = 0; x < board.Columns; x++)
            {
                for (int y = 0; y < board.Rows; y++)
                {
                    board.Cells[x, y].IsAlive = state[x, y];
                }
            }
        }
        
        static int CountElements(Board board)
        {
            int count = 0;
            for (int x = 0; x < board.Columns; x++)
            {
                for (int y = 0; y < board.Rows; y++)
                {
                    if (board.Cells[x, y].IsAlive)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        

        static void LoadSettings(string fileName, out int width, out int height, out int cellSize, out double liveDensity)
        {
            string json = File.ReadAllText(fileName);
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement root = doc.RootElement;
                width = root.GetProperty("Width").GetInt32();
                height = root.GetProperty("Height").GetInt32();
                cellSize = root.GetProperty("CellSize").GetInt32();
                liveDensity = root.GetProperty("LiveDensity").GetDouble();
            }
        }
   
        static void Main(string[] args)
        {
            int equalNum = 0;
            int numGen = 0;
            
            Reset();
            string settingsFileName = "settings.json";
            int width, height, cellSize;
            double liveDensity;
            LoadSettings(settingsFileName, out width, out height, out cellSize, out liveDensity);
            board = new Board(width, height, cellSize, liveDensity);
            
           while (true)
           {
                numGen++;

                Console.Clear();
                Render();
                
                int elementCount = CountElements(board);
               
                Console.WriteLine($"Количество элементов на поле: {elementCount}");

                board.Advance();
                
                int elementCountNext = CountElements(board);
                Console.WriteLine($"Next: {elementCountNext}");
                Console.WriteLine($"equalNum: {equalNum}");


                if (elementCountNext == elementCount)
                {
                    if (equalNum != 20)
                    {
                        equalNum++;
                    }
                }
                else
                {
                    equalNum = 0;
                }

                if (equalNum == 20)
                {
                    Console.WriteLine("равны");
                    break;
                }
                Thread.Sleep(100);
            } 
           string fileNameSave = "state.json"; 
           SaveState(fileNameSave);
           Console.WriteLine("конец");
           Console.WriteLine(numGen-20);
           
          var plt = new ScottPlot.Plot(600, 400);
          double[] values = { 26, 20, 23, 7, 16 };
          plt.AddBar(values);
          plt.SetAxisLimits(yMin: 0);
          plt.SaveFig("bar_quickstart.png");

            
     
        }
    }
}
