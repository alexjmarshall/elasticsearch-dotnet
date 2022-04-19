# elasticsearch-dotnet

## Instructions
1) download this repo
2) download elasticsearch 7.17.2 ([link](https://www.elastic.co/downloads/past-releases/elasticsearch-7-17-2)) and install
3) download .NET Core SDK 3.1.418 LTS ([link](https://dotnet.microsoft.com/en-us/download/dotnet/3.1)) and install
4) download NuGet package dump ([link](https://nusearch.blob.core.windows.net/dump/nuget-data-jul-2017.zip)) and extract it inside this project's directory (afterwards there should be a `nuget-data` folder containing ~1000 xml files inside the folder `elasticsearch-dotnet` or `elasticsearch-dotnet-main`)
5) install and start Elasticsearch (e.g. by extracting the download to the folder `elasticsearch-7.17.2`, navigating to that folder in a terminal and entering `.\bin\elasticsearch.bat`)
6) in a new terminal, navigate to this project's `src` folder and enter `dotnet restore` and `dotnet build`
7) navigate to `src\NuSearch.Indexer` and enter `dotnet run` to index the data into Elasticsearch (and grab a :coffee:)
8) after the data has been indexed, navigate to `src\NuSearch.Web` and enter `dotnet run` to start the Web application at <http://localhost:8080>
9) search for packages using the input box and filter the results by selecting one or more package authors

Thanks!
