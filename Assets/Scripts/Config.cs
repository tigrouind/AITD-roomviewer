using System.IO;

public class Config
{
	public static readonly string BaseDirectory = "GAMEDATA";

	public static string GetPath(string folder)
	{
		return Path.Combine(BaseDirectory, folder);
	}

	public static string GetPath(string folder, params object[] args)
	{
		return GetPath(string.Format(folder, args));
	}
}