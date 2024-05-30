using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Backend
{
    public interface IChargeService
    {
        /// <summary>
        /// Called when a charger is connected.
        /// </summary>
        /// <returns>
        /// The initial charge current allocated to the charger.
        /// </returns>

        double RequestCharge(Charger charger);

        /// <summary>
        /// Called when a charger's allocated or charge current is changed.
        /// </summary>
        void OnChange(IIdEntity charger, string changedProperty);
    }
    
    public class ChargeService : IChargeService
    {
        private ICollection<Charger> _charges;

    public double RequestCharge(Charger charger) //for charger we are getting the minimum available current
        {
            if (charger.Installation == null || charger.Installation.Chargers == null)
            {
                return 0;
            }

            _charges = (ICollection<Charger>)charger.Installation.Chargers;

            var activeChargers = _charges.Where(c => c.ConnectedVehicle != null || c.Id == charger.Id).ToList();

            var numberOfChargers = activeChargers.Count();

            //calculate the total current already allocated
            double totalAllocatedCurrent = activeChargers.Sum(c => c.AllocatedCurrent);

            //calculate the available current
            double availableCurrent = charger.Installation.CircuitBreakerCurrent - totalAllocatedCurrent;

            //calculate the maximum possible current allocation per charger
            double currentPerCharger = availableCurrent / numberOfChargers;

            Console.WriteLine($"totalAllocatedCurrent {totalAllocatedCurrent}");
            Console.WriteLine($"availableCurrent {availableCurrent}");
            Console.WriteLine($"charger.Installation.CircuitBreakerCurrent {charger.Installation.CircuitBreakerCurrent}");
            Console.WriteLine($"currentPerCharger {currentPerCharger}");

            if (currentPerCharger >= Charger.MIN_CURRENT)
            {
                currentPerCharger = Math.Min(currentPerCharger, Charger.MAX_CURRENT);

                foreach (var activeCharger in activeChargers) //distribute equally current to all active chargers
                {
                    activeCharger.AllocatedCurrent = currentPerCharger;
                }

                return currentPerCharger;
            }

            return 0;
        }

        public void OnChange(IIdEntity entity, string changedProperty)
        {
            //var currentcharger = Globals.ChargerQueue
            //    .FirstOrDefault(c => c.Id == entity.Id);
            var currentCharger = _charges.Where(c => c.Id == entity.Id).FirstOrDefault();

            if (currentCharger == null)
                return;

            if (currentCharger.ConnectedVehicle.IsFinished)
            {
                currentCharger.AllocatedCurrent = 0;
                RedistributeAllocatedCurrent(currentCharger, 0);
            }

            if (currentCharger.ConnectedVehicle.ChargeCurrent < currentCharger.AllocatedCurrent)
            {
                var unused = currentCharger.AllocatedCurrent - currentCharger.ConnectedVehicle.ChargeCurrent;
                currentCharger.AllocatedCurrent = currentCharger.ConnectedVehicle.ChargeCurrent;
                RedistributeAllocatedCurrent(currentCharger, unused);
            }

            //if the allocated current is more than the MaxChangeCurrent, the extraCharge should be devided into the other active chargers
            //var availableCurrent = currentCharger.AllocatedCurrent - currentCharger.ConnectedVehicle.MaxChargeCurrent;
            //RedistributeAllocatedCurrent(currentCharger, availableCurrent);

        }

        private void RedistributeAllocatedCurrent(Charger entity, double unused)
        {
            var active = _charges
                .Where(c => c.ConnectedVehicle != null && c.ConnectedVehicle.Charging && c.Id != entity.Id).ToList();

            //active.ToList().Remove(currentcharger); //now we have only the other active chargers
            var toShare = unused / active.Count();
            foreach (var other in active)
            {
                if ((other.AllocatedCurrent + toShare) > other.Installation.CircuitBreakerCurrent)
                    other.AllocatedCurrent -= Math.Max(Charger.MIN_CURRENT, toShare);

                if ((other.AllocatedCurrent + toShare) <= other.Installation.CircuitBreakerCurrent)
                    other.AllocatedCurrent += toShare;

                //RequestCharge(other);
            }
        }
    }
}