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

	/// <summary><para>收集当前可见的包围盒附件，计算多边形世界顶点，提供点和线段命中查询。</para>
	/// Collects each BoundingBoxAttachment that is visible and computes the world vertices for its polygon.
	/// The polygon vertices are provided along with convenience methods for doing hit detection.
	/// </summary>
	public class SkeletonBounds {
		/// <summary>可复用多边形缓冲池。</summary>
		private ExposedList<Polygon> polygonPool = new ExposedList<Polygon>();
		/// <summary>minX：包围盒最小 X 坐标；minY：包围盒最小 Y 坐标；maxX：包围盒最大 X 坐标；maxY：包围盒最大 Y 坐标。</summary>
		private float minX, minY, maxX, maxY;

		/// <summary>当前可见的包围盒附件集合。</summary>
		public ExposedList<BoundingBoxAttachment> BoundingBoxes { get; private set; }
		/// <summary>与包围盒附件对应的世界多边形集合。</summary>
		public ExposedList<Polygon> Polygons { get; private set; }
		/// <summary>包围盒最小 X 坐标。</summary>
		public float MinX { get { return minX; } set { minX = value; } }
		/// <summary>包围盒最小 Y 坐标。</summary>
		public float MinY { get { return minY; } set { minY = value; } }
		/// <summary>包围盒最大 X 坐标。</summary>
		public float MaxX { get { return maxX; } set { maxX = value; } }
		/// <summary>包围盒最大 Y 坐标。</summary>
		public float MaxY { get { return maxY; } set { maxY = value; } }
		/// <summary>宽度。</summary>
		public float Width { get { return maxX - minX; } }
		/// <summary>高度。</summary>
		public float Height { get { return maxY - minY; } }

		/// <summary>创建根据包围盒附件计算世界多边形和命中范围，并初始化关联数据及缓冲区。</summary>
		public SkeletonBounds () {
			BoundingBoxes = new ExposedList<BoundingBoxAttachment>();
			Polygons = new ExposedList<Polygon>();
		}

		/// <summary><para>清空旧多边形并重算可见包围盒；updateAabb 为 true 时计算总包围盒，为 false 时 AABB 快速检查不再排除目标。</para>
		/// Clears any previous polygons, finds all visible bounding box attachments,
		/// and computes the world vertices for each bounding box's polygon.</summary>
		/// <param name="skeleton">需要计算包围盒的骨架实例。 The skeleton.</param>
		/// <param name="updateAabb">是否重算总轴对齐包围盒；false 时 AABB 快速检测不再排除候选目标。 
		/// If true, the axis aligned bounding box containing all the polygons is computed.
		/// If false, the SkeletonBounds AABB methods will always return true.
		/// </param>
		public void Update (Skeleton skeleton, bool updateAabb) {
			ExposedList<BoundingBoxAttachment> boundingBoxes = BoundingBoxes;
			ExposedList<Polygon> polygons = Polygons;
			ExposedList<Slot> slots = skeleton.slots;
			int slotCount = slots.Count;

			boundingBoxes.Clear();
			for (int i = 0, n = polygons.Count; i < n; i++)
				polygonPool.Add(polygons.Items[i]);
			polygons.Clear();

			for (int i = 0; i < slotCount; i++) {
				Slot slot = slots.Items[i];
				if (!slot.bone.active) continue;
				BoundingBoxAttachment boundingBox = slot.attachment as BoundingBoxAttachment;
				if (boundingBox == null) continue;
				boundingBoxes.Add(boundingBox);

				Polygon polygon = null;
				int poolCount = polygonPool.Count;
				if (poolCount > 0) {
					polygon = polygonPool.Items[poolCount - 1];
					polygonPool.RemoveAt(poolCount - 1);
				} else
					polygon = new Polygon();
				polygons.Add(polygon);

				int count = boundingBox.worldVerticesLength;
				polygon.Count = count;
				if (polygon.Vertices.Length < count) polygon.Vertices = new float[count];
				boundingBox.ComputeWorldVertices(slot, polygon.Vertices);
			}

			if (updateAabb) {
				AabbCompute();
			} else {
				minX = int.MinValue;
				minY = int.MinValue;
				maxX = int.MaxValue;
				maxY = int.MaxValue;
			}
		}

		/// <summary>根据当前全部世界多边形计算最小轴对齐包围盒。</summary>
		private void AabbCompute () {
			float minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
			ExposedList<Polygon> polygons = Polygons;
			for (int i = 0, n = polygons.Count; i < n; i++) {
				Polygon polygon = polygons.Items[i];
				float[] vertices = polygon.Vertices;
				for (int ii = 0, nn = polygon.Count; ii < nn; ii += 2) {
					float x = vertices[ii];
					float y = vertices[ii + 1];
					minX = Math.Min(minX, x);
					minY = Math.Min(minY, y);
					maxX = Math.Max(maxX, x);
					maxY = Math.Max(maxY, y);
				}
			}
			this.minX = minX;
			this.minY = minY;
			this.maxX = maxX;
			this.maxY = maxY;
		}


		/// <summary><para>轴对齐包围盒包含指定点时返回 true。</para>Returns true if the axis aligned bounding box contains the point.</summary>
		public bool AabbContainsPoint (float x, float y) {
			return x >= minX && x <= maxX && y >= minY && y <= maxY;
		}

		/// <summary><para>轴对齐包围盒与指定线段相交时返回 true。</para>Returns true if the axis aligned bounding box intersects the line segment.</summary>
		public bool AabbIntersectsSegment (float x1, float y1, float x2, float y2) {
			float minX = this.minX;
			float minY = this.minY;
			float maxX = this.maxX;
			float maxY = this.maxY;
			if ((x1 <= minX && x2 <= minX) || (y1 <= minY && y2 <= minY) || (x1 >= maxX && x2 >= maxX) || (y1 >= maxY && y2 >= maxY))
				return false;
			float m = (y2 - y1) / (x2 - x1);
			float y = m * (minX - x1) + y1;
			if (y > minY && y < maxY) return true;
			y = m * (maxX - x1) + y1;
			if (y > minY && y < maxY) return true;
			float x = (minY - y1) / m + x1;
			if (x > minX && x < maxX) return true;
			x = (maxY - y1) / m + x1;
			if (x > minX && x < maxX) return true;
			return false;
		}

		/// <summary><para>此轴对齐包围盒与另一骨架的轴对齐包围盒相交时返回 true。</para>Returns true if the axis aligned bounding box intersects the axis aligned bounding box of the specified bounds.</summary>
		public bool AabbIntersectsSkeleton (SkeletonBounds bounds) {
			return minX < bounds.maxX && maxX > bounds.minX && minY < bounds.maxY && maxY > bounds.minY;
		}

		/// <summary><para>指定多边形包含点时返回 true。</para>Returns true if the polygon contains the point.</summary>
		public bool ContainsPoint (Polygon polygon, float x, float y) {
			float[] vertices = polygon.Vertices;
			int nn = polygon.Count;

			int prevIndex = nn - 2;
			bool inside = false;
			for (int ii = 0; ii < nn; ii += 2) {
				float vertexY = vertices[ii + 1];
				float prevY = vertices[prevIndex + 1];
				if ((vertexY < y && prevY >= y) || (prevY < y && vertexY >= y)) {
					float vertexX = vertices[ii];
					if (vertexX + (y - vertexY) / (prevY - vertexY) * (vertices[prevIndex] - vertexX) < x) inside = !inside;
				}
				prevIndex = ii;
			}
			return inside;
		}

		/// <summary><para>返回第一个包含指定点的包围盒附件，未命中为 null；大量检测前可先做 AABB 快速排除。</para>Returns the first bounding box attachment that contains the point, or null. When doing many checks, it is usually more
		/// efficient to only call this method if {@link #aabbContainsPoint(float, float)} returns true.</summary>
		public BoundingBoxAttachment ContainsPoint (float x, float y) {
			ExposedList<Polygon> polygons = Polygons;
			for (int i = 0, n = polygons.Count; i < n; i++)
				if (ContainsPoint(polygons.Items[i], x, y)) return BoundingBoxes.Items[i];
			return null;
		}

		/// <summary><para>返回第一个与指定线段相交的包围盒附件，未命中为 null；可先用 AABB 快速排除。</para>Returns the first bounding box attachment that contains the line segment, or null. When doing many checks, it is usually
		/// more efficient to only call this method if {@link #aabbIntersectsSegment(float, float, float, float)} returns true.</summary>
		public BoundingBoxAttachment IntersectsSegment (float x1, float y1, float x2, float y2) {
			ExposedList<Polygon> polygons = Polygons;
			for (int i = 0, n = polygons.Count; i < n; i++)
				if (IntersectsSegment(polygons.Items[i], x1, y1, x2, y2)) return BoundingBoxes.Items[i];
			return null;
		}

		/// <summary><para>多边形与指定线段相交时返回 true。</para>Returns true if the polygon contains the line segment.</summary>
		public bool IntersectsSegment (Polygon polygon, float x1, float y1, float x2, float y2) {
			float[] vertices = polygon.Vertices;
			int nn = polygon.Count;

			float width12 = x1 - x2, height12 = y1 - y2;
			float det1 = x1 * y2 - y1 * x2;
			float x3 = vertices[nn - 2], y3 = vertices[nn - 1];
			for (int ii = 0; ii < nn; ii += 2) {
				float x4 = vertices[ii], y4 = vertices[ii + 1];
				float det2 = x3 * y4 - y3 * x4;
				float width34 = x3 - x4, height34 = y3 - y4;
				float det3 = width12 * height34 - height12 * width34;
				float x = (det1 * width34 - width12 * det2) / det3;
				if (((x >= x3 && x <= x4) || (x >= x4 && x <= x3)) && ((x >= x1 && x <= x2) || (x >= x2 && x <= x1))) {
					float y = (det1 * height34 - height12 * det2) / det3;
					if (((y >= y3 && y <= y4) || (y >= y4 && y <= y3)) && ((y >= y1 && y <= y2) || (y >= y2 && y <= y1))) return true;
				}
				x3 = x4;
				y3 = y4;
			}
			return false;
		}

		/// <summary>查找与指定包围盒附件对应的世界多边形；未找到返回 null。</summary>
		public Polygon GetPolygon (BoundingBoxAttachment attachment) {
			int index = BoundingBoxes.IndexOf(attachment);
			return index == -1 ? null : Polygons.Items[index];
		}
	}

	/// <summary>交错存储 X、Y 浮点值的多边形顶点缓冲区。</summary>
	public class Polygon {
		/// <summary>多边形世界坐标，按 X、Y 交错存储，实际有效长度由 Count 指定。</summary>
		public float[] Vertices { get; set; }
		/// <summary>顶点数组中有效浮点值的数量，每个顶点占 X、Y 两个值。</summary>
		public int Count { get; set; }

		/// <summary>创建交错存储 X、Y 浮点值的多边形顶点缓冲区，并初始化关联数据及缓冲区。</summary>
		public Polygon () {
			Vertices = new float[16];
		}
	}
}
