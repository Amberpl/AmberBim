# AmberBim

Dodatek do Autodesk Revit 2021–2027, wersja 0.3.

## Kompilacja

Wymagane są Windows x64, SDK .NET 10 oraz biblioteki API wybranego
rocznika Revita. Visual Studio musi obsługiwać używany SDK (dla .NET 10:
Visual Studio 2026). Pakiety referencyjne .NET Framework 4.8 i zależności
są przywracane z NuGet.

| Revit | Domyślny framework | Konfiguracje rozwiązania |
| --- | --- | --- |
| 2021 | net48 | Debug2021 / Release2021 |
| 2022 | net48 | Debug2022 / Release2022 |
| 2023 | net48 | Debug2023 / Release2023 |
| 2024 | net48 | Debug2024 / Release2024 |
| 2025 | net8.0-windows | Debug2025 / Release2025 |
| 2026 | net8.0-windows | Debug2026 / Release2026 |
| 2027 | net10.0-windows | Debug2027 / Release2027 |

W Visual Studio wybierz konfigurację rocznika i platformę `x64`.
W PowerShell można zbudować dodatek następująco:

```powershell
dotnet build AmberBim.sln -c Release2024
dotnet build AmberBim.sln -c Release2027
```

Domyślna lokalizacja API to `C:\Program Files\Autodesk\Revit <rok>`.
Przy innej instalacji podaj katalog zawierający `RevitAPI.dll` i `RevitAPIUI.dll`:

```powershell
dotnet build AmberBim.sln -c Release2021 '-p:RevitApiPath=D:\Autodesk\Revit 2021'
```

API musi pochodzić z wybranego rocznika. Projekt sprawdza numery wersji
obu bibliotek i zgłasza błąd przy brakujących lub niezgodnych referencjach.
Nie kopiuje bibliotek Autodesk do katalogu wynikowego.

Dla aktualizacji Revita 2025 i 2026 korzystających z .NET 10:

```powershell
dotnet build AmberBim.sln -c Release2025 -p:RevitUseNet10=true
dotnet build AmberBim.sln -c Release2026 -p:RevitUseNet10=true
```

Te warianty wymagają hosta z aktualizacją do .NET 10. Dla bazowych wydań
2025/2026 użyj domyślnego wariantu .NET 8.

Można też kompilować projekt z parametrem rocznika:

```powershell
dotnet build AmberBim.csproj -c Release -p:RevitVersion=2024
```

Bez konfiguracji rocznika ani parametru `RevitVersion` projekt wybiera 2027.
Wyniki znajdują się w `bin\Revit<rok>\<konfiguracja>\<framework>\`:
`AmberBim.dll`, plik `AmberBim.addin` i wymagane zależności.
Każdy rocznik należy kompilować osobno; pliki pośrednie również są
rozdzielone według rocznika i frameworka.

## Polecenia

Revit 2021–2023 udostępnia przyciski `Solid` i `Not Solid`.
W wersjach 2024–2027 te przyciski są pominięte ze względu na usunięcie
odpowiedniego API. Wyświetlaniem zbrojenia steruje wtedy Revit.
`Unobscured`, `Obscured`, otwieranie i przeładowanie DWG pozostają dostępne.
DWG otwiera się w aplikacji skojarzonej z plikiem w Windows.

Okno informacji korzysta z WPF i WPF UI 4.3.0. Wyświetla wersję dodatku
oraz bieżący rok kalendarzowy. Przy wdrażaniu kopiuj także biblioteki
`Wpf.Ui.dll`, `Wpf.Ui.Abstractions.dll` i pozostałe zależności z katalogu
wynikowego właściwej konfiguracji.

Specyfikacja: [zgodność z Revitem](docs/revit-compatibility.md).


