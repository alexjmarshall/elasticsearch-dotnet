using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using Nest;
using NuSearch.Domain;
using NuSearch.Domain.Data;
using NuSearch.Domain.Model;

namespace NuSearch.Indexer
{
	class Program
	{
		private static ElasticClient Client { get; set; }
		private static NugetDumpReader DumpReader { get; set; }
		private static string CurrentIndexName { get; set; }
		static void Main(string[] args)
		{
			Client = NuSearchConfiguration.GetClient();
			var directory = args.Length > 0 && !string.IsNullOrEmpty(args[0]) 
				? args[0] 
				: NuSearchConfiguration.PackagePath;
			DumpReader = new NugetDumpReader(directory);
			CurrentIndexName = NuSearchConfiguration.CreateIndexName();

			CreateIndex();
			IndexDumps();
			SwapAlias();

			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();
		}

		static void IndexDumps()
		{
			Console.WriteLine("Setting up a lazy xml files reader that yields packages...");
			var packages = DumpReader.GetPackages();

			Console.Write("Indexing documents into Elasticsearch...");
			var waitHandle = new CountdownEvent(1);

			var bulkAll = Client.BulkAll(packages, b => b
				.Index(CurrentIndexName)
				.BackOffRetries(2)
				.BackOffTime("30s")
				.MaxDegreeOfParallelism(4)
				.Size(1000)
				.RefreshOnCompleted(true)
			);

			ExceptionDispatchInfo captureInfo = null;

			bulkAll.Subscribe(new BulkAllObserver(
				onNext: b => Console.Write("."),
				onError: e =>
				{
					captureInfo = ExceptionDispatchInfo.Capture(e);
					waitHandle.Signal();
				},
				onCompleted: () => waitHandle.Signal()
			));

			waitHandle.Wait();

			if (!(captureInfo is null)) {
				Console.WriteLine(captureInfo.SourceException.Message);
			}

			Console.WriteLine("Done.");
		}
		static void CreateIndex()
		{
			Client.Indices.Create(CurrentIndexName, i => i
				.Settings(s => s
					.NumberOfShards(2)
					.NumberOfReplicas(0)
					.Setting("index.mapping.nested_objects.limit", 12000)
					.Analysis(analysis => analysis
						.Tokenizers(tokenizers => tokenizers // register nuget-id-tokenizer
							.Pattern("nuget-id-tokenizer", p => p.Pattern(@"\W+")) // splits text on any non-word character
						)
						.TokenFilters(tokenfilters => tokenfilters
							.WordDelimiter("nuget-id-words", w => w // register nuget-id-words filter
								.SplitOnCaseChange()
								.PreserveOriginal()
								.SplitOnNumerics()
								.GenerateNumberParts(false)
								.GenerateWordParts()
							)
						)
						.Analyzers(analyzers => analyzers
							.Custom("nuget-id-analyzer", c => c
								.Tokenizer("nuget-id-tokenizer")
								.Filters("nuget-id-words", "lowercase")
							)
							.Custom("nuget-id-keyword", c => c
								.Tokenizer("keyword") // emits the provided text as a single term
								.Filters("lowercase")
							)
						)
					)
				)
				.Map<Package>(map => map
					.AutoMap()
					.Properties(ps => ps
						.Text(s => s
							.Name(p => p.Id)
							.Analyzer("nuget-id-analyzer")
							.Fields(f => f
								.Text(p => p.Name("keyword").Analyzer("nuget-id-keyword"))
								.Keyword(p => p.Name("raw")) // id.raw field has not been analyzed
							)
						)
						.Nested<PackageVersion>(n => n
							.Name(p => p.Versions.First())
							.AutoMap()
							.Properties(pps => pps
								.Nested<PackageDependency>(nn => nn
									.Name(pv => pv.Dependencies.First())
									.AutoMap()
								)
							)
						)
						.Nested<PackageAuthor>(n => n
							.Name(p => p.Authors.First())
							.AutoMap()
							.Properties(props => props
								.Text(t => t
									.Name(a => a.Name)
									.Fielddata()
									.Fields(fs => fs
										.Keyword(ss => ss
											.Name("raw")
										)
									)
								)
							)
						)
					)
				)
			);
		}
		// applies nusearch alias to the current live index and removes it from other indices
		// applies nusearch-old alias to a maximum of two previously made indices, and deletes the rest
		private static void SwapAlias()
		{
			var indexExists = Client.Indices.Exists(NuSearchConfiguration.LiveIndexAlias).Exists;

			Client.Indices.BulkAlias(aliases =>
			{
				if (indexExists)
				{
					aliases.Add(a => a
						.Alias(NuSearchConfiguration.OldIndexAlias)
						.Index(Client.GetIndicesPointingToAlias(NuSearchConfiguration.LiveIndexAlias).First())
					);
				}

				return aliases
					.Remove(a => a.Alias(NuSearchConfiguration.LiveIndexAlias).Index("*"))
					.Add(a => a.Alias(NuSearchConfiguration.LiveIndexAlias).Index(CurrentIndexName));
			});

			var oldIndices = Client.GetIndicesPointingToAlias(NuSearchConfiguration.OldIndexAlias)
				.OrderByDescending(name => name)
				.Skip(2);

			foreach (var oldIndex in oldIndices)
			{
				Client.Indices.Delete(oldIndex);
			}
		}
	}
}
