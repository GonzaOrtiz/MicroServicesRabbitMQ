1- RabbitMq en Docker

DESCARGAR RABBITMQ
docker run -d --hostname rabbit-server --name  rabbit-gonza-web -p 5672:5672 -p 156
72:15672 rabbitmq:3.9.12-management

rabbitmq-plugins list (listar plugins del rabitmq)
rabbitmq-plugins enable <pluginname> (habilitar un plugin )
ingresar a http://localhost:15672/
user: guest
password: guest
rabbitmqctl add_user gonza gonza
rabbitmqctl set_user_tags gonza administrator
rabbitmqctl set_permissions -p / gonza “.*” “.*” “.*” (para permisos globales)
rabbitmqctl delete_user guest
docker start rabbit-gonza-web
docker stoprabbit-gonza-web

(si da error checkar el siguiente link)
https://stackoverflow.com/questions/26811924/spring-amqp-rabbitmq-3-3-5-access-refused-login-was-refused-using-authentica/29177677#29177677



2 -Crear el proyecto con todas las referencias y librerias

3 - Crear la db en un contenedor docker (sino se puede crear y referencias una db de manera local)

//Iniciar la db en docker con las credenciales especificadas en el appSettings.json
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Gonza1994*" -p 1433:1433 --name "sqlgonza" -d mcr.microsoft.com/mssql/server
//luego nos conectamos desde sql server al servername: localhost, Authentication: SQL Server Authentication, (login y password del appsetings)
//creamos la db con el mismo nombre del appsettings

//Add-Migration "init" 
//(solo se usa un add-migration, este es un ej para apuntar a un context) Add-Migration -Context BankingDbContext
// Update-Database