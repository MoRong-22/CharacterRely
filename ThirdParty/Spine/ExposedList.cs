//
// System.Collections.Generic.List
//
// Authors:
//    Ben Maurer (bmaurer@ximian.com)
//    Martin Baulig (martin@ximian.com)
//    Carlos Alberto Cortez (calberto.cortez@gmail.com)
//    David Waite (mass@akuma.org)
//
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
// Copyright (C) 2005 David Waite
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Spine {
/// <summary>公开底层数组的可扩容列表；Items.Length 为容量，Count 为有效元素数量。</summary>
	[DebuggerDisplay("Count={Count}")]
	public class ExposedList<T> : IEnumerable<T> {
	/// <summary>直接可访问的底层数组；仅 [0, Count) 范围为有效数据，扩容会替换数组。</summary>
		public T[] Items;
	/// <summary>当前有效元素数量。</summary>
		public int Count;
	/// <summary>默认底层数组初始容量。</summary>
		private const int DefaultCapacity = 4;
	/// <summary>空列表共用的零长度数组。</summary>
		private static readonly T[] EmptyArray = new T[0];
	/// <summary>列表结构修改次数，用于检测枚举期间的非法修改。</summary>
		private int version;

	/// <summary>创建公开底层数组的可扩容列表；Items.Length 为容量，Count 为有效元素数量，并初始化关联数据及缓冲区。</summary>
		public ExposedList () {
			Items = EmptyArray;
		}

	/// <summary>创建公开底层数组的可扩容列表；Items.Length 为容量，Count 为有效元素数量，并初始化关联数据及缓冲区。</summary>
		public ExposedList (IEnumerable<T> collection) {
			CheckCollection(collection);

			// 能预先确定数量时，按所需大小初始化容量。 原注： initialize to needed size (if determinable)
			ICollection<T> c = collection as ICollection<T>;
			if (c == null) {
				Items = EmptyArray;
				AddEnumerable(collection);
			} else {
				Items = new T[c.Count];
				AddCollection(c);
			}
		}

	/// <summary>创建公开底层数组的可扩容列表；Items.Length 为容量，Count 为有效元素数量，并初始化关联数据及缓冲区。</summary>
		public ExposedList (int capacity) {
			if (capacity < 0)
				throw new ArgumentOutOfRangeException("capacity");
			Items = new T[capacity];
		}

	/// <summary>创建公开底层数组的可扩容列表；Items.Length 为容量，Count 为有效元素数量，并初始化关联数据及缓冲区。</summary>
		internal ExposedList (T[] data, int size) {
			Items = data;
			Count = size;
		}

	/// <summary>在列表末尾追加元素，容量不足时自动扩容。</summary>
		public void Add (T item) {
			// 尝试扩容前先检查是否需要扩容。 原注： If we check to see if we need to grow before trying to grow
			// 前置检查可提升扩容路径的性能。 原注： we can speed things up by 25%
			if (Count == Items.Length)
				GrowIfNeeded(1);
			Items[Count++] = item;
			version++;
		}

	/// <summary>按即将增加的元素数量检查并扩展容量，不改变有效数量。</summary>
		public void GrowIfNeeded (int addedCount) {
			int minimumSize = Count + addedCount;
			if (minimumSize > Items.Length)
				Capacity = Math.Max(Math.Max(Capacity * 2, DefaultCapacity), minimumSize);
		}

	/// <summary>调整有效元素数量并确保容量，缩小时清空多余引用；返回列表自身。</summary>
		public ExposedList<T> Resize (int newSize) {
			int itemsLength = Items.Length;
			var oldItems = Items;
			if (newSize > itemsLength) {
				Array.Resize(ref Items, newSize);
// 保留的参考代码：创建新容量的底层数组。 原注：				var newItems = new T[newSize];
// 保留的参考代码：复制原有有效元素。 原注：				Array.Copy(oldItems, newItems, Count);
// 保留的参考代码：替换底层数组。 原注：				Items = newItems;
			} else if (newSize < itemsLength) {
				// 清空引用类型元素，允许垃圾回收器回收不再使用的对象。 原注： Allow nulling of T reference type to allow GC.
				for (int i = newSize; i < itemsLength; i++)
					oldItems[i] = default(T);
			}
			Count = newSize;
			return this;
		}

	/// <summary>确保底层数组至少具有指定容量，不改变有效数量。</summary>
		public void EnsureCapacity (int min) {
			if (Items.Length < min) {
				int newCapacity = Items.Length == 0 ? DefaultCapacity : Items.Length * 2;
				// 保留的参考代码：将容量限制到数组最大长度。 原注：if ((uint)newCapacity > Array.MaxArrayLength) newCapacity = Array.MaxArrayLength;
				if (newCapacity < min) newCapacity = min;
				Capacity = newCapacity;
			}
		}

	/// <summary>检查起始索引和数量是否构成列表中的有效范围。</summary>
		private void CheckRange (int index, int count) {
			if (index < 0)
				throw new ArgumentOutOfRangeException("index");

			if (count < 0)
				throw new ArgumentOutOfRangeException("count");

			if ((uint)index + (uint)count > (uint)Count)
				throw new ArgumentException("index and count exceed length of list");
		}

	/// <summary>利用源集合数量一次扩容并复制追加元素。</summary>
		private void AddCollection (ICollection<T> collection) {
			int collectionCount = collection.Count;
			if (collectionCount == 0)
				return;

			GrowIfNeeded(collectionCount);
			collection.CopyTo(Items, Count);
			Count += collectionCount;
		}

	/// <summary>逐项枚举并追加未知数量的源集合。</summary>
		private void AddEnumerable (IEnumerable<T> enumerable) {
			foreach (T t in enumerable) {
				Add(t);
			}
		}

		// 额外提供已知数量的集合重载，因为 ExposedList 仅实现 IEnumerable。 原注： Additional overload provided because ExposedList<T> only implements IEnumerable<T>,
		// 若不知道最终数量，逐项插入可能多次扩容。 原注： leading to sub-optimal behavior: It grows multiple times as it assumes not
		// 此重载提前确定插入数量，减少重复扩容。 原注： to know the final size ahead of insertion.
	/// <summary>按源集合顺序追加全部元素。</summary>
		public void AddRange (ExposedList<T> list) {
			CheckCollection(list);

			int collectionCount = list.Count;
			if (collectionCount == 0)
				return;

			GrowIfNeeded(collectionCount);
			list.CopyTo(Items, Count);
			Count += collectionCount;

			version++;
		}

	/// <summary>按源集合顺序追加全部元素。</summary>
		public void AddRange (IEnumerable<T> collection) {
			CheckCollection(collection);

			ICollection<T> c = collection as ICollection<T>;
			if (c != null)
				AddCollection(c);
			else
				AddEnumerable(collection);
			version++;
		}

	/// <summary>在有序列表或指定范围内二分查找；未找到返回插入位置的按位取反值。</summary>
		public int BinarySearch (T item) {
			return Array.BinarySearch<T>(Items, 0, Count, item);
		}

	/// <summary>在有序列表或指定范围内二分查找；未找到返回插入位置的按位取反值。</summary>
		public int BinarySearch (T item, IComparer<T> comparer) {
			return Array.BinarySearch<T>(Items, 0, Count, item, comparer);
		}

	/// <summary>在有序列表或指定范围内二分查找；未找到返回插入位置的按位取反值。</summary>
		public int BinarySearch (int index, int count, T item, IComparer<T> comparer) {
			CheckRange(index, count);
			return Array.BinarySearch<T>(Items, index, count, item, comparer);
		}

	/// <summary>清空有效元素；可按参数决定是否同时清空底层数组引用。</summary>
		public void Clear (bool clearArray = true) {
			if (clearArray)
				Array.Clear(Items, 0, Items.Length);

			Count = 0;
			version++;
		}

	/// <summary>判断有效元素中是否包含指定值。</summary>
		public bool Contains (T item) {
			return Array.IndexOf<T>(Items, item, 0, Count) != -1;
		}

	/// <summary>逐项调用转换委托，生成指定元素类型的新列表。</summary>
		public ExposedList<TOutput> ConvertAll<TOutput> (Converter<T, TOutput> converter) {
			if (converter == null)
				throw new ArgumentNullException("converter");
			ExposedList<TOutput> u = new ExposedList<TOutput>(Count);
			for (int i = 0; i < Count; i++)
				u.Items[i] = converter(Items[i]);

			u.Count = Count;
			return u;
		}

	/// <summary>把有效元素或指定范围复制到目标数组的指定位置。</summary>
		public void CopyTo (T[] array) {
			Array.Copy(Items, 0, array, 0, Count);
		}

	/// <summary>把有效元素或指定范围复制到目标数组的指定位置。</summary>
		public void CopyTo (T[] array, int arrayIndex) {
			Array.Copy(Items, 0, array, arrayIndex, Count);
		}

	/// <summary>把有效元素或指定范围复制到目标数组的指定位置。</summary>
		public void CopyTo (int index, T[] array, int arrayIndex, int count) {
			CheckRange(index, count);
			Array.Copy(Items, index, array, arrayIndex, count);
		}



	/// <summary>是否存在满足条件的元素。</summary>
		public bool Exists (Predicate<T> match) {
			CheckMatch(match);
			return GetIndex(0, Count, match) != -1;
		}

	/// <summary>返回首个满足条件的元素，未找到返回类型默认值。</summary>
		public T Find (Predicate<T> match) {
			CheckMatch(match);
			int i = GetIndex(0, Count, match);
			return (i != -1) ? Items[i] : default(T);
		}

	/// <summary>检查匹配条件委托非空。</summary>
		private static void CheckMatch (Predicate<T> match) {
			if (match == null)
				throw new ArgumentNullException("match");
		}

	/// <summary>返回所有满足条件的元素组成的新列表。</summary>
		public ExposedList<T> FindAll (Predicate<T> match) {
			CheckMatch(match);
			return FindAllList(match);
		}

	/// <summary>遍历有效元素并收集全部满足条件的项。</summary>
		private ExposedList<T> FindAllList (Predicate<T> match) {
			ExposedList<T> results = new ExposedList<T>();
			for (int i = 0; i < Count; i++)
				if (match(Items[i]))
					results.Add(Items[i]);

			return results;
		}

	/// <summary>从指定起点或范围正向查找首个匹配元素索引，未找到返回 -1。</summary>
		public int FindIndex (Predicate<T> match) {
			CheckMatch(match);
			return GetIndex(0, Count, match);
		}

	/// <summary>从指定起点或范围正向查找首个匹配元素索引，未找到返回 -1。</summary>
		public int FindIndex (int startIndex, Predicate<T> match) {
			CheckMatch(match);
			CheckIndex(startIndex);
			return GetIndex(startIndex, Count - startIndex, match);
		}

	/// <summary>从指定起点或范围正向查找首个匹配元素索引，未找到返回 -1。</summary>
		public int FindIndex (int startIndex, int count, Predicate<T> match) {
			CheckMatch(match);
			CheckRange(startIndex, count);
			return GetIndex(startIndex, count, match);
		}

	/// <summary>按指定起点和数量正向查找首个匹配索引。</summary>
		private int GetIndex (int startIndex, int count, Predicate<T> match) {
			int end = startIndex + count;
			for (int i = startIndex; i < end; i++)
				if (match(Items[i]))
					return i;

			return -1;
		}

	/// <summary>反向查找最后一个匹配元素，未找到返回类型默认值。</summary>
		public T FindLast (Predicate<T> match) {
			CheckMatch(match);
			int i = GetLastIndex(0, Count, match);
			return i == -1 ? default(T) : Items[i];
		}

	/// <summary>在指定范围反向查找匹配元素索引，未找到返回 -1。</summary>
		public int FindLastIndex (Predicate<T> match) {
			CheckMatch(match);
			return GetLastIndex(0, Count, match);
		}

	/// <summary>在指定范围反向查找匹配元素索引，未找到返回 -1。</summary>
		public int FindLastIndex (int startIndex, Predicate<T> match) {
			CheckMatch(match);
			CheckIndex(startIndex);
			return GetLastIndex(0, startIndex + 1, match);
		}

	/// <summary>在指定范围反向查找匹配元素索引，未找到返回 -1。</summary>
		public int FindLastIndex (int startIndex, int count, Predicate<T> match) {
			CheckMatch(match);
			int start = startIndex - count + 1;
			CheckRange(start, count);
			return GetLastIndex(start, count, match);
		}

	/// <summary>按指定搜索范围反向查找最后一个匹配索引。</summary>
		private int GetLastIndex (int startIndex, int count, Predicate<T> match) {
			// 与 FindLastIndex 不同，此重载使用正常方向的搜索区间参数。 原注： unlike FindLastIndex, takes regular params for search range
			for (int i = startIndex + count; i != startIndex; )
				if (match(Items[--i]))
					return i;
			return -1;
		}

	/// <summary>依次对有效元素执行操作。</summary>
		public void ForEach (Action<T> action) {
			if (action == null)
				throw new ArgumentNullException("action");
			for (int i = 0; i < Count; i++)
				action(Items[i]);
		}

	/// <summary>取得按当前集合顺序遍历元素的枚举器。</summary>
		public Enumerator GetEnumerator () {
			return new Enumerator(this);
		}

	/// <summary>复制指定起点和数量的元素，返回新列表。</summary>
		public ExposedList<T> GetRange (int index, int count) {
			CheckRange(index, count);
			T[] tmpArray = new T[count];
			Array.Copy(Items, index, tmpArray, 0, count);
			return new ExposedList<T>(tmpArray, count);
		}

	/// <summary>正向查询元素索引，未找到返回 -1。</summary>
		public int IndexOf (T item) {
			return Array.IndexOf<T>(Items, item, 0, Count);
		}

	/// <summary>正向查询元素索引，未找到返回 -1。</summary>
		public int IndexOf (T item, int index) {
			CheckIndex(index);
			return Array.IndexOf<T>(Items, item, index, Count - index);
		}

	/// <summary>正向查询元素索引，未找到返回 -1。</summary>
		public int IndexOf (T item, int index, int count) {
			if (index < 0)
				throw new ArgumentOutOfRangeException("index");

			if (count < 0)
				throw new ArgumentOutOfRangeException("count");

			if ((uint)index + (uint)count > (uint)Count)
				throw new ArgumentOutOfRangeException("index and count exceed length of list");

			return Array.IndexOf<T>(Items, item, index, count);
		}

	/// <summary>按偏移量移动指定位置后的元素，供插入和删除复用。</summary>
		private void Shift (int start, int delta) {
			if (delta < 0)
				start -= delta;

			if (start < Count)
				Array.Copy(Items, start, Items, start + delta, Count - start);

			Count += delta;

			if (delta < 0)
				Array.Clear(Items, Count, -delta);
		}

	/// <summary>检查索引是否落在允许访问的列表范围。</summary>
		private void CheckIndex (int index) {
			if (index < 0 || (uint)index > (uint)Count)
				throw new ArgumentOutOfRangeException("index");
		}

	/// <summary>在指定索引插入元素，并后移原有元素。</summary>
		public void Insert (int index, T item) {
			CheckIndex(index);
			if (Count == Items.Length)
				GrowIfNeeded(1);
			Shift(index, 1);
			Items[index] = item;
			version++;
		}

	/// <summary>检查输入集合非空。</summary>
		private void CheckCollection (IEnumerable<T> collection) {
			if (collection == null)
				throw new ArgumentNullException("collection");
		}

	/// <summary>在指定索引插入一组元素，并保留源集合顺序。</summary>
		public void InsertRange (int index, IEnumerable<T> collection) {
			CheckCollection(collection);
			CheckIndex(index);
			if (collection == this) {
				T[] buffer = new T[Count];
				CopyTo(buffer, 0);
				GrowIfNeeded(Count);
				Shift(index, buffer.Length);
				Array.Copy(buffer, 0, Items, index, buffer.Length);
			} else {
				ICollection<T> c = collection as ICollection<T>;
				if (c != null)
					InsertCollection(index, c);
				else
					InsertEnumeration(index, collection);
			}
			version++;
		}

	/// <summary>按已知源集合数量扩容并在指定位置插入。</summary>
		private void InsertCollection (int index, ICollection<T> collection) {
			int collectionCount = collection.Count;
			GrowIfNeeded(collectionCount);

			Shift(index, collectionCount);
			collection.CopyTo(Items, index);
		}

	/// <summary>将未知长度的可枚举源插入指定位置。</summary>
		private void InsertEnumeration (int index, IEnumerable<T> enumerable) {
			foreach (T t in enumerable)
				Insert(index++, t);
		}

	/// <summary>反向查询最后一次出现的元素索引，未找到返回 -1。</summary>
		public int LastIndexOf (T item) {
			return Array.LastIndexOf<T>(Items, item, Count - 1, Count);
		}

	/// <summary>反向查询最后一次出现的元素索引，未找到返回 -1。</summary>
		public int LastIndexOf (T item, int index) {
			CheckIndex(index);
			return Array.LastIndexOf<T>(Items, item, index, index + 1);
		}

	/// <summary>反向查询最后一次出现的元素索引，未找到返回 -1。</summary>
		public int LastIndexOf (T item, int index, int count) {
			if (index < 0)
				throw new ArgumentOutOfRangeException("index", index, "index is negative");

			if (count < 0)
				throw new ArgumentOutOfRangeException("count", count, "count is negative");

			if (index - count + 1 < 0)
				throw new ArgumentOutOfRangeException("count", count, "count is too large");

			return Array.LastIndexOf<T>(Items, item, index, count);
		}

	/// <summary>移除首个匹配元素，成功时返回 true。</summary>
		public bool Remove (T item) {
			int loc = IndexOf(item);
			if (loc != -1)
				RemoveAt(loc);

			return loc != -1;
		}

	/// <summary>移除全部满足条件的元素，返回移除数量。</summary>
		public int RemoveAll (Predicate<T> match) {
			CheckMatch(match);
			int i = 0;
			int j = 0;

			// 找到第一个需要删除的元素。 原注： Find the first item to remove
			for (i = 0; i < Count; i++)
				if (match(Items[i]))
					break;

			if (i == Count)
				return 0;

			version++;

			// 继续删除其余满足条件的元素。 原注： Remove any additional items
			for (j = i + 1; j < Count; j++) {
				if (!match(Items[j]))
					Items[i++] = Items[j];
			}
			if (j - i > 0)
				Array.Clear(Items, i, j - i);

			Count = i;
			return (j - i);
		}

	/// <summary>移除指定索引的元素并前移后续元素。</summary>
		public void RemoveAt (int index) {
			if (index < 0 || (uint)index >= (uint)Count)
				throw new ArgumentOutOfRangeException("index");
			Shift(index, -1);
			Array.Clear(Items, Count, 1);
			version++;
		}

		// Spine 在原始集合基础上增加的方法。 原注： Spine Added Method
		// 参照 Stack.Pop，从列表末尾弹出元素；空列表抛出 InvalidOperationException。 原注： Based on Stack<T>.Pop(); https://referencesource.microsoft.com/#mscorlib/system/collections/stack.cs
		/// <summary><para>参照 Stack.Pop，从列表末尾弹出元素；空列表抛出 InvalidOperationException。</para>Pops the last item of the list. If the list is empty, Pop throws an InvalidOperationException.</summary>
		public T Pop () {
			if (Count == 0)
				throw new InvalidOperationException("List is empty. Nothing to pop.");

			int i = Count - 1;
			T item = Items[i];
			Items[i] = default(T);
			Count--;
			version++;
			return item;
		}

	/// <summary>移除指定范围的元素并整理剩余元素。</summary>
		public void RemoveRange (int index, int count) {
			CheckRange(index, count);
			if (count > 0) {
				Shift(index, -count);
				Array.Clear(Items, Count, count);
				version++;
			}
		}

	/// <summary>反转列表或指定范围内的元素顺序。</summary>
		public void Reverse () {
			Array.Reverse(Items, 0, Count);
			version++;
		}

	/// <summary>反转列表或指定范围内的元素顺序。</summary>
		public void Reverse (int index, int count) {
			CheckRange(index, count);
			Array.Reverse(Items, index, count);
			version++;
		}

	/// <summary>按默认顺序或指定比较器对列表或指定范围排序。</summary>
		public void Sort () {
			Array.Sort<T>(Items, 0, Count, Comparer<T>.Default);
			version++;
		}

	/// <summary>按默认顺序或指定比较器对列表或指定范围排序。</summary>
		public void Sort (IComparer<T> comparer) {
			Array.Sort<T>(Items, 0, Count, comparer);
			version++;
		}

	/// <summary>按默认顺序或指定比较器对列表或指定范围排序。</summary>
		public void Sort (Comparison<T> comparison) {
			Array.Sort<T>(Items, comparison);
			version++;
		}

	/// <summary>按默认顺序或指定比较器对列表或指定范围排序。</summary>
		public void Sort (int index, int count, IComparer<T> comparer) {
			CheckRange(index, count);
			Array.Sort<T>(Items, index, count, comparer);
			version++;
		}

	/// <summary>将有效元素复制为独立数组，不包含多余容量。</summary>
		public T[] ToArray () {
			T[] t = new T[Count];
			Array.Copy(Items, t, Count);

			return t;
		}

	/// <summary>将底层数组容量缩减到有效元素数量。</summary>
		public void TrimExcess () {
			Capacity = Count;
		}

	/// <summary>全部有效元素均满足条件时返回 true。</summary>
		public bool TrueForAll (Predicate<T> match) {
			CheckMatch(match);

			for (int i = 0; i < Count; i++)
				if (!match(Items[i]))
					return false;

			return true;
		}

	/// <summary>底层数组容量；设置时必须不小于有效元素数量。</summary>
		public int Capacity {
			get {
				return Items.Length;
			}
			set {
				if ((uint)value < (uint)Count)
					throw new ArgumentOutOfRangeException();

				Array.Resize(ref Items, value);
			}
		}

		#region Interface implementations.

	/// <summary>取得按当前集合顺序遍历元素的枚举器。</summary>
		IEnumerator<T> IEnumerable<T>.GetEnumerator () {
			return GetEnumerator();
		}

	/// <summary>取得按当前集合顺序遍历元素的枚举器。</summary>
		IEnumerator IEnumerable.GetEnumerator () {
			return GetEnumerator();
		}

		#endregion

	/// <summary>遍历 ExposedList 有效元素的枚举器。</summary>
		public struct Enumerator : IEnumerator<T>, IDisposable {
		/// <summary>所属的可扩容列表。</summary>
			private ExposedList<T> l;
		/// <summary>下一次枚举元素的索引。</summary>
			private int next;
		/// <summary>枚举开始时保存的集合修改版本。</summary>
			private int ver;
		/// <summary>当前枚举到的元素。</summary>
			private T current;

		/// <summary>创建遍历 ExposedList 有效元素的枚举器，并初始化关联数据及缓冲区。</summary>
			internal Enumerator (ExposedList<T> l)
				: this() {
				this.l = l;
				ver = l.version;
			}

		/// <summary>解除对列表的引用，结束本次枚举。</summary>
			public void Dispose () {
				l = null;
			}

		/// <summary>检查列表未在枚举过程中发生修改，且枚举器状态有效。</summary>
			private void VerifyState () {
				if (l == null)
					throw new ObjectDisposedException(GetType().FullName);
				if (ver != l.version)
					throw new InvalidOperationException(
							"Collection was modified; enumeration operation may not execute.");
			}

		/// <summary>移动到下一个元素，到达末尾返回 false。</summary>
			public bool MoveNext () {
				VerifyState();

				if (next < 0)
					return false;

				if (next < l.Count) {
					current = l.Items[next++];
					return true;
				}

				next = -1;
				return false;
			}

		/// <summary>当前枚举元素；枚举尚未开始或已经结束时访问会报错。</summary>
			public T Current {
				get {
					return current;
				}
			}

		/// <summary>验证列表版本后把枚举位置重置到第一项之前。</summary>
			void IEnumerator.Reset () {
				VerifyState();
				next = 0;
			}

		/// <summary>当前枚举元素；枚举尚未开始或已经结束时访问会报错。</summary>
			object IEnumerator.Current {
				get {
					VerifyState();
					if (next <= 0)
						throw new InvalidOperationException();
					return current;
				}
			}
		}
	}
}
