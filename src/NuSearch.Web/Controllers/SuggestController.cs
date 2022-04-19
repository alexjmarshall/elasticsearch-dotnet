using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nest;
using NuSearch.Domain.Model;
using NuSearch.Web.Models;

namespace NuSearch.Web.Controllers
{
    public class SuggestController : Controller
    {
        private readonly IElasticClient _client;

        public SuggestController(IElasticClient client) => _client = client;

        [HttpPost]
        public IActionResult Index([FromBody]SearchForm form)
        {
            // TODO: implement
        }
    }
}