using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nest;
using NuSearch.Domain.Model;
using NuSearch.Web.Models;

namespace NuSearch.Web.Controllers
{
	public class SearchController : Controller
    {
		private readonly IElasticClient _client;

		public SearchController(IElasticClient client) => _client = client;

	    [HttpGet]
        public IActionResult Index(SearchForm form)
        {
			var result = _client.Search<Package>(s => s
				.Size(25)
				// NEST functions map one-to-one with Elasticsearch query JSON
				.Query(q => q
					.MultiMatch(m => m
						.Fields(f => f.Fields(p => p.Id, p => p.Summary))
						.Operator(Operator.And)
						.Query(form.Query)
					)
				)
			);
			var model = new SearchViewModel()
			{
				Hits = result.Hits,
				Total = result.Total,
				Form = form
			};

	        return View(model);
		}
    }
}
