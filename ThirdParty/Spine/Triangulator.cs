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

using System;

namespace Spine {
	/// <summary>使用切耳法三角化多边形，并将三角形合并为凸多边形。</summary>
	public class Triangulator {
		/// <summary>合并生成的凸多边形列表。</summary>
		private readonly ExposedList<ExposedList<float>> convexPolygons = new ExposedList<ExposedList<float>>();
		/// <summary>各凸多边形在原顶点数组中的索引。</summary>
		private readonly ExposedList<ExposedList<int>> convexPolygonsIndices = new ExposedList<ExposedList<int>>();

		/// <summary>三角化过程中剩余顶点的索引缓存。</summary>
		private readonly ExposedList<int> indicesArray = new ExposedList<int>();
		/// <summary>各剩余顶点是否为凹角的缓存。</summary>
		private readonly ExposedList<bool> isConcaveArray = new ExposedList<bool>();
		/// <summary>三角形顶点索引，每三个索引组成一个三角形。</summary>
		private readonly ExposedList<int> triangles = new ExposedList<int>();

		/// <summary>可复用多边形缓冲池。</summary>
		private readonly Pool<ExposedList<float>> polygonPool = new Pool<ExposedList<float>>();
		/// <summary>可复用的多边形索引列表池。</summary>
		private readonly Pool<ExposedList<int>> polygonIndicesPool = new Pool<ExposedList<int>>();

		/// <summary>对多边形进行三角化，返回复用的顶点索引列表；下次调用会覆盖结果。</summary>
		public ExposedList<int> Triangulate (ExposedList<float> verticesArray) {
			var vertices = verticesArray.Items;
			int vertexCount = verticesArray.Count >> 1;

			var indicesArray = this.indicesArray;
			indicesArray.Clear();
			int[] indices = indicesArray.Resize(vertexCount).Items;
			for (int i = 0; i < vertexCount; i++)
				indices[i] = i;

			var isConcaveArray = this.isConcaveArray;
			bool[] isConcave = isConcaveArray.Resize(vertexCount).Items;
			for (int i = 0, n = vertexCount; i < n; ++i)
				isConcave[i] = IsConcave(i, vertexCount, vertices, indices);

			var triangles = this.triangles;
			triangles.Clear();
			triangles.EnsureCapacity(Math.Max(0, vertexCount - 2) << 2);

			while (vertexCount > 3) {
				// 寻找可切除的耳尖顶点。 原注： Find ear tip.
				int previous = vertexCount - 1, i = 0, next = 1;

				// 参考实现的外层循环标签。 原注： outer:
				while (true) {
					if (!isConcave[i]) {
						int p1 = indices[previous] << 1, p2 = indices[i] << 1, p3 = indices[next] << 1;
						float p1x = vertices[p1], p1y = vertices[p1 + 1];
						float p2x = vertices[p2], p2y = vertices[p2 + 1];
						float p3x = vertices[p3], p3y = vertices[p3 + 1];
						for (int ii = (next + 1) % vertexCount; ii != previous; ii = (ii + 1) % vertexCount) {
							if (!isConcave[ii]) continue;
							int v = indices[ii] << 1;
							float vx = vertices[v], vy = vertices[v + 1];
							if (PositiveArea(p3x, p3y, p1x, p1y, vx, vy)) {
								if (PositiveArea(p1x, p1y, p2x, p2y, vx, vy)) {
									if (PositiveArea(p2x, p2y, p3x, p3y, vx, vy)) goto break_outer; // 参考实现：跳出外层循环。 原注： break outer;
								}
							}
						}
						break;
					}
					break_outer:

					if (next == 0) {
						do {
							if (!isConcave[i]) break;
							i--;
						} while (i > 0);
						break;
					}

					previous = i;
					i = next;
					next = (next + 1) % vertexCount;
				}

				// 切除耳尖，生成一个三角形。 原注： Cut ear tip.
				triangles.Add(indices[(vertexCount + i - 1) % vertexCount]);
				triangles.Add(indices[i]);
				triangles.Add(indices[(i + 1) % vertexCount]);
				indicesArray.RemoveAt(i);
				isConcaveArray.RemoveAt(i);
				vertexCount--;

				int previousIndex = (vertexCount + i - 1) % vertexCount;
				int nextIndex = i == vertexCount ? 0 : i;
				isConcave[previousIndex] = IsConcave(previousIndex, vertexCount, vertices, indices);
				isConcave[nextIndex] = IsConcave(nextIndex, vertexCount, vertices, indices);
			}

			if (vertexCount == 3) {
				triangles.Add(indices[2]);
				triangles.Add(indices[0]);
				triangles.Add(indices[1]);
			}

			return triangles;
		}

