# Wymiana zestawień z Excelem

## Rola
Eksport zestawień do XLSX i aktualizacja parametrów istniejących
elementów modelu na podstawie zmian w wyeksportowanym pliku.

## Granica
- Funkcja działa na aktywnym dokumencie Revita.
- Nie wymaga zainstalowanego Microsoft Excel.
- Nie tworzy ani nie usuwa elementów modelu.

## Kontrakt
- ListSchedules() -> ScheduleInfo[]: lista zestawień do wyboru.
- Export(scheduleIds: string[], path: string) -> ExportReport:
  zapis wybranych zestawień w jednym skoroszycie XLSX.
- Import(path: string) -> ImportReport: walidacja pliku
  i zastosowanie zmian bez dodatkowego podglądu lub potwierdzenia.
- ScheduleInfo = { UniqueId: string, Name: string,
  CategoryId: string, CategoryName: string,
  ReadOnly: bool, Reason: string }.
- ExportReport = { Path: string, SheetCount: int }.
- ImportReport = { ChangedElements: int, ChangedValues: int,
  SkippedSheets: string[], Errors: string[] }.

## Eksport
- Użytkownik wybiera kilka zestawień i plik docelowy.
- Lista zestawień zawiera kolumnę „Kategoria”.
- Kategoria jest odczytywana z definicji zestawienia.
- Zestawienia wielokategorii mają opis „Wiele kategorii”,
  a nierozpoznana kategoria — „Nieustalona”.
- Nagłówek kolumny „Kategoria” udostępnia filtr z wyborem
  kategorii oraz opcją „Wszystkie kategorie”.
- Filtr kategorii działa łącznie z wyszukiwaniem po nazwie.
- Filtrowanie zachowuje zaznaczenia zestawień.
- Operacje zaznaczania widocznych zestawień obejmują
  wyłącznie pozycje spełniające oba filtry.
- Domyślna nazwa pliku to nazwa pierwszego wybranego
  zestawienia na liście z rozszerzeniem .xlsx.
- Znaki niedozwolone w nazwie pliku są zastępowane podkreśleniem.
- Każde zestawienie trafia do osobnego arkusza.
- Pierwszy arkusz zestawienia jest domyślnie aktywny.
- Arkusz „Info” znajduje się za wszystkimi arkuszami zestawień.
- Opisuje on zasady importu, identyfikatory, nagłówki,
  jednostki, pola tylko do odczytu i zmiany parametrów typu.
- Komórki parametrów typu mają jasnoniebieskie tło.
- Pola tylko do odczytu zachowują dodatkowe wyróżnienie tekstu.
- Arkusz „Info” zawiera legendę kolorów i ostrzeżenie,
  że zmiana parametru typu dotyczy wszystkich jego wystąpień.
- Nazwy arkuszy spełniają ograniczenia Excela i są unikalne.
- Eksport obejmuje nagłówki i wartości widocznych pól.
- Zestawienia grupowane są eksportowane tylko do odczytu.
- Edytowalne są parametry tekstowe, liczbowe i Tak/Nie,
  jednoznacznie przypisane do elementu lub jego typu.
- Pola obliczane, sumy, pola tylko do odczytu oraz dane
  z modeli podłączonych pozostają nieedytowalne.
- Pozostałe rodzaje zestawień są eksportowane tylko do odczytu.
- Ukryte dane techniczne identyfikują model, zestawienie,
  elementy, parametry, jednostki i pierwotne wartości.
- Pierwsza kolumna zestawienia zawiera UniqueId elementu i jest ukryta.
- ID jest uzupełniane także wtedy, gdy wartości innych komórek się powtarzają.
- Ukryty wiersz 1 zawiera identyfikatory parametrów.
- Widoczny wiersz 2 zawiera ich nazwy.
- Pola bez bezpośredniego parametru pozostają tylko do odczytu.
- Dla wiersza zbiorczego bez jednego elementu pozostaje pusta.
- Eksport nie pozostawia zmian w modelu.
- Udany eksport nie wyświetla komunikatu końcowego.
- Błędy eksportu są zgłaszane użytkownikowi.

## Import
- Import obsługuje pliki wygenerowane przez tę funkcję.
- Arkusz „Info” jest pomijany podczas importu.
- Elementy są odnajdywane po UniqueId z pierwszej kolumny.
- Parametry są mapowane po ID z ukrytego wiersza 1.
- Widoczne nazwy są opisowe; ich zmiana nie zmienia powiązań.
- Kolejność wierszy i kolumn danych może być zmieniana.
- Brakujące, nieznane lub niejednoznaczne ID parametrów blokują import.
- Arkusze tylko do odczytu są pomijane i wymieniane w raporcie.
- Zmiany parametrów typu dotyczą wszystkich jego wystąpień.
- Błędne wartości, obcy model, nieznane identyfikatory,
  sprzeczne zmiany i konflikty z aktualnymi danymi blokują zapis.
- Cały import jest wykonywany w jednej transakcji.
- Błąd zapisu wycofuje wszystkie zmiany tego importu.
- Usunięcie wiersza w Excelu nie usuwa elementu w Revicie.
- Ponowny import tego samego pliku nie powiela zmian.
- Po zakończeniu użytkownik otrzymuje raport zmian i pominięć.
- Raport importu jest wyświetlany w oknie WPF UI,
  spójnym z pozostałymi oknami dodatku, powiązanym z oknem Revita.
- Raport zawiera liczbę zmienionych elementów i wartości,
  pominięte arkusze oraz błędy.
