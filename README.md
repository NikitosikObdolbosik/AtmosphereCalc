# Калькулятор параметров атмосферы (ISA Atmosphere Calculator)

[![CI](https://github.com/NikitosikObdolbosik/AtmosphereCalc/actions/workflows/ci.yml/badge.svg)](https://github.com/NikitosikObdolbosik/AtmosphereCalc/actions/workflows/ci.yml)

Учебный командный проект на **C# и Avalonia UI** (.NET 10) для расчёта давления, плотности и температуры воздуха на разных высотах по модели Международной стандартной атмосферы (ISA).

Документация лежит в папке [`docs/`](./docs/):

* [Описание работы программы](./docs/description.md)
* [ТЗ для разработчика Core](./docs/TZ-Core.md)
* [ТЗ для UI-дизайнера](./docs/TZ-UI.md)
* [Гайд по работе с Git](./docs/Git-Guide.md)


## Возможности

Программа реализует всё, что описано в [`description.md`](./docs/description.md):

* **Ввод трёх параметров:** целевая высота (0–25 000 м, полем или слайдером), шаг построения профиля (от 100 м до целевой высоты) и температура у земли (−50…+50 °C, по ISA +15 °C).
* **Проверка ввода на лету:** под полем появляется понятное сообщение, а кнопка «Рассчитать» блокируется, пока есть ошибки.
* **Расчёт по кнопке «Рассчитать».** Если параметры изменились после расчёта, программа напоминает, что результат устарел.
* **Два слоя атмосферы:** тропосфера (0–11 000 м, −6,5 °C на 1 км) и нижняя стратосфера (11 000–25 000 м, изотермический слой).
* **Карточки текущих значений:** температура в °C и K, давление в Па и гПа, плотность в кг/м³ и название слоя.
* **Цветовая индикация температуры:** карточка плавно меняет цвет от тёплого к холодному.
* **График профиля** давления, плотности или температуры от 0 м до целевой высоты: оси с делениями и единицами («h, м», «P, Па»…), сетка, маркер целевой точки и параметры расчёта в углу.
* **Сохранение графика в PNG** (двойное разрешение) кнопкой «Сохранить PNG».
* **Таблица профиля** с шагом построения.
* **Экспорт профиля в CSV:** разделитель — запятая, дробная часть — точка, файл в UTF-8 с BOM, поэтому кириллица корректно открывается в Excel. Первая строка хранит параметры расчёта, по ним расчёт можно повторить:

  ```text
  # app=AtmosphereCalc 1.0.0; model=ISA-2layer; target_altitude_m=10000; step_m=500; sea_level_temperature_c=15; points=21
  altitude_m,temperature_c,temperature_k,pressure_pa,pressure_hpa,density_kg_m3,layer
  0,15.00,288.15,101325.00,1013.25,1.2250,Тропосфера
  ```

  В pandas файл читается так: `pd.read_csv("atmosphere_profile.csv", comment="#")`.

## 1. Архитектура проекта (MVVM)

Проект построен по паттерну **Model — View — ViewModel** (Avalonia + CommunityToolkit.Mvvm) и разделён на два проекта по ролям:

```text
AtmosphereCalc.UI (View)  ──Binding──►  AtmosphereCalc.Core (ViewModel ──► Model)
   Avalonia, окно, график                 без Avalonia: физика, логика, проверки
```

`AtmosphereCalc.Core` не ссылается на Avalonia, поэтому независимость модели и ViewModel от интерфейса видна прямо по зависимостям проектов, а тесты Core запускаются без окна.

| Слой | Где лежит | Ответственность |
|------|-----------|-----------------|
| **Model** | `src/AtmosphereCalc.Core/Models/` | `AtmosphereCalculator` — расчёт по ISA, проверка параметров, построение профиля; `AtmosphereData` — результат; `ProfileCsvExporter` — экспорт в CSV |
| **ViewModel** | `src/AtmosphereCalc.Core/ViewModels/` | `MainViewModel` — поля ввода и их проверка, команды «Рассчитать», «Экспорт в CSV», «Сохранить PNG», данные для карточек, графика и таблицы; `AxisTicks` — деления осей |
| Интерфейсы сервисов | `src/AtmosphereCalc.Core/Services/` | `IFileSaveService` — сохранение файлов, `IChartSnapshotProvider` — снимок графика. ViewModel знает только интерфейсы |
| **View** | `src/AtmosphereCalc.UI/Views/` | `MainWindow.axaml` — разметка и привязки; `Controls/ProfileChart` — отрисовка графика. Code-behind без бизнес-логики |
| Конвертеры | `src/AtmosphereCalc.UI/Converters/` | Данные ViewModel → кисти (цвет температуры, цвет ошибки) |
| Реализации сервисов | `src/AtmosphereCalc.UI/Services/` | `FileSaveService` — системный диалог сохранения Avalonia |
| Точка сборки | `src/AtmosphereCalc.UI/App.axaml.cs` | Создаёт окно, сервисы и ViewModel и связывает их |

## 2. Структура репозитория и роли

```text
AtmosphereCalc/
├── AtmosphereCalc.slnx              # решение: 2 проекта src/ + 2 проекта tests/      — TL
├── Directory.Build.props            # общие настройки: .NET 10, nullable, версия     — TL
├── global.json                      # версия .NET SDK                                 — TL
├── .github/workflows/ci.yml         # CI: сборка и тесты на Linux, Windows, macOS     — TL
├── docs/                            # описание программы, ТЗ, гайд по Git             — TL
├── src/
│   ├── AtmosphereCalc.Core/         # библиотека без Avalonia                          — Core
│   │   ├── Models/                  # физика атмосферы, экспорт CSV
│   │   ├── ViewModels/              # состояние и логика интерфейса
│   │   └── Services/                # интерфейсы сервисов (контракт для UI)
│   └── AtmosphereCalc.UI/           # приложение Avalonia                              — UI
│       ├── Views/                   # окно и график (.axaml)
│       ├── Converters/              # конвертеры для привязок
│       ├── Services/                # реализации сервисов на Avalonia
│       ├── App.axaml, Program.cs    # тема и запуск (App.axaml.cs — точка сборки, TL)
│       └── app.manifest
└── tests/
    ├── AtmosphereCalc.Core.Tests/   # тесты модели и ViewModel                        — Core
    └── AtmosphereCalc.UI.Tests/     # тесты конвертеров                               — UI
```

| Роль | За что отвечает |
|------|-----------------|
| **Тимлид (TL)** | Решение, `Directory.Build.props`, `global.json`, CI, `docs/`, README, `App.axaml.cs`; ревью и мерж PR, защита `main` |
| **Core** | `src/AtmosphereCalc.Core/`, `tests/AtmosphereCalc.Core.Tests/`; верификация модели |
| **UI** | `src/AtmosphereCalc.UI/` (кроме `App.axaml.cs`), `tests/AtmosphereCalc.UI.Tests/`; скриншоты |

Связь Core и UI — публичные свойства и команды `MainViewModel`, к которым привязано окно. Их переименование или удаление ломает интерфейс, поэтому такие изменения идут через PR с ревью UI, а новое свойство Core мержит раньше, чем UI начнёт к нему привязываться.

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
dotnet run --project src/AtmosphereCalc.UI
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

Юнит-тесты (xUnit) разделены по ролям так же, как код:

| Файл | Что проверяет |
|------|---------------|
| `tests/AtmosphereCalc.Core.Tests/Models/AtmosphereCalculatorTests.cs` | Значения на уровне моря, градиент температуры, изотермию стратосферы, давление по таблице ISA, температуру у земли, слои, непрерывность на 11 км, проверку параметров (включая NaN и бесконечность), построение профиля с шагом |
| `tests/AtmosphereCalc.Core.Tests/Models/ProfileCsvExporterTests.cs` | Строка параметров, заголовок и строки CSV, формат чисел независимо от языка системы |
| `tests/AtmosphereCalc.Core.Tests/ViewModels/MainViewModelTests.cs` | Расчёт по кнопке, сообщения об ошибках и блокировку кнопки, слайдер, таблицу, подписи осей, экспорт CSV и PNG (с подменой сервиса сохранения и снимка графика) |
| `tests/AtmosphereCalc.Core.Tests/ViewModels/AxisTicksTests.cs` | Деления осей: «круглый» шаг, охват данных, подписи без «-0» |
| `tests/AtmosphereCalc.UI.Tests/Converters/ConverterTests.cs` | Цвет карточки температуры и цвет текста ошибки |

Запуск всех тестов из корня репозитория:

```bash
dotnet test AtmosphereCalc.slnx
```

В IDE тесты видны в окне *Test Explorer* (Visual Studio), *Unit Tests* (Rider) или *Testing* (VS Code).

## 5. Используемые технологии

* [Avalonia UI 12](https://avaloniaui.net/) — кроссплатформенный UI-фреймворк.
* [xUnit](https://xunit.net/) — юнит-тесты.
* [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) — `ObservableObject`, `[ObservableProperty]`.
