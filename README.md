# Калькулятор параметров атмосферы (ISA Atmosphere Calculator)

Учебный командный проект на **C# и Avalonia UI** (.NET 10) для расчёта давления, плотности и температуры воздуха на разных высотах по модели Международной стандартной атмосферы (ISA).

Подробная документация проекта находится в папке [`docs/`](./docs/):
* [Описание работы программы (`docs/description.md`)](./docs/description.md)
* [ТЗ для разработчика Core (`docs/TZ-Core.md`)](./docs/TZ-Core.md)
* [ТЗ для UI-дизайнера (`docs/TZ-UI.md`)](./docs/TZ-UI.md)
* [Инструкция по работе с Git (`docs/Git-Guide.md`)](./docs/Git-Guide.md)

---

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
| **Model** | `AtmosphereCalc/Models/` | `AtmosphereCalculator` — расчёт по ISA, `AtmosphereData` — результат. Не зависит от UI. |
| **ViewModel** | `AtmosphereCalc/ViewModels/` | `MainViewModel` — ввод высоты, валидация, синхронизация поля и слайдера, форматирование значений. Не знает о контролах Avalonia. |
| **View** | `AtmosphereCalc/Views/` | `MainWindow.axaml` — разметка и привязки; `Controls/PressureChart` — отрисовка графика. Code-behind без бизнес-логики. |
| Конвертеры | `AtmosphereCalc/Converters/` | Преобразование данных ViewModel в кисти (цвет температуры, цвет ошибки). |

## 2. Структура проекта

```text
LABA-development/
├── AtmosphereCalc.slnx          # решение
├── global.json                  # версия .NET SDK
├── docs/                        # документация и ТЗ
└── AtmosphereCalc/
    ├── Models/                  # Model: физика атмосферы
    ├── ViewModels/              # ViewModel: состояние и логика UI
    ├── Views/                   # View: окна и контролы (.axaml)
    │   └── Controls/            # PressureChart — график давления
    ├── Converters/              # конвертеры для привязок
    ├── App.axaml(.cs)           # точка входа Avalonia
    └── Program.cs               # Main
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
dotnet run --project AtmosphereCalc
```

Только сборка:

```bash
dotnet build
```

### Из IDE

* **Visual Studio 2026** (Windows): открыть `AtmosphereCalc.slnx`, нажать F5.
* **JetBrains Rider** (Windows / macOS / Linux): открыть `AtmosphereCalc.slnx`, нажать Run.
* **VS Code**: установить расширения *C# Dev Kit* и *Avalonia for VS Code*, открыть папку проекта и запустить через *Run and Debug*.

## 4. Используемые технологии

* [Avalonia UI 12](https://avaloniaui.net/) — кроссплатформенный UI-фреймворк.
* [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — `ObservableObject`, `[ObservableProperty]`.
