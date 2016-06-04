using UnityEngine;
using System.Collections;
using System.Runtime.InteropServices;
using System;
using System.Diagnostics;
using System.Linq;

public class ProcessHelper
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
	private static extern bool CloseHandle(IntPtr hObject);

	[DllImport("kernel32.dll")]
	private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

	[DllImport("kernel32.dll")]
	private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

	private readonly IntPtr processHandle;

	public ProcessHelper(IntPtr processHandle)
	{
		this.processHandle = processHandle;
	}

	~ProcessHelper()
	{
		Close();
	}

	public static ProcessHelper OpenProcess(int processId)
	{
		return new ProcessHelper(OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_WM_READ, false, processId));
	}

	public long Read(byte[] buffer, long offset, int count)
	{
		IntPtr bytesRead;
		if (ReadProcessMemory(processHandle, new IntPtr(offset), buffer, count, out bytesRead))
		{
			return (long)bytesRead;
		}
		return 0;
	}

	public void Close()
	{
		CloseHandle(processHandle);
	}

	public long SearchForBytePattern(byte[] pattern)
	{
		MEMORY_BASIC_INFORMATION mem_info = new MEMORY_BASIC_INFORMATION();

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

				long bytesRead;
				while (bytesToRead > 0 && (bytesRead = Read(buffer, readPosition, Math.Min(buffer.Length, bytesToRead))) > 0)
				{
					//search bytes pattern
					int index = IndexOf(buffer, pattern, (int)bytesRead);
					if (index != -1)
					{
						return readPosition + index;
					}

					readPosition += bytesRead;
					bytesToRead -= (int)bytesRead;
				}
			}

			// move to next memory region
			min_address = (long)mem_info.BaseAddress + (long)mem_info.RegionSize;
		}

		return -1;
	}
	
	private int IndexOf(byte[] x, byte[] y, int count)
	{
		for (int i = 0; i < count - y.Length + 1; i++)
			if (IsMatch(x, y, i))
				return i;
		return -1;
	}

	private bool IsMatch(byte[] x, byte[] y, int index)
	{
		for (int j = 0; j < y.Length; j++)
			if (x[j + index] != y[j])
				return false;
		return true;
	}
}