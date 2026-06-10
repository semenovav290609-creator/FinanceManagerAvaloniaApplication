using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaApplication1.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        // Приватные переменные состояния
        private decimal _currentBalance;
        private decimal _totalSpent;
        private string _nameWaste = string.Empty;
        private string _selectedCategory = "Прочее";
        private decimal _price;
        private decimal _balanceInput;

        private string _dreamName = string.Empty;
        private decimal _dreamTarget;
        private decimal _dreamCurrent;
        private decimal _dreamInvest;

        public MainWindowViewModel()
        {
        }

        // Инициализация данных при старте приложения
        public async Task InitializeDataAsync()
        {
            try
            {
                await LoadHistoryAsync();
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Data initialization error: {ex.Message}");
            }
        }

        // Команды для элементов интерфейса
        public ICommand AddWasteCommand => new AsyncRelayCommand(AddWasteAsync);
        public ICommand AddBalanceCommand => new AsyncRelayCommand(AddBalanceAsync);
        public ICommand DelHistoryCommand => new RelayCommand(DelHistory);
        public ICommand SaveDreamGoalCommand => new AsyncRelayCommand(SaveDreamGoalAsync);
        public ICommand InvestInDreamCommand => new AsyncRelayCommand(InvestInDreamAsync);
        public ICommand ResetDreamCommand => new AsyncRelayCommand(ResetDreamAsync);

        // Публичные свойства для привязки данных
        public decimal CurrentBalance { get => _currentBalance; set { _currentBalance = value; OnPropertyChanged(); } }
        public decimal TotalSpent { get => _totalSpent; set { _totalSpent = value; OnPropertyChanged(); } }
        public string NameWaste { get => _nameWaste; set { _nameWaste = value; OnPropertyChanged(); } }
        public string SelectedCategory { get => _selectedCategory; set { _selectedCategory = value; OnPropertyChanged(); } }
        public decimal Price { get => _price; set { _price = value; OnPropertyChanged(); } }
        public decimal BalanceInput { get => _balanceInput; set { _balanceInput = value; OnPropertyChanged(); } }

        public string DreamName { get => _dreamName; set { _dreamName = value; OnPropertyChanged(); UpdateDreamTexts(); } }
        public decimal DreamTarget { get => _dreamTarget; set { _dreamTarget = value; OnPropertyChanged(); UpdateDreamTexts(); } }
        public decimal DreamCurrent { get => _dreamCurrent; set { _dreamCurrent = value; OnPropertyChanged(); UpdateDreamTexts(); } }
        public decimal DreamInvest { get => _dreamInvest; set { _dreamInvest = value; OnPropertyChanged(); } }

        // Вычисляемые свойства для вывода информации о цели
        public string DreamTitle => string.IsNullOrEmpty(DreamName) ? "Накопления на мечту" : $"Накопления: {DreamName}";
        public string DreamProgress => DreamTarget <= 0 ? "Задайте цель и сумму" : $"Собрано: {DreamCurrent:N2} ₽ из {DreamTarget:N2} ₽";
        public double DreamProgressPercent => DreamTarget <= 0 ? 0 : (double)((DreamCurrent / DreamTarget) * 100);
        public string DreamLeft
        {
            get
            {
                if (DreamTarget <= 0) return "Осталось накопить: 0 ₽";
                decimal left = DreamTarget - DreamCurrent;
                return left > 0 ? $"Осталось накопить: {left:N2} ₽" : "🎉 Ура! Цель достигнута!";
            }
        }

        // Коллекция истории транзакций
        public ObservableCollection<Expense> History { get; set; } = new ObservableCollection<Expense>();
        // Загрузка сохраненного состояния приложения
        public async Task LoadAllDataAsync()
        {
            try
            {
                var loadedData = await FileService.LoadAsync<AppState>("dream.json");

                if (loadedData != null)
                {
                    CurrentBalance = loadedData.Balance;
                    TotalSpent = loadedData.TotalSpent;
                    DreamName = loadedData.DreamName;
                    DreamTarget = loadedData.DreamTarget;
                    DreamCurrent = loadedData.DreamCurrent;

                    UpdateDreamTexts();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading state: {ex.Message}");
            }
        }

        // Загрузка списка расходов из файла
        public async Task LoadHistoryAsync()
        {
            try
            {
                var loadedExpenses = await FileService.LoadAsync<List<Expense>>("history.json");
                if (loadedExpenses != null)
                {
                    History.Clear();
                    decimal tempTotalSpent = 0;

                    foreach (var expense in loadedExpenses)
                    {
                        History.Add(expense);
                        tempTotalSpent += expense.Amount;
                    }
                    TotalSpent = tempTotalSpent;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading history: {ex.Message}");
            }
        }

        // Добавление новой транзакции
        public async Task AddWasteAsync()
        {
            if (Price <= 0) return;

            CurrentBalance -= Price;
            TotalSpent += Price;

            var newExpense = new Expense
            {
                Title = string.IsNullOrWhiteSpace(NameWaste) ? "Без названия" : NameWaste,
                Category = string.IsNullOrEmpty(SelectedCategory) ? "Прочее" : SelectedCategory,
                Amount = Price
            };

            History.Add(newExpense);

            await FileService.SaveAsync("history.json", History);
            await SaveDreamStateAsync();

            NameWaste = string.Empty;
            Price = 0;
        }

        // Пополнение баланса аккаунта
        public async Task AddBalanceAsync()
        {
            if (BalanceInput <= 0) return;

            CurrentBalance += BalanceInput;
            await SaveDreamStateAsync();

            BalanceInput = 0;
        }

        // Полное удаление истории транзакций
        public void DelHistory()
        {
            History.Clear();
            TotalSpent = 0;
            FileService.DeleteFile("history.json");
            _ = SaveDreamStateAsync();
        }

        // Сохранение параметров финансовой цели
        public async Task SaveDreamGoalAsync()
        {
            if (string.IsNullOrWhiteSpace(DreamName)) DreamName = "Моя мечта";
            UpdateDreamTexts();
            await SaveDreamStateAsync();
        }

        // Перевод средств со счета в накопления
        public async Task InvestInDreamAsync()
        {
            if (DreamInvest <= 0 || DreamInvest > CurrentBalance || DreamTarget <= 0) return;

            CurrentBalance -= DreamInvest;
            DreamCurrent += DreamInvest;
            DreamInvest = 0;

            await SaveDreamStateAsync();
        }

        // Сброс текущей цели с возвратом средств на баланс
        public async Task ResetDreamAsync()
        {
            CurrentBalance += DreamCurrent;
            DreamName = string.Empty;
            DreamTarget = 0;
            DreamCurrent = 0;
            DreamInvest = 0;

            await SaveDreamStateAsync();
        }

        // Обновление вычисляемых строковых полей интерфейса
        private void UpdateDreamTexts()
        {
            OnPropertyChanged(nameof(DreamTitle));
            OnPropertyChanged(nameof(DreamProgress));
            OnPropertyChanged(nameof(DreamLeft));
            OnPropertyChanged(nameof(DreamProgressPercent));
        }

        // Запись текущего состояния в json-файл
        public async Task SaveDreamStateAsync()
        {
            var state = new AppState
            {
                Balance = CurrentBalance,
                TotalSpent = TotalSpent,
                DreamName = DreamName,
                DreamTarget = DreamTarget,
                DreamCurrent = DreamCurrent
            };
            await FileService.SaveAsync("dream.json", state);
        }
    }

    // Модель данных для одной транзакции
    public class Expense
    {
        public string Title { get; set; } = "Без названия";
        public string Category { get; set; } = "Прочее";
        public decimal Amount { get; set; }
    }

    // Модель данных для общего состояния приложения
    public class AppState
    {
        public decimal Balance { get; set; }
        public decimal TotalSpent { get; set; }
        public string DreamName { get; set; } = string.Empty;
        public decimal DreamTarget { get; set; }
        public decimal DreamCurrent { get; set; }
    }
}
