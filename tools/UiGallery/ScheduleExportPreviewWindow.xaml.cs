using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using FluentWindow = Wpf.Ui.Controls.FluentWindow;

namespace AmberBim.UiGallery
{
    public partial class ScheduleExportPreviewWindow : FluentWindow
    {
        private readonly List<PreviewSchedule> schedules = new List<PreviewSchedule>
        {
            new PreviewSchedule("Zestawienie drzwi — parter", "Drzwi"),
            new PreviewSchedule("Zestawienie drzwi — piętro 1", "Drzwi"),
            new PreviewSchedule("Zestawienie okien", "Okna"),
            new PreviewSchedule("Zestawienie ścian zewnętrznych", "Ściany"),
            new PreviewSchedule("Zestawienie ścian wewnętrznych", "Ściany"),
            new PreviewSchedule("Powierzchnie pomieszczeń", "Pomieszczenia"),
            new PreviewSchedule("Zestawienie stropów", "Stropy"),
            new PreviewSchedule("Zestawienie słupów", "Słupy konstrukcyjne"),
            new PreviewSchedule("Zestawienie wyposażenia", "Wiele kategorii"),
            new PreviewSchedule("Bilans powierzchni — kondygnacje", "Pomieszczenia", true),
            new PreviewSchedule("Zestawienie materiałów", "Wiele kategorii", true)
        };
        private string selectedCategory;

        public ScheduleExportPreviewWindow()
        {
            InitializeComponent();
            SchedulesGrid.ItemsSource = schedules;
        }

        private void OpenCategoryFilter(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu { PlacementTarget = CategoryFilterButton, Placement = PlacementMode.Bottom };
            AddCategory(menu, "Wszystkie kategorie", null);
            menu.Items.Add(new Separator());
            foreach (var category in schedules.Select(s => s.CategoryName).Distinct().OrderBy(s => s))
                AddCategory(menu, category, category);
            CategoryFilterButton.ContextMenu = menu;
            menu.IsOpen = true;
        }

        private void AddCategory(ContextMenu menu, string label, string category)
        {
            var item = new MenuItem { Header = label, IsCheckable = true, IsChecked = selectedCategory == category };
            item.Click += delegate
            {
                selectedCategory = category;
                CategoryFilterButton.Foreground = (Brush)FindResource(category == null
                    ? "TextFillColorPrimaryBrush" : "AccentTextFillColorPrimaryBrush");
                CategoryFilterButton.ToolTip = category == null ? "Filtruj zestawienia po kategorii" : "Kategoria: " + category;
                ApplyFilter();
            };
            menu.Items.Add(item);
        }

        private void FilterChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void CommitSelection()
        {
            SchedulesGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            SchedulesGrid.CommitEdit(DataGridEditingUnit.Row, true);
        }

        private void ApplyFilter()
        {
            if (SchedulesGrid == null || SchedulesGrid.ItemsSource == null) return;
            CommitSelection();
            var search = SearchBox.Text.Trim();
            CollectionViewSource.GetDefaultView(SchedulesGrid.ItemsSource).Filter = item =>
            {
                var schedule = (PreviewSchedule)item;
                return schedule.Name.IndexOf(search, StringComparison.CurrentCultureIgnoreCase) >= 0 &&
                    (selectedCategory == null || schedule.CategoryName == selectedCategory);
            };
        }

        private void SelectVisible(object sender, RoutedEventArgs e)
        {
            CommitSelection();
            foreach (var item in SchedulesGrid.Items.Cast<PreviewSchedule>()) item.IsSelected = true;
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
            MessageBox.Show(this, "To podgląd okna na przykładowych zestawieniach. Eksport pliku jest dostępny w dodatku Revit.",
                "Podgląd eksportu", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Cancel(object sender, RoutedEventArgs e) { Close(); }
        private void HandleKey(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { e.Handled = true; Close(); }
        }
    }

    public sealed class PreviewSchedule
    {
        public string Name { get; }
        public string CategoryName { get; }
        public bool IsSelected { get; set; }
        public string Status { get; }

        public PreviewSchedule(string name, string category, bool readOnly = false)
        {
            Name = name;
            CategoryName = category;
            Status = readOnly ? "Tylko do odczytu — zestawienie grupowane" : "Edycja parametrów";
        }
    }
}
