# Zgodność AmberBim z Revitem

## Kompilacja
- Obsługiwane wersje: Revit 2021–2027, platforma x64.
- Każdy rocznik ma osobną konfigurację Debug i Release.
- Revit 2021–2024: .NET Framework 4.8.
- Revit 2025–2026: net8.0-windows dla wydań bazowych.
- Revit 2025–2026: opcjonalny wariant net10.0-windows
  dla aktualizacji korzystających z .NET 10.
- Revit 2027: net10.0-windows.
- Kompilacja korzysta z API odpowiadającego wybranemu rocznikowi.
- Ścieżkę do bibliotek Revita można nadpisać parametrem.
- Biblioteki Autodesk nie są kopiowane do wyniku kompilacji.
- Wyniki różnych roczników i konfiguracji są rozdzielone.
- Brak wymaganych bibliotek powoduje czytelny błąd kompilacji.

## Dostosowanie poleceń
- Revit 2021–2023 zachowuje polecenia Solid i Not Solid.
- Revit 2024–2027 nie udostępnia tych przycisków;
  sposób wyświetlania zbrojenia kontroluje Revit.
- Otwieranie DWG działa przez skojarzoną aplikację Windows
  także w wersjach opartych na nowoczesnym .NET.
- Polecenia Unobscured, Obscured i Reload DWG pozostają dostępne.
- Manifest dodatku jest dołączany do wyniku kompilacji.
