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
			var numAuthors = form.Authors.Length;
			var result = _client.Search<Package>(s => s
				.From((form.Page - 1) * form.PageSize)
				.Size(form.PageSize)
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
									.Field(p => p.Id.Suffix("keyword"), 1000)
									.Field(p => p.Id, 1.5)
									.Field(p => p.Summary, 0.8)
								)
								.Operator(Operator.And)
								.Query(form.Query)
							)
						)
					) && +q.Nested(n => n // + here wraps query in a bool query filter clause, so as not to calculate a score for this query
						.Path(p => p.Authors)
						.Query(nq => +nq
							.TermsSet(c => c
								.Name("named_query")
								.Field(p => p.Authors.First().Name)
								.Terms(form.Authors)
								.MinimumShouldMatchScript(s => s
									.Source(numAuthors.ToString())
								)
							)
						)
					)
				)
				.Aggregations(a => a
					.Nested("authors", n => n
						.Path(p => p.Authors)
						.Aggregations(aa => aa
							.Terms("author-names", ts => ts
								.Field(p => p.Authors.First().Name)
							)
						)
					)
				)
				.Sort(sort =>
				{
					if (form.Sort == SearchSort.Downloads)
					{
						return sort.Descending(p => p.DownloadCount);
					}
					if (form.Sort == SearchSort.Recent)
					{
						return sort.Field(sortField => sortField
							.Nested(n => n
								.Path(p => p.Versions)
							)
							.Field(p => p.Versions.First().LastUpdated)
							.Descending()
						);
					}
					return sort.Descending(SortSpecialField.Score);
				})
			);

			var authors = result.Aggregations.Nested("authors")
                .Terms("author-names")
                .Buckets
                .ToDictionary(k => k.Key, v => v.DocCount);

			var model = new SearchViewModel()
			{
				Hits = result.Hits,
				Total = result.Total,
				Form = form,
				TotalPages = (int)Math.Ceiling(result.Total / (double)form.PageSize),
				Authors = authors
			};

	        return View(model);
		}
    }
}
