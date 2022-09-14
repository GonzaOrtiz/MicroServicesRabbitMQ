using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
    //instanciamos el consumer del canal(lee la data de la queue)
    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += (model, ea) =>
    {
        var body = ea.Body.Span;
        var message =Encoding.UTF8.GetString(body);
        Console.WriteLine($"Mensaje recibido {message}");
    };
    //Luego de rebir el msj lo eliminamos de la queue
    //es importante importante la segunda prop booleana del channel.BasicConsume("GonzaQueue", true) en true => indica que se ha leido el msj)
    channel.BasicConsume("GonzaQueue", true, consumer);
    Console.WriteLine($"Presione enter para salir del consumer");
    Console.ReadLine();
}