		/// <summary>将三角形合并为凸多边形集合，用于高效裁剪；结果缓冲区由实例复用。</summary>
		public ExposedList<ExposedList<float>> Decompose (ExposedList<float> verticesArray, ExposedList<int> triangles) {
			var vertices = verticesArray.Items;
			var convexPolygons = this.convexPolygons;
			for (int i = 0, n = convexPolygons.Count; i < n; i++) {
				polygonPool.Free(convexPolygons.Items[i]);
			}
			convexPolygons.Clear();

			var convexPolygonsIndices = this.convexPolygonsIndices;
			for (int i = 0, n = convexPolygonsIndices.Count; i < n; i++) {
				polygonIndicesPool.Free(convexPolygonsIndices.Items[i]);
			}
			convexPolygonsIndices.Clear();

			var polygonIndices = polygonIndicesPool.Obtain();
			polygonIndices.Clear();

			var polygon = polygonPool.Obtain();
			polygon.Clear();

			// 相邻三角形能够构成三角扇时合并。 原注： Merge subsequent triangles if they form a triangle fan.
			int fanBaseIndex = -1, lastWinding = 0;
			int[] trianglesItems = triangles.Items;
			for (int i = 0, n = triangles.Count; i < n; i += 3) {
				int t1 = trianglesItems[i] << 1, t2 = trianglesItems[i + 1] << 1, t3 = trianglesItems[i + 2] << 1;
				float x1 = vertices[t1], y1 = vertices[t1 + 1];
				float x2 = vertices[t2], y2 = vertices[t2 + 1];
				float x3 = vertices[t3], y3 = vertices[t3 + 1];

				// 若最后一个三角形与当前三角形共用基边，检查合并后是否仍为凸多边形。 原注： If the base of the last triangle is the same as this triangle, check if they form a convex polygon (triangle fan).
				var merged = false;
				if (fanBaseIndex == t1) {
					int o = polygon.Count - 4;
					float[] p = polygon.Items;
					int winding1 = Winding(p[o], p[o + 1], p[o + 2], p[o + 3], x3, y3);
					int winding2 = Winding(x3, y3, p[0], p[1], p[2], p[3]);
					if (winding1 == lastWinding && winding2 == lastWinding) {
						polygon.Add(x3);
						polygon.Add(y3);
						polygonIndices.Add(t3);
						merged = true;
					}
				}

				// 不能合并时，以当前三角形作为新的基底。 原注： Otherwise make this triangle the new base.
				if (!merged) {
					if (polygon.Count > 0) {
						convexPolygons.Add(polygon);
						convexPolygonsIndices.Add(polygonIndices);
					} else {
						polygonPool.Free(polygon);
						polygonIndicesPool.Free(polygonIndices);
					}
					polygon = polygonPool.Obtain();
					polygon.Clear();
					polygon.Add(x1);
					polygon.Add(y1);
					polygon.Add(x2);
					polygon.Add(y2);
					polygon.Add(x3);
					polygon.Add(y3);
					polygonIndices = polygonIndicesPool.Obtain();
					polygonIndices.Clear();
					polygonIndices.Add(t1);
					polygonIndices.Add(t2);
					polygonIndices.Add(t3);
					lastWinding = Winding(x1, y1, x2, y2, x3, y3);
					fanBaseIndex = t1;
				}
			}

			if (polygon.Count > 0) {
				convexPolygons.Add(polygon);
				convexPolygonsIndices.Add(polygonIndices);
			}

			// 遍历多边形，尝试把剩余三角形合并到已有三角扇中。 原注： Go through the list of polygons and try to merge the remaining triangles with the found triangle fans.
			for (int i = 0, n = convexPolygons.Count; i < n; i++) {
				polygonIndices = convexPolygonsIndices.Items[i];
				if (polygonIndices.Count == 0) continue;
				int firstIndex = polygonIndices.Items[0];
				int lastIndex = polygonIndices.Items[polygonIndices.Count - 1];

				polygon = convexPolygons.Items[i];
				int o = polygon.Count - 4;
				float[] p = polygon.Items;
				float prevPrevX = p[o], prevPrevY = p[o + 1];
				float prevX = p[o + 2], prevY = p[o + 3];
				float firstX = p[0], firstY = p[1];
				float secondX = p[2], secondY = p[3];
				int winding = Winding(prevPrevX, prevPrevY, prevX, prevY, firstX, firstY);

				for (int ii = 0; ii < n; ii++) {
					if (ii == i) continue;
					var otherIndices = convexPolygonsIndices.Items[ii];
					if (otherIndices.Count != 3) continue;
					int otherFirstIndex = otherIndices.Items[0];
					int otherSecondIndex = otherIndices.Items[1];
					int otherLastIndex = otherIndices.Items[2];

					var otherPoly = convexPolygons.Items[ii];
					float x3 = otherPoly.Items[otherPoly.Count - 2], y3 = otherPoly.Items[otherPoly.Count - 1];

					if (otherFirstIndex != firstIndex || otherSecondIndex != lastIndex) continue;
					int winding1 = Winding(prevPrevX, prevPrevY, prevX, prevY, x3, y3);
					int winding2 = Winding(x3, y3, firstX, firstY, secondX, secondY);
					if (winding1 == winding && winding2 == winding) {
						otherPoly.Clear();
						otherIndices.Clear();
						polygon.Add(x3);
						polygon.Add(y3);
						polygonIndices.Add(otherLastIndex);
						prevPrevX = prevX;
						prevPrevY = prevY;
						prevX = x3;
						prevY = y3;
						ii = 0;
					}
				}
			}

			// 移除前面合并过程中产生的空多边形。 原注： Remove empty polygons that resulted from the merge step above.
			for (int i = convexPolygons.Count - 1; i >= 0; i--) {
				polygon = convexPolygons.Items[i];
				if (polygon.Count == 0) {
					convexPolygons.RemoveAt(i);
					polygonPool.Free(polygon);
					polygonIndices = convexPolygonsIndices.Items[i];
					convexPolygonsIndices.RemoveAt(i);
					polygonIndicesPool.Free(polygonIndices);
				}
			}

			return convexPolygons;
		}

		/// <summary>判断指定多边形顶点在当前顶点顺序下是否为凹角。</summary>
		static private bool IsConcave (int index, int vertexCount, float[] vertices, int[] indices) {
			int previous = indices[(vertexCount + index - 1) % vertexCount] << 1;
			int current = indices[index] << 1;
			int next = indices[(index + 1) % vertexCount] << 1;
			return !PositiveArea(vertices[previous], vertices[previous + 1], vertices[current], vertices[current + 1], vertices[next],
				vertices[next + 1]);
		}

		/// <summary>通过有向面积符号判断三点的绕序。</summary>
		static private bool PositiveArea (float p1x, float p1y, float p2x, float p2y, float p3x, float p3y) {
			return p1x * (p3y - p2y) + p2x * (p1y - p3y) + p3x * (p2y - p1y) >= 0;
		}

		/// <summary>返回三点的绕序符号，用于判断能否合并凸多边形。</summary>
		static private int Winding (float p1x, float p1y, float p2x, float p2y, float p3x, float p3y) {
			float px = p2x - p1x, py = p2y - p1y;
			return p3x * py - p3y * px + px * p1y - p1x * py >= 0 ? 1 : -1;
		}
	}
}
