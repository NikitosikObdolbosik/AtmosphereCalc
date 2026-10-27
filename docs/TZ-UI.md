# Техническое задание: UI-дизайнер (Avalonia UI)

Твоя задача — отвечать за визуальное оформление окон, вёрстку `.axaml`, график и отзывчивость интерфейса.

## Твои папки

| Папка | Что там |
|---|---|
| `src/AtmosphereCalc.UI/Views/` | Главное окно `MainWindow.axaml` и график `Controls/ProfileChart` |
| `src/AtmosphereCalc.UI/Converters/` | Конвертеры для привязок (цвет температуры, цвет ошибки) |
| `src/AtmosphereCalc.UI/Services/` | Реализации сервисов на Avalonia (диалог сохранения файла) |
| `src/AtmosphereCalc.UI/App.axaml` | Тема и стили приложения |
| `tests/AtmosphereCalc.UI.Tests/` | Тесты конвертеров |

`App.axaml.cs` — точка сборки приложения (создаёт окно, сервисы и ViewModel), её ведёт тимлид.

## Граница с Core

Данные и команды для окна берутся из `MainViewModel` в проекте `AtmosphereCalc.Core`. В code-behind (`*.axaml.cs`) — только отрисовка, без расчётов и проверок ввода. Если для экрана нужно новое свойство ViewModel — попроси его у Core (Issue), а не считай в окне.

## Что нужно делать

1. Клонировать репозиторий и создать свою ветку от `main`:
   ```bash
   git checkout -b feature/ui-<коротко-о-задаче>
   ```
2. Проверить, что приложение запускается и выглядит как задумано:
   ```bash
   dotnet run --project src/AtmosphereCalc.UI
   ```
3. Перед PR прогнать тесты:
   ```bash
   dotnet test AtmosphereCalc.slnx
   ```
4. Открыть PR в `main` с описанием «Что сделано / Как проверено / Что не сделано» и скриншотом, попросить ревью у Core или тимлида.
