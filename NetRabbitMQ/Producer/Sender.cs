using RabbitMQ.Client;
using System.Text;

//instanciamos el connectionFactory
var factory = new ConnectionFactory
{
    HostName = "localhost",
    UserName = "gonza",
    Password = "gonza",
};
//Instanciamos una conexión de contexto 
//Instanciamos el canal de comunicacion dentro del conexión
using (var connection = factory.CreateConnection())
using (var channel = connection.CreateModel())
{
    //creamos el canal
    channel.QueueDeclare("GonzaQueue", false, false, false, null);
    //creamos el mensaje
    string message = "probando rabbitMQ en .NET";
    var body = Encoding.UTF8.GetBytes(message);
    //enviamos el mensaje
    channel.BasicPublish("", "GonzaQueue", null, body);
    Console.WriteLine($"Mensaje enviado: {message}");
}

Console.WriteLine($"Presiona enter para salir");
Console.ReadLine();

//https://stackoverflow.com/questions/26811924/spring-amqp-rabbitmq-3-3-5-access-refused-login-was-refused-using-authentica/29177677#29177677
