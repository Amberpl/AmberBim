using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using AmberBim.ScheduleExcel;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;

namespace AmberBim.Views
{
    public partial class ScheduleExportWindow : FluentWindow
    {
        private readonly IReadOnlyList<ScheduleInfo> schedules;
        private string selectedCategoryId;
        public IReadOnlyList<string> SelectedIds => schedules.Where(s => s.IsSelected).Select(s => s.UniqueId).ToList();

        public ScheduleExportWindow(IReadOnlyList<ScheduleInfo> schedules)
        {
            InitializeComponent();
            this.schedules = schedules;
            SchedulesGrid.ItemsSource = schedules;
        }

        private void FilterChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void OpenCategoryFilter(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = CategoryFilterButton, Placement = PlacementMode.Bottom };
            AddCategory(menu, "Wszystkie kategorie", null);
            menu.Items.Add(new Separator());
            foreach (var category in schedules.GroupBy(s => s.CategoryId).Select(g => g.First()).OrderBy(s => s.CategoryName))
                AddCategory(menu, category.CategoryName, category.CategoryId);
            CategoryFilterButton.ContextMenu = menu;
            menu.IsOpen = true;
        }

        private void AddCategory(ContextMenu menu, string label, string categoryId)
        {
            var item = new MenuItem { Header = label, IsCheckable = true, IsChecked = selectedCategoryId == categoryId };
            item.Click += delegate
            {
                selectedCategoryId = categoryId;
                CategoryFilterButton.Foreground = (Brush)FindResource(categoryId == null
                    ? "TextFillColorPrimaryBrush" : "AccentTextFillColorPrimaryBrush");
                CategoryFilterButton.ToolTip = categoryId == null ? "Filtruj zestawienia po kategorii" : "Kategoria: " + label;
                ApplyFilter();
            };
            menu.Items.Add(item);
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
            CollectionViewSource.GetDefaultView(SchedulesGrid.ItemsSource).Filter = item =>
                ScheduleSelection.Matches((ScheduleInfo)item, search, selectedCategoryId);
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
