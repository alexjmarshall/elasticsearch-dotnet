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

		static void Main(string[] args)
		{
			Client = NuSearchConfiguration.GetClient();
			var directory = args.Length > 0 && !string.IsNullOrEmpty(args[0]) 
				? args[0] 
				: NuSearchConfiguration.PackagePath;
			DumpReader = new NugetDumpReader(directory);

			IndexDumps();

			Console.WriteLine("Press any key to exit.");
			Console.ReadKey();
		}

		static void IndexDumps()
		{
			var packages = DumpReader.GetPackages().Take(100);
			
			Console.Write("Indexing documents into Elasticsearch...");

			var result = Client.IndexMany(packages);

			if (!result.IsValid)
			{
				foreach (var item in result.ItemsWithErrors)
				{
					Console.WriteLine("Failed to index document {0}: {1}", item.Id, item.Error);
				}
			}

			Console.WriteLine("Done.");
		}

		static void CreateIndex()
		{
			Client.Indices.Create("nusearch", i => i
				.Settings(s => s
					.NumberOfShards(2)
					.NumberOfReplicas(0)
					.Setting("index.mapping.nested_objects.limit", 12000)
				)
				.Map<Package>(map => map
					.AutoMap()
					.Properties(ps => ps
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
								)
							)
						)
					)
				)
			);
		}
	}
}
