using BooterActor.Interfaces;
using Microsoft.ServiceFabric.Actors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MeterSimulatorActor.Interfaces;

namespace BooterActor
{
    /// <remarks>
    /// Each ActorID maps to an instance of this class.
    /// The IProjName  interface (in a separate DLL that client code can
    /// reference) defines the operations exposed by ProjName objects.
    /// </remarks>
    internal class BooterActor : StatefulActor<BooterActor.ActorState>, IBooterActor
    {
        /// <summary>
        /// This class contains each actor's replicated state.
        /// Each instance of this class is serialized and replicated every time an actor's state is saved.
        /// For more information, see http://aka.ms/servicefabricactorsstateserialization
        /// </summary>
        [DataContract]
        internal sealed class ActorState
        {
            [DataMember]
            public List<IMeterSimulatorActor> MeterSimulators { get; set; }
        }

        /// <summary>
        /// This method is called whenever an actor is activated.
        /// </summary>
        protected override Task OnActivateAsync()
        {
            if (this.State == null)
            {
                // This is the first time this actor has ever been activated.
                // Set the actor's initial state values.
                this.State = new ActorState { MeterSimulators = new List<IMeterSimulatorActor>() };
            }

            ActorEventSource.Current.ActorMessage(this, "Meter simulator state initialized with {0} meters.", this.State.MeterSimulators.Count);
            return Task.FromResult(true);
        }


        async Task IBooterActor.Boot(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var actorId = ActorId.NewId();
                var actor = ActorProxy.Create<IMeterSimulatorActor>(actorId, ApplicationName);
                State.MeterSimulators.Add(actor);
                await actor.StartMeterSimulator();
            }

            ActorEventSource.Current.ActorMessage(this, "Booted {0} meter simulators", count);
        }

        async Task IBooterActor.StopAll()
        {
            var count = State.MeterSimulators.Count;

            foreach (var actor in State.MeterSimulators)
                await actor.StopMeterSimulator();

            State.MeterSimulators = new List<IMeterSimulatorActor>();

            ActorEventSource.Current.ActorMessage(this, "Stopped {0} meter simulators.", count);
        }
    }
}
