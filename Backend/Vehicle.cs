using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Backend
{
    public class Vehicle : IIdEntity
    {
        public Vehicle(IMessageQueue messageQueue)
        {
            _messageQueue = messageQueue;

            _messageQueue.Subscribe((entity, prop) =>
            {
                if (entity is not Charger charger) return;
                if (prop == nameof(charger.AllocatedCurrent) && entity == this.Charger)
                {
                    this.ChargeCurrent = charger.AllocatedCurrent;
                }
            });
        }
        
        private readonly IMessageQueue _messageQueue;
        private CancellationTokenSource _cancellationTokenSource;

        [JsonIgnore]
        public Charger Charger { get; private set; }

        public void Disconnect()
        {
            this.ConnectTime = null;
            
            if(_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource = null;
            }

            this.ChargeCurrent = 0;
            this.Charger = null;
            Console.WriteLine("Vehicle is disconnected");
        }

        public void ConnectAndStartCharging(Charger charger)
        {
            if (_cancellationTokenSource != null)
            {
                throw new Exception("Vehicle is already charging");
            }
            
            this.Charger = charger;
            _cancellationTokenSource = new CancellationTokenSource();
            this.ConnectTime = DateTimeOffset.UtcNow;

            Task.Run(
                async () =>
                {
                    await this.ChargeLoop(_cancellationTokenSource);

                    _cancellationTokenSource = null;
                    Console.WriteLine("Vehicle charging is stopped");
                });
        }

        private async Task ChargeLoop(CancellationTokenSource cancellationTokenSource)
        {
            var stopWatch = new System.Diagnostics.Stopwatch();

            this.UpdateChargeCurrent();
            
            while (!this.IsFinished && !cancellationTokenSource.IsCancellationRequested)
            {
                this.UpdateChargeCurrent();
                Console.WriteLine($"Vehicle charging with {this.ChargeCurrent}A (total: {(double) this.BatteryChargeWh / 1000}kWh)");
                var hourSinceLast = stopWatch.Elapsed.TotalSeconds / 15;
                stopWatch.Restart();

                this.BatteryChargeWh = Math.Min(
                    this.BatteryCapacityWh,
                    this.BatteryChargeWh + (long) (this.ChargeCurrent * 230 * hourSinceLast)
                );

                await Task.Delay(TimeSpan.FromSeconds(1), _cancellationTokenSource.Token);
            }

            if (this.IsFinished)
                Console.WriteLine("Vehicle fully charged");
            else
                Console.WriteLine("Vehicle charging aborted");

            this.ChargeCurrent = 0;
        }

        public double UpdateChargeCurrent()
        {
            this.ChargeCurrent = this.ResolveChargeCurrent();
            return this.ChargeCurrent;
        }
        
        private double ResolveChargeCurrent()
        {
            if(this.Charger.Installation.CircuitBreakerTripped)
            {
                return 0;
            }

            if(this.BatteryChargeWh >= this.BatteryCapacityWh)
            {
                return 0;
            }
            
            return Math.Min(this.AvailableCurrent, this.MaxChargeCurrent);
        }

        /// <summary>
        /// The maximum charge current depends on the battery charge level.
        /// Use this property to check the current maximum.
        /// </summary>
        public double MaxChargeCurrent
        {
            get
            {
                var divisor = this.IsRampDown(this.BatteryChargeWh) ? 4 : 1;
                return Charger.MAX_CURRENT / divisor;
            }
        }

        /// <summary>
        /// When battery is nearing capacity maximum charge rate will drop.
        /// </summary>
        private bool IsRampDown(long chargeWh)
        {
            return chargeWh >= this.BatteryCapacityWh / 2;
        }

        public bool IsFinished => this.BatteryChargeWh >= this.BatteryCapacityWh;

        public Guid Id => this.Charger?.Id ?? Guid.Empty;

        public string Name => this.Charger?.Name ?? "<disconnected>";

        public DateTimeOffset? ConnectTime
        {
            get;
            private set;
        }

        public long BatteryCapacityWh { get { return 14720; } }

        public bool Charging { get { return this.ChargeCurrent > 0; } }

        private double _chargeCurrent = 0;

        public double ChargeCurrent
        {
            get
            {
                return _chargeCurrent;
            }

            private set
            {
                var changed = _chargeCurrent != value;
                
                _chargeCurrent = value;
                
                if (changed)
                {
                    _messageQueue.NotifyChange(this, nameof(this.ChargeCurrent));
                }
            }
        }

        public double AvailableCurrent
        {
            get { return this.Charger != null ? this.Charger.AllocatedCurrent : 0; }
        }

        public long BatteryChargeWh { get; set; }
    }
}