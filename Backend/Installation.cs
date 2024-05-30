using System;
using System.Collections.Generic;
using System.Linq;
using Backend.Controllers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Backend
{
    public class Installation
    {
        public Installation(Guid id, string name, double circuitBreakerCurrent, IEnumerable<Charger> chargers, IChargeService chargeService, IMessageQueue messageQueue)
        {
            this.Id = id;
            this.Name = name;
            this.CircuitBreakerCurrent = circuitBreakerCurrent;
            this.Chargers = chargers ?? throw new ArgumentNullException(nameof(chargers));
            
            _service = chargeService ?? throw new ArgumentNullException(nameof(chargeService));
            
            messageQueue.Subscribe(this.OnChange);
        }

        private readonly IChargeService _service;

        public IEnumerable<Charger> Chargers { get; private set; }

        public Guid Id { get; private set; }
        public string Name { get; private set; }

        public double CircuitBreakerCurrent { get; private set; }

        public bool CircuitBreakerTripped { get; private set; }

        public Installation Initialize()
        {
            if (this.Chargers != null)
            {
                foreach (var charger in this.Chargers)
                {
                    charger.Installation = this;
                }
            }

            return this;
        }

        /// <summary>
        /// This method will be called when a vehicle is requesting charge.
        /// You must evaluate other charger is the installation, and determine
        /// how much current is available for the charger.
        /// </summary>
        public double RequestCharge(Charger charger)
        {
            return _service.RequestCharge(charger);
        }

        private void OnChange(IIdEntity entity, string changedProperty)
        {
            this.AssertCircuitBreaker();
            _service.OnChange(entity, changedProperty);
        }

        private void AssertCircuitBreaker()
        {
            var chargeCurrents = this.Chargers
                .Where(_ => _.ConnectedVehicle != null)
                .Sum(_ => _.ConnectedVehicle.ChargeCurrent);

            if (chargeCurrents > this.CircuitBreakerCurrent)
            {
                Console.WriteLine("##########################################");
                Console.WriteLine($"##### CIRCUIT BREAKER TRIPPED!");
                Console.WriteLine("##########################################");

                this.CircuitBreakerTripped = true;
                this.CircuitBreakerCurrent = 0;
            }
        }
    }
}