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

#if (UNITY_5 || UNITY_5_3_OR_NEWER || UNITY_WSA || UNITY_WP8 || UNITY_WP8_1)
#define IS_UNITY
#endif

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

#if WINDOWS_STOREAPP
using System.Threading.Tasks;
using Windows.Storage;
#endif

namespace Spine {
	/// <summary>纹理图集，保存纹理页面及命名区域，并通过纹理加载器连接渲染资源。</summary>
	public class Atlas : IEnumerable<AtlasRegion> {
		/// <summary>图集纹理页集合。</summary>
		readonly List<AtlasPage> pages = new List<AtlasPage>();
		/// <summary>图集命名纹理区域集合。</summary>
		List<AtlasRegion> regions = new List<AtlasRegion>();
		/// <summary>连接图集与具体渲染纹理的加载器。</summary>
		TextureLoader textureLoader;

		#region IEnumerable implementation
		/// <summary>取得按当前集合顺序遍历元素的枚举器。</summary>
		public IEnumerator<AtlasRegion> GetEnumerator () {
			return regions.GetEnumerator();
		}

		/// <summary>取得按当前集合顺序遍历元素的枚举器。</summary>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator () {
			return regions.GetEnumerator();
		}
		#endregion

		#if !(IS_UNITY)
		#if WINDOWS_STOREAPP
		private async Task ReadFile(string path, TextureLoader textureLoader) {
			var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
			var file = await folder.GetFileAsync(path).AsTask().ConfigureAwait(false);
			using (var reader = new StreamReader(await file.OpenStreamForReadAsync().ConfigureAwait(false))) {
				try {
					Load(reader, Path.GetDirectoryName(path), textureLoader);
				} catch (Exception ex) {
					throw new Exception("Error reading atlas file: " + path, ex);
				}
			}
		}

		public Atlas(string path, TextureLoader textureLoader) {
			this.ReadFile(path, textureLoader).Wait();
		}
		#else

