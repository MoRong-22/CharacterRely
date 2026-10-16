/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated May 1, 2019. Replaces all prior versions.
 *
 * Copyright (c) 2013-2019, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
 * NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS
 * INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/
/* 中文说明：感谢原始 OrderedDictionary 集合实现的作者，原始项目链接见英文说明。
*****************************************************************************
 * Thanks to Travis Parks
 * https://github.com/jehugaleahsa/truncon.collections.OrderedDictionary
 *****************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace Spine.Collections
{
	/// <summary><para>保持插入顺序的泛型字典，支持按索引获取键；查找索引需线性搜索，插入和删除可能移动后续索引。</para>
	/// Represents a dictionary that tracks the order that items were added.
	/// </summary>
	/// <typeparam name="TKey">字典键的类型。 The type of the dictionary keys.</typeparam>
	/// <typeparam name="TValue">字典值的类型。 The type of the dictionary values.</typeparam>
	/// <remarks>支持键和顺序索引互相查询；索引查找为线性操作，插入删除需调整后续项索引。 
	/// This dictionary makes it possible to get the index of a key and a key based on an index.
	/// It can be costly to find the index of a key because it must be searched for linearly.
	/// It can be costly to insert a key/value pair because other key's indexes must be adjusted.
	/// It can be costly to remove a key/value pair because other keys' indexes must be adjusted.
	/// </remarks>
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(OrderedDictionaryDebugView<,>))]
	public sealed class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IList<KeyValuePair<TKey, TValue>>
	{
		/// <summary>从键到有序列表索引的映射。</summary>
		private readonly Dictionary<TKey, int> dictionary;
		/// <summary>按插入顺序保存的键列表。</summary>
		private readonly List<TKey> keys;
		/// <summary>与键列表逐项对应的值列表。</summary>
		private readonly List<TValue> values;
		/// <summary>集合的修改版本号，用于检测枚举期间字典是否发生变化。</summary>
		private int version;

		/// <summary>枚举过程中集合被修改时的异常提示。</summary>
		private const string CollectionModifiedMessage = "Collection was modified; enumeration operation may not execute.";
		/// <summary>尝试修改只读集合时的异常提示。</summary>
		private const string EditReadOnlyListMessage = "An attempt was made to edit a read-only list.";
		/// <summary>索引越界时的异常提示。</summary>
		private const string IndexOutOfRangeMessage = "The index is negative or outside the bounds of the collection.";

		/// <summary><para>创建默认容量和默认键比较器的有序字典。</para>
		/// Initializes a new instance of an OrderedDictionary.
		/// </summary>
		public OrderedDictionary ()
			: this(0, null) {
		}

		/// <summary><para>按初始容量创建有序字典；容量不得为负。</para>
		/// Initializes a new instance of an OrderedDictionary.
		/// </summary>
		/// <param name="capacity">字典初始容量。 The initial capacity of the dictionary.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">初始容量小于零。 The capacity is less than zero.</exception>
		public OrderedDictionary (int capacity)
			: this(capacity, null) {
		}

		/// <summary><para>按指定键比较器创建有序字典。</para>
		/// Initializes a new instance of an OrderedDictionary.
		/// </summary>
		/// <param name="comparer">用于比较键是否相等的比较器。 The equality comparer to use to compare keys.</param>
		public OrderedDictionary (IEqualityComparer<TKey> comparer)
			: this(0, comparer) {
		}

		/// <summary><para>按指定初始容量和键比较器创建有序字典。</para>
		/// Initializes a new instance of an OrderedDictionary.
		/// </summary>
		/// <param name="capacity">字典初始容量。 The initial capacity of the dictionary.</param>
		/// <param name="comparer">用于比较键是否相等的比较器。 The equality comparer to use to compare keys.</param>
		public OrderedDictionary (int capacity, IEqualityComparer<TKey> comparer) {
			dictionary = new Dictionary<TKey, int>(capacity, comparer ?? EqualityComparer<TKey>.Default);
			keys = new List<TKey>(capacity);
			values = new List<TValue>(capacity);
		}

		/// <summary><para>用于判断键相等的比较器。</para>
		/// Gets the equality comparer used to compare keys.
		/// </summary>
		public IEqualityComparer<TKey> Comparer {
			get {
				return dictionary.Comparer;
			}
		}

		/// <summary><para>追加键值对；键为 null 或已存在时抛出异常。</para>
		/// Adds the given key/value pair to the dictionary.
		/// </summary>
		/// <param name="key">要追加的键，不能为 null 或与已有键重复。 The key to add to the dictionary.</param>
		/// <param name="value">与该键关联的值。 The value to associated with the key.</param>
		/// <exception cref="System.ArgumentException">指定键已经存在。 The given key already exists in the dictionary.</exception>
		/// <exception cref="System.ArgumentNullException">键为 null。 The key is null.</exception>
		public void Add (TKey key, TValue value) {
			dictionary.Add(key, values.Count);
			keys.Add(key);
			values.Add(value);
			++version;
		}

		/// <summary><para>在指定索引插入键值对；键必须非空且唯一，索引必须在可插入范围内。</para>
		/// Inserts the given key/value pair at the specified index.
		/// </summary>
		/// <param name="index">插入键值对的位置索引。 The index to insert the key/value pair.</param>
		/// <param name="key">要插入的键。 The key to insert.</param>
		/// <param name="value">要插入的值。 The value to insert.</param>
		/// <exception cref="System.ArgumentException">指定键已经存在。 The given key already exists in the dictionary.</exception>
		/// <exception cref="System.ArgumentNullException">键为 null。 The key is null.</exception>
		/// <exception cref="System.ArgumentOutOfRangeException">索引为负或超过字典允许的范围。 The index is negative -or- larger than the size of the dictionary.</exception>
		public void Insert (int index, TKey key, TValue value) {
			if (index < 0 || index > values.Count) {
				throw new ArgumentOutOfRangeException("index", index, IndexOutOfRangeMessage);
			}
			dictionary.Add(key, index);
			for (int keyIndex = index; keyIndex != keys.Count; ++keyIndex) {
				var otherKey = keys[keyIndex];
				dictionary[otherKey] += 1;
			}
			keys.Insert(index, key);
			values.Insert(index, value);
			++version;
		}

		/// <summary><para>判断字典是否包含指定键；键不能为 null。</para>
		/// Determines whether the given key exists in the dictionary.
		/// </summary>
		/// <param name="key">要查询的键。 The key to look for.</param>
		/// <returns>包含指定键时为 true，否则为 false。 True if the key exists in the dictionary; otherwise, false.</returns>
		/// <exception cref="System.ArgumentNullException">键为 null。 The key is null.</exception>
		public bool ContainsKey (TKey key) {
			return dictionary.ContainsKey(key);
		}

		/// <summary><para>返回指定索引位置的键；索引必须在有效范围内。</para>
		/// Gets the key at the given index.
		/// </summary>
		/// <param name="index">要取得的键在顺序列表中的索引。 The index of the key to get.</param>
		/// <returns>指定索引对应的键。 The key at the given index.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">索引为负或超过键集合范围。 The index is negative -or- larger than the number of keys.</exception>
		public TKey GetKey (int index) {
			return keys[index];
		}

		/// <summary><para>线性查询键的索引，未找到返回 -1；时间复杂度 O(n)。</para>
		/// Gets the index of the given key.
		/// </summary>
		/// <param name="key">需要查找索引的键。 The key to get the index of.</param>
		/// <returns>键在字典中的索引，未找到返回 -1。 The index of the key in the dictionary -or- -1 if the key is not found.</returns>
		/// <remarks>此操作时间复杂度为 O(n)。 The operation runs in O(n).</remarks>
		public int IndexOf (TKey key) {
			int index;
			if (dictionary.TryGetValue(key, out index)) {
				return index;
			}
			return -1;
		}

		/// <summary><para>按插入顺序返回字典中的键集合。</para>
		/// Gets the keys in the dictionary in the order they were added.
		/// </summary>
		public KeyCollection Keys {
			get {
				return new KeyCollection(this.dictionary);
			}
		}

		/// <summary><para>按键移除键值对；找到并移除返回 true，未找到返回 false。</para>
		/// Removes the key/value pair with the given key from the dictionary.
		/// </summary>
		/// <param name="key">要删除的键值对中的键。 The key of the pair to remove.</param>
		/// <returns>找到并移除了键值对时返回 true，否则为 false。 True if the key was found and the pair removed; otherwise, false.</returns>
		/// <exception cref="System.ArgumentNullException">键为 null。 The key is null.</exception>
		public bool Remove (TKey key) {
			int index;
			if (dictionary.TryGetValue(key, out index)) {
				RemoveAt(index);
				return true;
			}
			return false;
		}

		/// <summary><para>移除指定索引的键值对，并调整后续索引。</para>
		/// Removes the key/value pair at the given index.
		/// </summary>
		/// <param name="index">要删除的键值对的索引。 The index of the key/value pair to remove.</param>
		/// <exception cref="System.ArgumentOutOfRangeException">索引为负或超过字典允许的范围。 The index is negative -or- larger than the size of the dictionary.</exception>
		public void RemoveAt (int index) {
			var key = keys[index];
			for (int keyIndex = index + 1; keyIndex < keys.Count; ++keyIndex) {
				var otherKey = keys[keyIndex];
				dictionary[otherKey] -= 1;
			}
			dictionary.Remove(key);
			keys.RemoveAt(index);
			values.RemoveAt(index);
			++version;
		}

		/// <summary><para>尝试查找键对应的值；找到返回 true，否则返回 false 并输出 TValue 的默认值。</para>
		/// Tries to get the value associated with the given key. If the key is not found,
		/// default(TValue) value is stored in the value.
		/// </summary>
		/// <param name="key">需要查找关联值的键。 The key to get the value for.</param>
		/// <param name="value">接收查找结果，未找到时为值类型的默认值。 The value used to hold the results.</param>
		/// <returns>找到指定键时返回 true，否则为 false。 True if the key was found; otherwise, false.</returns>
		/// <exception cref="System.ArgumentNullException">键为 null。 The key is null.</exception>
		public bool TryGetValue (TKey key, out TValue value) {
			int index;
			if (dictionary.TryGetValue(key, out index)) {
				value = values[index];
				return true;
			}
			value = default(TValue);
			return false;
		}

		/// <summary><para>返回字典中的值集合。</para>
		/// Gets the values in the dictionary.
		/// </summary>
		public ValueCollection Values {
			get {
				return new ValueCollection(values);
			}
		}

		/// <summary><para>按索引读取或修改值；索引必须在有效范围内。</para>
		/// Gets or sets the value at the given index.
		/// </summary>
		/// <param name="index">要读取的值在顺序列表中的索引。 The index of the value to get.</param>
		/// <returns>指定索引对应的值。 The value at the given index.</returns>
		/// <exception cref="System.ArgumentOutOfRangeException">索引为负或超出字典范围。 The index is negative -or- beyond the length of the dictionary.</exception>
		public TValue this[int index] {
			get {
				return values[index];
			}

			set {
				values[index] = value;
			}
		}

		/// <summary><para>按键读取或设置值；键不能为 null，读取不存在的键会抛出异常。</para>
		/// Gets or sets the value associated with the given key.
		/// </summary>
		/// <param name="key">读取或设置关联值时使用的键。 The key to get the associated value by or to associate with the value.</param>
		/// <returns>指定键关联的值。 The value associated with the given key.</returns>
		/// <exception cref="System.ArgumentNullException">键为 null。 The key is null.</exception>
		/// <exception cref="System.Collections.Generic.KeyNotFoundException">字典中不存在指定键。 The key is not in the dictionary.</exception>
		public TValue this[TKey key] {
			get {
				return values[dictionary[key]];
			}
			set {
				int index;
				if (dictionary.TryGetValue(key, out index)) {
					keys[index] = key;
					values[index] = value;
				}
				else {
					Add(key, value);
				}
			}
		}

		/// <summary><para>清空全部键值对。</para>
		/// Removes all key/value pairs from the dictionary.
		/// </summary>
		public void Clear () {
			dictionary.Clear();
			keys.Clear();
			values.Clear();
			++version;
		}

		/// <summary><para>字典中的键值对数量。</para>
		/// Gets the number of key/value pairs in the dictionary.
		/// </summary>
		public int Count {
			get {
				return dictionary.Count;
			}
		}

		/// <summary><para>按插入顺序枚举全部键值对。</para>
		/// Gets the key/value pairs in the dictionary in the order they were added.
		/// </summary>
		/// <returns>按插入顺序遍历键值对的枚举器。 An enumerator over the key/value pairs in the dictionary.</returns>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator () {
			int startVersion = version;
			for (int index = 0; index != keys.Count; ++index) {
				var key = keys[index];
				var value = values[index];
				yield return new KeyValuePair<TKey, TValue>(key, value);
				if (version != startVersion) {
					throw new InvalidOperationException(CollectionModifiedMessage);
				}
			}
		}

		/// <summary>查询键和值都匹配的键值对索引，未找到返回 -1。</summary>
		int IList<KeyValuePair<TKey, TValue>>.IndexOf (KeyValuePair<TKey, TValue> item) {
			int index;
			if (dictionary.TryGetValue(item.Key, out index) && Equals(values[index], item.Value)) {
				return index;
			}
			return -1;
		}

		/// <summary>在指定索引插入键值对，并后移后续条目；键必须唯一。</summary>
		void IList<KeyValuePair<TKey, TValue>>.Insert (int index, KeyValuePair<TKey, TValue> item) {
			Insert(index, item.Key, item.Value);
		}

		/// <summary>通过列表接口按索引读取或替换完整键值对，维持键映射与顺序一致。</summary>
		KeyValuePair<TKey, TValue> IList<KeyValuePair<TKey, TValue>>.this[int index] {
			get {
				TKey key = keys[index];
				TValue value = values[index];
				return new KeyValuePair<TKey, TValue>(key, value);
			}
			set {
				TKey key = keys[index];
				if (dictionary.Comparer.Equals(key, value.Key)) {
					dictionary[value.Key] = index;
				}
				else {
					dictionary.Add(value.Key, index); // 键已经存在时会抛出异常。 原注： will throw if key already exists
					dictionary.Remove(key);
				}
				keys[index] = value.Key;
				values[index] = value.Value;
			}
		}

		/// <summary>通过字典接口返回按插入顺序组织的键集合。</summary>
		ICollection<TKey> IDictionary<TKey, TValue>.Keys {
			get {
				return Keys;
			}
		}

		/// <summary>通过字典接口返回按插入顺序组织的值集合。</summary>
		ICollection<TValue> IDictionary<TKey, TValue>.Values {
			get {
				return Values;
			}
		}

		/// <summary>通过集合接口在字典末尾追加键值对，键重复时抛出异常。</summary>
		void ICollection<KeyValuePair<TKey, TValue>>.Add (KeyValuePair<TKey, TValue> item) {
			Add(item.Key, item.Value);
		}

		/// <summary>判断字典是否包含键和值都匹配的键值对。</summary>
		bool ICollection<KeyValuePair<TKey, TValue>>.Contains (KeyValuePair<TKey, TValue> item) {
			int index;
			if (dictionary.TryGetValue(item.Key, out index) && Equals(values[index], item.Value)) {
				return true;
			}
			return false;
		}

		/// <summary>按插入顺序将全部键值对复制到目标数组的指定起始索引。</summary>
		void ICollection<KeyValuePair<TKey, TValue>>.CopyTo (KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
			if (array == null) {
				throw new ArgumentNullException("array");
			}
			if (arrayIndex < 0) {
				throw new ArgumentOutOfRangeException("arrayIndex", arrayIndex, IndexOutOfRangeMessage);
			}
			for (int index = 0; index != keys.Count && arrayIndex < array.Length; ++index, ++arrayIndex) {
				var key = keys[index];
				var value = values[index];
				array[arrayIndex] = new KeyValuePair<TKey, TValue>(key, value);
			}
		}

		/// <summary>此有序字典允许修改，返回 false。</summary>
		bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly {
			get {
				return false;
			}
		}

		/// <summary>仅当键和值都匹配时移除该键值对，成功移除返回 true。</summary>
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove (KeyValuePair<TKey, TValue> item) {
			ICollection<KeyValuePair<TKey, TValue>> self = this;
			if (self.Contains(item)) {
				return Remove(item.Key);
			}
			return false;
		}

		/// <summary>取得按当前集合顺序遍历元素的枚举器。</summary>
		IEnumerator IEnumerable.GetEnumerator () {
			return GetEnumerator();
		}

		/// <summary><para>有序字典键集合的只读包装。</para>
		/// Wraps the keys in an OrderDictionary.
		/// </summary>
		public sealed class KeyCollection : ICollection<TKey>
		{
			/// <summary>共享的键到有序列表索引映射。</summary>
			private readonly Dictionary<TKey, int> dictionary;

			/// <summary><para>创建键集合包装，所属字典不得为 null。</para>
			/// Initializes a new instance of a KeyCollection.
			/// </summary>
			/// <param name="dictionary">由当前集合视图包装的有序字典。 The OrderedDictionary whose keys to wrap.</param>
			/// <exception cref="System.ArgumentNullException">字典为 null。 The dictionary is null.</exception>
			internal KeyCollection (Dictionary<TKey, int> dictionary) {
				this.dictionary = dictionary;
			}

			/// <summary><para>将键复制到指定数组的起始索引；数组容量须足够，索引不得为负。</para>
			/// Copies the keys from the OrderedDictionary to the given array, starting at the given index.
			/// </summary>
			/// <param name="array">用于接收键的目标数组。 The array to copy the keys to.</param>
			/// <param name="arrayIndex">目标数组开始写入键的位置。 The index into the array to start copying the keys.</param>
			/// <exception cref="System.ArgumentNullException">目标数组为 null。 The array is null.</exception>
			/// <exception cref="System.ArgumentOutOfRangeException">目标数组起始索引为负。 The arrayIndex is negative.</exception>
			/// <exception cref="System.ArgumentException">从指定起始索引开始的数组剩余容量不足以容纳全部键。 The array, starting at the given index, is not large enough to contain all the keys.</exception>
			public void CopyTo (TKey[] array, int arrayIndex) {
				dictionary.Keys.CopyTo(array, arrayIndex);
			}

			/// <summary><para>所属有序字典中的键数量。</para>
			/// Gets the number of keys in the OrderedDictionary.
			/// </summary>
			public int Count {
				get {
					return dictionary.Count;
				}
			}

			/// <summary><para>取得按字典顺序枚举键的枚举器。</para>
			/// Gets an enumerator over the keys in the OrderedDictionary.
			/// </summary>
			/// <returns>用于当前集合遍历的枚举器。 The enumerator.</returns>
			public IEnumerator<TKey> GetEnumerator () {
				return dictionary.Keys.GetEnumerator();
			}

			/// <summary>判断有效元素中是否包含指定值。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			bool ICollection<TKey>.Contains (TKey item) {
				return dictionary.ContainsKey(item);
			}

			/// <summary>只读集合不允许此修改操作，调用时抛出 NotSupportedException。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			void ICollection<TKey>.Add (TKey item) {
				throw new NotSupportedException(EditReadOnlyListMessage);
			}

			/// <summary>只读集合不允许此修改操作，调用时抛出 NotSupportedException。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			void ICollection<TKey>.Clear () {
				throw new NotSupportedException(EditReadOnlyListMessage);
			}

			/// <summary>键集合只读，返回 true。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			bool ICollection<TKey>.IsReadOnly {
				get {
					return true;
				}
			}

			/// <summary>只读集合不允许此修改操作，调用时抛出 NotSupportedException。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			bool ICollection<TKey>.Remove (TKey item) {
				throw new NotSupportedException(EditReadOnlyListMessage);
			}

			/// <summary>取得按当前集合顺序遍历元素的枚举器。</summary>
			IEnumerator IEnumerable.GetEnumerator () {
				return GetEnumerator();
			}
		}

		/// <summary><para>有序字典值集合的只读包装。</para>
		/// Wraps the keys in an OrderDictionary.
		/// </summary>
		public sealed class ValueCollection : ICollection<TValue>
		{
			/// <summary>所属字典的值列表。</summary>
			private readonly List<TValue> values;

			/// <summary><para>创建值集合包装，所属字典不得为 null。</para>
			/// Initializes a new instance of a ValueCollection.
			/// </summary>
			/// <param name="values">由当前值集合视图包装的有序字典。 The OrderedDictionary whose keys to wrap.</param>
			/// <exception cref="System.ArgumentNullException">字典为 null。 The dictionary is null.</exception>
			internal ValueCollection (List<TValue> values) {
				this.values = values;
			}

			/// <summary><para>将值复制到指定数组起始索引；数组必须非空且容量足够。</para>
			/// Copies the values from the OrderedDictionary to the given array, starting at the given index.
			/// </summary>
			/// <param name="array">用于接收值的目标数组。 The array to copy the values to.</param>
			/// <param name="arrayIndex">目标数组开始写入值的位置。 The index into the array to start copying the values.</param>
			/// <exception cref="System.ArgumentNullException">目标数组为 null。 The array is null.</exception>
			/// <exception cref="System.ArgumentOutOfRangeException">目标数组起始索引为负。 The arrayIndex is negative.</exception>
			/// <exception cref="System.ArgumentException">从指定起始索引开始的数组剩余容量不足以容纳全部值。 The array, starting at the given index, is not large enough to contain all the values.</exception>
			public void CopyTo (TValue[] array, int arrayIndex) {
				values.CopyTo(array, arrayIndex);
			}

			/// <summary><para>所属有序字典中的值数量。</para>
			/// Gets the number of values in the OrderedDictionary.
			/// </summary>
			public int Count {
				get {
					return values.Count;
				}
			}

			/// <summary><para>取得按字典顺序枚举值的枚举器。</para>
			/// Gets an enumerator over the values in the OrderedDictionary.
			/// </summary>
			/// <returns>用于当前集合遍历的枚举器。 The enumerator.</returns>
			public IEnumerator<TValue> GetEnumerator () {
				return values.GetEnumerator();
			}

			/// <summary>判断有效元素中是否包含指定值。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			bool ICollection<TValue>.Contains (TValue item) {
				return values.Contains(item);
			}

			/// <summary>只读集合不允许此修改操作，调用时抛出 NotSupportedException。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			void ICollection<TValue>.Add (TValue item) {
				throw new NotSupportedException(EditReadOnlyListMessage);
			}

			/// <summary>只读集合不允许此修改操作，调用时抛出 NotSupportedException。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			void ICollection<TValue>.Clear () {
				throw new NotSupportedException(EditReadOnlyListMessage);
			}

			/// <summary>值集合只读，返回 true。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			bool ICollection<TValue>.IsReadOnly {
				get {
					return true;
				}
			}

			/// <summary>只读集合不允许此修改操作，调用时抛出 NotSupportedException。</summary>
			[EditorBrowsable(EditorBrowsableState.Never)]
			bool ICollection<TValue>.Remove (TValue item) {
				throw new NotSupportedException(EditReadOnlyListMessage);
			}

			/// <summary>取得按当前集合顺序遍历元素的枚举器。</summary>
			IEnumerator IEnumerable.GetEnumerator () {
				return GetEnumerator();
			}
		}
	}

	/// <summary>调试器中展示有序字典键值条目的视图。</summary>
	internal class OrderedDictionaryDebugView<TKey, TValue>
	{
		/// <summary>所属有序字典。</summary>
		private readonly OrderedDictionary<TKey, TValue> dictionary;

		/// <summary>创建调试器中展示有序字典键值条目的视图，并初始化关联数据及缓冲区。</summary>
		public OrderedDictionaryDebugView (OrderedDictionary<TKey, TValue> dictionary) {
			this.dictionary = dictionary;
		}

		/// <summary>有效元素存储的底层数组。</summary>
		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public KeyValuePair<TKey, TValue>[] Items {
			get {
				return dictionary.ToArray();
			}
		}
	}

	/// <summary><para>提供从集合构建有序字典的扩展方法。</para>
	/// Provides extensions methods for constructing instances of <see cref="OrderedDictionary{TKey, TValue}"/>.
	/// </summary>
	public static class CollectionExtensions
	{
		#region ToOrderedDictionary

		/// <summary><para>从源集合构建有序字典，用 keySelector 提取每项的键，元素本身作为值，并保留遍历顺序。</para>
		/// Creates a new OrderedDictionary from the given collection, using the key selector to extract the key.
		/// </summary>
		/// <typeparam name="TSource">源集合中元素的类型。 The type of the items in the collection.</typeparam>
		/// <typeparam name="TKey">提取的键类型。 The type of the key.</typeparam>
		/// <param name="source">构建有序字典的源元素集合。 The items to created the OrderedDictionary from.</param>
		/// <param name="keySelector">从源元素提取键的委托。 A delegate that can extract a key from an item in the collection.</param>
		/// <returns>从提取键映射到源元素、保持源顺序的有序字典。 An OrderedDictionary mapping the extracted keys to their values.</returns>
		public static OrderedDictionary<TKey, TSource> ToOrderedDictionary<TSource, TKey> (this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) {
			return ToOrderedDictionary(source, keySelector, null);
		}

		/// <summary><para>从源集合构建有序字典，用键选择委托提取键，使用指定比较器判断键相等。</para>
		/// Creates a new OrderedDictionary from the given collection, using the key selector to extract the key.
		/// The key comparer is passed to the OrderedDictionary for comparing the extracted keys.
		/// </summary>
		/// <typeparam name="TSource">源集合中元素的类型。 The type of the items in the collection.</typeparam>
		/// <typeparam name="TKey">提取的键类型。 The type of the key.</typeparam>
		/// <param name="source">构建有序字典的源元素集合。 The items to created the OrderedDictionary from.</param>
		/// <param name="keySelector">从源元素提取键的委托。 A delegate that can extract a key from an item in the collection.</param>
		/// <param name="comparer">比较字典中所提取键的相等比较器。 The key equality comparer to use to compare keys in the dictionary.</param>
		/// <returns>从提取键映射到源元素、保持源顺序的有序字典。 An OrderedDictionary mapping the extracted keys to their values.</returns>
		public static OrderedDictionary<TKey, TSource> ToOrderedDictionary<TSource, TKey> (
			this IEnumerable<TSource> source,
			Func<TSource, TKey> keySelector,
			IEqualityComparer<TKey> comparer) {
			if (source == null) {
				throw new ArgumentNullException("source");
			}
			if (keySelector == null) {
				throw new ArgumentNullException("keySelector");
			}
			var dictionary = new OrderedDictionary<TKey, TSource>(comparer);
			foreach (TSource item in source) {
				TKey key = keySelector(item);
				dictionary.Add(key, item);
			}
			return dictionary;
		}

		#endregion
	}
}
