using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Gtk;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;

namespace PublishToAzure
{
	class PublishToAzureHandler : CommandHandler
	{
		protected override async void Run()
		{
			string publishSettingsContent = GetPublishSettingsFileContent();

			if (!String.IsNullOrEmpty(publishSettingsContent))
			{

				//TODO: Create a new publish settings file from MSDeployProfile for the imported settings.
				//var currentProject = IdeApp.ProjectOperations.CurrentSelectedProject;
				//var filesInProject = currentProject.Files;
				//var pubxmlFile = filesInProject.FirstOrDefault(pubxml => pubxml.FilePath.FileNameWithoutExtension.EndsWith("MSDeployProfile", StringComparison.OrdinalIgnoreCase));
				//if (!File.Exists(pubxmlFile.FilePath))
				//{
				//	MessageService.ShowError("MSDeploy template pubxml file not found!");
				//	return;
				//}

				// Read the settings from publishSettings File.
				XmlDocument publishSettingsDoc = new XmlDocument();
				publishSettingsDoc.PreserveWhitespace = true;
				try
				{
					publishSettingsDoc.LoadXml(publishSettingsContent);
				}
				catch(Exception e)
				{
					MessageService.ShowError(e.Message);
				}

				if (publishSettingsDoc != null)
				{
					var publishProfileNode = publishSettingsDoc.SelectSingleNode("//publishData/publishProfile");
					if (publishProfileNode == null)
					{
						MessageService.ShowError("Publishing failed. Invalid publish settings file");
						return;
					}

					// Get the publish values from publish settings file.
					var siteName = publishProfileNode.Attributes.GetNamedItem("msdeploySite")?.InnerText;
					var userId = publishProfileNode.Attributes.GetNamedItem("userName")?.InnerText;
					var password = publishProfileNode.Attributes.GetNamedItem("userPWD")?.InnerText;
					var destinationAppUrl = publishProfileNode.Attributes.GetNamedItem("destinationAppUrl")?.InnerText;

					// Trigger XBuild
					MonoDevelop.Projects.CustomCommand buildAndPublishCommand = new MonoDevelop.Projects.CustomCommand();
					var deployTemplate = "xbuild {0} /p:deployOnBuild=true /p:DeployIISPath={1} /p:UserName={2} /p:Password={3} /p:publishProfile=\"MSDeployProfile\" /p:Configuration=Release";
					buildAndPublishCommand.Command = String.Format(deployTemplate, IdeApp.ProjectOperations.CurrentSelectedProject.FileName.FullPath,
																	siteName,
																	userId,
																	password);
					
					buildAndPublishCommand.Name = "Build and Publish";
					buildAndPublishCommand.Type = MonoDevelop.Projects.CustomCommandType.Custom;
					buildAndPublishCommand.WorkingDir = IdeApp.ProjectOperations.CurrentSelectedProject.BaseDirectory.FullPath;
					bool buildStatus = await buildAndPublishCommand.Execute(IdeApp.Workbench.ProgressMonitors.GetBuildProgressMonitor(),
																		    IdeApp.Workspace,
																		    IdeApp.Workspace.ActiveConfiguration);

					if (buildStatus)
					{
						IdeApp.Workbench.StatusBar.ShowMessage("ASP.Net Core App Published Successfully.");
						DesktopService.ShowUrl(destinationAppUrl);
					}
					else
					{
						IdeApp.Workbench.StatusBar.ShowMessage("Publish Operation Failed!.");
					}
				}
			}
		}


		protected override void Update(CommandInfo info)
		{
			info.Enabled = IdeApp.ProjectOperations.CurrentSelectedProject != null;
		}

		private string GetPublishSettingsFileContent()
		{
			string publishSettingsContent = null;

			var fileChooser = new Gtk.FileChooserDialog("Choose a PublishSettings file",
														IdeApp.Workbench.RootWindow,
														FileChooserAction.Open,
			                                            "Cancel", ResponseType.Cancel,
			                                            "Download Publish Settings file", ResponseType.Help,
														"Publish to Azure", ResponseType.Accept);

			FileFilter filter = new FileFilter();
			filter.AddPattern("*.PublishSettings");
			filter.Name = "Publish Settings file";
			fileChooser.AddFilter(filter);

			try
			{
				int result = 0;
				do
				{
					result = fileChooser.Run();
					switch (result)
					{
						case (int)ResponseType.Accept:
							if (File.Exists(fileChooser.Filename))
							{
								publishSettingsContent = File.ReadAllText(fileChooser.Filename);
							}
							break;

						case (int)ResponseType.Help:
							DesktopService.ShowUrl("http://portal.azure.com");
							break;

						default:
							break;
					}
				}
				while (result == (int)ResponseType.Help);
			}
			finally
			{
				fileChooser.Destroy();
			}

			return publishSettingsContent;
		}
	}
}