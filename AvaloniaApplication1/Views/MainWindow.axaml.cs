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
            LoadDreamState();
        }

        // 1. КНОПКА: ДОБАВИТЬ ТРАТУ
        private void AddWaste(object? sender, RoutedEventArgs e)
        {
            decimal waste = (decimal)(Price.Value ?? 0);
            if (waste <= 0) return; // Защита от нулевых трат

            _currentBalance -= waste;
            _totalSpent += waste;

            balance.Text = $"Общий баланс: {_currentBalance:N2} ₽";
            TotalSpent.Text = $"Итого потрачено: {_totalSpent:N2} ₽";

            // Безопасное получение текста категории в Avalonia UI
            string selectedCategory = "Прочее";
            if (category.SelectedItem is ComboBoxItem item)
            {
                selectedCategory = item.Content?.ToString() ?? "Прочее";
            }

            var nameBlock = new TextBlock { Text = string.IsNullOrWhiteSpace(NameWaste.Text) ? "Без названия" : NameWaste.Text, FontWeight = FontWeight.Bold, FontSize = 16 };
            var categoryBlock = new TextBlock { Text = $"Категория: {selectedCategory}", FontSize = 12, Foreground = (IBrush)Brush.Parse("#7F8C8D") };
            var priceBlock = new TextBlock { Text = $"-{waste:N2} ₽", Foreground = (IBrush)Brush.Parse("#C0392B"), FontSize = 14 };

            var itemContainer = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(0, 0, 0, 12) };
            itemContainer.Children.Add(nameBlock);
            itemContainer.Children.Add(categoryBlock);
            itemContainer.Children.Add(priceBlock);

            HistoryListBox.Items.Add(itemContainer);

            SaveHistory();
            SaveDreamState();

            NameWaste.Text = string.Empty;
            category.SelectedIndex = -1; // Сбрасываем выбор в ComboBox
            Price.Value = 0;
        }

        // 2. КНОПКА: ПОПОЛНИТЬ БАЛАНС
        private void AddBalance(object? sender, RoutedEventArgs e)
        {
            _currentBalance += (decimal)(EnterAddBalance.Value ?? 0);
            balance.Text = $"Общий баланс: {_currentBalance} ₽";
            SaveDreamState();
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

        private string _dreamName = "";
        private decimal _dreamTarget = 0;
        private decimal _dreamCurrent = 0;

        // КНОПКА: УСТАНОВИТЬ ЦЕЛЬ (Кнопка "ОК")
        private void SaveDreamGoal(object? sender, RoutedEventArgs e)
        {
            _dreamName = string.IsNullOrWhiteSpace(DreamNameInput.Text) ? "Моя мечта" : DreamNameInput.Text;
            _dreamTarget = (decimal)(DreamTargetInput.Value ?? 0);

            UpdateDreamUI();
            SaveDreamState();
        }

        // КНОПКА: ВЛОЖИТЬ ДЕНЬГИ В КОПИЛКУ (Кнопка "Вложить")
        private void InvestInDream(object? sender, RoutedEventArgs e)
        {
            decimal amount = (decimal)(DreamInvestInput.Value ?? 0);

            // Проверки: сумма > 0, денег хватает на балансе, цель вообще создана
            if (amount <= 0 || amount > _currentBalance || _dreamTarget <= 0) return;

            _currentBalance -= amount; // Вычитаем из основного баланса
            _dreamCurrent += amount;   // Переносим в копилку

            // Обновляем строку баланса на экране
            balance.Text = $"Общий баланс: {_currentBalance:N2} ₽";

            UpdateDreamUI();
            SaveDreamState();

            DreamInvestInput.Value = 0; // Сбрасываем счетчик ввода
        }

        // КНОПКА: СБРОСИТЬ КОПИЛКУ (Кнопка "Сброс")
        private void ResetDream(object? sender, RoutedEventArgs e)
        {
            // Возвращаем накопленные деньги обратно на счет перед удалением цели
            _currentBalance += _dreamCurrent;
            balance.Text = $"Общий баланс: {_currentBalance:N2} ₽";

            _dreamName = "";
            _dreamTarget = 0;
            _dreamCurrent = 0;

            // Очищаем текстовые поля ввода
            DreamNameInput.Text = string.Empty;
            DreamTargetInput.Value = 0;
            DreamInvestInput.Value = 0;

            UpdateDreamUI();
            SaveDreamState();
        }

        // Обновление текстов копилки и прогресс-бара на экране
        private void UpdateDreamUI()
        {
            if (_dreamTarget > 0)
            {
                decimal leftToSave = _dreamTarget - _dreamCurrent;
                if (leftToSave < 0) leftToSave = 0;

                DreamTitleText.Text = $"Накопления: {_dreamName}";
                DreamProgressText.Text = $"Собрано: {_dreamCurrent:N2} ₽ из {_dreamTarget:N2} ₽";
                DreamLeftText.Text = leftToSave > 0 ? $"Осталось накопить: {leftToSave:N2} ₽" : "🎉 Ура! Цель достигнута!";

                // Процент для полосы прогресса
                DreamProgressBar.Value = (double)((_dreamCurrent / _dreamTarget) * 100);
            }
            else
            {
                DreamTitleText.Text = "Накопления на мечту";
                DreamProgressText.Text = "Задайте цель и сумму";
                DreamLeftText.Text = "Осталось накопить: 0 ₽";
                DreamProgressBar.Value = 0;
            }
        }

        // Сохранение состояния копилки в JSON
        private void SaveDreamState()
        {
            var data = new Dictionary<string, string>
    {
        { "Name", _dreamName },
        { "Target", _dreamTarget.ToString() },
        { "Current", _dreamCurrent.ToString() },
        { "MainBalance", _currentBalance.ToString() } // <-- ДОБАВИЛИ СТРОКУ
    };
            string json = JsonSerializer.Serialize(data);
            File.WriteAllText("dream.json", json);
        }

        // Последний метод загрузки состояния копилки
        // Загрузка состояния копилки И текущего баланса
        private void LoadDreamState()
        {
            if (!File.Exists("dream.json")) return;
            try
            {
                string json = File.ReadAllText("dream.json");
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (data != null)
                {
                    _dreamName = data.GetValueOrDefault("Name", "");
                    decimal.TryParse(data.GetValueOrDefault("Target", "0"), out _dreamTarget);
                    decimal.TryParse(data.GetValueOrDefault("Current", "0"), out _dreamCurrent);

                    // Читаем баланс из файла. Если файла нет — останется 0
                    decimal.TryParse(data.GetValueOrDefault("MainBalance", "0"), out _currentBalance);

                    DreamNameInput.Text = _dreamName;
                    DreamTargetInput.Value = (decimal)_dreamTarget;

                    // Обновляем текст баланса на экране при старте
                    balance.Text = $"Общий баланс: {_currentBalance:N2} ₽";

                    UpdateDreamUI();
                }
            }
            catch { }
        }

    }
}






