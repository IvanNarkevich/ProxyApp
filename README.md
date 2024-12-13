<b>Установка и запуск:</b>
1) По пути C:\Users\\{имя пользователя} создать папку, если не существует, с именем ".aspnet", внутри которой создать папку, если не существует, с именем "https"
2) Из командной строки запустить следующие команды:
   - dotnet dev-certs https -ep %USERPROFILE%\\.aspnet\https\dockerсуке.pfx -p Password1!
   - dotnet dev-certs https --trust
3) После распаковки архива с файлами перейти в командной строке в папку, содержащую файл docker-compose.yml, и запустить команду 
    <br>docker-compose up --build
4) В клиентском приложении изменить client -> .env -> REACT_APP_BASE_URL на https://localhost:7122/
5) Перейти на https://localhost:7122/ для задания правил и просмотра статистики
