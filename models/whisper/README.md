# Папка для моделей Whisper

Поместите сюда файлы моделей Whisper с расширением `.bin` или `.ggml`.

## Поддерживаемые форматы:
- `.bin` - формат ggml (рекомендуется)
- `.ggml` - оригинальный формат

## Рекомендуемые модели:

### small.bin (244MB) - ⭐ РЕКОМЕНДУЕТСЯ
Оптимальное соотношение скорости и качества
```
https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-small.bin
```

### base.bin (74MB) - Быстрая
Хорошая для слабых ПК
```
https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
```

### medium.bin (769MB) - Качественная
Для мощных ПК
```
https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-medium.bin
```

## После загрузки:
1. Поместите файлы в эту папку
2. Перезапустите MORT
3. Выберите модель в настройках STT

## Структура должна быть:
```
models/whisper/
├── small.bin
├── base.bin
└── medium.bin
```
