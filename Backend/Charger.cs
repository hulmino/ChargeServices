using Microsoft.AspNetCore.Mvc;
using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Backend
{
    public class Charger : IIdEntity
    {
        public const double MIN_CURRENT = 10;
        public const double MAX_CURRENT = 32;

        public Charger(Guid id, string name, IMessageQueue messageQueue)
        {
            this.Id = id;
            this.Name = name ?? throw new ArgumentNullException(nameof(name));

            _messageQueue = messageQueue ?? throw new ArgumentNullException(nameof(messageQueue));
        }

        private readonly IMessageQueue _messageQueue;

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        [JsonIgnore]
        public Installation Installation { get; set; }

        public Guid? InstallationId
        {
            get
            {
                if(this.Installation == null) return null;
                return this.Installation.Id;
            }
        }

        public Vehicle ConnectedVehicle { get; set; }

        private double _allocatedCurrent;
        public double AllocatedCurrent
        {
            get { return _allocatedCurrent; }
            set
            {
                if(_allocatedCurrent == value)
                {
                    return;
                }

                if(value < 0)
                {
                    throw new InvalidOperationException($"Cannot allocate less than 0 ({value})");
                }
                else if(value > MAX_CURRENT)
                {
                    throw new InvalidOperationException($"Cannot allocate more than MaxCurrent ({value}/{MAX_CURRENT})");
                }
                if(value > 0 && value < MIN_CURRENT)
                {
                    throw new InvalidOperationException($"Cannot allocate less than MinCurrent ({value}/{MIN_CURRENT})");
                }

                _allocatedCurrent = value;

                this.ConnectedVehicle?.UpdateChargeCurrent();

                _messageQueue.NotifyChange(this, nameof(this.AllocatedCurrent));
            }
        }

        public void ConnectVehicle(IMessageQueue _messageQueue)
        {
            if(this.ConnectedVehicle != null)
            {
                throw new InvalidOperationException("Charger is already connected");
            }

            this.AllocatedCurrent = this.Installation.RequestCharge(this);

            var vehicle = new Vehicle(_messageQueue);

            this.ConnectedVehicle = vehicle;
            this.ConnectedVehicle.ConnectAndStartCharging(this);
        }

        public void DisconnectVehicle()
        {
            if(this.ConnectedVehicle == null)
            {
                throw new InvalidOperationException("Charger is already disconnected");
            }

            this.ConnectedVehicle.Disconnect();
            this.ConnectedVehicle = null;
            this.AllocatedCurrent = 0;
        }
    }
}