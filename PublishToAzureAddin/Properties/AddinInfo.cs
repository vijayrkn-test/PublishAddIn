using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly: Addin(
	"PublishToAzureAddin",
	Namespace = "PublishToAzureAddin",
	Version = "1.0"
)]

[assembly: AddinName("PublishToAzureAddin")]
[assembly: AddinCategory("IDE extensions")]
[assembly: AddinDescription("PublishToAzureAddin")]
[assembly: AddinAuthor("vijayramakrishnan")]