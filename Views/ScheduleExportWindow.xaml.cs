using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using AmberBim.ScheduleExcel;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;

namespace AmberBim.Views
{
    public partial class ScheduleExportWindow : FluentWindow
    {
        private readonly IReadOnlyList<ScheduleInfo> schedules;
        public IReadOnlyList<string> SelectedIds => schedules.Where(s => s.IsSelected).Select(s => s.UniqueId).ToList();

        public ScheduleExportWindow(IReadOnlyList<ScheduleInfo> schedules)
        {
            InitializeComponent();
            this.schedules = schedules;
            SchedulesGrid.ItemsSource = schedules;
            CategoryFilter.ItemsSource = new[] { new ScheduleInfo { CategoryName = "Wszystkie kategorie" } }
                .Concat(schedules.GroupBy(s => s.CategoryId).Select(g => g.First()).OrderBy(s => s.CategoryName)).ToList();
            CategoryFilter.SelectedIndex = 0;
        }

        private void FilterChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CategoryChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CommitSelection()
        {
            SchedulesGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Cell, true);
            SchedulesGrid.CommitEdit(System.Windows.Controls.DataGridEditingUnit.Row, true);
        }

        private void ApplyFilter()
        {
            if (SchedulesGrid == null || SchedulesGrid.ItemsSource == null) return;
            CommitSelection();
            var search = SearchBox.Text.Trim();
            var categoryId = (CategoryFilter.SelectedItem as ScheduleInfo)?.CategoryId;
            CollectionViewSource.GetDefaultView(SchedulesGrid.ItemsSource).Filter = item =>
                ScheduleSelection.Matches((ScheduleInfo)item, search, categoryId);
        }

        private void SelectVisible(object sender, RoutedEventArgs e)
        {
            CommitSelection();
            foreach (var item in SchedulesGrid.Items.Cast<ScheduleInfo>()) item.IsSelected = true;
            SchedulesGrid.Items.Refresh();
        }

        private void ClearSelection(object sender, RoutedEventArgs e)
        {
            CommitSelection();
            foreach (var item in schedules) item.IsSelected = false;
            SchedulesGrid.Items.Refresh();
        }

        private void Accept(object sender, RoutedEventArgs e)
        {
            CommitSelection();
            if (SelectedIds.Count == 0)
            {
                MessageBox.Show(this, "Zaznacz co najmniej jedno zestawienie.", "AmberBIM", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
        }

        private void Cancel(object sender, RoutedEventArgs e) { DialogResult = false; }
        private void HandleKey(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { e.Handled = true; Close(); } }
    }
}
