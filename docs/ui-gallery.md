# AmberBIM UI Gallery

## Cel
- Galeria umożliwia przeglądanie okien AmberBIM bez uruchamiania Revita.
- Jest osobną aplikacją WPF w katalogu `tools/UiGallery/`.
- Korzysta z tej samej wersji WPF UI co dodatek.
- Nie wymaga bibliotek ani instalacji Revita.

## Przeglądanie
- Użytkownik wybiera okno i jego wariant, a następnie otwiera podgląd.
- Pierwszym oknem jest Info z wariantami Obecne i Kompaktowe.
- Podgląd jest rzeczywistym oknem WPF z działającymi kontrolkami.
- Okno można zamknąć przyciskiem Zamknij lub klawiszem Escape.
- Po zamknięciu podglądu użytkownik wraca do galerii.

## Współdzielenie widoków
- Wariant Obecne korzysta z plików istniejącego okna AboutWindow.
- Wariant Kompaktowe jest osobnym projektem wyglądu w galerii.
- Zatwierdzone widoki i zasoby są współdzielone z dodatkiem.
- Galeria korzysta z przykładowych danych dla widoków zależnych od modelu.
- Dodanie wariantu do galerii nie zmienia okna używanego w Revit.

## Weryfikacja
- Galeria kompiluje się i uruchamia bez zależności od Revita.
- Oba warianty Info otwierają się i obsługują zamknięcie oraz odnośnik.
