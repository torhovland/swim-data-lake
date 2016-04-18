using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ServiceFabricWeb.Models
{
    public class ActorBoot
    {
        public ActorBoot()
        {
            NumberOfActors = 100;
            Message = "No actions yet.";
        }

        public int NumberOfActors { get; set; }
        public string Message { get; set; }
    }
}
