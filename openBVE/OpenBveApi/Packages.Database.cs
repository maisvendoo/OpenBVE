﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace OpenBveApi.Packages
{
	/* ----------------------------------------
	 * TODO: This part of the API is unstable.
	 *       Modifications can be made at will.
	 * ---------------------------------------- */

	/// <summary>Defines a package database</summary>
	[Serializable]
	[XmlType("openBVE")]
	public class PackageDatabase
	{
		/// <summary>The installed routes</summary>
		public List<Package> InstalledRoutes;

		/// <summary>The installed trains</summary>
		public List<Package> InstalledTrains;

		/// <summary>The installed other items</summary>
		public List<Package> InstalledOther;
	}

	/// <summary>Stores the current package database</summary>
	public static class Database
	{
		/// <summary>The current package database</summary>
		public static PackageDatabase currentDatabase;
		private static string currentDatabaseFolder;
		private static string currentDatabaseFile;
		/// <summary>Call this method to save the package list to disk.</summary>
		/// <remarks>Returns false if an error was encountered whilst saving the database.</remarks>
		public static bool SaveDatabase()
		{
			try
			{
				if (!Directory.Exists(currentDatabaseFolder))
				{
					Directory.CreateDirectory(currentDatabaseFolder);
				}
				if (File.Exists(currentDatabaseFile))
				{

					File.Delete(currentDatabaseFile);
				}
				using (StreamWriter sw = new StreamWriter(currentDatabaseFile))
				{
					XmlSerializer listWriter = new XmlSerializer(typeof(PackageDatabase));
					listWriter.Serialize(sw, currentDatabase);
				}
			}
			catch (Exception)
			{
				return false;
			}
			return true;
		}

		/// <summary>Loads a package database XML file as the current database</summary>
		/// <param name="Folder">The root database folder</param>
		/// <param name="File">The database file</param>
		/// <returns>Whether the loading succeded</returns>
		public static bool LoadDatabase(string Folder, string File)
		{
			currentDatabaseFolder = Folder;
			currentDatabaseFile = File;
			if (System.IO.File.Exists(currentDatabaseFile))
			{
				try
				{
					XmlSerializer listReader = new XmlSerializer(typeof(PackageDatabase));
					using (XmlReader reader = XmlReader.Create(currentDatabaseFile))
					{
						currentDatabase = (PackageDatabase) listReader.Deserialize(reader);
					}
				}
				catch
				{
					//Loading the DB failed, so just create a new one
					currentDatabase = null;
				}
			}
			if (currentDatabase == null)
			{
				currentDatabase = new PackageDatabase
				{
					InstalledRoutes = new List<Package>(),
					InstalledTrains = new List<Package>(),
					InstalledOther = new List<Package>()
				};
				return false;
			}
			return true;
		}




		/*
		 * DATABASE FUNCTIONS
		 * 
		 */

		/// <summary>Checks a list of dependancies or reccomendations to see if they are installed</summary>
		public static List<Package> checkDependsReccomends(List<Package> currentList)
		{
			foreach (Package currentDependancy in currentList)
			{
				switch (currentDependancy.PackageType)
				{
					case PackageType.Route:
						if (DependancyMet(currentDependancy, currentDatabase.InstalledRoutes))
						{
							currentList.Remove(currentDependancy);
						}
						break;
					case PackageType.Train:
						if (DependancyMet(currentDependancy, currentDatabase.InstalledTrains))
						{
							currentList.Remove(currentDependancy);
						}
						break;
					case PackageType.Other:
						if (DependancyMet(currentDependancy, currentDatabase.InstalledOther))
						{
							currentList.Remove(currentDependancy);
						}
						break;
				}
				if (currentList.Count == 0) break;
			}
			if (currentList.Count == 0)
			{
				//Return null if there are no unmet dependancies
				return null;
			}
			return currentList;
		}

		/// <summary>Checks whether a dependancy is met by a member of a list of packages</summary>
		/// <param name="dependancy">The dependancy</param>
		/// <param name="currentList">The package list to check</param>
		public static bool DependancyMet(Package dependancy, List<Package> currentList)
		{
			if (currentList == null)
			{
				return false;
			}
			foreach (Package Package in currentList)
			{
				//Check GUID
				if (Package.GUID == dependancy.GUID)
				{
					if ((dependancy.MinimumVersion == null || dependancy.MinimumVersion <= Package.PackageVersion) &&
						(dependancy.MaximumVersion == null || dependancy.MaximumVersion >= Package.PackageVersion))
					{
						//If the version is OK, remove
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>Checks whether uninstalling a package will break any dependancies</summary>
		/// <param name="packagesToRemove">The list of packages that will be removed</param>
		/// <returns>A list of potentially broken packages</returns>
		public static List<Package> CheckUninstallDependancies(List<Package> packagesToRemove)
		{
			List<Package> brokenPackages = new List<Package>();
			foreach (Package Route in currentDatabase.InstalledRoutes)
			{
				foreach (Package dependancy in Route.Dependancies)
				{
					foreach (Package packageToRemove in packagesToRemove)
					{
						if (packageToRemove.GUID == dependancy.GUID)
						{
							brokenPackages.Add(Route);
							break;
						}
					}
				}
			}
			foreach (Package Train in currentDatabase.InstalledTrains)
			{
				foreach (Package dependancy in Train.Dependancies)
				{
					foreach (Package packageToRemove in packagesToRemove)
					{
						if (packageToRemove.GUID == dependancy.GUID)
						{
							brokenPackages.Add(Train);
							break;
						}
					}
				}
			}
			foreach (Package Other in currentDatabase.InstalledRoutes)
			{
				foreach (Package dependancy in Other.Dependancies)
				{
					foreach (Package packageToRemove in packagesToRemove)
					{
						if (packageToRemove.GUID == dependancy.GUID)
						{
							brokenPackages.Add(Other);
							break;
						}
					}
				}
			}
			return brokenPackages;
		}

	}

	/// <summary>Contains the database functions</summary>
	public class DatabaseFunctions
	{
		/// <summary>This function takes a list of files, and returns the files with corrected relative paths for compression or extraction</summary>
		/// <param name="tempList">The file list</param>
		/// <returns>The file list with corrected relative paths</returns>
		public static List<PackageFile> FindFileLocations(List<PackageFile> tempList)
		{
			//Now determine whether this is part of a recognised folder structure
			for (int i = 0; i < tempList.Count; i++)
			{
				if (tempList[i].relativePath.StartsWith("\\Railway\\", StringComparison.OrdinalIgnoreCase))
				{
					//Extraction path is the root railway folder
					for (int j = 0; j < tempList.Count; j++)
					{
						tempList[j].relativePath = tempList[j].relativePath.Remove(0,8);
					}
					return tempList;
				}
				if (tempList[i].relativePath.StartsWith("\\Train\\", StringComparison.OrdinalIgnoreCase))
				{
					//Extraction path is the root train folder
					for (int j = 0; j < tempList.Count; j++)
					{
						tempList[j].relativePath = tempList[j].relativePath.Remove(0, 7);
					}
					return tempList;
				}
				if (tempList[i].relativePath.StartsWith("\\Route", StringComparison.OrdinalIgnoreCase))
				{
					//Needs to be extracted to the root railway folder
					return tempList;
				}
				if (tempList[i].relativePath.StartsWith("\\Object", StringComparison.OrdinalIgnoreCase))
				{
					//Needs to be extracted to the root railway folder
					return tempList;
				}
				if (tempList[i].relativePath.StartsWith("\\Sound", StringComparison.OrdinalIgnoreCase))
				{
					//Needs to be extracted to the root railway folder
					return tempList;
				}

			}

			for (int i = 0; i < tempList.Count; i++)
			{
				var TestCase = tempList[i].absolutePath.Replace(tempList[i].relativePath, "");
				if (TestCase.EndsWith("Railway", StringComparison.OrdinalIgnoreCase))
				{
					//Extraction path is the root folder
					return tempList;
				}
				if (TestCase.EndsWith("Train", StringComparison.OrdinalIgnoreCase))
				{
					//Extraction path is the root folder
					return tempList;
				}
				if (TestCase.EndsWith("Route", StringComparison.OrdinalIgnoreCase))
				{
					//Needs to be extracted to the root railway folder
					for (int j = 0; j < tempList.Count; j++)
					{
						tempList[j].relativePath = "\\Route" + tempList[j].relativePath;
					}
					return tempList;
				}
				if (TestCase.EndsWith("Object", StringComparison.OrdinalIgnoreCase))
				{
					//Needs to be extracted to the root railway folder
					for (int j = 0; j < tempList.Count; j++)
					{
						tempList[j].relativePath = "\\Object" + tempList[j].relativePath;
					}
					return tempList;
				}
				if (TestCase.EndsWith("Sound", StringComparison.OrdinalIgnoreCase))
				{
					//Needs to be extracted to the root railway folder
					for (int j = 0; j < tempList.Count; j++)
					{
						tempList[j].relativePath = "\\Sound" + tempList[j].relativePath;
					}
					return tempList;
				}

			}
			//So, this doesn't have any easily findable folders
			//We'll have to do this the hard way.
			//Remember that people can store stuff in odd places
			int SoundFiles = 0;
			int ImageFiles = 0;
			int ObjectFiles = 0;
			int RouteFiles = 0;
			int TrainFiles = 0;
			for (int i = 0; i < tempList.Count; i++)
			{
				if (tempList[i].relativePath.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
				{
					SoundFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
				{
					ImageFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
				{
					ImageFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".tiff", StringComparison.OrdinalIgnoreCase))
				{
					ImageFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".ace", StringComparison.OrdinalIgnoreCase))
				{
					ImageFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".b3d", StringComparison.OrdinalIgnoreCase))
				{
					ObjectFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
				{
					//Why on earth are CSV files both routes and objects??!!
					RouteFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".animated", StringComparison.OrdinalIgnoreCase))
				{
					ObjectFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".rw", StringComparison.OrdinalIgnoreCase))
				{
					RouteFiles++;
				}
				else if (tempList[i].relativePath.EndsWith("train.dat", StringComparison.OrdinalIgnoreCase) || tempList[i].relativePath.EndsWith("panel.cfg", StringComparison.OrdinalIgnoreCase)
				|| tempList[i].relativePath.EndsWith("panel2.cfg", StringComparison.OrdinalIgnoreCase) || tempList[i].relativePath.EndsWith("extensions.cfg", StringComparison.OrdinalIgnoreCase)
				|| tempList[i].relativePath.EndsWith("ats.cfg", StringComparison.OrdinalIgnoreCase) || tempList[i].relativePath.EndsWith("train.txt", StringComparison.OrdinalIgnoreCase))
				{
					TrainFiles++;
				}
				else if (tempList[i].relativePath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
				{
					//Not sure about this one
					//TXT files are commonly used for includes though
					RouteFiles++;
				}
				
				
			}
			//We've counted the number of files found:
			if (SoundFiles != 0 && ObjectFiles == 0 && ImageFiles == 0)
			{
				//This would appear to be a subfolder of the SOUND folder
				for (int j = 0; j < tempList.Count; j++)
				{
					tempList[j].relativePath = "\\Sound" + tempList[j].relativePath;
				}
				return tempList;
			}
			if (RouteFiles != 0 && ImageFiles < 20 && ObjectFiles == 0)
			{
				//If this is a ROUTE subfolder, we should not find any b3d objects, and
				//there should be less than 20 images
				for (int j = 0; j < tempList.Count; j++)
				{
					tempList[j].relativePath = "\\Route" + tempList[j].relativePath;
				}
				return tempList;
			}
			if ((ObjectFiles != 0 || RouteFiles != 0) && ImageFiles > 20 && (TrainFiles < 2 || ImageFiles > 200))
			{
				//We have csv or b3d files and more than 20 images
				//this means it's almost certainly an OBJECT subfolder

				//HACK:
				//If more than 200 images but also train stuff, assume it's a route object folder containing a misplaced DLL or something
				//NWM.....
				for (int j = 0; j < tempList.Count; j++)
				{
					tempList[j].relativePath = "\\Object" + tempList[j].relativePath;
				}
				return tempList;
			}
			if (TrainFiles > 2 && ImageFiles > 2 && SoundFiles > 2)
			{
				for (int j = 0; j < tempList.Count; j++)
				{
					tempList[j].relativePath = "\\Train" + tempList[j].relativePath;
				}
			}
			//Can't decide, so just return the base list......
			return tempList;
		}

		/// <summary>This function cleans empty subdirectories after the uninstallation of a package</summary>
		/// <param name="currentDirectory">The directory to clean (Normally the root package install directory)</param>
		/// <param name="Result">The results output string</param>
		public static void cleanDirectory(string currentDirectory, ref string Result)
		{
			if (!Directory.Exists(currentDirectory))
			{
				return;
			}
			foreach (var directory in Directory.EnumerateDirectories(currentDirectory))
			{
				cleanDirectory(directory, ref Result);		
			}
			IEnumerable<string> entries = Directory.EnumerateFileSystemEntries(currentDirectory);
			if (!entries.Any())
			{
				Directory.Delete(currentDirectory, false);
				Result += currentDirectory + " deleted successfully. \r\n";
			}
			else
			{
				if (entries.Count() == 1 && File.Exists(currentDirectory + "thumbs.db"))
				{
					//thumbs.db files are auto-generated by Windows in any folder with pictures....
					File.Delete(currentDirectory + "thumbs.db");
					Directory.Delete(currentDirectory, false);
					Result += currentDirectory + " deleted successfully. \r\n";
				}
			}
		}
	}

}
