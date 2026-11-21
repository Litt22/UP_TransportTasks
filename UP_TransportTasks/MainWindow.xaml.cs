using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UP_TransportTasks
{
    public partial class MainWindow : Window
    {
        private TextBox[,] costBoxes;
        private TextBox[] supplyBoxes;
        private TextBox[] demandBoxes;
        private bool isInitializing = true;
        private readonly Random rnd = new Random();

        private string lastMethodName = "";
        private int[,] lastCosts;
        private int[,] lastPlan;
        private int[] lastSupply;
        private int[] lastDemand;
        private int lastTotalCost;

        public MainWindow()
        {
            InitializeComponent();

            for (int i = 2; i <= 10; i++)
            {
                CmbRows.Items.Add(i);
                CmbCols.Items.Add(i);
            }

            CmbRows.SelectedItem = 3;
            CmbCols.SelectedItem = 5;
            isInitializing = false;

            BuildMatrixGrid(3, 5);
        }

        private void Size_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (isInitializing || CmbRows.SelectedItem == null || CmbCols.SelectedItem == null) return;
            BuildMatrixGrid((int)CmbRows.SelectedItem, (int)CmbCols.SelectedItem);
        }

        private void BuildMatrixGrid(int rows, int cols)
        {
            MatrixGrid.Children.Clear();
            MatrixGrid.RowDefinitions.Clear();
            MatrixGrid.ColumnDefinitions.Clear();

            costBoxes = new TextBox[rows, cols];
            supplyBoxes = new TextBox[rows];
            demandBoxes = new TextBox[cols];

            for (int r = 0; r < rows + 2; r++)
                MatrixGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(30) });

            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(95) });
            for (int c = 0; c < cols; c++)
                MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            MatrixGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });

            AddHeader(MatrixGrid, "Поставщик", 0, 0);
            for (int j = 0; j < cols; j++) AddHeader(MatrixGrid, $"B{j + 1}", 0, j + 1);
            AddHeader(MatrixGrid, "Запасы (a)", 0, cols + 1, Brushes.DarkBlue);

            for (int i = 0; i < rows; i++)
            {
                AddHeader(MatrixGrid, $"A{i + 1}", i + 1, 0);
                for (int j = 0; j < cols; j++)
                {
                    var tb = CreateCellBox(Brushes.White);
                    costBoxes[i, j] = tb;
                    Grid.SetRow(tb, i + 1);
                    Grid.SetColumn(tb, j + 1);
                    MatrixGrid.Children.Add(tb);
                }

                var tbSup = CreateCellBox(new SolidColorBrush(Color.FromRgb(238, 245, 255)));
                tbSup.TextChanged += BalanceInput_TextChanged;
                supplyBoxes[i] = tbSup;
                Grid.SetRow(tbSup, i + 1);
                Grid.SetColumn(tbSup, cols + 1);
                MatrixGrid.Children.Add(tbSup);
            }

            AddHeader(MatrixGrid, "Потребн. (b)", rows + 1, 0, Brushes.DarkGreen);
            for (int j = 0; j < cols; j++)
            {
                var tbDem = CreateCellBox(new SolidColorBrush(Color.FromRgb(238, 255, 238)));
                tbDem.TextChanged += BalanceInput_TextChanged;
                demandBoxes[j] = tbDem;
                Grid.SetRow(tbDem, rows + 1);
                Grid.SetColumn(tbDem, j + 1);
                MatrixGrid.Children.Add(tbDem);
            }

            UpdateLiveBalanceIndicator();
        }

        private void AddHeader(Grid targetGrid, string text, int row, int col, Brush color = null)
        {
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = FontWeights.Bold,
                Foreground = color ?? Brushes.Black,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, col);
            targetGrid.Children.Add(tb);
        }

        private TextBox CreateCellBox(Brush bg)
        {
            return new TextBox
            {
                Margin = new Thickness(2),
                Background = bg,
                TextAlignment = TextAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
        }

        private void BalanceInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isInitializing) UpdateLiveBalanceIndicator();
        }

        private void UpdateLiveBalanceIndicator()
        {
            if (supplyBoxes == null || demandBoxes == null) return;

            int sumA = 0, sumB = 0;
            foreach (var tb in supplyBoxes)
            {
                if (!int.TryParse(tb.Text, out int v) || v < 0)
                {
                    TxtLiveBalanceStatus.Text = "Заполните запасы и потребности";
                    TxtLiveBalanceStatus.Foreground = Brushes.Gray;
                    BtnBalance.Visibility = Visibility.Collapsed;
                    return;
                }
                sumA += v;
            }
            foreach (var tb in demandBoxes)
            {
                if (!int.TryParse(tb.Text, out int v) || v < 0)
                {
                    TxtLiveBalanceStatus.Text = "Заполните запасы и потребности";
                    TxtLiveBalanceStatus.Foreground = Brushes.Gray;
                    BtnBalance.Visibility = Visibility.Collapsed;
                    return;
                }
                sumB += v;
            }

            if (sumA == sumB)
            {
                TxtLiveBalanceStatus.Text = $"Задача сбалансированная ({sumA} = {sumB})";
                TxtLiveBalanceStatus.Foreground = new SolidColorBrush(Color.FromRgb(27, 94, 32));
                BtnBalance.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtLiveBalanceStatus.Text = $"Задача несбалансированная ({sumA} ≠ {sumB})";
                TxtLiveBalanceStatus.Foreground = new SolidColorBrush(Color.FromRgb(230, 81, 0));
                BtnBalance.Visibility = Visibility.Visible;
            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            int rows = (int)CmbRows.SelectedItem;
            int cols = (int)CmbCols.SelectedItem;

            isInitializing = true;
            BuildMatrixGrid(rows, cols);
            isInitializing = false;

            ResultGrid.Children.Clear();
            ResultGrid.RowDefinitions.Clear();
            ResultGrid.ColumnDefinitions.Clear();
            TxtMethodTitle.Text = "Выберите метод расчета";
            TxtCellsInfo.Text = "Занятых клеток: -";
            TxtTotalCost.Text = "-";
            lastPlan = null;
            UpdateLiveBalanceIndicator();
        }

        private void BtnImportExcel_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Файлы Excel CSV (*.csv)|*.csv|Все файлы (*.*)|*.*",
                Title = "Импорт таблицы из Excel"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                var lines = File.ReadAllLines(dlg.FileName, Encoding.UTF8)
                                .Where(l => !string.IsNullOrWhiteSpace(l))
                                .ToList();

                var numericRows = new List<List<int>>();

                foreach (var line in lines)
                {
                    if (line.StartsWith("Опорный план") || line.StartsWith("Итоговая стоимость")) break;

                    var parts = line.Split(new[] { ';', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    var nums = new List<int>();
                    foreach (var p in parts)
                    {
                        if (int.TryParse(p.Trim(), out int val))
                            nums.Add(val);
                    }
                    if (nums.Count >= 3)
                        numericRows.Add(nums);
                }

                if (numericRows.Count < 3)
                    throw new Exception("Недостаточно числовых строк в файле.");

                int rows = numericRows.Count - 1;
                int cols = numericRows[0].Count - 1;

                if (rows < 2 || rows > 10 || cols < 2 || cols > 10)
                    throw new Exception("Размер матрицы в файле должен быть от 2x2 до 10x10.");

                isInitializing = true;
                CmbRows.SelectedItem = rows;
                CmbCols.SelectedItem = cols;
                BuildMatrixGrid(rows, cols);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                        costBoxes[i, j].Text = numericRows[i][j].ToString();

                    supplyBoxes[i].Text = numericRows[i][cols].ToString();
                }

                for (int j = 0; j < cols; j++)
                    demandBoxes[j].Text = numericRows[rows][j].ToString();

                isInitializing = false;
                UpdateLiveBalanceIndicator();
                MessageBox.Show("Данные успешно загружены из файла!", "Импорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                isInitializing = false;
                MessageBox.Show($"Не удалось прочитать файл.\nУбедитесь, что файл сохранен из Excel в формате «CSV (разделители - запятые)».\n\nДетали: {ex.Message}",
                                "Ошибка импорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnExportExcel_Click(object sender, RoutedEventArgs e)
        {
            int rows = costBoxes.GetLength(0);
            int cols = costBoxes.GetLength(1);
            var dlg = new SaveFileDialog
            {
                Filter = "Файл Excel CSV (.csv)|.csv",
                FileName = "Транспортная_задача.csv",
                Title = "Экспорт в Excel"
            };
            if (dlg.ShowDialog() != true) return;
            try
            {
                var sb = new StringBuilder();
                sb.Append("Исходная матрица;");
                for (int j = 0; j < cols; j++) sb.Append($"B{j + 1};");
                sb.AppendLine("Запасы");
                for (int i = 0; i < rows; i++)
                {
                    sb.Append($"A{i + 1};");
                    for (int j = 0; j < cols; j++)
                        sb.Append($"{costBoxes[i, j].Text};");
                    sb.AppendLine($"{supplyBoxes[i].Text}");
                }
                sb.Append("Потребности;");
                for (int j = 0; j < cols; j++)
                    sb.Append($"{demandBoxes[j].Text};");
                sb.AppendLine();
                if (lastPlan != null)
                {
                    sb.AppendLine();
                    sb.Append($"Опорный план ({lastMethodName});");
                    for (int j = 0; j < lastDemand.Length; j++) sb.Append($"B{j + 1};");
                    sb.AppendLine("Запасы");
                    for (int i = 0; i < lastSupply.Length; i++)
                    {
                        sb.Append($"A{i + 1};");
                        for (int j = 0; j < lastDemand.Length; j++)
                            sb.Append($"{lastPlan[i, j]};");
                        sb.AppendLine($"{lastSupply[i]}");
                    }
                    sb.Append("Потребности;");
                    for (int j = 0; j < lastDemand.Length; j++)
                        sb.Append($"{lastDemand[j]};");
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine($"Итоговая стоимость перевозки:;{lastTotalCost}");
                }
                File.WriteAllText(dlg.FileName, sb.ToString(), new UTF8Encoding(true));
                MessageBox.Show("Файл успешно экспортирован! Откройте его в Excel.", "Экспорт завершен", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка экспорта", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnBalance_Click(object sender, RoutedEventArgs e)
        {
            int rows = costBoxes.GetLength(0);
            int cols = costBoxes.GetLength(1);
            int[,] oldCosts = new int[rows, cols];
            int[] oldSupply = new int[rows];
            int[] oldDemand = new int[cols];
            try
            {
                for (int i = 0; i < rows; i++)
                {
                    oldSupply[i] = int.Parse(supplyBoxes[i].Text);
                    for (int j = 0; j < cols; j++)
                        oldCosts[i, j] = int.TryParse(costBoxes[i, j].Text, out int c) ? c : 0;
                }
                for (int j = 0; j < cols; j++)
                    oldDemand[j] = int.Parse(demandBoxes[j].Text);
            }
            catch
            {
                MessageBox.Show("Заполните числовые значения перед балансировкой.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int sumA = oldSupply.Sum();
            int sumB = oldDemand.Sum();
            if (sumA == sumB) return;
            isInitializing = true;
            if (sumA < sumB)
            {
                int newRows = rows + 1;
                BuildMatrixGrid(newRows, cols);
                for (int i = 0; i < rows; i++)
                {
                    supplyBoxes[i].Text = oldSupply[i].ToString();
                    for (int j = 0; j < cols; j++) costBoxes[i, j].Text = oldCosts[i, j].ToString();
                }
                for (int j = 0; j < cols; j++)
                {
                    demandBoxes[j].Text = oldDemand[j].ToString();
                    costBoxes[rows, j].Text = "0";
                    costBoxes[rows, j].Background = new SolidColorBrush(Color.FromRgb(255, 248, 225));
                }
                supplyBoxes[rows].Text = (sumB - sumA).ToString();
                supplyBoxes[rows].Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
            }
            else
            {
                int newCols = cols + 1;
                BuildMatrixGrid(rows, newCols);
                for (int i = 0; i < rows; i++)
                {
                    supplyBoxes[i].Text = oldSupply[i].ToString();
                    for (int j = 0; j < cols; j++) costBoxes[i, j].Text = oldCosts[i, j].ToString();
                    costBoxes[i, cols].Text = "0";
                    costBoxes[i, cols].Background = new SolidColorBrush(Color.FromRgb(255, 248, 225));
                }
                for (int j = 0; j < cols; j++) demandBoxes[j].Text = oldDemand[j].ToString();
                demandBoxes[cols].Text = (sumA - sumB).ToString();
                demandBoxes[cols].Background = new SolidColorBrush(Color.FromRgb(255, 243, 224));
            }
            isInitializing = false;
            UpdateLiveBalanceIndicator();
        }
        private void BtnFillBalanced_Click(object sender, RoutedEventArgs e)
        {
            FillRandomMatrix(true);
        }
        private void BtnFillUnbalanced_Click(object sender, RoutedEventArgs e)
        {
            FillRandomMatrix(false);
        }
        private void FillRandomMatrix(bool balanced)
        {
            int rows = (int)CmbRows.SelectedItem;
            int cols = (int)CmbCols.SelectedItem;
            isInitializing = true;
            BuildMatrixGrid(rows, cols);
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    costBoxes[i, j].Text = rnd.Next(10, 55).ToString();
            int[] sup = new int[rows];
            for (int i = 0; i < rows; i++)
                sup[i] = rnd.Next(10, 35) * 10;
            int total = sup.Sum();
            if (!balanced)
            {
                int delta = rnd.Next(2, 10) * 10;
                total = rnd.Next(2) == 0 ? total + delta : Math.Max(cols * 20, total - delta);
            }
            int[] dem = new int[cols];
            int remaining = total;
            for (int j = 0; j < cols - 1; j++)
            {
                int maxVal = Math.Max(10, remaining - (cols - 1 - j) * 10);
                int val = rnd.Next(1, Math.Max(2, maxVal / 10)) * 10;
                dem[j] = val;
                remaining -= val;
            }
            dem[cols - 1] = remaining;
            for (int i = 0; i < rows; i++) supplyBoxes[i].Text = sup[i].ToString();
            for (int j = 0; j < cols; j++) demandBoxes[j].Text = dem[j].ToString();
            isInitializing = false;
            UpdateLiveBalanceIndicator();
        }
        private bool TryReadData(out int[,] costs, out int[] supply, out int[] demand)
        {
            int rows = costBoxes.GetLength(0);
            int cols = costBoxes.GetLength(1);
            costs = new int[rows, cols];
            supply = new int[rows];
            demand = new int[cols];
            try
            {
                for (int i = 0; i < rows; i++)
                {
                    supply[i] = int.Parse(supplyBoxes[i].Text);
                    if (supply[i] < 0) throw new Exception();
                    for (int j = 0; j < cols; j++)
                    {
                        costs[i, j] = int.Parse(costBoxes[i, j].Text);
                        if (costs[i, j] < 0) throw new Exception();
                    }
                }
                for (int j = 0; j < cols; j++)
                {
                    demand[j] = int.Parse(demandBoxes[j].Text);
                    if (demand[j] < 0) throw new Exception();
                }
            }
            catch
            {
                MessageBox.Show("Проверьте ввод! Все ячейки должны быть заполнены целыми неотрицательными числами.",
                "Ошибка данных", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
            if (supply.Sum() != demand.Sum())
            {
                MessageBox.Show("Задача несбалансированная! Сначала нажмите кнопку «Сбалансировать».",
                "Требуется балансировка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
        private void BtnNorthWest_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadData(out int[,] costs, out int[] supply, out int[] demand)) return;
            int[] origSupply = (int[])supply.Clone();
            int[] origDemand = (int[])demand.Clone();
            int rows = supply.Length;
            int cols = demand.Length;
            int[,] plan = new int[rows, cols];
            int i = 0, j = 0;
            while (i < rows && j < cols)
            {
                int qty = Math.Min(supply[i], demand[j]);
                plan[i, j] = qty;
                supply[i] -= qty;
                demand[j] -= qty;
                if (supply[i] == 0) i++;
                else if (demand[j] == 0) j++;
            }
            DisplayResult("Метод северо-западного угла", costs, plan, origSupply, origDemand);
        }
        private void BtnMinCost_Click(object sender, RoutedEventArgs e)
        {
            if (!TryReadData(out int[,] costs, out int[] supply, out int[] demand)) return;
            int[] origSupply = (int[])supply.Clone();
            int[] origDemand = (int[])demand.Clone();
            int rows = supply.Length;
            int cols = demand.Length;
            int[,] plan = new int[rows, cols];
            bool[] rowClosed = new bool[rows];
            bool[] colClosed = new bool[cols];
            while (rowClosed.Contains(false) && colClosed.Contains(false))
            {
                int minCost = int.MaxValue;
                int minI = -1, minJ = -1;
                for (int i = 0; i < rows; i++)
                {
                    if (rowClosed[i]) continue;
                    for (int j = 0; j < cols; j++)
                    {
                        if (colClosed[j]) continue;
                        if (costs[i, j] < minCost)
                        {
                            minCost = costs[i, j];
                            minI = i;
                            minJ = j;
                        }
                    }
                }
                if (minI == -1) break;
                int qty = Math.Min(supply[minI], demand[minJ]);
                plan[minI, minJ] = qty;
                supply[minI] -= qty;
                demand[minJ] -= qty;
                if (supply[minI] == 0) rowClosed[minI] = true;
                if (demand[minJ] == 0) colClosed[minJ] = true;
            }
            DisplayResult("Метод минимальных элементов", costs, plan, origSupply, origDemand);
        }
        private void DisplayResult(string methodName, int[,] costs, int[,] plan, int[] origSupply, int[] origDemand)
        {
            int rows = plan.GetLength(0);
            int cols = plan.GetLength(1);
            TxtMethodTitle.Text = methodName;
            ResultGrid.Children.Clear();
            ResultGrid.RowDefinitions.Clear();
            ResultGrid.ColumnDefinitions.Clear();
            for (int r = 0; r <= rows + 1; r++)
                ResultGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(32) });
            ResultGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
            for (int c = 0; c < cols; c++)
                ResultGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(68) });
            ResultGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            AddResultCell("Поставщик", 0, 0, Color.FromRgb(226, 232, 240), Brushes.Black, true);
            for (int j = 0; j < cols; j++)
                AddResultCell($"B{j + 1}", 0, j + 1, Color.FromRgb(226, 232, 240), Brushes.Black, true);
            AddResultCell("Запасы", 0, cols + 1, Color.FromRgb(219, 234, 254), Brushes.DarkBlue, true);
            int totalCost = 0;
            int activeCells = 0;
            for (int i = 0; i < rows; i++)
            {
                AddResultCell($"A{i + 1}", i + 1, 0, Color.FromRgb(241, 245, 249), Brushes.Black, true);
                for (int j = 0; j < cols; j++)
                {
                    if (plan[i, j] > 0)
                    {
                        totalCost += plan[i, j] * costs[i, j];
                        activeCells++;
                        AddResultCell(plan[i, j].ToString(), i + 1, j + 1, Color.FromRgb(220, 252, 231), Brushes.Black, true);
                    }
                    else
                    {
                        AddResultCell("0", i + 1, j + 1, Color.FromRgb(255, 255, 255), Brushes.Gray, false);
                    }
                }
                AddResultCell(origSupply[i].ToString(), i + 1, cols + 1, Color.FromRgb(239, 246, 255), Brushes.DarkBlue, true);
            }
            AddResultCell("Потребности", rows + 1, 0, Color.FromRgb(220, 252, 231), Brushes.DarkGreen, true);
            for (int j = 0; j < cols; j++)
                AddResultCell(origDemand[j].ToString(), rows + 1, j + 1, Color.FromRgb(240, 253, 244), Brushes.DarkGreen, true);
            AddResultCell("", rows + 1, cols + 1, Color.FromRgb(241, 245, 249), Brushes.Black, false);
            TxtCellsInfo.Text = $"Занятых клеток: {activeCells} из {rows + cols - 1}";
            TxtTotalCost.Text = $"{totalCost} ед.";
            lastMethodName = methodName;
            lastCosts = costs;
            lastPlan = plan;
            lastSupply = origSupply;
            lastDemand = origDemand;
            lastTotalCost = totalCost;
        }
        private void AddResultCell(string text, int row, int col, Color bgColor, Brush fgColor, bool isBold)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(bgColor),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                BorderThickness = new Thickness(0.5)
            };
            var tb = new TextBlock
            {
                Text = text,
                FontWeight = isBold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = fgColor,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            border.Child = tb;
            Grid.SetRow(border, row);
            Grid.SetColumn(border, col);
            ResultGrid.Children.Add(border);
        }
    }
}
