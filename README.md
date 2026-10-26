# Калькулятор параметров атмосферы (ISA Atmosphere Calculator)

Учебный командный проект на **C# и Avalonia UI** (.NET 10) для расчёта давления, плотности и температуры воздуха на разных высотах по модели Международной стандартной атмосферы (ISA).


## Возможности

* Ввод высоты от 0 до 20 000 м с клавиатуры или слайдером. Поле и слайдер обновляют друг друга.
* Мгновенный расчёт давления (Па), плотности (кг/м³) и температуры (°C) по модели ISA.
* Два слоя атмосферы: тропосфера (0–11 000 м) и нижняя стратосфера (11 000–20 000 м).
* Цвет карточки температуры плавно меняется от тёплого к холодному.
* График падения давления с высотой и маркером текущей точки.
* Проверка ввода: сообщение об ошибке, если введено не число или высота вне диапазона.

## 1. Архитектура проекта (MVVM)

Проект построен по паттерну **Model — View — ViewModel** (Avalonia + CommunityToolkit.Mvvm):

```text
View (.axaml, привязки) ◄──Binding──► ViewModel (состояние и логика UI) ──► Model (физика атмосферы)
```

| Слой | Папка | Ответственность |
|------|-------|-----------------|
| **Model** | `Models/` | `AtmosphereCalculator` — расчёт по ISA, `AtmosphereData` — результат. Не зависит от UI. |
| **ViewModel** | `ViewModels/` | `MainViewModel` — ввод высоты, валидация, синхронизация поля и слайдера, форматирование значений. Не знает о контролах Avalonia. |
| **View** | `Views/` | `MainWindow.axaml` — разметка и привязки; `Controls/PressureChart` — отрисовка графика. Code-behind без бизнес-логики. |
| Конвертеры | `Converters/` | Преобразование данных ViewModel в кисти (цвет температуры, цвет ошибки). |

## 2. Структура проекта

```text
AtmosphereCalc/
├── AtmosphereCalc.slnx          # решение (приложение + тесты)
├── AtmosphereCalc.csproj        # проект приложения
├── global.json                  # версия .NET SDK
├── Models/                      # Model: физика атмосферы
├── ViewModels/                  # ViewModel: состояние и логика UI
├── Views/                       # View: окна и контролы (.axaml)
│   └── Controls/                # PressureChart — график давления
├── Converters/                  # конвертеры для привязок
├── App.axaml(.cs)               # точка входа Avalonia
├── Program.cs                   # Main
├── AtmosphereCalc.Tests/        # юнит-тесты (xUnit)
└── *.md                         # README, описание программы, ТЗ, гайд по Git
```

## 3. Запуск

### Требования

* [.NET SDK 10](https://dotnet.microsoft.com/download) (версия задана в `global.json`).

Проверить установленные SDK:

```bash
dotnet --list-sdks
```

На macOS SDK можно поставить через Homebrew:

```bash
brew install --cask dotnet-sdk
```

### Из терминала

В корне репозитория:

```bash
dotnet run --project AtmosphereCalc.csproj
```

Только сборка:

```bash
dotnet build AtmosphereCalc.slnx
```

### Из IDE

* **Visual Studio 2026** (Windows): открыть `AtmosphereCalc.slnx`, нажать F5.
* **JetBrains Rider** (Windows / macOS / Linux): открыть `AtmosphereCalc.slnx`, нажать Run.
* **VS Code**: установить расширения *C# Dev Kit* и *Avalonia for VS Code*, открыть папку проекта и запустить через *Run and Debug*.

## 4. Тесты

Юнит-тесты лежат в `AtmosphereCalc.Tests/` (xUnit):

| Файл | Что проверяет |
|------|---------------|
| `Models/AtmosphereCalculatorTests.cs` | Значения на уровне моря, градиент температуры в тропосфере, изотермию стратосферы, давление по таблице ISA, непрерывность на 11 км, закон идеального газа, ошибку вне диапазона, профиль для графика |
| `ViewModels/MainViewModelTests.cs` | Начальные значения, ввод с точкой и запятой, сообщения об ошибках, синхронизацию поля и слайдера, уведомления `PropertyChanged` |
| `Converters/ConverterTests.cs` | Цвет карточки температуры и цвет текста ошибки |

Запуск всех тестов из корня репозитория:

```bash
dotnet test AtmosphereCalc.slnx
```

В IDE тесты видны в окне *Test Explorer* (Visual Studio), *Unit Tests* (Rider) или *Testing* (VS Code).

## 5. Используемые технологии

* [Avalonia UI 12](https://avaloniaui.net/) — кроссплатформенный UI-фреймворк.
* [xUnit](https://xunit.net/) — юнит-тесты.
* [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — `ObservableObject`, `[ObservableProperty]`.
