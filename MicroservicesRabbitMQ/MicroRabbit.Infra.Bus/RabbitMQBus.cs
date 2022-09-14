using MediatR;
using MicroRabbit.Domain.Core.Bus;
using MicroRabbit.Domain.Core.Commands;
using MicroRabbit.Domain.Core.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace MicroRabbit.Infra.Bus
{
    public class RabbitMQBus : IEventBus
    {
        private readonly RabbitMQSettings _rabbitMQSettings;
        private readonly IMediator _mediator;
        private readonly Dictionary<string, List<Type>> _handlers;
        private readonly List<Type> _eventTypes;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public RabbitMQBus( IMediator mediator, IServiceScopeFactory serviceScopeFactory, IOptions<RabbitMQSettings> rabbitMQSettings)
        {
            _mediator = mediator;
            _serviceScopeFactory = serviceScopeFactory;
            _handlers = new Dictionary<string, List<Type>>();
            _eventTypes = new List<Type>();
            _rabbitMQSettings = rabbitMQSettings.Value;
        }
        //publisher
        public void Publish<T>(T @event) where T : Event
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMQSettings.Hostname,
                UserName = _rabbitMQSettings.Username,
                Password = _rabbitMQSettings.Password
            };
            // se abre la conexión
            using (var connection = factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                // el nombre del queue va a ser el que se obtiene del tipo del evento del parametro generico
                var eventName = @event.GetType().Name;
                //declaramos el canal
                channel.QueueDeclare(eventName, false, false, false, null);
                //mensaje del evento
                var message = JsonConvert.SerializeObject(@event);
                //parseamos a json
                var body = Encoding.UTF8.GetBytes(message);
                //publicamos
                channel.BasicPublish("", eventName, null, body);
            }
        }

        public Task SendCommand<T>(T command) where T : Command
        {
            return _mediator.Send(command);
        }

        public void Subscribe<T, TH>()
            where T : Event
            where TH : IEventHandler<T>
        {
            //obtenemos los valores genericos
            var eventName = typeof(T).Name;
            var handlerType = typeof(TH);
            //si el valor no se encuentra incluido en la lista eventtypes lo agregamos
            if (!_eventTypes.Contains(typeof(T))) _eventTypes.Add(typeof(T));
            //si el valor no se encuentra incluido en la lista handlers lo agregamos e inicializamos una lista
            if (!_handlers.ContainsKey(eventName)) _handlers.Add(eventName, new List<Type>());
            //si en handler ya contiene ese tipo event(key) namem, lanzamos una exception
            if (_handlers[eventName].Any(s => s.GetType() == handlerType))
                throw new ArgumentException(@$"El handler exception {handlerType.Name}, 
                                           ya fue registrado por {eventName}", nameof(handlerType));
            //Si pasa de largo la exception agregamos el handlerType a la lista
            _handlers[eventName].Add(handlerType);

            StartBasicConsume<T>();
        }

        private void StartBasicConsume<T>() where T : Event
        {
            var factory = new ConnectionFactory
            {
                HostName = _rabbitMQSettings.Hostname,
                UserName = _rabbitMQSettings.Username,
                Password = _rabbitMQSettings.Password,
                DispatchConsumersAsync = true
            };
            //abrimos la conexiòn
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();
            //nombre del queue a consumir (el que obtenemos del objeto t de tipo Event)
            var eventName = typeof(T).Name;
            //declaramos el canal con el nombre que recibimos de T
            channel.QueueDeclare(eventName, false, false, false, null);
            //consumimos los eventos
            var consumer = new AsyncEventingBasicConsumer(channel);
            //Cuando se recibe un evento, se va a leer en este momento
            consumer.Received += Consumer_Received;
            //notificamos y limpiamos el msj de la queue
            channel.BasicConsume(eventName, true, consumer);
        }
        //Consumir la data que llega
        private async Task Consumer_Received(object sender, BasicDeliverEventArgs e)
        {
            //obtener nombre del evento (queue o routing key)
            var eventName = e.RoutingKey;
            var message = Encoding.UTF8.GetString(e.Body.Span);
            try
            {
                await ProcessEvent(eventName, message).ConfigureAwait(false);
            }
            catch(Exception ex) { 
            
            }
        }
        //Procesar los eventos que llegan al consumer en el
        //momento en el que llega un mensaje al queue y posteriormente arrojamos un evento
        private async Task ProcessEvent(string eventName, string message)
        {
            if (_handlers.ContainsKey(eventName))
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var subscriptions = _handlers[eventName];
                    //recorremos los subscriptores
                    foreach (var subscription in subscriptions)
                    {
                        var handler = scope.ServiceProvider.GetService(subscription);  //Activator.CreateInstance(subscription);
                        if (handler == null) continue;
                        var eventType = _eventTypes.SingleOrDefault(t => t.Name == eventName);
                        var @event = JsonConvert.DeserializeObject(message, eventType);
                        //instanciamos el obj subscriptor del consumer
                        var concreteType = typeof(IEventHandler<>).MakeGenericType(eventType);
                        //Disparamos el evento
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new object[] { @event });

                    }
                }
            }
        }
    }
}
