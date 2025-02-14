using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public static class DosBoxSearch
{
	static IEnumerable<int> GetProcesses()
	{
		foreach (var processId in Process.GetProcesses()
				.Where(x => GetProcessName(x).StartsWith("DOSBOX", StringComparison.InvariantCultureIgnoreCase))
				.Select(x => x.Id))
		{
			yield return processId;
		}
	}

	static string GetProcessName(Process process)
	{
		try
		{
			//could fail because not enough permissions (eg : admin process)
			return process.ProcessName;
		}
		catch
		{
			return string.Empty;
		}
	}

	static IEnumerable<ProcessMemory> GetProcessReaders()
	{
		foreach (var processId in GetProcesses())
		{
			var proc = new ProcessMemory(processId);
			proc.BaseAddress = proc.SearchFor16MRegion();
			yield return proc;
		}
	}

	public static bool TryGetMemoryReader(out ProcessMemory reader)
	{
		var processes = GetProcessReaders()
			.ToArray();

		var process = processes
			.FirstOrDefault(IsAITDProcess);

		foreach (var proc in processes.Where(x => x != process))
		{
			proc.Close();
		}

		reader = process;
		return process != null;
	}

	static bool IsAITDProcess(ProcessMemory reader)
	{
		var mcbData = new byte[16384];
		return reader.BaseAddress != -1 && reader.Read(mcbData, 0, mcbData.Length) > 0 && DosMCB.GetMCBs(mcbData)
			.Any(x => x.Name.StartsWith("AITD") || x.Name.StartsWith("INDARK") || x.Name.StartsWith("TIMEGATE") || x.Name.StartsWith("TATOU"));
	}

	public static bool TryGetExeEntryPoint(byte[] memory, out int entryPoint)
	{
		int psp = DosMCB.GetMCBs(memory)
			.Where(x => x.Size > 100 * 1024 && x.Size < 200 * 1024 && x.Owner != 0) //is AITD exe loaded yet?
			.Select(x => x.Owner)
			.FirstOrDefault();

		if (psp > 0)
		{
			entryPoint = psp + 0x100;
			return true;
		}

		entryPoint = -1;
		return false;
	}
}