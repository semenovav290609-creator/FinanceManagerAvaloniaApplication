using CommunityToolkit.Mvvm.Input;
using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AvaloniaApplication1.ViewModels
{
    // Класс-модель для точного маппинга данных из таблицы app_state через Dapper
    public class UserDbRow
    {
        public decimal balance { get; set; }
        public decimal totalspent { get; set; }
        public string dreamname { get; set; } = string.Empty;
        public decimal dreamcurrent { get; set; }
    }

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
            var fileService = new FileService();
            fileService.CreateDatabaseTable();

            await LoadAllDataAsync();  // Загрузит баланс из таблицы app_state
            await LoadHistoryAsync();  // Загрузит строки трат из таблицы expenses
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

        // Загрузка сохраненного состояния приложения (Таблица app_state)
        public async Task LoadAllDataAsync()
        {
            string connString = "Host=localhost;Username=postgres;Password=Ar300919;Database=postgres";

            try
            {
                using var connection = new NpgsqlConnection(connString);

                string sql = @"
                SELECT 
                    ""Общий баланс"" AS balance, 
                    ""Итого потрачено"" AS totalspent, 
                    ""Мечта"" AS dreamname, 
                    ""Накоплено на мечту"" AS dreamcurrent 
                FROM public.app_state 
                LIMIT 1;";

                var loadedData = await connection.QueryFirstOrDefaultAsync<UserDbRow>(sql);

                if (loadedData != null)
                {
                    CurrentBalance = loadedData.balance;
                    TotalSpent = loadedData.totalspent;
                    DreamName = loadedData.dreamname;
                    DreamCurrent = loadedData.dreamcurrent;

                    UpdateDreamTexts();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading state from DB: {ex.Message}");
            }
        }
        // Загрузка списка расходов из базы данных (Таблица expenses)
        public async Task LoadHistoryAsync()
        {
            string connString = "Host=localhost;Username=postgres;Password=Ar300919;Database=postgres";

            try
            {
                using var connection = new NpgsqlConnection(connString);

                string sql = @"
                SELECT 
                    ""Название траты"" AS Title, 
                    ""Цена траты"" AS Amount, 
                    ""Категория траты"" AS Category 
                FROM public.expenses
                ORDER BY id ASC;";

                var loadedExpenses = await connection.QueryAsync<dynamic>(sql);

                if (loadedExpenses != null)
                {
                    History.Clear();
                    foreach (var exp in loadedExpenses)
                    {
                        if (exp != null)
                        {
                            var expense = new Expense
                            {
                                Title = (string)(exp.title ?? "Без названия"),
                                Amount = (decimal)(exp.amount ?? 0m),
                                Category = (string)(exp.category ?? "Прочее")
                            };
                            // Вставляем наверх списка, чтобы свежие траты были первыми
                            History.Insert(0, expense);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ОШИБКА ЗАГРУЗКИ ИСТОРИИ ИЗ БД: {ex.Message}");
            }
        }

        // Добавление новой транзакции
        public async Task AddWasteAsync()
        {
            if (Price <= 0) return;

            decimal newBalance = CurrentBalance - Price;
            decimal newTotalSpent = TotalSpent + Price;

            CurrentBalance = newBalance;
            TotalSpent = newTotalSpent;

            var newExpense = new Expense
            {
                Title = string.IsNullOrWhiteSpace(NameWaste) ? "Без названия" : NameWaste,
                Category = string.IsNullOrEmpty(SelectedCategory) ? "Прочее" : SelectedCategory,
                Amount = Price
            };

            History.Add(newExpense);

            string connString = "Host=localhost;Username=postgres;Password=Ar300919;Database=postgres";

            try
            {
                using var connection = new NpgsqlConnection(connString);

                // Шаг 1: Обновляем баланс в таблице app_state
                string updateGlobalSql = @"
                UPDATE public.app_state 
                SET 
                    ""Общий баланс"" = @Balance,
                    ""Итого потрачено"" = @TotalSpent;";

                await connection.ExecuteAsync(updateGlobalSql, new { Balance = newBalance, TotalSpent = newTotalSpent });

                // Шаг 2: Вставляем покупку в отдельную таблицу expenses
                string insertExpenseSql = @"
                INSERT INTO public.expenses (
                    ""Название траты"", 
                    ""Цена траты"", 
                    ""Категория траты""
                )
                VALUES (@Title, @Amount, @Category);";

                await connection.ExecuteAsync(insertExpenseSql, new
                {
                    Title = newExpense.Title,
                    Amount = newExpense.Amount,
                    Category = newExpense.Category
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"!!! ОШИБКА БАЗЫ: {ex.Message}");
            }

            NameWaste = string.Empty;
            Price = 0;
        }
        // Пополнение баланса аккаунта
        public async Task AddBalanceAsync()
        {
            if (BalanceInput <= 0) return;

            CurrentBalance += BalanceInput;

            string connString = "Host=localhost;Username=postgres;Password=Ar300919;Database=postgres";
            try
            {
                using var connection = new NpgsqlConnection(connString);
                string sql = @"UPDATE public.app_state SET ""Общий баланс"" = @Balance;";
                await connection.ExecuteAsync(sql, new { Balance = CurrentBalance });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error adding balance to DB: {ex.Message}");
            }

            BalanceInput = 0;
        }

        // Полное удаление истории транзакций
        public void DelHistory()
        {
            History.Clear();
            TotalSpent = 0;

            string connString = "Host=localhost;Username=postgres;Password=Ar300919;Database=postgres";

            try
            {
                using var connection = new NpgsqlConnection(connString);

                string updateGlobalSql = @"UPDATE public.app_state SET ""Итого потрачено"" = 0.00;";
                connection.Execute(updateGlobalSql);

                string truncateExpensesSql = @"TRUNCATE TABLE public.expenses;";
                connection.Execute(truncateExpensesSql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing history in DB: {ex.Message}");
            }
        }

        // Общий асинхронный метод для сохранения состояния Мечты в базу данных
        public async Task SaveDreamStateAsync()
        {
            string connString = "Host=localhost;Username=postgres;Password=Ar300919;Database=postgres";

            try
            {
                using var connection = new NpgsqlConnection(connString);

                string sql = @"
                UPDATE public.app_state 
                SET 
                    ""Общий баланс"" = @Balance,
                    ""Итого потрачено"" = @TotalSpent,
                    ""Мечта"" = @DreamName,
                    ""Накоплено на мечту"" = @DreamCurrent;";

                await connection.ExecuteAsync(sql, new
                {
                    Balance = CurrentBalance,
                    TotalSpent = TotalSpent,
                    DreamName = DreamName,
                    DreamCurrent = DreamCurrent
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving dream state to DB: {ex.Message}");
            }
        }

        // Сохранение параметров financial цели
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
    } // Конец класса MainWindowViewModel

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