		/// <summary>读取指定图集数据，并使用传入的纹理加载器绑定页面资源。</summary>
		public Atlas (string path, TextureLoader textureLoader) {

			#if WINDOWS_PHONE
			Stream stream = Microsoft.Xna.Framework.TitleContainer.OpenStream(path);
			using (StreamReader reader = new StreamReader(stream)) {
			#else
			using (StreamReader reader = new StreamReader(path)) {
			#endif // Windows Phone 平台条件编译区结束。 原注： WINDOWS_PHONE

				try {
					Load(reader, Path.GetDirectoryName(path), textureLoader);
				} catch (Exception ex) {
					throw new Exception("Error reading atlas file: " + path, ex);
				}

			}
		}
		#endif // Windows Store 平台条件编译区结束。 原注： WINDOWS_STOREAPP

		#endif

		/// <summary>读取指定图集数据，并使用传入的纹理加载器绑定页面资源。</summary>
		public Atlas (TextReader reader, string dir, TextureLoader textureLoader) {
			Load(reader, dir, textureLoader);
		}

		/// <summary>用现有页面和区域集合创建图集，不接管纹理加载器。</summary>
		public Atlas (List<AtlasPage> pages, List<AtlasRegion> regions) {
			this.pages = pages;
			this.regions = regions;
			this.textureLoader = null;
		}

		/// <summary>解析图集文本中的页和区域信息，并通过纹理加载器加载每一页。</summary>
		private void Load (TextReader reader, string imagesDir, TextureLoader textureLoader) {
			if (textureLoader == null) throw new ArgumentNullException("textureLoader", "textureLoader cannot be null.");
			this.textureLoader = textureLoader;

			string[] tuple = new string[4];
			AtlasPage page = null;
			while (true) {
				string line = reader.ReadLine();
				if (line == null) break;
				if (line.Trim().Length == 0)
					page = null;
				else if (page == null) {
					page = new AtlasPage();
					page.name = line;

					if (ReadTuple(reader, tuple) == 2) { // 仅旧版 TexturePacker 生成的图集允许省略 size 字段。 原注： size is only optional for an atlas packed with an old TexturePacker.
						page.width = int.Parse(tuple[0], CultureInfo.InvariantCulture);
						page.height = int.Parse(tuple[1], CultureInfo.InvariantCulture);
						ReadTuple(reader, tuple);
					}
					page.format = (Format)Enum.Parse(typeof(Format), tuple[0], false);

					ReadTuple(reader, tuple);
					page.minFilter = (TextureFilter)Enum.Parse(typeof(TextureFilter), tuple[0], false);
					page.magFilter = (TextureFilter)Enum.Parse(typeof(TextureFilter), tuple[1], false);

					string direction = ReadValue(reader);
					page.uWrap = TextureWrap.ClampToEdge;
					page.vWrap = TextureWrap.ClampToEdge;
					if (direction == "x")
						page.uWrap = TextureWrap.Repeat;
					else if (direction == "y")
						page.vWrap = TextureWrap.Repeat;
					else if (direction == "xy")
						page.uWrap = page.vWrap = TextureWrap.Repeat;

					textureLoader.Load(page, Path.Combine(imagesDir, line));

					pages.Add(page);

				} else {
					AtlasRegion region = new AtlasRegion();
					region.name = line;
					region.page = page;

					string rotateValue = ReadValue(reader);
					if (rotateValue == "true")
						region.degrees = 90;
					else if (rotateValue == "false")
						region.degrees = 0;
					else
						region.degrees = int.Parse(rotateValue);
					region.rotate = region.degrees == 90;

					ReadTuple(reader, tuple);
					int x = int.Parse(tuple[0], CultureInfo.InvariantCulture);
					int y = int.Parse(tuple[1], CultureInfo.InvariantCulture);

					ReadTuple(reader, tuple);
					int width = int.Parse(tuple[0], CultureInfo.InvariantCulture);
					int height = int.Parse(tuple[1], CultureInfo.InvariantCulture);

					region.u = x / (float)page.width;
					region.v = y / (float)page.height;
					if (region.rotate) {
						region.u2 = (x + height) / (float)page.width;
						region.v2 = (y + width) / (float)page.height;
					} else {
						region.u2 = (x + width) / (float)page.width;
						region.v2 = (y + height) / (float)page.height;
					}
					region.x = x;
					region.y = y;
					region.width = Math.Abs(width);
					region.height = Math.Abs(height);

					if (ReadTuple(reader, tuple) == 4) { // 九宫格切分参数 split 可选。 原注： split is optional
						region.splits = new [] {int.Parse(tuple[0], CultureInfo.InvariantCulture),
												int.Parse(tuple[1], CultureInfo.InvariantCulture),
												int.Parse(tuple[2], CultureInfo.InvariantCulture),
												int.Parse(tuple[3], CultureInfo.InvariantCulture)};

						if (ReadTuple(reader, tuple) == 4) { // pad 参数可选，但只有存在 split 时才出现。 原注： pad is optional, but only present with splits
							region.pads = new [] {int.Parse(tuple[0], CultureInfo.InvariantCulture),
												int.Parse(tuple[1], CultureInfo.InvariantCulture),
												int.Parse(tuple[2], CultureInfo.InvariantCulture),
												int.Parse(tuple[3], CultureInfo.InvariantCulture)};

							ReadTuple(reader, tuple);
						}
					}

					region.originalWidth = int.Parse(tuple[0], CultureInfo.InvariantCulture);
					region.originalHeight = int.Parse(tuple[1], CultureInfo.InvariantCulture);

					ReadTuple(reader, tuple);
					region.offsetX = int.Parse(tuple[0], CultureInfo.InvariantCulture);
					region.offsetY = int.Parse(tuple[1], CultureInfo.InvariantCulture);

					region.index = int.Parse(ReadValue(reader), CultureInfo.InvariantCulture);

					regions.Add(region);
				}
			}
		}

		/// <summary>从图集文本的当前键值行中读取冒号后的内容。</summary>
		static string ReadValue (TextReader reader) {
			string line = reader.ReadLine();
			int colon = line.IndexOf(':');
			if (colon == -1) throw new Exception("Invalid line: " + line);
			return line.Substring(colon + 1).Trim();
		}

		/// <summary><para>读取逗号分隔的数值元组，返回有效数值数量（1、2 或 4）。</para>Returns the number of tuple values read (1, 2 or 4).</summary>
		static int ReadTuple (TextReader reader, string[] tuple) {
			string line = reader.ReadLine();
			int colon = line.IndexOf(':');
			if (colon == -1) throw new Exception("Invalid line: " + line);
			int i = 0, lastMatch = colon + 1;
			for (; i < 3; i++) {
				int comma = line.IndexOf(',', lastMatch);
				if (comma == -1) break;
				tuple[i] = line.Substring(lastMatch, comma - lastMatch).Trim();
				lastMatch = comma + 1;
			}
			tuple[i] = line.Substring(lastMatch).Trim();
			return i + 1;
		}

		/// <summary>反转全部图集区域的 V 方向纹理坐标。</summary>
		public void FlipV () {
			for (int i = 0, n = regions.Count; i < n; i++) {
				AtlasRegion region = regions[i];
				region.v = 1 - region.v;
				region.v2 = 1 - region.v2;
			}
		}

		/// <summary><para>按名称返回首个图集区域，未找到返回 null；使用字符串比较，频繁使用时应缓存结果。</para>Returns the first region found with the specified name. This method uses string comparison to find the region, so the result
		/// should be cached rather than calling this method multiple times.</summary>
		/// <returns>找到的图集区域，未找到时为 null。 The region, or null.</returns>
		public AtlasRegion FindRegion (string name) {
			for (int i = 0, n = regions.Count; i < n; i++)
				if (regions[i].name == name) return regions[i];
			return null;
		}

		/// <summary>通过配置的 TextureLoader 卸载各图集页关联的渲染对象。</summary>
		public void Dispose () {
			if (textureLoader == null) return;
			for (int i = 0, n = pages.Count; i < n; i++)
				textureLoader.Unload(pages[i].rendererObject);
		}
	}

