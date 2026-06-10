using Avalonia.Controls;
using AvaloniaApplication1.ViewModels;
using System;
using System.Threading.Tasks;

namespace AvaloniaApplication1.Views
{
    public partial class MainWindow : Window
    {
        // Ссылка на ViewModel текущего контекста данных
        private MainWindowViewModel? _viewModel => DataContext as MainWindowViewModel;

        public MainWindow()
        {
            InitializeComponent();

            // Инициализация загрузки при открытии формы
            Opened += MainWindow_Opened;
        }

        private async void MainWindow_Opened(object? sender, EventArgs e)
        {
            // Небольшая задержка для завершения построения интерфейса
            await Task.Delay(100);

            if (_viewModel != null)
            {
                // Вызов метода загрузки локальных данных
                await _viewModel.InitializeDataAsync();
            }
        }
    }
}
