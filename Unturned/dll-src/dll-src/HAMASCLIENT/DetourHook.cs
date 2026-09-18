using System;
using System.Reflection;
using System.Runtime.InteropServices;
public class DetourHook
{
	public DetourHook(MethodInfo originalMethod, MethodInfo hookMethod)
	{
		this.originalMethod = originalMethod;
		this.methodPointer = originalMethod.MethodHandle.GetFunctionPointer();
		this.originalBytes = new byte[12];
		Marshal.Copy(this.methodPointer, this.originalBytes, 0, 12);
		this.hookBytes = new byte[12];
		this.hookBytes[0] = 72;
		this.hookBytes[1] = 184;
		Array.Copy(BitConverter.GetBytes(hookMethod.MethodHandle.GetFunctionPointer().ToInt64()), 0, this.hookBytes, 2, 8);
		this.hookBytes[10] = byte.MaxValue;
		this.hookBytes[11] = 224;
		this.isApplied = false;
	}
	public void Apply()
	{
		this.isApplied = true;
		Marshal.Copy(this.hookBytes, 0, this.methodPointer, 12);
	}
	public void Revert()
	{
		this.isApplied = false;
		Marshal.Copy(this.originalBytes, 0, this.methodPointer, 12);
	}
	public MethodInfo originalMethod;
	public IntPtr methodPointer;
	public byte[] originalBytes;
	public byte[] hookBytes;
	public bool isApplied;
}
