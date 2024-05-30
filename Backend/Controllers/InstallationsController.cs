using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InstallationsController : ControllerBase
    {
        public InstallationsController(IInstallationRepository installationRepository)
        {
            _installationRepository = installationRepository;
        }

        private readonly IInstallationRepository _installationRepository;
        
        [HttpGet]
        public IEnumerable<Installation> Get()
        {
            return _installationRepository.FindAll();
        }
    }
}
