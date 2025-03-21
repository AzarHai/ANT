using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace ANT
{
    public partial class MainWindow : Window
    {
        private Graph graph;

        public MainWindow()
        {
            InitializeComponent();
        }

        // Загрузка графа из файла
        private void LoadGraph_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Текстовые файлы|*.txt" };
            if (openFileDialog.ShowDialog() == true)
            {
                graph = Graph.LoadFromFile(openFileDialog.FileName);
                MessageBox.Show("Граф загружен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DrawGraph(); // Визуализация графа после загрузки
            }
        }


        // Запуск алгоритма
        private void RunAlgorithm_Click(object sender, RoutedEventArgs e)
        {
            if (graph == null)
            {
                MessageBox.Show("Сначала загрузите граф!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                int start = int.Parse(StartVertex.Text);
                int end = int.Parse(EndVertex.Text);
                int antCount = int.Parse(AntCount.Text);
                int iterations = int.Parse(IterationCount.Text);
                double evaporation = double.Parse(Evaporation.Text);
                double alpha = double.Parse(Alpha.Text);
                double beta = double.Parse(Beta.Text);
                double initialPheromone = double.Parse(InitialPheromone.Text);
                double antPheromoneCapacity = double.Parse(AntPheromoneCapacity.Text);

                if (start < 0 || start >= graph.Size || end < 0 || end >= graph.Size)
                {
                    MessageBox.Show("Начальная или конечная вершина выходит за пределы графа!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Муравьиный алгоритм
                AntColony antColony = new AntColony(graph, antCount, iterations, evaporation, alpha, beta, initialPheromone, antPheromoneCapacity, this);
                var (antPath, antLength, pheromonesBefore, pheromonesAfter) = antColony.FindShortestPath(start, end);

                string antResult = antPath == null || antPath.Count == 0
                    ? "Муравьиный алгоритм: путь не найден."
                    : $"Муравьиный алгоритм:\nПуть: {string.Join(" -> ", antPath)}\nДлина пути: {antLength}\nФеромоны до: {pheromonesBefore}\nФеромоны после: {pheromonesAfter}";

                // Алгоритм Дейкстры
                List<int> dijkstraPath = graph.Dijkstra(start, end);
                string dijkstraResult = dijkstraPath.Count == 0
                    ? "Алгоритм Дейкстры: путь не найден."
                    : $"Алгоритм Дейкстры:\nПуть: {string.Join(" -> ", dijkstraPath)}\nДлина пути: {dijkstraPath.Select((v, i) => i > 0 ? graph.Matrix[dijkstraPath[i - 1], v] : 0).Sum()}";

                // Обновляем текстовые блоки
                AntAlgorithmResult.Text = antResult;
                DijkstraResult.Text = dijkstraResult;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void OpenWindow1_Click(object sender, RoutedEventArgs e)
        {
            // Save the current window's fullscreen state
            bool isFullscreen = this.WindowState == WindowState.Maximized;
            // Open the new window
            Window1 window1 = new Window1();
            // Apply the fullscreen state to the new window if the current one is fullscreen
            if (isFullscreen)
            {
                window1.WindowState = WindowState.Maximized;
            }

            window1.Show();
            this.Close(); // Close the current window if desired
        }
        private void DrawGraph()
        {
            if (graph == null) return;

            GraphCanvas.Children.Clear();
            int nodeCount = graph.Size;
            double canvasWidth = GraphCanvas.ActualWidth;
            double canvasHeight = GraphCanvas.ActualHeight;

            Point[] positions = new Point[nodeCount];

            // Расставляем вершины по кругу
            double centerX = canvasWidth / 2;
            double centerY = canvasHeight / 2;
            double radius = Math.Min(canvasWidth, canvasHeight) / 2;

            for (int i = 0; i < nodeCount; i++)
            {
                double angle = i * 2 * Math.PI / nodeCount;
                positions[i] = new Point(centerX + radius * Math.Cos(angle), centerY + radius * Math.Sin(angle));

                // Рисуем вершину
                Ellipse vertex = new Ellipse
                {
                    Width = 30,
                    Height = 30,
                    Fill = Brushes.LightBlue,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };

                Canvas.SetLeft(vertex, positions[i].X - 15);
                Canvas.SetTop(vertex, positions[i].Y - 15);
                GraphCanvas.Children.Add(vertex);

                // Подписываем вершину
                TextBlock label = new TextBlock
                {
                    Text = i.ToString(),
                    Foreground = Brushes.Black,
                    FontWeight = FontWeights.Bold
                };

                Canvas.SetLeft(label, positions[i].X - 5);
                Canvas.SetTop(label, positions[i].Y - 10);
                GraphCanvas.Children.Add(label);
            }

            // Рисуем рёбра со стрелками
            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = 0; j < nodeCount; j++)
                {
                    if (graph.Matrix[i, j] > 0)
                    {
                        DrawArrow(positions[i], positions[j]);

                        // Добавляем вес ребра
                        TextBlock weightLabel = new TextBlock
                        {
                            Text = graph.Matrix[i, j].ToString(),
                            Foreground = Brushes.Red,
                            FontWeight = FontWeights.Bold
                        };

                        double midX = (positions[i].X + positions[j].X) / 2;
                        double midY = (positions[i].Y + positions[j].Y) / 2;

                        Canvas.SetLeft(weightLabel, midX);
                        Canvas.SetTop(weightLabel, midY);
                        GraphCanvas.Children.Add(weightLabel);
                    }
                }
            }
        }
        private void DrawArrow(Point start, Point end)
        {
            double vertexRadius = 15; // Радиус вершины (половина размера)

            // Вычисляем вектор направления
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double length = Math.Sqrt(dx * dx + dy * dy);

            // Смещаем начальную и конечную точки ближе к границе вершины
            double offsetX = (dx / length) * vertexRadius;
            double offsetY = (dy / length) * vertexRadius;

            Point adjustedStart = new Point(start.X + offsetX, start.Y + offsetY);
            Point adjustedEnd = new Point(end.X - offsetX, end.Y - offsetY);

            // Рисуем линию
            Line line = new Line
            {
                X1 = adjustedStart.X,
                Y1 = adjustedStart.Y,
                X2 = adjustedEnd.X,
                Y2 = adjustedEnd.Y,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            GraphCanvas.Children.Add(line);

            // Добавляем стрелку
            DrawArrowhead(adjustedStart, adjustedEnd);
        }

        private void DrawArrowhead(Point start, Point end)
        {
            double arrowSize = 10; // Длина стрелки
            double angle = Math.Atan2(end.Y - start.Y, end.X - start.X);

            Point arrowPoint1 = new Point(
                end.X - arrowSize * Math.Cos(angle - Math.PI / 6),
                end.Y - arrowSize * Math.Sin(angle - Math.PI / 6)
            );

            Point arrowPoint2 = new Point(
                end.X - arrowSize * Math.Cos(angle + Math.PI / 6),
                end.Y - arrowSize * Math.Sin(angle + Math.PI / 6)
            );

            Polygon arrowHead = new Polygon
            {
                Points = new PointCollection { end, arrowPoint1, arrowPoint2 },
                Fill = Brushes.Black
            };

            GraphCanvas.Children.Add(arrowHead);
        }


        public class AntColony
        {
            private Graph graph;
            private int antCount;
            private int iterations;
            private double evaporation;
            private double alpha;
            private double beta;
            private double[,] pheromones;
            private MainWindow mainWindow;
            private double initialPheromone;
            private double antPheromoneCapacity;
            public AntColony(Graph graph, int antCount, int iterations, double evaporation, double alpha, double beta, double initialPheromone, double antPheromoneCapacity, MainWindow mainWindow)
            {
                this.graph = graph;
                this.antCount = antCount;
                this.iterations = iterations;
                this.evaporation = evaporation;
                this.alpha = alpha;
                this.beta = beta;
                this.initialPheromone = initialPheromone;
                this.antPheromoneCapacity = antPheromoneCapacity;
                this.mainWindow = mainWindow;

                // Инициализация массива феромонов
                this.pheromones = new double[graph.Size, graph.Size];

                for (int i = 0; i < graph.Size; i++)
                    for (int j = 0; j < graph.Size; j++)
                        pheromones[i, j] = graph.Matrix[i, j] > 0 ? initialPheromone : 0;


            }

            private double GetTotalPheromone()
            {
                double total = 0;
                for (int i = 0; i < graph.Size; i++)
                    for (int j = 0; j < graph.Size; j++)
                        total += pheromones[i, j]; // Массив феромонов

                return total;
            }
            public (List<int> path, double length, double pheromonesBefore, double pheromonesAfter) FindShortestPath(int start, int end)
            {
                List<int> bestPath = null;
                double bestLength = double.MaxValue;
                // Вывод феромонов до выполнения алгоритма
                double pheromonesBefore = GetTotalPheromone();

                for (int iter = 0; iter < iterations; iter++)
                {
                    List<int> path = new List<int> { start };
                    int current = start;
                    double length = 0;

                    while (current != end)
                    {
                        int next = SelectNextNode(current, path);
                        if (next == -1) break;

                        length += graph.Matrix[current, next];
                        path.Add(next);
                        current = next;
                    }

                    if (length < bestLength)
                    {
                        bestPath = new List<int>(path);
                        bestLength = length;
                    }
                    DepositPheromones(path, length);
                    EvaporatePheromones();
                }
                // Вывод феромонов после выполнения алгоритма
                double pheromonesAfter = GetTotalPheromone();
                return (bestPath, bestLength, pheromonesBefore, pheromonesAfter);
            }

            private int SelectNextNode(int current, List<int> path)
            {
                List<int> candidates = new List<int>();
                List<double> probabilities = new List<double>();
                double sum = 0.0;

                for (int i = 0; i < graph.Size; i++)
                {
                    if (!path.Contains(i) && graph.Matrix[current, i] > 0) // Вершина не должна быть посещена ранее
                    {
                        double pheromone = Math.Pow(pheromones[current, i], alpha);
                        double heuristic = Math.Pow(1.0 / graph.Matrix[current, i], beta); // Чем меньше расстояние, тем лучше
                        double probability = pheromone * heuristic;

                        candidates.Add(i);
                        probabilities.Add(probability);
                        sum += probability;
                    }
                }

                if (candidates.Count == 0) return -1; // Нет доступных вершин

                // Выбираем вершину на основе вероятности
                double rand = new Random().NextDouble() * sum;
                double cumulative = 0.0;

                for (int i = 0; i < candidates.Count; i++)
                {
                    cumulative += probabilities[i];
                    if (rand <= cumulative)
                        return candidates[i];
                }

                return candidates[candidates.Count - 1]; // Запасной вариант, если округление дало сбой
            }

            private void EvaporatePheromones()
            {
                for (int i = 0; i < graph.Size; i++)
                    for (int j = 0; j < graph.Size; j++)
                        pheromones[i, j] *= (1 - evaporation);
            }

            private void DepositPheromones(List<int> path, double length)
            {
                double pheromoneToDeposit = antPheromoneCapacity / length; // Чем короче путь, тем больше феромонов

                for (int i = 0; i < path.Count - 1; i++)
                {
                    int from = path[i];
                    int to = path[i + 1];
                    pheromones[from, to] += pheromoneToDeposit;
                }
            }


        }
        public class Graph
        {
            public int[,] Matrix { get; private set; }
            public int Size { get; private set; }

            // Загрузка графа из файла (матрица смежности)
            public static Graph LoadFromFile(string path)
            {
                var lines = File.ReadAllLines(path);
                int size = lines.Length;
                int[,] matrix = new int[size, size];

                for (int i = 0; i < size; i++)
                {
                    // Используем Split(',') для разделения по запятой
                    var weights = lines[i].Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int j = 0; j < size; j++)
                    {
                        matrix[i, j] = int.Parse(weights[j]); // Преобразуем строку в число
                    }
                }

                return new Graph { Matrix = matrix, Size = size };
            }
            public List<int> Dijkstra(int start, int end)
            {
                int n = Size;
                int[] distances = new int[n];
                bool[] visited = new bool[n];
                int[] previous = new int[n];

                for (int i = 0; i < n; i++)
                {
                    distances[i] = int.MaxValue;
                    visited[i] = false;
                    previous[i] = -1;
                }

                distances[start] = 0;

                for (int i = 0; i < n; i++)
                {
                    int minDist = int.MaxValue, minIndex = -1;

                    for (int j = 0; j < n; j++)
                    {
                        if (!visited[j] && distances[j] < minDist)
                        {
                            minDist = distances[j];
                            minIndex = j;
                        }
                    }

                    if (minIndex == -1)
                        break;

                    visited[minIndex] = true;

                    for (int j = 0; j < n; j++)
                    {
                        if (Matrix[minIndex, j] > 0 && !visited[j])
                        {
                            int newDist = distances[minIndex] + Matrix[minIndex, j];

                            if (newDist < distances[j])
                            {
                                distances[j] = newDist;
                                previous[j] = minIndex;
                            }
                        }
                    }
                }

                List<int> path = new List<int>();
                for (int at = end; at != -1; at = previous[at])
                {
                    path.Add(at);
                }
                path.Reverse();

                return path.Count > 1 ? path : new List<int>();
            }

        }

    }
}
