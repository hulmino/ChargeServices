using System;
using System.Collections.Generic;

namespace Backend
{
    public interface IInstallationRepository
    {
        IEnumerable<Installation> FindAll();
    }

    public class InstallationRepository : IInstallationRepository
    {
        /// <summary>
        /// Installations are hard coded since this is a simplified application without a proper database backend.
        /// NOTE: The repository should be configured as a singleton.
        /// </summary>
        public InstallationRepository(IMessageQueue messageQueue, IChargeService chargeService)
        {
            var installations = new[]
            {
                new Installation(
                    Guid.Parse("715bf20a-f68f-41d1-8294-f3246705bcd0"),
                    "Installation 1",
                    32,
                    new[]
                    {
                        new Charger(Guid.Parse("f1a2d0ef-490f-4903-ab83-69d5c5a47d73"), "Charger 1", messageQueue),
                        new Charger(Guid.Parse("04c820c7-a583-49af-8a18-a187959eb0dd"), "Charger 2", messageQueue),
                        new Charger(Guid.Parse("77c45095-b0dc-45d7-b51b-430e79d7fbf2"), "Charger 3", messageQueue),
                        new Charger(Guid.Parse("72a8a53d-b275-4b2b-bdc5-80a478d06606"), "Charger 4", messageQueue),
                        new Charger(Guid.Parse("dc8293fd-3538-4ae0-a665-460dddf656f9"), "Charger 5", messageQueue)
                    },
                    chargeService,
                    messageQueue
                )
            };

            foreach (var installation in installations)
            {
                foreach (var charger in installation.Chargers)
                {
                    charger.Installation = installation;
                }
            }

            _installations = installations;
        }
        
        private readonly IEnumerable<Installation> _installations;

        public IEnumerable<Installation> FindAll()
        {
            return _installations;
        }
    }
}