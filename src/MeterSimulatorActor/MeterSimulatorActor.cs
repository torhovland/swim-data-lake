using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MeterSimulatorActor.Interfaces;
using MeterSimulatorPortable;
using Microsoft.ServiceBus.Messaging;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;

namespace MeterSimulatorActor
{
    /// <remarks>
    ///     Each ActorID maps to an instance of this class.
    ///     The IProjName  interface (in a separate DLL that client code can
    ///     reference) defines the operations exposed by ProjName objects.
    /// </remarks>
    internal class MeterSimulatorActor : StatefulActor<MeterSimulatorActor.ActorState>, IMeterSimulatorActor,
        IRemindable
    {
        private const double MinLat = 50.65;
        private const double MaxLat = 54.36;
        private const double MinLon = 14.75;
        private const double MaxLon = 23.32;
        private const string EventHubName = "tor-eventhub";

        private const string ConnectionString =
            "Endpoint=sb://tor-ns.servicebus.windows.net/;SharedAccessKeyName=SendRule;SharedAccessKey=secret9S/7bkOQhEVTO7OfHDB+NO99k+0BVDLM=";

        EventHubClient eventHubClient;

        /// <summary>
        ///     This method is called whenever an actor is activated.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            eventHubClient = EventHubClient.CreateFromConnectionString(ConnectionString, EventHubName);

            if (State == null)
            {
                // This is the first time this actor has ever been activated.
                // Set the actor's initial state values.

                var crypto = new RNGCryptoServiceProvider();
                var seed = new byte[8];
                crypto.GetBytes(seed);
                var random = new Random(BitConverter.ToInt32(seed, 0));

                State = new ActorState
                {
                    RandomSeed = random.Next(),
                    MeterId = Guid.NewGuid(),
                    ConsumptionFactor = Between(random, 2000, 10000),
                    Lat = Between(random, MinLat, MaxLat),
                    Lon = Between(random, MinLon, MaxLon),
                    MeterReading = .0
                };
            }

            return Task.FromResult(true);
        }

        public async Task StartMeterSimulator()
        {
            await RegisterReminderAsync(
                "meter reading " + State.MeterId, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(1),
                ActorReminderAttributes.None);
        }

        public async Task StopMeterSimulator()
        {
            var reminder = GetReminder("meter reading " + State.MeterId);
            await UnregisterReminderAsync(reminder);
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] context, TimeSpan dueTime, TimeSpan period)
        {
            var now = DateTime.UtcNow;
            var simulator = new MeterSimulator(State.RandomSeed);
            var consumption = simulator.SimulatedConsumption(now, State.ConsumptionFactor);
            State.MeterReading += consumption;

            await Task.Delay(1000 - DateTime.Now.Millisecond);

            dynamic message = new
            {
                MeterId = State.MeterId,
                Lat = State.Lat,
                Lon = State.Lon,
                Time = now,
                Reading = State.MeterReading,
                ConsumptionFactor = State.ConsumptionFactor
            };

            using (var writer = new StringWriter())
            {
                var json = new JsonSerializer();
                json.Serialize(writer, message);
                var jsonString = writer.ToString();

                try
                {
                    await eventHubClient.SendAsync(new EventData(Encoding.UTF8.GetBytes(jsonString)));
                }
                catch (Exception ex)
                {
                    ActorEventSource.Current.ActorMessage(this, "ERROR: {0}", ex.ToString());
                }

                // Log what the service is doing
                ActorEventSource.Current.ActorMessage(this, "Working-{0}", jsonString);
            }
        }

        private double Between(Random random, double min, double max)
        {
            return min + random.NextDouble()*(max - min);
        }

        /// <summary>
        ///     This class contains each actor's replicated state.
        ///     Each instance of this class is serialized and replicated every time an actor's state is saved.
        ///     For more information, see http://aka.ms/servicefabricactorsstateserialization
        /// </summary>
        [DataContract]
        internal sealed class ActorState
        {
            [DataMember]
            public int RandomSeed { get; set; }

            [DataMember]
            public Guid MeterId { get; set; }

            [DataMember]
            public double MeterReading { get; set; }

            [DataMember]
            public double ConsumptionFactor { get; set; }

            [DataMember]
            public double Lat { get; set; }

            [DataMember]
            public double Lon { get; set; }
        }
    }
}