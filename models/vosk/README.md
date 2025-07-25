# Папка для моделей Vosk

Поместите сюда распакованные папки с моделями Vosk.

## Рекомендуемые модели:

### Русский язык

#### vosk-model-ru-0.42 (1.5GB) - ⭐ РЕКОМЕНДУЕТСЯ
Лучшее качество для русского языка
```
https://alphacephei.com/vosk/models/vosk-model-ru-0.42.zip
```

#### vosk-model-small-ru-0.22 (45MB) - Компактная
Для слабых ПК или быстрой работы
```
https://alphacephei.com/vosk/models/vosk-model-small-ru-0.22.zip
```

### Английский язык

#### vosk-model-en-us-0.22 (1.8GB)
Лучшее качество для английского языка
```
https://alphacephei.com/vosk/models/vosk-model-en-us-0.22.zip
```

#### vosk-model-small-en-us-0.15 (40MB)
Компактная английская модель
```
https://alphacephei.com/vosk/models/vosk-model-small-en-us-0.15.zip
```

## Установка:
1. Скачайте ZIP архивы моделей
2. Распакуйте каждый архив в отдельную папку
3. Поместите папки в models/vosk/
4. Перезапустите MORT

## Правильная структура:
```
models/vosk/
├── vosk-model-ru-0.42/
│   ├── am/
│   ├── conf/
│   ├── graph/
│   ├── ivector/
│   └── README
├── vosk-model-small-ru-0.22/
│   ├── am/
│   ├── conf/
│   └── graph/
└── vosk-model-en-us-0.22/
    ├── am/
    ├── conf/
    ├── graph/
    ├── ivector/
    └── README
```

## Примечания:
- Каждая модель должна быть в отдельной папке
- Внутри папки модели должны быть файлы: am/, conf/, graph/
- Большие модели дают лучшее качество, но требуют больше RAM
- Можно установить несколько моделей для разных языков
