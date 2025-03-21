using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Text;

namespace ANT
{
    public partial class Window1 : Window
    {
        private List<City> cities = new List<City>();  // Список городов
        private double[,] pheromones;  // Матрица феромонов
        private double[,] distances;  // Матрица расстояний между городами
        private Random rand = new Random();
        public Window1()
        {
            InitializeComponent();
        }
        private void OpenWindow1_Click(object sender, RoutedEventArgs e)
        {
            // Save the current window's fullscreen state
            bool isFullscreen = this.WindowState == WindowState.Maximized;
            // Open the new window
            MainWindow window1 = new MainWindow();
            // Apply the fullscreen state to the new window if the current one is fullscreen
            if (isFullscreen)
            {
                window1.WindowState = WindowState.Maximized;
            }

            window1.Show();
            this.Close(); // Close the current window if desired
        }

        private void GenerateCities(int cityCount)
        {
            Random rand = new Random();
            cities.Clear();
            for (int i = 0; i < cityCount; i++)
            {
                cities.Add(new City(rand.Next(50, 750), rand.Next(50, 550)));
            }

            InitializeDistances();  // Инициализация расстояний после добавления всех городов
            DrawGraph();
        }
        private void CreateNewGraph_Click(object sender, RoutedEventArgs e)
        {
            int cityCount = int.Parse(StartVertex.Text);  // Читаем количество городов
            GenerateCities(cityCount);  // Генерируем города
            InitializeDistances();  // Инициализация расстояний
            InitializePheromones();  // Инициализация феромонов
        }


