# Plan weryfikacji

- Kompilacja i sprawdzenie działania wyłącznie w Revicie 2024.
- Eksport kilku zestawień i ponowne odczytanie skoroszytu.
- Poprawne wyświetlanie kategorii zwykłego zestawienia,
  zestawienia wielokategorii i kategorii nierozpoznanej.
- Filtr kategorii działa razem z wyszukiwaniem po nazwie.
- Wyczyszczenie filtra przywraca wszystkie kategorie.
- Filtrowanie zachowuje zaznaczenia; zaznaczanie widocznych
  obejmuje tylko wyniki aktualnego filtrowania.
- Domyślna nazwa pliku dla jednego i kilku zestawień,
  również przy znakach niedozwolonych w nazwach plików.
- Po udanym eksporcie nie pojawia się komunikat końcowy.
- Pierwsze zestawienie jest aktywne po otwarciu skoroszytu,
  a „Info” znajduje się za arkuszami zestawień.
- Parametry typu są wyróżnione kolorem, opisanym w legendzie;
  pola tylko do odczytu pozostają rozpoznawalne.
- Poprawne nazwy arkuszy, polskie znaki i wartości liczbowe.
- Zestawienia grupowane pozostają tylko do odczytu.
- Import tekstu, liczb, Tak/Nie i parametrów typu.
- Poprawne jednostki i identyfikacja po przestawieniu wierszy i kolumn.
- UniqueId znajduje się w pierwszej, ukrytej kolumnie zestawienia.
- Powtarzające się wartości nie powodują utraty ID elementów.
- ID parametrów znajdują się w ukrytym wierszu nad ich nazwami.
- Zmiana widocznej nazwy nie zmienia powiązania parametru.
- Brakujące, nieznane lub niejednoznaczne ID parametrów blokują import.
- Arkusz „Info” zawiera instrukcję i jest pomijany podczas importu.
- Odrzucenie obcego modelu, uszkodzonych identyfikatorów,
  błędnych wartości i sprzecznych zmian tego samego parametru.
- Wycofanie całego importu po błędzie zapisu.
- Ponowny import nie zmienia już zgodnych wartości.
- Raport importu prezentuje wyniki i błędy w oknie WPF UI.
- Testy plików i walidacji mogą działać poza Revitem;
  faktyczne zmiany modelu wymagają sprawdzenia w Revicie 2024.
