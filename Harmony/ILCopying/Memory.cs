﻿using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Harmony.ILCopying
{
	public static class Memory
	{
		public static long WriteJump(long memory, long destination)
		{
			if (IntPtr.Size == sizeof(long))
			{
				memory = WriteBytes(memory, new byte[] { 0x48, 0xB8 });
				memory = WriteLong(memory, destination);
				memory = WriteBytes(memory, new byte[] { 0xFF, 0xE0 });
			}
			else
			{
				memory = WriteByte(memory, 0x68);
				memory = WriteInt(memory, (int)destination);
				memory = WriteByte(memory, 0xc3);
			}
			return memory;
		}

		private readonly static FieldInfo f_DynamicMethod_m_method =
			// .NET
			typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance) ??
			// Mono
			typeof(DynamicMethod).GetField("mhandle", BindingFlags.NonPublic | BindingFlags.Instance);

		private readonly static MethodInfo f_DynamicMethod_GetMethodDescriptor =
			typeof(DynamicMethod).GetMethod("GetMethodDescriptor", BindingFlags.NonPublic | BindingFlags.Instance);

		private static RuntimeMethodHandle GetMethodHandle(MethodBase method)
		{
			RuntimeMethodHandle handle;

			if (method is DynamicMethod)
			{
				if (f_DynamicMethod_GetMethodDescriptor != null)
				{
					// DynamicMethod actually generates its m_methodHandle on-the-fly and therefore
					// we should call GetMethodDescriptor to force it to be created.
					handle = (RuntimeMethodHandle)f_DynamicMethod_GetMethodDescriptor.Invoke(method, new object[0]);
				}
				else
				{
					handle = (RuntimeMethodHandle)f_DynamicMethod_m_method.GetValue(method);
				}
			}
			else
			{
				handle = method.MethodHandle;
			}

			return handle;
		}

		public static long GetMethodStart(MethodBase method)
		{
			RuntimeMethodHandle handle = GetMethodHandle(method);

			/* Required to ensure that the method is already JITed and the method start doesn't change.
			 * This seemingly only affects the .NET Framework.
			 * - ade
			 */
			RuntimeHelpers.PrepareMethod(handle);
			return handle.GetFunctionPointer().ToInt64();
		}

		public static unsafe long WriteByte(long memory, byte value)
		{
			var p = (byte*)memory;
			*p = value;
			return memory + sizeof(byte);
		}

		public static unsafe long WriteBytes(long memory, byte[] values)
		{
			foreach (var value in values)
				memory = WriteByte(memory, value);
			return memory;
		}

		public static unsafe long WriteInt(long memory, int value)
		{
			var p = (int*)memory;
			*p = value;
			return memory + sizeof(int);
		}

		public static unsafe long WriteLong(long memory, long value)
		{
			var p = (long*)memory;
			*p = value;
			return memory + sizeof(long);
		}
	}
}