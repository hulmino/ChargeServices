using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChargersController : ControllerBase
    {
        public ChargersController(IInstallationRepository installationRepository, IMessageQueue messageQueue)
        {
            _installationRepository = installationRepository ?? throw new ArgumentNullException(nameof(installationRepository));
            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        private readonly IInstallationRepository _installationRepository;
        private readonly IMessageQueue _messageQueue;
        
        private Installation GetInstallation(Guid installationId)
        {
            return _installationRepository
                .FindAll()
                .Single(i => i.Id == installationId);
        }

        private Charger GetCharger(Guid chargerId)
        {
            return _installationRepository
                .FindAll()
                .SelectMany(i => i.Chargers)
                .Single(c => c.Id == chargerId);
        }
        //
        [HttpGet("{installationId}")]
        public IEnumerable<Charger> Get(Guid installationId)
        {
            var installation = this.GetInstallation(installationId);

            return installation != null
                ? installation.Chargers
                : null;
        }

        [HttpPost("{id}/connect")]
        public Charger Connect(Guid id)
        {
            var charger = this.GetCharger(id);
            charger.ConnectVehicle(_messageQueue);
            return charger;
        }

        [HttpPost("{id}/disconnect")]
        public Charger Disconnect(Guid id)
        {
            var charger = this.GetCharger(id);
            charger.DisconnectVehicle();

            //checking if any charger in the queue is getting disconnected
            if (Globals.ChargerQueue.Contains(charger)) 
            {
                Globals.ChargerQueue.Remove(charger);
            }

            return charger;
        }
    }
}
