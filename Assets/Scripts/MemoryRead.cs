using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Linq;

public static class MemoryRead
{
	const int PROCESS_QUERY_INFORMATION = 0x0400;
	const int PROCESS_WM_READ = 0x0010;
	const int MEM_COMMIT = 0x00001000;
	const int MEM_PRIVATE = 0x20000;
	const int PAGE_READWRITE = 0x04;

	[StructLayout(LayoutKind.Sequential)]
	private struct MEMORY_BASIC_INFORMATION
	{
		public IntPtr BaseAddress;
		public IntPtr AllocationBase;
		public uint AllocationProtect;
		public IntPtr RegionSize;
		public int State;
		public int Protect;
		public int Type;
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32.dll")]
	private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

	[DllImport("kernel32.dll")]
	private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

	public static bool GetAddress(string processName, byte[] arrayToFind, out string errorMessage, out Func<byte[], int, int, bool> readMemory)
	{
		readMemory = null;
		errorMessage = string.Empty;

		Process process = Process.GetProcesses()
            .FirstOrDefault(x =>
			{
				string name;
				try
				{
					name = x.ProcessName; 
				}
				catch
				{ 
					name = string.Empty;
				} 
				return name.StartsWith(processName, StringComparison.InvariantCultureIgnoreCase);
			});

		if (process == null)
		{
			errorMessage = "Cannot find DOSBOX process";
			return false;
		}

		IntPtr processHandle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, process.Id);            
		MEMORY_BASIC_INFORMATION mem_info = new MEMORY_BASIC_INFORMATION();

		long address = -1;    
		long min_address = 0;
		long max_address = 0x7FFFFFFF;
		byte[] buffer = new byte[65536];

		//scan process memory regions
		while (min_address < max_address
		             && VirtualQueryEx(processHandle, (IntPtr)min_address, out mem_info, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) > 0)
		{           
			//check if memory region is accessible
			if (mem_info.Protect == PAGE_READWRITE && mem_info.State == MEM_COMMIT && (mem_info.Type & MEM_PRIVATE) == MEM_PRIVATE)
			{                                              
				long readPosition = (long)mem_info.BaseAddress;
				int bytesToRead = (int)mem_info.RegionSize;

				IntPtr bytesRead;
				while (bytesToRead > 0 && ReadProcessMemory(processHandle, new IntPtr(readPosition), buffer,
					                   Math.Min(buffer.Length, bytesToRead), out bytesRead) && bytesRead != IntPtr.Zero)
				{                   
					//search bytes pattern
					int index = ArrayExtensions.IndexOf(buffer, arrayToFind, 0, (int)bytesRead);
					if (index != -1)
					{
						address = readPosition + index; 
						break;
					}

					readPosition += (long)bytesRead;
					bytesToRead -= (int)bytesRead;                  
				}

				if (address != -1)
				{
					break;
				}
			}
                
			// move to next memory region
			min_address = (long)mem_info.BaseAddress + (long)mem_info.RegionSize;
		}

		if (address != -1)
		{
			readMemory = (array, offset, count) =>
			{
				IntPtr bytesRead;
				return ReadProcessMemory(processHandle, new IntPtr(address + offset), array, count, out bytesRead);
			};

			return true;
		}
		else
		{
			errorMessage = "Cannot find player data in DOSBOX process memory.";
			return false;
		}
	}
}

public static class ArrayExtensions
{
	private static bool isMatch(byte[] x, byte[] y, int index)
	{
		for (int j = 0; j < y.Length; ++j)
			if (x[j + index] != y[j])
				return false;
		return true;
	}

	public static int IndexOf(this byte[] x, byte[] y, int startIndex, int count)
	{
		for (int i = startIndex; i < count - y.Length + 1; ++i)
			if (isMatch(x, y, i))
				return i;
		return -1;
	}
}