	/// <summary>图集纹理像素格式。</summary>
	public enum Format {
		/// <summary>仅包含透明度的纹理格式。</summary>
		Alpha,
		/// <summary>单通道强度纹理格式。</summary>
		Intensity,
		/// <summary>亮度和透明度双通道格式。</summary>
		LuminanceAlpha,
		/// <summary>RGB 分别使用 5、6、5 位的像素格式。</summary>
		RGB565,
		/// <summary>RGBA 每通道 4 位的像素格式。</summary>
		RGBA4444,
		/// <summary>RGB 每通道 8 位的像素格式。</summary>
		RGB888,
		/// <summary>RGBA 每通道 8 位的像素格式。</summary>
		RGBA8888
	}

	/// <summary>图集纹理缩放和多级纹理采样方式。</summary>
	public enum TextureFilter {
		/// <summary>最近邻采样，保留像素边缘。</summary>
		Nearest,
		/// <summary>线性采样，平滑相邻像素。</summary>
		Linear,
		/// <summary>使用多级纹理过滤。</summary>
		MipMap,
		/// <summary>级内最近邻采样，级间选最近级。</summary>
		MipMapNearestNearest,
		/// <summary>级内线性采样，级间选最近级。</summary>
		MipMapLinearNearest,
		/// <summary>级内最近邻采样，级间线性混合。</summary>
		MipMapNearestLinear,
		/// <summary>级内和级间均线性混合。</summary>
		MipMapLinearLinear
	}

	/// <summary>纹理坐标越界时的采样方式。</summary>
	public enum TextureWrap {
		/// <summary>越界后交替镜像重复纹理。</summary>
		MirroredRepeat,
		/// <summary>越界时取纹理边缘颜色。</summary>
		ClampToEdge,
		/// <summary>越界时重复纹理。</summary>
		Repeat
	}

	/// <summary>图集中的一张纹理页面及其采样参数。</summary>
	public class AtlasPage {
		/// <summary>名称。</summary>
		public string name;
		/// <summary>纹理像素格式。</summary>
		public Format format;
		/// <summary>缩小时的采样方式。</summary>
		public TextureFilter minFilter;
		/// <summary>放大时的采样方式。</summary>
		public TextureFilter magFilter;
		/// <summary>U 方向越界采样方式。</summary>
		public TextureWrap uWrap;
		/// <summary>V 方向越界采样方式。</summary>
		public TextureWrap vWrap;
		/// <summary>渲染后端关联对象；图集页通常为纹理，附件通常为 AtlasRegion。</summary>
		public object rendererObject;
		/// <summary>width：宽度；height：高度。</summary>
		public int width, height;

		/// <summary>创建浅表副本，引用类型字段仍指向原对象。</summary>
		public AtlasPage Clone () {
			return MemberwiseClone() as AtlasPage;
		}
	}

	/// <summary>一张图集页面中的命名区域及裁剪、旋转和 UV 信息。</summary>
	public class AtlasRegion {
		/// <summary>所属图集页面。</summary>
		public AtlasPage page;
		/// <summary>名称。</summary>
		public string name;
		/// <summary>图集中的区域像素位置与裁剪后的尺寸。</summary>
		public int x, y, width, height;
		/// <summary>u：纹理区域起始 U 坐标；v：纹理区域起始 V 坐标；u2：纹理区域结束 U 坐标；v2：纹理区域结束 V 坐标。</summary>
		public float u, v, u2, v2;
		/// <summary>区域裁剪时从原始图像左下侧移除的像素偏移。</summary>
		public float offsetX, offsetY;
		/// <summary>originalWidth：裁剪前的原始宽度；originalHeight：裁剪前的原始高度。</summary>
		public int originalWidth, originalHeight;
		/// <summary>区域序列编号，无编号时通常为 -1。</summary>
		public int index;
		/// <summary>图集区域是否旋转 90 度存储。</summary>
		public bool rotate;
		/// <summary>区域在图集中的旋转角度。</summary>
		public int degrees;
		/// <summary>九宫格边界切分距离。</summary>
		public int[] splits;
		/// <summary>九宫格内容内边距。</summary>
		public int[] pads;

		/// <summary>创建浅表副本，引用类型字段仍指向原对象。</summary>
		public AtlasRegion Clone () {
			return MemberwiseClone() as AtlasRegion;
		}
	}

	/// <summary>图集纹理加载及卸载接口，由具体渲染环境实现。</summary>
	public interface TextureLoader {
		/// <summary>按路径加载页面纹理，并设置页面渲染对象及实际宽高。</summary>
		void Load (AtlasPage page, string path);
		/// <summary>卸载此加载器所拥有的纹理资源；共享资源可由实现选择保留。</summary>
		void Unload (Object texture);
	}
}