        private void DrawGraph()
        {
            GraphCanvas.Children.Clear();

            // Увеличиваем размер вершин и добавляем номера вершин
            for (int i = 0; i < cities.Count; i++)
            {
                Ellipse cityEllipse = new Ellipse
                {
                    Width = 20, // Увеличиваем размер
                    Height = 20, // Увеличиваем размер
                    Fill = Brushes.LightBlue
                };
                Canvas.SetLeft(cityEllipse, cities[i].X - 10);  // Центрируем эллипс относительно города
                Canvas.SetTop(cityEllipse, cities[i].Y - 10);
                GraphCanvas.Children.Add(cityEllipse);

                // Добавляем текст с номером вершины
                TextBlock cityLabel = new TextBlock
                {
                    Text = i.ToString(),  // Номер вершины
                    Foreground = Brushes.Black,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(cityLabel, cities[i].X - 7);  // Центрируем текст
                Canvas.SetTop(cityLabel, cities[i].Y - 7);  // Центрируем текст
                GraphCanvas.Children.Add(cityLabel);
            }

            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = i + 1; j < cities.Count; j++)
                {
                    Line line = new Line
                    {
                        X1 = cities[i].X,
                        Y1 = cities[i].Y,
                        X2 = cities[j].X,
                        Y2 = cities[j].Y,
                        Stroke = Brushes.Gray,
                        StrokeThickness = 1
                    };

                    // Вычисляем центр дуги для размещения текста
                    double centerX = (cities[i].X + cities[j].X) / 2;
                    double centerY = (cities[i].Y + cities[j].Y) / 2;

                    // Отображаем вес дуги
                    TextBlock edgeWeight = new TextBlock
                    {
                        Text = distances[i, j].ToString("F2"),  // Округляем до двух знаков
                        Foreground = Brushes.Green,
                        FontSize = 12
                    };

                    // Убеждаемся, что веса не перекрывают друг друга
                    double weightOffset = 20;
                    Canvas.SetLeft(edgeWeight, centerX - weightOffset);
                    Canvas.SetTop(edgeWeight, centerY - weightOffset);

                    GraphCanvas.Children.Add(line);
                    GraphCanvas.Children.Add(edgeWeight);
                }
            }
        }
        private void RunAlgorithm_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(AntCount.Text, out int numberOfAnts) || numberOfAnts < 1 ||
                !int.TryParse(IterationCount.Text, out int iterations) || iterations < 1 ||
                !double.TryParse(InitialPheromone.Text, out double initialPheromone) || initialPheromone <= 0 ||
                !double.TryParse(AntPheromoneCapacity.Text, out double antPheromoneCapacity) || antPheromoneCapacity <= 0 ||
                !double.TryParse(Evaporation.Text, out double evaporation) || evaporation < 0 || evaporation > 1 ||
                !double.TryParse(Alpha.Text, out double alpha) || alpha <= 0 ||
                !double.TryParse(Beta.Text, out double beta) || beta <= 0)
            {
                MessageBox.Show("Введите корректные параметры муравьиного алгоритма!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            // Инициализация феромонов перед запуском алгоритма
            InitializePheromones();
            AntAlgorithmResult.Clear(); // Очищаем текстовый блок перед выводом новых данных

            double initialPheromoneTotal = GetTotalPheromone();
            // Строка для вывода всех путей
            StringBuilder allPaths = new StringBuilder();
            List<int> bestPath = null;
            double bestPathLength = double.MaxValue;

            for (int iter = 0; iter < iterations; iter++)
            {
                foreach (var ant in Enumerable.Range(0, numberOfAnts).Select(_ => new Ant()))
                {
                    int startCity = rand.Next(cities.Count);
                    ant.VisitCity(startCity, 0, initialPheromone);

                    while (ant.Path.Count < cities.Count)
                    {
                        int nextCity = ChooseNextCity(ant, alpha, beta);
                        if (nextCity == -1) break;

                        double distance = distances[ant.Path.Last(), nextCity];
                        double pheromone = pheromones[ant.Path.Last(), nextCity];
                        ant.VisitCity(nextCity, distance, pheromone);
                    }

                    if (ant.Path.Count == cities.Count)
                    {
                        int firstCity = ant.Path.First();
                        ant.VisitCity(firstCity, distances[ant.Path.Last(), firstCity], pheromones[ant.Path.Last(), firstCity]);
                    }
                    // Добавляем путь муравья в строку
                    allPaths.AppendLine($"Путь муравья: {string.Join(" -> ", ant.Path)}\n (Длина: {ant.PathLength:F2})");
                    if (ant.PathLength < bestPathLength)
                    {
                        bestPathLength = ant.PathLength;
                        bestPath = new List<int>(ant.Path);
                    }
                }

                // Обновляем феромоны
                UpdatePheromones(evaporation, alpha, beta);
            }

           
            double finalPheromoneTotal = GetTotalPheromone();
            AntAlgorithmResult.Text = $"Общее значение феромонов до начала алгоритма: {initialPheromoneTotal:F2}\n" +
                                      $"Лучший найденный путь: {string.Join(" -> ", bestPath)}\n" +
                                      $"Длина пути: {bestPathLength:F2}\n" +
                                      $"Общее значение феромонов после завершения алгоритма: {finalPheromoneTotal:G}\n";
            Result.Text= $"Все пути:\n{allPaths.ToString()}";
        }

        


        private double GetTotalPheromone()
        {
            double totalPheromone = 0;
            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = i + 1; j < cities.Count; j++)
                {
                    totalPheromone += pheromones[i, j];
                }
            }
            return totalPheromone;
        }

        private void UpdatePheromones(double evaporation, double alpha, double beta)
        {
            // Испарение феромонов
            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = i + 1; j < cities.Count; j++)
                {
                    pheromones[i, j] *= (1 - evaporation);  // Испарение
                    if (pheromones[i, j] < 0) pheromones[i, j] = 0; // Убедитесь, что феромоны не опускаются ниже 0
                    pheromones[j, i] = pheromones[i, j];  // Симметричность феромонов
                }
            }

            // Обновление феромонов на основе пути муравьев
            foreach (var ant in Enumerable.Range(0, cities.Count).Select(_ => new Ant()))
            {
                for (int i = 1; i < ant.Path.Count; i++)
                {
                    int city1 = ant.Path[i - 1];
                    int city2 = ant.Path[i];

                    // Расчет феромонов, которые оставляет муравей
                    double pheromoneDeposit = 1.0 / ant.PathLength;  // Величина феромона зависит от длины пути
                    pheromones[city1, city2] += pheromoneDeposit;
                    pheromones[city2, city1] = pheromones[city1, city2];
                }
            }
        }


        private int ChooseNextCity(Ant ant, double alpha, double beta)
        {
            List<int> unvisitedCities = Enumerable.Range(0, cities.Count).Except(ant.Path).ToList();
            if (unvisitedCities.Count == 0) return -1;

            double totalWeight = unvisitedCities.Sum(city =>
                Math.Pow(pheromones[ant.Path.Last(), city], alpha) * Math.Pow(1.0 / distances[ant.Path.Last(), city], beta));

            double randomValue = rand.NextDouble() * totalWeight;
            foreach (int city in unvisitedCities)
            {
                double probability = Math.Pow(pheromones[ant.Path.Last(), city], alpha) * Math.Pow(1.0 / distances[ant.Path.Last(), city], beta);
                if (randomValue < probability) return city;
                randomValue -= probability;
            }

            return unvisitedCities[0];
        }


        private void InitializeDistances()
        {
            distances = new double[cities.Count, cities.Count];
            for (int i = 0; i < cities.Count; i++)
            {
                for (int j = i + 1; j < cities.Count; j++)
                {
                    double distance = Math.Sqrt(Math.Pow(cities[i].X - cities[j].X, 2) + Math.Pow(cities[i].Y - cities[j].Y, 2));
                    distances[i, j] = distances[j, i] = distance;
                }
            }
        }

        private void InitializePheromones()
        {
            pheromones = new double[cities.Count, cities.Count];

            double initialPheromoneValue;
            if (double.TryParse(InitialPheromone.Text, out initialPheromoneValue) && initialPheromoneValue > 0)
            {
                // Заполняем матрицу феромонов начальным значением
                for (int i = 0; i < cities.Count; i++)
                {
                    for (int j = i + 1; j < cities.Count; j++)
                    {
                        pheromones[i, j] = initialPheromoneValue;
                        pheromones[j, i] = pheromones[i, j];  // Симметричность
                    }
                }
            }
            else
            {
                MessageBox.Show("Введите корректное начальное количество феромонов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private double GetEuclideanDistance(City city1, City city2)
        {
            return Math.Sqrt(Math.Pow(city1.X - city2.X, 2) + Math.Pow(city1.Y - city2.Y, 2));
        }

       
    }

    public class City
    {
        public int X { get; set; }
        public int Y { get; set; }

        public City(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public class Ant
    {
        public List<int> Path { get; set; }  // Путь муравья
        public double PathLength { get; set; }  // Длина пути
        public double TotalPheromone { get; set; }  // Суммарный феромон

        public Ant()
        {
            Path = new List<int>();
            PathLength = 0;
            TotalPheromone = 0;
        }

        public void VisitCity(int cityIndex, double distance, double pheromone)
        {
            Path.Add(cityIndex);
            PathLength += distance;
            TotalPheromone += pheromone;  // Учитываем феромоны
        }
    }
}
