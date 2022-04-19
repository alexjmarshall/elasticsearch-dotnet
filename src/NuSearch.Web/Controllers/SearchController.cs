﻿using System;
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
					.FunctionScore(fs => fs
						.MaxBoost(50)
						.Functions(ff => ff
							.FieldValueFactor(fvf => fvf
								.Field(p => p.DownloadCount) // function score query allows the Download count itself to influence score
								.Factor(0.0001)
							)
						)
						.Query(query => query
							.MultiMatch(m => m
								.Fields(f => f
									.Field(p => p.Id.Suffix("keyword"), 1.5) // boost matches on Id keyword and Id terms over Summary terms
									.Field(p => p.Id, 1.5)
									.Field(p => p.Summary, 0.8)
								)
								.Operator(Operator.And)
								.Query(form.Query)
							)
						)
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
