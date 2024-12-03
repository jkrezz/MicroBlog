Для добавления картинок в хранилище minio необходимо его запустить. Это можно сделать двумя способами:
1 способ:
1) Скачать с официального сайта https://min.io/docs/minio/windows/index.html под windows (или версию для linux). 
2) Файл с расширением .exe поместить в любую директорию.
3) Открыть терминал в данной папке и запустить хранилище с помощью команды 
.\minio.exe server C:\minio --console-address :9001.

2 способ (развернуть с помощью Dockerfile):
1) В терминале, находясь в директории проекта, собрать сервис:  docker build -t blog-service .
2) docker compose up -d

Данные для входа в minio:
"minioadmin"
"minioadmin"