using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json; // Подключил официальную библиотеку для работы с JSON

namespace AvaloniaApplication1.Views
{
    // Класс-модель для удобного сохранения данных в JSON
    public class TransactionSaveModel
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        private decimal _currentBalance = 0;
        private decimal _totalSpent = 0;

        public MainWindow()
        {
            InitializeComponent();
            LoadHistory();
        }

        // 1. КНОПКА: ДОБАВИТЬ ТРАТУ
        private void AddWaste(object? sender, RoutedEventArgs e)
        {
            decimal waste = (decimal)(Price.Value ?? 0);
            _currentBalance -= waste;
            _totalSpent += waste;

            balance.Text = $"Общий баланс: {_currentBalance} ₽";
            TotalSpent.Text = $"Итого потрачено: {_totalSpent} ₽";

            var nameBlock = new TextBlock { Text = NameWaste.Text, FontWeight = FontWeight.Bold, FontSize = 16 };
            var categoryBlock = new TextBlock { Text = $"Категория: {category.Text}", FontSize = 12, Foreground = (IBrush)Brush.Parse("#7F8C8D") };
            var priceBlock = new TextBlock { Text = $"-{waste} ₽", Foreground = (IBrush)Brush.Parse("#C0392B"), FontSize = 14 };

            var itemContainer = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 12) };
            itemContainer.Children.Add(nameBlock);
            itemContainer.Children.Add(categoryBlock);
            itemContainer.Children.Add(priceBlock);

            HistoryListBox.Items.Add(itemContainer);

            SaveHistory(); // Сохраняем уже в настоящем формате JSON

            NameWaste.Text = string.Empty;
            category.Text = string.Empty;
            Price.Value = 0;
        }

        // 2. КНОПКА: ПОПОЛНИТЬ БАЛАНС
        private void AddBalance(object? sender, RoutedEventArgs e)
        {
            _currentBalance += (decimal)(EnterAddBalance.Value ?? 0);
            balance.Text = $"Общий баланс: {_currentBalance} ₽";
            EnterAddBalance.Value = 0;
        }

        // 3. КНОПКА: ОЧИСТИТЬ ВСЮ ИСТОРИЮ
        private void DelHistory(object? sender, RoutedEventArgs e)
        {
            HistoryListBox.Items.Clear();
            _totalSpent = 0;
            TotalSpent.Text = $"Итого потрачено: {_totalSpent} ₽";

            if (File.Exists("history.json"))
            {
                File.Delete("history.json");
            }
        }

        // НАСТОЯЩЕЕ СОХРАНЕНИЕ В JSON
        private void SaveHistory()
        {
            var saveList = new List<TransactionSaveModel>();

            foreach (var item in HistoryListBox.Items)
            {
                if (item is StackPanel panel && panel.Children.Count >= 3)
                {
                    var name = (panel.Children[0] as TextBlock)?.Text ?? "";
                    var cat = (panel.Children[1] as TextBlock)?.Text ?? "";
                    var price = (panel.Children[2] as TextBlock)?.Text ?? "";

                    saveList.Add(new TransactionSaveModel { Name = name, Category = cat, Price = price });
                }
            }

            string jsonString = JsonSerializer.Serialize(saveList, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All)
            });
            
            File.WriteAllText("history.json", jsonString);
        }


        // НАСТОЯЩАЯ ЗАГРУЗКА ИЗ JSON
        private void LoadHistory()
        {
            if (!File.Exists("history.json")) return;

            try
            {
                string jsonString = File.ReadAllText("history.json");
                // Читаем данные из JSON сразу готовым списком объектов
                var loadedTransactions = JsonSerializer.Deserialize<List<TransactionSaveModel>>(jsonString);

                if (loadedTransactions == null) return;

                foreach (var transaction in loadedTransactions)
                {
                    var nameBlock = new TextBlock { Text = transaction.Name, FontWeight = FontWeight.Bold, FontSize = 16 };
                    var categoryBlock = new TextBlock { Text = transaction.Category, FontSize = 12, Foreground = (IBrush)Brush.Parse("#7F8C8D") };
                    var priceBlock = new TextBlock { Text = transaction.Price, Foreground = (IBrush)Brush.Parse("#C0392B"), FontSize = 14 };

                    var itemContainer = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 12) };
                    itemContainer.Children.Add(nameBlock);
                    itemContainer.Children.Add(categoryBlock);
                    itemContainer.Children.Add(priceBlock);

                    HistoryListBox.Items.Add(itemContainer);

                    // Считаем сумму потраченного
                    string rawPrice = transaction.Price.Replace("-", "").Replace(" ₽", "").Trim();
                    if (decimal.TryParse(rawPrice, out decimal parsedPrice))
                    {
                        _totalSpent += parsedPrice;
                    }
                }

                TotalSpent.Text = $"Итого потрачено: {_totalSpent} ₽";
            }
            catch (Exception)
            {
                // Если файл JSON поврежден, программа просто не упадет при старте
            }
        }
    }
}
