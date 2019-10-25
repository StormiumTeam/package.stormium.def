using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace DefaultNamespace
{
	public unsafe struct GenComparer<TToCompare, TData>
		where TToCompare : struct
		where TData : struct, IEquatable<TData>
	{
		private int m_Offset;
		private int m_DataSize;
		private int m_DataAlign;

		internal GenComparer(int offset)
		{
			m_Offset = offset;

			m_DataSize  = UnsafeUtility.SizeOf<TData>();
			m_DataAlign = UnsafeUtility.AlignOf<TData>();
		}

		public bool Equals(TToCompare x, TData y)
		{
			var pointer = UnsafeUtility.Malloc(m_DataSize, m_DataAlign, Allocator.Temp);
			UnsafeUtility.MemCpy(pointer, (byte*) UnsafeUtility.AddressOf(ref x) + m_Offset, m_DataSize);
			UnsafeUtility.CopyPtrToStructure(pointer, out TData copy);

			var result = copy.Equals(y);

			UnsafeUtility.Free(pointer, Allocator.Temp);

			return result;
		}
	}

	public static unsafe class GenComparer
	{
		public static GenComparer<TToCompare, TData> Get<TToCompare, TData>(ref TToCompare origin, ref TData data)
			where TToCompare : struct
			where TData : struct, IEquatable<TData>
		{
			var addr1 = (int) UnsafeUtility.AddressOf(ref origin);
			var addr2 = (int) UnsafeUtility.AddressOf(ref data);

			return new GenComparer<TToCompare, TData>(addr2 - addr1);
		}

		public static bool Contains<T, TData>(this NativeArray<T> array, ref T from, ref TData to, TData compare)
			where T : struct
			where TData : struct, IEquatable<TData>
		{
			var comparer = Get(ref from, ref to);
			var length = array.Length;
			var ptr = array.GetUnsafePtr();
			for (var i = 0; i != length; i++)
			{
				if (comparer.Equals(UnsafeUtility.ReadArrayElement<T>(ptr, i), compare))
					return true;
			}

			return false;
		}
	}
}