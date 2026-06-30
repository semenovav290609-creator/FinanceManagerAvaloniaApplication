using Dapper;
using Npgsql;
using System;

namespace AvaloniaApplication1.ViewModels
{
    public class FileService
    {
        private readonly string connString = "Host=localhost;Username=postgres;Password=Ar300919;Database=postgres";

        public void CreateDatabaseTable()
        {
            try
            {
                using var connection = new NpgsqlConnection(connString);

                // 1. Таблица СТАТУСА (только баланс и мечта, здесь всегда будет ровно ОДНА строка)
                string createStateTableSql = @"
                CREATE TABLE IF NOT EXISTS public.app_state (
                    id SERIAL PRIMARY KEY,
                    ""Общий баланс"" NUMERIC(15, 2) NOT NULL DEFAULT 0.00,
                    ""Итого потрачено"" NUMERIC(15, 2) NOT NULL DEFAULT 0.00,
                    ""Мечта"" TEXT NOT NULL DEFAULT '',
                    ""Накоплено на мечту"" NUMERIC(15, 2) NOT NULL DEFAULT 0.00
                );";
                connection.Execute(createStateTableSql);

                // 2. Таблица ТРАТ (сюда будут просто складываться все покупки)
                string createExpensesTableSql = @"
                CREATE TABLE IF NOT EXISTS public.expenses (
                    id SERIAL PRIMARY KEY,
                    ""Название траты"" TEXT NOT NULL DEFAULT '',
                    ""Цена траты"" NUMERIC(15, 2) NOT NULL DEFAULT 0.00,
                    ""Категория траты"" TEXT NOT NULL DEFAULT ''
                );";
                connection.Execute(createExpensesTableSql);

                // 3. Создаем первую строку состояния, если таблица пустая
                long stateCount = connection.ExecuteScalar<long>("SELECT COUNT(*) FROM public.app_state;");
                if (stateCount == 0)
                {
                    connection.Execute(@"INSERT INTO public.app_state (""Общий баланс"", ""Итого потрачено"", ""Мечта"", ""Накоплено на мечту"") 
                                         VALUES (0.00, 0.00, '', 0.00);");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании таблиц: {ex.Message}");
                throw;
            }
        }
    }
}
