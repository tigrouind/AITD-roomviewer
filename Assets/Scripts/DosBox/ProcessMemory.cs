using System.Runtime.InteropServices;
using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProcessMemory
{
	const uint PROCESS_QUERY_INFORMATION = 0x0400;
	const uint PROCESS_VM_READ = 0x0010;
	const uint PROCESS_VM_WRITE = 0x0020;
	const uint PROCESS_VM_OPERATION = 0x0008;
	const uint MEM_COMMIT = 0x00001000;
	const uint MEM_PRIVATE = 0x20000;
	const uint PAGE_READWRITE = 0x04;

	[StructLayout(LayoutKind.Sequential)]
	private struct MEMORY_BASIC_INFORMATION
	{
		public IntPtr BaseAddress;
		public IntPtr AllocationBase;
		public uint AllocationProtect;
		public IntPtr RegionSize;
		public uint State;
		public uint Protect;
		public uint Type;
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

	[DllImport("kernel32.dll")]
	private static extern bool CloseHandle(IntPtr hObject);

	[DllImport("kernel32.dll")]
	private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

	[DllImport("kernel32.dll")]
	private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesWritten);

	[DllImport("kernel32.dll")]
	private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

	[SerializeField]
	private long processHandle;

	public long BaseAddress;

	public ProcessMemory(int processId)
	{
		processHandle = (long)OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ | PROCESS_VM_WRITE | PROCESS_VM_OPERATION, false, processId);
	}

	public int Read(byte[] buffer, int offset, int count)
	{
		IntPtr bytesRead;
		if (ReadProcessMemory(new IntPtr(processHandle), new IntPtr(BaseAddress + offset), buffer, count, out bytesRead))
		{
			return (int)bytesRead;
		}
		return 0;
	}

	public int Write(byte[] buffer, int offset, int count)
	{
		IntPtr bytesWritten;
		if (WriteProcessMemory(new IntPtr(processHandle), new IntPtr(BaseAddress + offset), buffer, count, out bytesWritten))
		{
			return (int)bytesWritten;
		}
		return 0;
	}

	public void Close()
	{
		if (new IntPtr(processHandle) != IntPtr.Zero)
		{
			CloseHandle(new IntPtr(processHandle));
			processHandle = 0;
		}
	}

	public long SearchFor16MRegion()
	{
		byte[] memory = new byte[4096];

		//scan process memory regions
		foreach (var mem_info in GetMemoryRegions())
		{
			IntPtr bytesRead;
			//check if memory region is accessible
			//skip regions smaller than 16M (default DOSBOX memory size)
			if (mem_info.Protect == PAGE_READWRITE && mem_info.State == MEM_COMMIT && (mem_info.Type & MEM_PRIVATE) == MEM_PRIVATE
				&& (int)mem_info.RegionSize >= 1024 * 1024 * 16
				&& ReadProcessMemory(new IntPtr(processHandle), mem_info.BaseAddress, memory, memory.Length, out bytesRead)
				&& Utils.IndexOf(memory, Encoding.ASCII.GetBytes("CON ")) != -1)
			{
				return (long)mem_info.BaseAddress + 32; //skip Windows 32-bytes memory allocation header
			}
		}

		return -1;
	}

	IEnumerable<MEMORY_BASIC_INFORMATION> GetMemoryRegions(long min_address = 0, long max_address = 0x7FFFFFFF)
	{
		MEMORY_BASIC_INFORMATION mem_info;

		//scan process memory regions
		while (min_address < max_address
			&& VirtualQueryEx(new IntPtr(processHandle), (IntPtr)min_address, out mem_info, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION))) > 0)
		{
			yield return mem_info;

			// move to next memory region
			min_address = (long)mem_info.BaseAddress + (long)mem_info.RegionSize;
		}
	}

	public int SearchForBytePattern(int offset, int bytesToRead, Func<byte[], int> searchFunction)
	{
		byte[] buffer = new byte[81920];

		long readPosition = BaseAddress + offset;
		IntPtr bytesRead;
		while (bytesToRead > 0 && ReadProcessMemory(new IntPtr(processHandle), new IntPtr(readPosition), buffer, Math.Min(buffer.Length, bytesToRead), out bytesRead))
		{
			//search bytes pattern
			int index = searchFunction(buffer);
			if (index != -1)
			{
				return (int)(readPosition + index - BaseAddress);
			}

			readPosition += (int)bytesRead;
			bytesToRead -= (int)bytesRead;
		}

		return -1;
	}
}