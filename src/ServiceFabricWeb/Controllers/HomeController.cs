using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BooterActor.Interfaces;
using Microsoft.AspNet.Mvc;
using Microsoft.ServiceFabric.Actors;
using ServiceFabricWeb.Models;

namespace ServiceFabricWeb.Controllers
{
    public class HomeController : Controller
    {
        const string ApplicationName = "fabric:/ServiceFabricApplication";
        ActorBoot model = new ActorBoot();

        public IActionResult Index()
        {
            return View(model);
        }

        [HttpPost]
        public ActionResult Index(ActorBoot model, string button)
        {
            if (button == "boot")
            {
                Boot(model.NumberOfActors);
                model.Message = $"Booted {model.NumberOfActors} actors.";
            }
            else if (button == "stop")
            {
                Stop();
                model.Message = "Stopped actors.";
            }

            return View(model);
        }

        void Boot(int count)
        {
            var actorId = new ActorId("BooterActor");
            var actor = ActorProxy.Create<IBooterActor>(actorId, ApplicationName);
            actor.Boot(count);
        }

        void Stop()
        {
            var actorId = new ActorId("BooterActor");
            var actor = ActorProxy.Create<IBooterActor>(actorId, ApplicationName);
            actor.StopAll();
        }
    }
}
